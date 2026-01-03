using System.Text.Json.Nodes;
using FluentValidation;
using FluentValidation.Validators;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers;

/// <summary>
/// Transforms OpenAPI document by enriching schemas with FluentValidation rules.
/// Extracts validation constraints from FluentValidation validators and applies them to the OpenAPI schemas.
/// This transformer runs after all schemas are generated and modifies them to inline validation constraints.
/// </summary>
public class FluentValidationSchemaTransformer : IOpenApiDocumentTransformer
{
	readonly IServiceProvider _serviceProvider;

	public FluentValidationSchemaTransformer(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
	}

	public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
	{
		if (document.Components?.Schemas is null)
		{
			return Task.CompletedTask;
		}

		// Process each schema in the document
		foreach (var schemaEntry in document.Components.Schemas.ToList())
		{
			string schemaName = schemaEntry.Key;
			IOpenApiSchema schemaInterface = schemaEntry.Value;

			if (schemaInterface is not OpenApiSchema schema)
			{
				continue;
			}

			// Try to find the corresponding .NET type for this schema
			Type? modelType = FindTypeForSchema(schemaName);
			if (modelType is null)
			{
				continue;
			}

			// Get the validator for this type from DI
			Type validatorType = typeof(IValidator<>).MakeGenericType(modelType);
			object? validator = _serviceProvider.GetService(validatorType);

			if (validator is null)
			{
				continue;
			}

			// Apply validation rules to this schema
			ApplyValidationRules(schema, (IValidator)validator, document);
		}

		return Task.CompletedTask;
	}

	static Type? FindTypeForSchema(string schemaName)
	{
		// Try to find the type by its full name
		// Schema names are typically the full type name with + replaced by .
		string typeName = schemaName.Replace('+', '.');
		
		// Try to load the type from all loaded assemblies
		foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			Type? type = assembly.GetType(typeName);
			if (type is not null)
			{
				return type;
			}
		}

		return null;
	}

	static void ApplyValidationRules(OpenApiSchema schema, IValidator validator, OpenApiDocument document)
	{
		if (schema.Properties is null || schema.Properties.Count == 0)
		{
			return;
		}

		IValidatorDescriptor descriptor = validator.CreateDescriptor();
		HashSet<string> requiredProperties = [];

		// Get all members that have validators and process their rules
		foreach (var memberGroup in descriptor.GetMembersWithValidators())
		{
			string memberName = memberGroup.Key;
			foreach (IValidationRule rule in descriptor.GetRulesForMember(memberName))
			{
				ProcessRule(rule, schema, requiredProperties, document);
			}
		}

		// Apply required properties to schema
		if (requiredProperties.Count > 0)
		{
			schema.Required ??= new HashSet<string>();
			foreach (string prop in requiredProperties)
			{
				schema.Required.Add(prop);
			}
		}

		// Add extension to indicate schema is enriched with FluentValidation
		schema.Extensions ??= new Dictionary<string, IOpenApiExtension>();
		schema.Extensions["x-validation-source"] = new JsonNodeExtension(JsonValue.Create("FluentValidation"));
	}

	static void ProcessRule(IValidationRule rule, OpenApiSchema schema, HashSet<string> requiredProperties, OpenApiDocument document)
	{
		string? propertyName = rule.PropertyName;
		if (string.IsNullOrEmpty(propertyName))
		{
			return;
		}

		// Convert property name to camelCase for OpenAPI
		string schemaPropertyName = ToCamelCase(propertyName);

		// Ensure the property exists in the schema
		if (schema.Properties is null || !schema.Properties.ContainsKey(schemaPropertyName))
		{
			return;
		}

		IOpenApiSchema propertySchemaInterface = schema.Properties[schemaPropertyName];
		OpenApiSchema propertySchema;
		bool isComplexTypeReference = false;
		
		// If the property is a reference, we need to replace it with an inline schema
		// so we can add validation constraints (but only for primitive types and enums)
		if (propertySchemaInterface is OpenApiSchemaReference schemaRef)
		{
			// Check if this is a primitive type or enum that we can inline
			string? refId = schemaRef.Reference?.Id;
			if (refId is not null && (IsPrimitiveType(refId) || IsEnumType(refId, document)))
			{
				// Create an inline schema based on the reference
				propertySchema = CreateInlineSchemaFromReference(schemaRef, document);
				// Replace the reference with the inline schema
				schema.Properties[schemaPropertyName] = propertySchema;
			}
			else
			{
				// For complex types, keep the reference but still process NotNull/NotEmpty validators
				// to add the property to the required array
				isComplexTypeReference = true;
				propertySchema = new OpenApiSchema(); // Dummy schema, won't be used
			}
		}
		else if (propertySchemaInterface is OpenApiSchema existingSchema)
		{
			propertySchema = existingSchema;
		}
		else
		{
			return;
		}

		// Process each component validator for this property
		foreach (IPropertyValidator validator in rule.Components.Select(c => c.Validator))
		{
			// For complex type references, only process NotNull/NotEmpty to add to required
			if (isComplexTypeReference)
			{
				if (validator is INotNullValidator or INotEmptyValidator)
				{
					requiredProperties.Add(schemaPropertyName);
				}
			}
			else
			{
				ApplyValidatorConstraints(validator, propertySchema, requiredProperties, schemaPropertyName);
			}
		}
	}

	static bool IsPrimitiveType(string refId)
	{
		return refId.Contains("System.String") ||
		       refId.Contains("System.Int32") ||
		       refId.Contains("System.Int64") ||
		       refId.Contains("System.Decimal") ||
		       refId.Contains("System.Double") ||
		       refId.Contains("System.Single") ||
		       refId.Contains("System.Boolean") ||
		       refId.Contains("System.DateTime") ||
		       refId.Contains("System.DateTimeOffset") ||
		       refId.Contains("System.Guid");
	}

	static bool IsEnumType(string refId, OpenApiDocument document)
	{
		// Check if the refId exists in the document's schemas and if it has enum values
		if (document.Components?.Schemas?.TryGetValue(refId, out var schema) == true)
		{
			// Check if the schema is an enum schema (has enum extension or is an integer with enum-related extensions)
			if (schema is OpenApiSchema openApiSchema)
			{
				return openApiSchema.Extensions?.ContainsKey("enum") == true ||
				       openApiSchema.Extensions?.ContainsKey("x-enum-varnames") == true;
			}
		}
		return false;
	}

	static OpenApiSchema CreateInlineSchemaFromReference(OpenApiSchemaReference schemaRef, OpenApiDocument document)
	{
		// Map common system types to their OpenAPI equivalents
		string? refId = schemaRef.Reference?.Id;
		
		if (refId is null)
		{
			return new OpenApiSchema();
		}

		// Check if this is an enum type - if so, copy the enum schema
		if (document.Components?.Schemas?.TryGetValue(refId, out var referencedSchema) == true && 
		    referencedSchema is OpenApiSchema enumSchema &&
		    (enumSchema.Extensions?.ContainsKey("enum") == true || enumSchema.Extensions?.ContainsKey("x-enum-varnames") == true))
		{
			// Copy the enum schema including all its properties and extensions
			var inlineEnumSchema = new OpenApiSchema
			{
				Type = enumSchema.Type,
				Format = enumSchema.Format,
				Description = enumSchema.Description,
				Extensions = enumSchema.Extensions != null ? new Dictionary<string, IOpenApiExtension>(enumSchema.Extensions) : null
			};
			return inlineEnumSchema;
		}

		// Handle common .NET types
		if (refId.Contains("System.String"))
		{
			return new OpenApiSchema { Type = JsonSchemaType.String };
		}
		if (refId.Contains("System.Int32") || refId.Contains("System.Int64"))
		{
			return new OpenApiSchema { Type = JsonSchemaType.Integer };
		}
		if (refId.Contains("System.Decimal") || refId.Contains("System.Double") || refId.Contains("System.Single"))
		{
			return new OpenApiSchema { Type = JsonSchemaType.Number };
		}
		if (refId.Contains("System.Boolean"))
		{
			return new OpenApiSchema { Type = JsonSchemaType.Boolean };
		}
		if (refId.Contains("System.DateTime") || refId.Contains("System.DateTimeOffset"))
		{
			return new OpenApiSchema { Type = JsonSchemaType.String, Format = "date-time" };
		}
		if (refId.Contains("System.Guid"))
		{
			return new OpenApiSchema { Type = JsonSchemaType.String, Format = "uuid" };
		}

		// For complex types (non-primitives), we can't easily inline them
		// Return a basic schema - the validation won't apply to these
		return new OpenApiSchema();
	}

	static void ApplyValidatorConstraints(
		IPropertyValidator validator,
		OpenApiSchema propertySchema,
		HashSet<string> requiredProperties,
		string propertyName)
	{
		switch (validator)
		{
			case INotNullValidator:
			case INotEmptyValidator:
				requiredProperties.Add(propertyName);
				// For strings, NotEmpty means minLength = 1
				if (IsStringSchema(propertySchema))
				{
					propertySchema.MinLength = 1;
				}
				break;

			// Check specific length validators before the general ILengthValidator
			case IMinimumLengthValidator minLengthValidator:
				if (IsStringSchema(propertySchema) && minLengthValidator.Min > 0)
				{
					propertySchema.MinLength = minLengthValidator.Min;
				}
				break;

			case IMaximumLengthValidator maxLengthValidator:
				if (IsStringSchema(propertySchema) && maxLengthValidator.Max > 0)
				{
					propertySchema.MaxLength = maxLengthValidator.Max;
				}
				break;

			case ILengthValidator lengthValidator:
				if (IsStringSchema(propertySchema))
				{
					if (lengthValidator.Max > 0)
					{
						propertySchema.MaxLength = lengthValidator.Max;
					}
					if (lengthValidator.Min > 0)
					{
						propertySchema.MinLength = lengthValidator.Min;
					}
				}
				break;

			case IComparisonValidator comparisonValidator:
				ApplyComparisonConstraints(comparisonValidator, propertySchema);
				break;

			case IBetweenValidator betweenValidator:
				ApplyBetweenConstraints(betweenValidator, propertySchema);
				break;

			case IRegularExpressionValidator regexValidator:
				if (IsStringSchema(propertySchema) && !string.IsNullOrEmpty(regexValidator.Expression))
				{
					propertySchema.Pattern = regexValidator.Expression;
				}
				break;

			case IEmailValidator:
				if (IsStringSchema(propertySchema))
				{
					propertySchema.Format = "email";
				}
				break;

			default:
				// Check for enum validators (IsEnumName, IsInEnum) using reflection
				// These validators have an EnumType property we can extract
				ApplyEnumValidatorConstraints(validator, propertySchema);
				break;
		}
	}

	static void ApplyComparisonConstraints(IComparisonValidator comparisonValidator, OpenApiSchema propertySchema)
	{
		// FluentValidation 12.x changed the API - ValueToCompare is now a property that returns an object
		// We need to use reflection to get the actual comparison value since it might be stored differently
		object? valueToCompare = null;
		
		// Try to get ValueToCompare property
		var valueProperty = comparisonValidator.GetType().GetProperty("ValueToCompare");
		if (valueProperty is not null)
		{
			valueToCompare = valueProperty.GetValue(comparisonValidator);
		}
		
		// If it's a Func<object>, invoke it to get the actual value
		if (valueToCompare is Func<object> funcValue)
		{
			valueToCompare = funcValue();
		}
		else if (valueToCompare is Delegate delegateValue)
		{
			valueToCompare = delegateValue.DynamicInvoke();
		}
		
		if (valueToCompare is null)
		{
			return;
		}

		string? numericValue = ConvertToString(valueToCompare);
		if (numericValue is null)
		{
			return;
		}

		switch (comparisonValidator.Comparison)
		{
			case Comparison.GreaterThan:
				propertySchema.Minimum = numericValue;
				propertySchema.ExclusiveMinimum = "true";
				break;
			case Comparison.GreaterThanOrEqual:
				propertySchema.Minimum = numericValue;
				propertySchema.ExclusiveMinimum = "false";
				break;
			case Comparison.LessThan:
				propertySchema.Maximum = numericValue;
				propertySchema.ExclusiveMaximum = "true";
				break;
			case Comparison.LessThanOrEqual:
				propertySchema.Maximum = numericValue;
				propertySchema.ExclusiveMaximum = "false";
				break;
		}
	}

	static void ApplyBetweenConstraints(IBetweenValidator betweenValidator, OpenApiSchema propertySchema)
	{
		// Use reflection to get From and To properties since the interface might have changed
		object? fromValue = null;
		object? toValue = null;
		
		var fromProperty = betweenValidator.GetType().GetProperty("From");
		var toProperty = betweenValidator.GetType().GetProperty("To");
		
		if (fromProperty is not null)
		{
			fromValue = fromProperty.GetValue(betweenValidator);
		}
		
		if (toProperty is not null)
		{
			toValue = toProperty.GetValue(betweenValidator);
		}
		
		// Handle Func values - FluentValidation may wrap values in Func<object>
		if (fromValue is Func<object> fromFunc)
		{
			fromValue = fromFunc();
		}
		else if (fromValue is Delegate fromDelegate)
		{
			fromValue = fromDelegate.DynamicInvoke();
		}
		
		if (toValue is Func<object> toFunc)
		{
			toValue = toFunc();
		}
		else if (toValue is Delegate toDelegate)
		{
			toValue = toDelegate.DynamicInvoke();
		}
		
		string? from = ConvertToString(fromValue);
		string? to = ConvertToString(toValue);

		if (from is not null)
		{
			propertySchema.Minimum = from;
			propertySchema.ExclusiveMinimum = "false";
		}

		if (to is not null)
		{
			propertySchema.Maximum = to;
			propertySchema.ExclusiveMaximum = "false";
		}
	}

	static string? ConvertToString(object value)
	{
		// Handle different types that FluentValidation might pass
		switch (value)
		{
			case int intValue:
				return intValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
			case long longValue:
				return longValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
			case decimal decimalValue:
				return decimalValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
			case double doubleValue:
				return doubleValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
			case float floatValue:
				return floatValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
			default:
				// Try to convert as a last resort
				try
				{
					return Convert.ToDecimal(value).ToString(System.Globalization.CultureInfo.InvariantCulture);
				}
				catch
				{
					return null;
				}
		}
	}

	static void ApplyEnumValidatorConstraints(IPropertyValidator validator, OpenApiSchema propertySchema)
	{
		// Check if this is a StringEnumValidator or similar enum validator
		// StringEnumValidator has a _enumNames field we can use
		var validatorType = validator.GetType();
		
		// Check for _enumNames field (present in StringEnumValidator from IsEnumName)
		var enumNamesField = validatorType.GetField("_enumNames", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		if (enumNamesField is not null)
		{
			string[]? enumNames = enumNamesField.GetValue(validator) as string[];
			if (enumNames is not null && enumNames.Length > 0)
			{
				// Find the enum type by matching the names
				Type? enumType = FindEnumTypeByNames(enumNames);
				if (enumType is not null)
				{
					EnrichSchemaWithEnumValues(propertySchema, enumType);
					return;
				}
			}
		}
		
		// Check if this is a PredicateValidator from IsInEnum extension method
		// IsInEnum creates a Must validator with a predicate that captures the enum type
		if (validatorType.Name.StartsWith("PredicateValidator"))
		{
			// First, try to extract from error message
			var errorMessageSource = validatorType.GetProperty("ErrorMessageSource", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
			if (errorMessageSource is not null)
			{
				object? messageSource = errorMessageSource.GetValue(validator);
				if (messageSource is not null)
				{
					var messageProperty = messageSource.GetType().GetProperty("Message", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
					if (messageProperty is not null)
					{
						string? message = messageProperty.GetValue(messageSource) as string;
						if (message is not null && (message.Contains("must be a valid value of enum") || message.Contains("must be empty or a valid value of enum")))
						{
							Type? enumType = ExtractEnumTypeFromMessage(message);
							if (enumType is not null)
							{
								EnrichSchemaWithEnumValues(propertySchema, enumType);
								return;
							}
						}
					}
				}
			}
			
			// Fallback: Try to extract from predicate closure
			// The IsInEnum extension captures the enumType in a nested closure structure
			var predicateField = validatorType.GetField("_predicate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			if (predicateField is not null)
			{
				object? predicate = predicateField.GetValue(validator);
				if (predicate is Delegate predicateDelegate)
				{
					// Recursively search through nested closures
					Type? enumType = FindEnumTypeInClosure(predicateDelegate.Target);
					if (enumType is not null)
					{
						EnrichSchemaWithEnumValues(propertySchema, enumType);
						return;
					}
				}
			}
		}
		
	static Type? FindEnumTypeInClosure(object? target, int maxDepth = 5)
	{
		if (target is null || maxDepth <= 0)
		{
			return null;
		}
		
		var fields = target.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		foreach (var field in fields)
		{
			object? fieldValue = field.GetValue(target);
			
			// Check if this field is directly an enum type
			if (fieldValue is Type enumType && enumType.IsEnum)
			{
				return enumType;
			}
			
			// If it's a delegate, recurse into its closure
			if (fieldValue is Delegate del && del.Target is not null)
			{
				Type? found = FindEnumTypeInClosure(del.Target, maxDepth - 1);
				if (found is not null)
				{
					return found;
				}
			}
		}
		
		return null;
	}
		// Fallback: Try to extract enum type from generic arguments
		// Some enum validators might store the enum type as a generic parameter
		if (validatorType.IsGenericType)
		{
			var genericArgs = validatorType.GetGenericArguments();
			foreach (var arg in genericArgs)
			{
				if (arg.IsEnum)
				{
					EnrichSchemaWithEnumValues(propertySchema, arg);
					return;
				}
			}
			
			// Check base type generic arguments as well
			var baseType = validatorType.BaseType;
			while (baseType is not null && baseType.IsGenericType)
			{
				foreach (var arg in baseType.GetGenericArguments())
				{
					if (arg.IsEnum)
					{
						EnrichSchemaWithEnumValues(propertySchema, arg);
						return;
					}
				}
				baseType = baseType.BaseType;
			}
		}
	}
	
	static Type? ExtractEnumTypeFromMessage(string message)
	{
		// Message format: "{PropertyName} must be a valid value of enum EnumName." or
		// "{PropertyName} must be empty or a valid value of enum EnumName."
		// Extract the enum name between "enum " and "."
		int enumIndex = message.IndexOf("enum ", StringComparison.Ordinal);
		if (enumIndex == -1)
		{
			return null;
		}
		
		int startIndex = enumIndex + 5; // Length of "enum "
		int endIndex = message.IndexOf('.', startIndex);
		if (endIndex == -1)
		{
			endIndex = message.Length;
		}
		
		string enumName = message.Substring(startIndex, endIndex - startIndex).Trim();
		
		// Search for the enum type by name across all loaded assemblies
		foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			try
			{
				foreach (var type in assembly.GetTypes())
				{
					if (type.IsEnum && type.Name == enumName)
					{
						return type;
					}
				}
			}
			catch
			{
				// Skip assemblies that can't be inspected
				continue;
			}
		}
		
		return null;
	}

	static Type? FindEnumTypeByNames(string[] names)
	{
		// Search for an enum type that has exactly these member names
		foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			try
			{
				foreach (var type in assembly.GetTypes())
				{
					if (!type.IsEnum)
					{
						continue;
					}

					string[] typeEnumNames = Enum.GetNames(type);
					if (typeEnumNames.Length == names.Length && 
					    typeEnumNames.OrderBy(n => n).SequenceEqual(names.OrderBy(n => n)))
					{
						return type;
					}
				}
			}
			catch
			{
				// Skip assemblies that can't be inspected
				continue;
			}
		}

		return null;
	}

	static void EnrichSchemaWithEnumValues(OpenApiSchema schema, Type enumType)
	{
		// Get all enum values and names
		Array enumValues = Enum.GetValues(enumType);
		string[] enumNames = Enum.GetNames(enumType);

		List<JsonNode> values = [];
		List<string> varNames = [];
		Dictionary<string, string> descriptions = [];

		// Determine if this is a string schema or integer schema
		// Check the existing type first - if it's already set to String, this is a string enum validator (IsEnumName)
		// Otherwise, it should be an integer type for actual enum properties or IsInEnum on int properties
		bool isStringSchema = schema.Type.HasValue && schema.Type.Value == JsonSchemaType.String;
		
		// If type is not set, determine it based on the underlying enum type
		// For actual enum properties, we should use the enum's underlying type (typically int)
		if (!schema.Type.HasValue)
		{
			// Get the underlying type of the enum (byte, int, long, etc.)
			Type underlyingType = Enum.GetUnderlyingType(enumType);
			
			// Map to appropriate JSON schema type
			if (underlyingType == typeof(byte) || underlyingType == typeof(sbyte) ||
				underlyingType == typeof(short) || underlyingType == typeof(ushort) ||
				underlyingType == typeof(int) || underlyingType == typeof(uint) ||
				underlyingType == typeof(long) || underlyingType == typeof(ulong))
			{
				schema.Type = JsonSchemaType.Integer;
				
				// Set format based on the underlying type
				if (underlyingType == typeof(long) || underlyingType == typeof(ulong))
				{
					schema.Format = "int64";
				}
				else
				{
					schema.Format = "int32";
				}
			}
			else
			{
				// Fallback to string for edge cases
				schema.Type = JsonSchemaType.String;
				isStringSchema = true;
			}
		}

		for (int i = 0; i < enumValues.Length; i++)
		{
			object enumValue = enumValues.GetValue(i)!;
			string enumName = enumNames[i];
			
			// For string schemas, add the enum names as valid values
			// For integer schemas, add the numeric values
			if (isStringSchema)
			{
				values.Add(JsonValue.Create(enumName)!);
			}
			else
			{
				long numericValue = Convert.ToInt64(enumValue);
				values.Add(JsonValue.Create(numericValue)!);
			}
			
			varNames.Add(enumName);

			// Check for Description attribute
			var field = enumType.GetField(enumName);
			var descriptionAttr = field?.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false)
				.OfType<System.ComponentModel.DescriptionAttribute>()
				.FirstOrDefault();

			if (descriptionAttr is not null && !string.IsNullOrWhiteSpace(descriptionAttr.Description))
			{
				descriptions[enumName] = descriptionAttr.Description;
			}
		}

		// Add the enum values
		schema.Extensions ??= new Dictionary<string, IOpenApiExtension>();
		schema.Extensions["enum"] = new JsonNodeExtension(new JsonArray(values.ToArray()));
		
		// Add x-enum-varnames extension for member names (only for integer schemas, not string schemas)
		// For string schemas, the enum values already contain the names, so x-enum-varnames would be redundant
		if (!isStringSchema)
		{
			schema.Extensions["x-enum-varnames"] = new JsonNodeExtension(new JsonArray(varNames.Select(n => JsonValue.Create(n)!).ToArray()));
		}

		// Add x-enum-descriptions extension if any descriptions are present
		if (descriptions.Count > 0)
		{
			JsonObject descObj = [];
			foreach (var kvp in descriptions)
			{
				descObj[kvp.Key] = kvp.Value;
			}
			schema.Extensions["x-enum-descriptions"] = new JsonNodeExtension(descObj);
		}

		// Add a description to the schema if it doesn't have one
		if (string.IsNullOrWhiteSpace(schema.Description))
		{
			schema.Description = $"Enum: {string.Join(", ", varNames)}";
		}
	}

	static bool IsStringSchema(OpenApiSchema schema)
	{
		// In OpenAPI 3.1, Type is JsonSchemaType? enum
		return schema.Type.HasValue && schema.Type.Value == JsonSchemaType.String;
	}

	static string ToCamelCase(string str)
	{
		if (string.IsNullOrEmpty(str) || char.IsLower(str[0]))
		{
			return str;
		}

		return char.ToLowerInvariant(str[0]) + str[1..];
	}
}
