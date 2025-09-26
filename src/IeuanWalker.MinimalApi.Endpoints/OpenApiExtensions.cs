using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using FluentValidation;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using System.Collections;

namespace IeuanWalker.MinimalApi.Endpoints;

public static class OpenApiExtensions
{
	public static RouteHandlerBuilder WithResponse(this RouteHandlerBuilder builder, int statusCode, string description, string? contentType = null)
	{
		builder.WithResponse(null, statusCode, description, contentType);

		return builder;
	}

	public static RouteHandlerBuilder WithResponse<T>(this RouteHandlerBuilder builder, int statusCode, string description, string? contentType = null)
	{
		builder.WithResponse(typeof(T), statusCode, description, contentType);

		return builder;
	}

	public static RouteHandlerBuilder WithFluentValidationSchema<T>(this RouteHandlerBuilder builder, AbstractValidator<T> validator)
		where T : class
	{
		builder.AddOpenApiOperationTransformer((operation, context, cancellationToken) =>
		{
			ApplyValidationConstraints(operation, validator);
			return Task.CompletedTask;
		});

		return builder;
	}

	public static RouteHandlerBuilder WithFluentValidationSchema<T>(this RouteHandlerBuilder builder)
		where T : class
	{
		builder.AddOpenApiOperationTransformer((operation, context, cancellationToken) =>
		{
			// Try to resolve the validator from DI container
			IValidator<T>? validator = context.ApplicationServices.GetService(typeof(IValidator<T>)) as IValidator<T>;
			if (validator is AbstractValidator<T> abstractValidator)
			{
				ApplyValidationConstraints(operation, abstractValidator);
			}

			return Task.CompletedTask;
		});

		return builder;
	}

	static RouteHandlerBuilder WithResponse(this RouteHandlerBuilder builder, Type? type, int statusCode, string description, string? contentType = null)
	{
		if (contentType is null)
		{
			builder.WithMetadata(new ProducesResponseTypeMetadata(statusCode, type ?? typeof(void)));
		}
		else
		{
			builder.WithMetadata(new ProducesResponseTypeMetadata(statusCode, type ?? typeof(void), [contentType]));
		}

		builder.AddOpenApiOperationTransformer((operation, _, _) =>
		{
			operation.Responses?[statusCode.ToString()].Description = description;

			return Task.CompletedTask;
		});

		return builder;
	}

	static void ApplyValidationConstraints<T>(Microsoft.OpenApi.OpenApiOperation operation, AbstractValidator<T> validator)
	{
		List<PropertyValidationRule> rules = ValidationRuleExtractor.ExtractRules(validator);

		// Apply constraints to request body schema if it exists
		if (operation.RequestBody?.Content != null)
		{
			foreach (OpenApiMediaType mediaType in operation.RequestBody.Content.Values)
			{
				if (mediaType.Schema != null)
				{
					// Instead of applying rules immediately, we need to defer this until schemas are resolved
					// Store the rules and model type for later application
					ApplyRulesToSchemaWithContext(mediaType.Schema, rules, typeof(T), operation);
				}
			}
		}
	}

	static void ApplyRulesToSchemaWithContext(object schema, List<PropertyValidationRule> rules, Type modelType, Microsoft.OpenApi.OpenApiOperation operation)
	{
		Type schemaType = schema.GetType();

		System.Diagnostics.Debug.WriteLine($"=== SCHEMA DEBUG INFO ===");
		System.Diagnostics.Debug.WriteLine($"Schema type: {schemaType.FullName}");
		System.Diagnostics.Debug.WriteLine($"Model type: {modelType.FullName}");

		// Handle OpenApiSchemaReference - try different resolution approaches
		if (schemaType.Name == "OpenApiSchemaReference" || schemaType.FullName?.Contains("OpenApiSchemaReference") == true)
		{
			System.Diagnostics.Debug.WriteLine("Detected OpenApiSchemaReference - attempting resolution");

			// Check if this is a reference to a component schema
			PropertyInfo? referenceProperty = schemaType.GetProperty("Reference");
			if (referenceProperty != null)
			{
				object? reference = referenceProperty.GetValue(schema);
				if (reference != null)
				{
					System.Diagnostics.Debug.WriteLine($"Schema reference object: {reference.GetType().FullName}");

					// Try to get the reference ID/name
					var refIdProperty = reference.GetType().GetProperty("Id") ?? reference.GetType().GetProperty("ReferenceId");
					if (refIdProperty != null)
					{
						object? refId = refIdProperty.GetValue(reference);
						System.Diagnostics.Debug.WriteLine($"Reference ID: {refId}");
					}
				}
			}

			// For schema references, we'll apply validation rules directly to the reference object
			// The actual validation will happen when the schema is resolved
			System.Diagnostics.Debug.WriteLine("Applying validation rules to schema reference properties");

			// Apply rules directly to the reference object's properties
			ApplyRulesToSchemaReference(schema, rules, modelType);
			return;
		}

		// For non-reference schemas, use the original logic
		//ApplyRulesToSchema(schema, rules, modelType);
	}

	static void ApplyRulesToSchemaReference(object schemaReference, List<PropertyValidationRule> rules, Type modelType)
	{
		PropertyInfo[] modelProperties = modelType.GetProperties();
		Type schemaType = schemaReference.GetType();

		System.Diagnostics.Debug.WriteLine($"Applying rules directly to schema reference of type: {schemaType.FullName}");
		System.Diagnostics.Debug.WriteLine($"Processing {modelProperties.Length} model properties against {rules.Count} validation rules");

		// Apply global schema constraints based on the validation rules
		foreach (PropertyInfo modelProperty in modelProperties)
		{
			List<PropertyValidationRule> propertyRules = rules.Where(r => r.PropertyName == modelProperty.Name && !r.IsNestedProperty).ToList();

			if (propertyRules.Count == 0)
			{
				continue;
			}

			System.Diagnostics.Debug.WriteLine($"Processing property: {modelProperty.Name} ({propertyRules.Count} rules)");

			// Apply rules that can be set at the schema reference level
			foreach (PropertyValidationRule rule in propertyRules)
			{
				System.Diagnostics.Debug.WriteLine($"Attempting to apply rule: {rule.RuleName} to schema reference");
				ApplyValidationRuleToSchemaReference(schemaReference, modelProperty, rule);
			}

			// Handle required properties at the schema reference level
			if (propertyRules.Any(r => r.RuleName == "NotEmpty" || r.RuleName == "NotNull"))
			{
				System.Diagnostics.Debug.WriteLine($"Marking property '{modelProperty.Name}' as required on schema reference");
				AddToRequiredSetOnReference(schemaReference, GetJsonPropertyName(modelProperty.Name));
			}
		}
	}

	static void ApplyValidationRuleToSchemaReference(object schemaReference, PropertyInfo modelProperty, PropertyValidationRule rule)
	{
		try
		{
			// For schema references, we can still try to set certain global constraints
			// that might be available on the reference object itself
			switch (rule.RuleName)
			{
				case "MinimumLength":
					if (rule.Parameters.TryGetValue("MinLength", out object? minLength) && minLength is int minLengthValue)
					{
						SetSchemaProperty(schemaReference, "MinLength", minLengthValue);
					}
					break;

				case "MaximumLength":
					if (rule.Parameters.TryGetValue("MaxLength", out object? maxLength) && maxLength is int maxLengthValue)
					{
						SetSchemaProperty(schemaReference, "MaxLength", maxLengthValue);
					}
					break;

				case "Length":
					if (rule.Parameters.TryGetValue("MinLength", out object? lengthMin) && lengthMin is int minValue)
					{
						SetSchemaProperty(schemaReference, "MinLength", minValue);
					}
					if (rule.Parameters.TryGetValue("MaxLength", out object? lengthMax) && lengthMax is int maxValue)
					{
						SetSchemaProperty(schemaReference, "MaxLength", maxValue);
					}
					break;

				case "GreaterThan":
					if (rule.Parameters.TryGetValue("Minimum", out object? gtMin))
					{
						decimal? gtMinValue = ConvertToDecimal(gtMin);
						if (gtMinValue.HasValue)
						{
							SetSchemaProperty(schemaReference, "Minimum", gtMinValue.Value.ToString());
							SetSchemaProperty(schemaReference, "ExclusiveMinimum", "true");
						}
					}
					break;

				case "GreaterThanOrEqualTo":
					if (rule.Parameters.TryGetValue("Minimum", out object? gteMin))
					{
						decimal? gteMinValue = ConvertToDecimal(gteMin);
						if (gteMinValue.HasValue)
						{
							SetSchemaProperty(schemaReference, "Minimum", gteMinValue.Value.ToString());
						}
					}
					break;

				case "LessThan":
					if (rule.Parameters.TryGetValue("Maximum", out object? ltMax))
					{
						decimal? ltMaxValue = ConvertToDecimal(ltMax);
						if (ltMaxValue.HasValue)
						{
							SetSchemaProperty(schemaReference, "Maximum", ltMaxValue.Value.ToString());
							SetSchemaProperty(schemaReference, "ExclusiveMaximum", "true");
						}
					}
					break;

				case "LessThanOrEqualTo":
					if (rule.Parameters.TryGetValue("Maximum", out object? lteMax))
					{
						decimal? lteMaxValue = ConvertToDecimal(lteMax);
						if (lteMaxValue.HasValue)
						{
							SetSchemaProperty(schemaReference, "Maximum", lteMaxValue.Value.ToString());
						}
					}
					break;

				case "EmailAddress":
					SetSchemaProperty(schemaReference, "Format", "email");
					break;

				case "Matches":
					if (rule.Parameters.TryGetValue("Pattern", out object? pattern) && pattern is string patternValue)
					{
						SetSchemaProperty(schemaReference, "Pattern", patternValue);
					}
					break;
			}

			System.Diagnostics.Debug.WriteLine($"Applied rule {rule.RuleName} to schema reference");
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Failed to apply rule {rule.RuleName} to schema reference: {ex.Message}");
		}
	}

	static void AddToRequiredSetOnReference(object schemaReference, string propertyName)
	{
		try
		{
			PropertyInfo? requiredProperty = schemaReference.GetType().GetProperty("Required");
			if (requiredProperty != null && requiredProperty.CanWrite)
			{
				object? requiredSet = requiredProperty.GetValue(schemaReference);
				if (requiredSet == null)
				{
					// Try to create appropriate collection type
					Type propertyType = requiredProperty.PropertyType;
					if (propertyType == typeof(ISet<string>) || propertyType.IsAssignableFrom(typeof(HashSet<string>)))
					{
						requiredSet = new HashSet<string>();
					}
					else if (propertyType.IsAssignableFrom(typeof(List<string>)))
					{
						requiredSet = new List<string>();
					}

					if (requiredSet != null)
					{
						requiredProperty.SetValue(schemaReference, requiredSet);
					}
				}

				if (requiredSet is ICollection<string> requiredCollection)
				{
					requiredCollection.Add(propertyName);
					System.Diagnostics.Debug.WriteLine($"Added '{propertyName}' to required set on schema reference");
				}
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Failed to add required property to schema reference: {ex.Message}");
		}
	}

	static void ApplyValidationRule(object schema, PropertyValidationRule rule)
	{
		Type schemaType = schema.GetType();

		try
		{
			switch (rule.RuleName)
			{
				case "NotEmpty":
					// Only apply to string types
					PropertyInfo? typeProperty = schemaType.GetProperty("Type");
					object? schemaTypeValue = typeProperty?.GetValue(schema);
					if (IsStringType(schemaTypeValue))
					{
						SetSchemaProperty(schema, "MinLength", 1);
					}
					break;

				case "MinimumLength":
					if (rule.Parameters.TryGetValue("MinLength", out object? minLength) && minLength is int minLengthValue)
					{
						SetSchemaProperty(schema, "MinLength", minLengthValue);
					}
					else if (TryExtractLengthFromMessage(rule.ErrorMessage, out int extractedMinLength))
					{
						SetSchemaProperty(schema, "MinLength", extractedMinLength);
					}
					break;

				case "MaximumLength":
					if (rule.Parameters.TryGetValue("MaxLength", out object? maxLength) && maxLength is int maxLengthValue)
					{
						SetSchemaProperty(schema, "MaxLength", maxLengthValue);
					}
					else if (TryExtractLengthFromMessage(rule.ErrorMessage, out int extractedMaxLength))
					{
						SetSchemaProperty(schema, "MaxLength", extractedMaxLength);
					}
					break;

				case "Length":
					if (rule.Parameters.TryGetValue("MinLength", out object? lengthMin) && lengthMin is int minValue)
					{
						SetSchemaProperty(schema, "MinLength", minValue);
					}
					if (rule.Parameters.TryGetValue("MaxLength", out object? lengthMax) && lengthMax is int maxValue)
					{
						SetSchemaProperty(schema, "MaxLength", maxValue);
					}
					if (!rule.Parameters.ContainsKey("MinLength") && !rule.Parameters.ContainsKey("MaxLength"))
					{
						if (TryExtractLengthRangeFromMessage(rule.ErrorMessage, out int extractedLengthMin, out int extractedLengthMax))
						{
							SetSchemaProperty(schema, "MinLength", extractedLengthMin);
							SetSchemaProperty(schema, "MaxLength", extractedLengthMax);
						}
					}
					break;

				case "EmailAddress":
					SetSchemaProperty(schema, "Format", "email");
					break;

				case "Matches":
					if (rule.Parameters.TryGetValue("Pattern", out object? pattern) && pattern is string patternValue)
					{
						SetSchemaProperty(schema, "Pattern", patternValue);
					}
					break;

				case "GreaterThan":
					if (rule.Parameters.TryGetValue("Minimum", out object? gtMin))
					{
						decimal? gtMinValue = ConvertToDecimal(gtMin);
						if (gtMinValue.HasValue)
						{
							SetSchemaProperty(schema, "Minimum", gtMinValue.Value);
							SetSchemaProperty(schema, "ExclusiveMinimum", rule.Parameters.ContainsKey("ExclusiveMinimum") && (bool)rule.Parameters["ExclusiveMinimum"]);
						}
					}
					break;

				case "GreaterThanOrEqualTo":
					if (rule.Parameters.TryGetValue("Minimum", out object? gteMin))
					{
						decimal? gteMinValue = ConvertToDecimal(gteMin);
						if (gteMinValue.HasValue)
						{
							SetSchemaProperty(schema, "Minimum", gteMinValue.Value);
							SetSchemaProperty(schema, "ExclusiveMinimum", false);
						}
					}
					break;

				case "LessThan":
					if (rule.Parameters.TryGetValue("Maximum", out object? ltMax))
					{
						decimal? ltMaxValue = ConvertToDecimal(ltMax);
						if (ltMaxValue.HasValue)
						{
							SetSchemaProperty(schema, "Maximum", ltMaxValue.Value);
							SetSchemaProperty(schema, "ExclusiveMaximum", rule.Parameters.ContainsKey("ExclusiveMaximum") && (bool)rule.Parameters["ExclusiveMaximum"]);
						}
					}
					break;

				case "LessThanOrEqualTo":
					if (rule.Parameters.TryGetValue("Maximum", out object? lteMax))
					{
						decimal? lteMaxValue = ConvertToDecimal(lteMax);
						if (lteMaxValue.HasValue)
						{
							SetSchemaProperty(schema, "Maximum", lteMaxValue.Value);
							SetSchemaProperty(schema, "ExclusiveMaximum", false);
						}
					}
					break;

				case "InclusiveBetween":
					if (rule.Parameters.TryGetValue("Minimum", out object? rangeMin))
					{
						decimal? rangeMinValue = ConvertToDecimal(rangeMin);
						if (rangeMinValue.HasValue)
						{
							SetSchemaProperty(schema, "Minimum", rangeMinValue.Value);
							SetSchemaProperty(schema, "ExclusiveMinimum", false);
						}
					}
					if (rule.Parameters.TryGetValue("Maximum", out object? rangeMax))
					{
						decimal? rangeMaxValue = ConvertToDecimal(rangeMax);
						if (rangeMaxValue.HasValue)
						{
							SetSchemaProperty(schema, "Maximum", rangeMaxValue.Value);
							SetSchemaProperty(schema, "ExclusiveMaximum", false);
						}
					}
					break;
			}
		}
		catch
		{
			// Ignore any reflection errors when setting schema properties
		}
	}

	static void SetSchemaProperty(object schema, string propertyName, object value)
	{
		try
		{
			PropertyInfo? property = schema.GetType().GetProperty(propertyName);
			if (property != null && property.CanWrite)
			{
				// Handle type conversion if needed
				if (property.PropertyType != value.GetType())
				{
					if (property.PropertyType == typeof(int?) && value is int intValue)
					{
						property.SetValue(schema, (int?)intValue);
						return;
					}
					if (property.PropertyType == typeof(decimal?) && value is decimal decimalValue)
					{
						property.SetValue(schema, (decimal?)decimalValue);
						return;
					}
					if (property.PropertyType == typeof(bool?) && value is bool boolValue)
					{
						property.SetValue(schema, (bool?)boolValue);
						return;
					}
				}

				property.SetValue(schema, value);
			}
		}
		catch
		{
			// Ignore property setting errors
		}
	}

	static bool IsStringType(object? schemaTypeValue)
	{
		if (schemaTypeValue == null)
		{
			return false;
		}

		// Handle both string representation and enum values
		return schemaTypeValue.ToString() == "string" ||
			   schemaTypeValue.ToString() == "String" ||
			   schemaTypeValue.ToString().Contains("String");
	}

	static decimal? ConvertToDecimal(object value)
	{
		try
		{
			return Convert.ToDecimal(value);
		}
		catch
		{
			return null;
		}
	}

	static bool IsComplexType(Type type)
	{
		// Remove nullable wrapper
		Type actualType = Nullable.GetUnderlyingType(type) ?? type;

		return !actualType.IsPrimitive
			&& actualType != typeof(string)
			&& actualType != typeof(DateTime)
			&& actualType != typeof(DateTimeOffset)
			&& actualType != typeof(TimeSpan)
			&& actualType != typeof(Guid)
			&& actualType != typeof(decimal)
			&& !actualType.IsEnum;
	}

	static bool IsCollectionType(Type type)
	{
		return type.IsGenericType &&
			(type.GetGenericTypeDefinition() == typeof(List<>) ||
			type.GetGenericTypeDefinition() == typeof(ICollection<>) ||
			type.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
			type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)));
	}

	static Type? GetCollectionElementType(Type collectionType)
	{
		if (collectionType.IsGenericType)
		{
			return collectionType.GetGenericArguments().FirstOrDefault();
		}

		Type? enumerableInterface = collectionType.GetInterfaces()
			.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

		return enumerableInterface?.GetGenericArguments().FirstOrDefault();
	}

	static bool TryExtractLengthFromMessage(string errorMessage, out int length)
	{
		// Extract number from messages like "'Title' must be at least 1 characters" or "'Title' must be 200 characters or fewer"
		Match match = Regex.Match(errorMessage, @"(\d+)");
		if (match.Success && int.TryParse(match.Value, out length))
		{
			return true;
		}
		length = 0;
		return false;
	}

	static bool TryExtractLengthRangeFromMessage(string errorMessage, out int minLength, out int maxLength)
	{
		// Extract range from messages like "'Title' must be between 2 and 50 characters"
		Match match = Regex.Match(errorMessage, @"between (\d+) and (\d+)");
		if (match.Success && int.TryParse(match.Groups[1].Value, out minLength) && int.TryParse(match.Groups[2].Value, out maxLength))
		{
			return true;
		}
		minLength = maxLength = 0;
		return false;
	}

	static bool TryExtractNumberFromMessage(string errorMessage, out decimal number)
	{
		Match match = Regex.Match(errorMessage, @"(\d+(?:\.\d+)?)");
		if (match.Success && decimal.TryParse(match.Value, out number))
		{
			return true;
		}
		number = 0;
		return false;
	}

	static bool TryExtractRangeFromMessage(string errorMessage, out decimal min, out decimal max)
	{
		Match match = Regex.Match(errorMessage, @"between ([\d.]+) and ([\d.]+)");
		if (match.Success && decimal.TryParse(match.Groups[1].Value, out min) && decimal.TryParse(match.Groups[2].Value, out max))
		{
			return true;
		}
		min = max = 0;
		return false;
	}

	static bool TryExtractPatternFromMessage(string errorMessage, out string pattern)
	{
		// This would need more sophisticated parsing based on your regex patterns
		// For now, return empty - could be enhanced to extract regex from error messages
		pattern = string.Empty;
		return false;
	}

	static string GetJsonPropertyName(string propertyName)
	{
		// Convert to camelCase for JSON
		return char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
	}
}
