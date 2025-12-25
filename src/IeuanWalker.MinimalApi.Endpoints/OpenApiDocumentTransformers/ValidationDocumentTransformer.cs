using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using IeuanWalker.MinimalApi.Endpoints.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers;

/// <summary>
/// OpenAPI document transformer that applies validation rules from WithValidation to schemas
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class ValidationDocumentTransformer : IOpenApiDocumentTransformer
{
	public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
	{
		// Iterate through all operations to find endpoints with validation metadata
		EndpointDataSource? endpointDataSource = context.ApplicationServices.GetService(typeof(EndpointDataSource)) as EndpointDataSource;
		if (endpointDataSource == null)
		{
			return Task.CompletedTask;
		}

		foreach (var operation in endpointDataSource.Endpoints)
		{
			if (operation is RouteEndpoint routeEndpoint)
			{
				// Check for validation metadata
				var metadataItems = routeEndpoint.Metadata.GetOrderedMetadata<object>();
				foreach (var metadata in metadataItems)
				{
					// Use reflection to check if this is a ValidationMetadata<T>
					Type metadataType = metadata.GetType();
					if (metadataType.IsGenericType && metadataType.GetGenericTypeDefinition().Name.Contains("ValidationMetadata"))
					{
						// Extract the configuration and request type
						PropertyInfo? configProp = metadataType.GetProperty("Configuration");
						if (configProp?.GetValue(metadata) is object config)
						{
							Type requestType = metadataType.GetGenericArguments()[0];
							ApplyValidationToSchemas(document, requestType, config);
						}
					}
				}
			}
		}

		return Task.CompletedTask;
	}

	static void ApplyValidationToSchemas(OpenApiDocument document, Type requestType, object configurationObj)
	{
		// Get the schema name for the request type
		string schemaName = requestType.FullName ?? requestType.Name;

		// Find the schema in components
		if (document.Components?.Schemas == null ||
		    !document.Components.Schemas.TryGetValue(schemaName, out IOpenApiSchema? schemaInterface))
		{
			return;
		}

		if (schemaInterface is not OpenApiSchema schema || schema.Properties == null)
		{
			return;
		}

		// Extract rules from the configuration object
		PropertyInfo? rulesProp = configurationObj.GetType().GetProperty("Rules");
		if (rulesProp?.GetValue(configurationObj) is not IEnumerable<Validation.ValidationRule> rules)
		{
			return;
		}

		// Group rules by property name
		var rulesByProperty = rules.GroupBy(r => r.PropertyName);

		// Track required properties
		List<string> requiredProperties = [];

		// Apply validation rules to properties
		foreach (var propertyRules in rulesByProperty)
		{
			string propertyKey = ToCamelCase(propertyRules.Key);

			// Check if any rule for this property is RequiredRule
			if (propertyRules.Any(r => r is Validation.RequiredRule))
			{
				requiredProperties.Add(propertyKey);
			}

			if (schema.Properties.TryGetValue(propertyKey, out IOpenApiSchema? propertySchemaInterface))
			{
				// Create inline schema with all validation constraints for this property
				schema.Properties[propertyKey] = CreateInlineSchemaWithAllValidation(propertySchemaInterface, propertyRules.ToList());
			}
		}

		// Set required properties on the schema
		if (requiredProperties.Count > 0)
		{
			schema.Required = new HashSet<string>(requiredProperties);
		}
	}

	static OpenApiSchema CreateInlineSchemaWithAllValidation(IOpenApiSchema originalSchema, List<Validation.ValidationRule> rules)
	{
		// Get the type from the first rule (all rules for same property should have same type)
		JsonSchemaType? schemaType = rules.Select(GetSchemaType).FirstOrDefault(t => t != null);
		string? format = rules.Select(GetSchemaFormat).FirstOrDefault(f => f != null);

		// Create inline schema - set properties after creation to avoid initialization issues
		OpenApiSchema inlineSchema = new();
		
		// Set type and format separately
		if (schemaType.HasValue)
		{
			inlineSchema.Type = schemaType.Value;
		}
		if (format != null)
		{
			inlineSchema.Format = format;
		}

		Console.WriteLine($"[DEBUG] Created inline schema: Type={inlineSchema.Type}, Format={inlineSchema.Format}");

		// Collect all rule descriptions
		List<string> ruleDescriptions = [];

		// Apply all rules to this schema
		foreach (var rule in rules)
		{
			Console.WriteLine($"[DEBUG] Applying rule: {rule.GetType().Name} for property {rule.PropertyName}");
			
			// Get human-readable description for this rule
			string? ruleDescription = GetRuleDescription(rule);
			if (!string.IsNullOrEmpty(ruleDescription))
			{
				ruleDescriptions.Add(ruleDescription);
			}
			
			// Apply rule to schema (for non-custom rules)
			if (!IsCustomRule(rule))
			{
				ApplyRuleToSchema(rule, inlineSchema);
			}
			
			Console.WriteLine($"[DEBUG] After applying rule - Minimum='{inlineSchema.Minimum}', Maximum='{inlineSchema.Maximum}'");
		}

		// Add all rule descriptions to the description field if any exist
		if (ruleDescriptions.Count > 0)
		{
			string rulesSection = "Validation rules:\n" + string.Join("\n", ruleDescriptions.Select(msg => $"- {msg}"));
			
			// Append to existing description if present, otherwise set new description
			if (!string.IsNullOrEmpty(inlineSchema.Description))
			{
				inlineSchema.Description = $"{inlineSchema.Description}\n\n{rulesSection}";
			}
			else
			{
				inlineSchema.Description = rulesSection;
			}
		}

		return inlineSchema;
	}

	static bool IsCustomRule(Validation.ValidationRule rule)
	{
		Type ruleType = rule.GetType();
		return ruleType.IsGenericType && ruleType.GetGenericTypeDefinition() == typeof(Validation.CustomRule<>);
	}

	static string? GetRuleDescription(Validation.ValidationRule rule)
	{
		return rule switch
		{
			Validation.RequiredRule => "Required",
			
			Validation.StringLengthRule stringLengthRule => GetStringLengthDescription(stringLengthRule),
			
			Validation.PatternRule patternRule => $"Must match pattern: {patternRule.Pattern}",
			
			Validation.EmailRule => "Must be a valid email address",
			
			Validation.UrlRule => "Must be a valid URL",
			
			Validation.RangeRule<int> intRange => GetRangeDescription(intRange.Minimum, intRange.Maximum, intRange.ExclusiveMinimum, intRange.ExclusiveMaximum),
			
			Validation.RangeRule<long> longRange => GetRangeDescription(longRange.Minimum, longRange.Maximum, longRange.ExclusiveMinimum, longRange.ExclusiveMaximum),
			
			Validation.RangeRule<decimal> decimalRange => GetRangeDescription(decimalRange.Minimum, decimalRange.Maximum, decimalRange.ExclusiveMinimum, decimalRange.ExclusiveMaximum),
			
			Validation.RangeRule<double> doubleRange => GetRangeDescription(doubleRange.Minimum, doubleRange.Maximum, doubleRange.ExclusiveMinimum, doubleRange.ExclusiveMaximum),
			
			Validation.RangeRule<float> floatRange => GetRangeDescription(floatRange.Minimum, floatRange.Maximum, floatRange.ExclusiveMinimum, floatRange.ExclusiveMaximum),
			
			// For custom rules, return the error message directly
			_ when IsCustomRule(rule) => rule.ErrorMessage,
			
			_ => null
		};
	}

	static string? GetStringLengthDescription(Validation.StringLengthRule rule)
	{
		if (rule.MinLength.HasValue && rule.MaxLength.HasValue)
		{
			return $"Length must be between {rule.MinLength.Value} and {rule.MaxLength.Value} characters";
		}
		else if (rule.MinLength.HasValue)
		{
			return $"Minimum length: {rule.MinLength.Value} characters";
		}
		else if (rule.MaxLength.HasValue)
		{
			return $"Maximum length: {rule.MaxLength.Value} characters";
		}
		return null;
	}

	static string? GetRangeDescription<T>(T? minimum, T? maximum, bool exclusiveMin, bool exclusiveMax) where T : struct, IComparable<T>
	{
		if (minimum.HasValue && maximum.HasValue)
		{
			string minOperator = exclusiveMin ? ">" : ">=";
			string maxOperator = exclusiveMax ? "<" : "<=";
			return $"Must be {minOperator} {minimum.Value} and {maxOperator} {maximum.Value}";
		}
		else if (minimum.HasValue)
		{
			string minOperator = exclusiveMin ? ">" : ">=";
			return $"Must be {minOperator} {minimum.Value}";
		}
		else if (maximum.HasValue)
		{
			string maxOperator = exclusiveMax ? "<" : "<=";
			return $"Must be {maxOperator} {maximum.Value}";
		}
		return null;
	}

	static void ApplyRuleToSchema(Validation.ValidationRule rule, OpenApiSchema schema)
	{
		switch (rule)
		{
			case Validation.RequiredRule:
				// Required is handled at the parent schema level
				break;

			case Validation.StringLengthRule stringLengthRule:
				if (stringLengthRule.MinLength.HasValue)
				{
					schema.MinLength = stringLengthRule.MinLength.Value;
				}
				if (stringLengthRule.MaxLength.HasValue)
				{
					schema.MaxLength = stringLengthRule.MaxLength.Value;
				}
				break;

			case Validation.PatternRule patternRule:
				schema.Pattern = patternRule.Pattern;
				break;

			case Validation.EmailRule:
				schema.Format = "email";
				break;

			case Validation.UrlRule:
				schema.Format = "uri";
				break;

			case Validation.RangeRule<int> intRangeRule:
				if (intRangeRule.Minimum.HasValue)
				{
					schema.Minimum = intRangeRule.Minimum.Value.ToString();
					schema.ExclusiveMinimum = intRangeRule.ExclusiveMinimum.ToString().ToLowerInvariant();
					Console.WriteLine($"[DEBUG] Set int Minimum={schema.Minimum}, ExclusiveMinimum={schema.ExclusiveMinimum} from rule values Min={intRangeRule.Minimum.Value}, ExMin={intRangeRule.ExclusiveMinimum}");
				}
				if (intRangeRule.Maximum.HasValue)
				{
					schema.Maximum = intRangeRule.Maximum.Value.ToString();
					schema.ExclusiveMaximum = intRangeRule.ExclusiveMaximum.ToString().ToLowerInvariant();
					Console.WriteLine($"[DEBUG] Set int Maximum={schema.Maximum}, ExclusiveMaximum={schema.ExclusiveMaximum} from rule values Max={intRangeRule.Maximum.Value}, ExMax={intRangeRule.ExclusiveMaximum}");
				}
				break;

			case Validation.RangeRule<decimal> decimalRangeRule:
				if (decimalRangeRule.Minimum.HasValue)
				{
					schema.Minimum = decimalRangeRule.Minimum.Value.ToString();
					schema.ExclusiveMinimum = decimalRangeRule.ExclusiveMinimum.ToString().ToLowerInvariant();
				}
				if (decimalRangeRule.Maximum.HasValue)
				{
					schema.Maximum = decimalRangeRule.Maximum.Value.ToString();
					schema.ExclusiveMaximum = decimalRangeRule.ExclusiveMaximum.ToString().ToLowerInvariant();
				}
				break;

			case Validation.RangeRule<double> doubleRangeRule:
				if (doubleRangeRule.Minimum.HasValue)
				{
					schema.Minimum = doubleRangeRule.Minimum.Value.ToString();
					schema.ExclusiveMinimum = doubleRangeRule.ExclusiveMinimum.ToString().ToLowerInvariant();
				}
				if (doubleRangeRule.Maximum.HasValue)
				{
					schema.Maximum = doubleRangeRule.Maximum.Value.ToString();
					schema.ExclusiveMaximum = doubleRangeRule.ExclusiveMaximum.ToString().ToLowerInvariant();
				}
				break;

			case Validation.RangeRule<float> floatRangeRule:
				if (floatRangeRule.Minimum.HasValue)
				{
					schema.Minimum = floatRangeRule.Minimum.Value.ToString();
					schema.ExclusiveMinimum = floatRangeRule.ExclusiveMinimum.ToString().ToLowerInvariant();
				}
				if (floatRangeRule.Maximum.HasValue)
				{
					schema.Maximum = floatRangeRule.Maximum.Value.ToString();
					schema.ExclusiveMaximum = floatRangeRule.ExclusiveMaximum.ToString().ToLowerInvariant();
				}
				break;

			case Validation.RangeRule<long> longRangeRule:
				if (longRangeRule.Minimum.HasValue)
				{
					schema.Minimum = longRangeRule.Minimum.Value.ToString();
					schema.ExclusiveMinimum = longRangeRule.ExclusiveMinimum.ToString().ToLowerInvariant();
				}
				if (longRangeRule.Maximum.HasValue)
				{
					schema.Maximum = longRangeRule.Maximum.Value.ToString();
					schema.ExclusiveMaximum = longRangeRule.ExclusiveMaximum.ToString().ToLowerInvariant();
				}
				break;
		}
	}

	static JsonSchemaType? GetSchemaType(Validation.ValidationRule rule)
	{
		return rule switch
		{
			Validation.StringLengthRule => JsonSchemaType.String,
			Validation.PatternRule => JsonSchemaType.String,
			Validation.EmailRule => JsonSchemaType.String,
			Validation.UrlRule => JsonSchemaType.String,
			Validation.RangeRule<int> => JsonSchemaType.Integer,
			Validation.RangeRule<long> => JsonSchemaType.Integer,
			Validation.RangeRule<decimal> => JsonSchemaType.Number,
			Validation.RangeRule<double> => JsonSchemaType.Number,
			Validation.RangeRule<float> => JsonSchemaType.Number,
			_ => null
		};
	}

	static string? GetSchemaFormat(Validation.ValidationRule rule)
	{
		return rule switch
		{
			Validation.EmailRule => "email",
			Validation.UrlRule => "uri",
			Validation.RangeRule<int> => "int32",
			Validation.RangeRule<long> => "int64",
			Validation.RangeRule<float> => "float",
			Validation.RangeRule<double> => "double",
			_ => null
		};
	}

	static string ToCamelCase(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return value;
		}

		return char.ToLowerInvariant(value[0]) + value[1..];
	}
}
