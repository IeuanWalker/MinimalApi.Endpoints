using IeuanWalker.MinimalApi.Endpoints.Extensions;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers;

/// <summary>
/// OpenAPI document transformer that applies validation rules from both WithValidation and FluentValidation to schemas
/// </summary>
sealed partial class ValidationDocumentTransformer : IOpenApiDocumentTransformer
{
	public bool AutoDocumentFluentValdation { get; set; } = true;
	public bool AppendRulesToPropertyDescription { get; set; } = true;
	public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
	{
		// Dictionary to track all request types and their validation rules (from both manual and FluentValidation)
		Dictionary<Type, (List<Validation.ValidationRule>, bool appendRulesToPropertyDescription)> allValidationRules = [];

		// Step 1: Discover FluentValidation rules
		if (AutoDocumentFluentValdation)
		{
			DiscoverFluentValidationRules(context, allValidationRules);
		}

		// Step 2: Discover manual WithValidation rules (these override FluentValidation per property)
		DiscoverManualValidationRules(context, allValidationRules);

		// Step 3: Apply all collected rules to OpenAPI schemas
		foreach (KeyValuePair<Type, (List<Validation.ValidationRule> rules, bool appendRulesToPropertyDescription)> kvp in allValidationRules)
		{
			Type requestType = kvp.Key;
			List<Validation.ValidationRule> rules = kvp.Value.rules;
			bool typeAppendRulesToPropertyDescription = kvp.Value.appendRulesToPropertyDescription;

			ApplyValidationToSchemas(document, requestType, rules, typeAppendRulesToPropertyDescription, AppendRulesToPropertyDescription);
		}

		return Task.CompletedTask;
	}

	static void ApplyValidationToSchemas(OpenApiDocument document, Type requestType, List<Validation.ValidationRule> rules, bool typeAppendRulesToPropertyDescription, bool appendRulesToPropertyDescription)
	{
		// Get the schema name for the request type
		string schemaName = requestType.FullName ?? requestType.Name;

		// Find the schema in components
		if (document.Components?.Schemas is null || !document.Components.Schemas.TryGetValue(schemaName, out IOpenApiSchema? schemaInterface))
		{
			return;
		}

		if (schemaInterface is not OpenApiSchema schema || schema.Properties is null)
		{
			return;
		}

		// Group rules by property name
		IEnumerable<IGrouping<string, Validation.ValidationRule>> rulesByProperty = rules.GroupBy(r => r.PropertyName);

		// Track required properties
		List<string> requiredProperties = [];

		// Apply validation rules to properties
		foreach (IGrouping<string, Validation.ValidationRule> propertyRules in rulesByProperty)
		{
			string propertyKey = propertyRules.Key.ToCamelCase();

			// Check if any rule for this property is RequiredRule
			bool hasRequiredRule = propertyRules.Any(r => r is Validation.RequiredRule);
			if (hasRequiredRule)
			{
				requiredProperties.Add(propertyKey);
			}

			// Check if there are any rules other than RequiredRule and DescriptionRule
			// If only RequiredRule/DescriptionRule exist, we don't need to modify the schema inline
			// (RequiredRule is handled by adding to required array, DescriptionRule doesn't apply to nested objects)
			bool hasOtherRules = propertyRules.Any(r => r is not Validation.RequiredRule and not Validation.DescriptionRule);

			// Only create inline schema if there are validation rules other than just Required
			// This preserves $ref for nested objects that only have Required validation
			if (hasOtherRules)
			{
				if (schema.Properties.TryGetValue(propertyKey, out IOpenApiSchema? propertySchemaInterface))
				{

					// Create inline schema with all validation constraints for this property
					schema.Properties[propertyKey] = CreateInlineSchemaWithAllValidation(propertySchemaInterface, [.. propertyRules], typeAppendRulesToPropertyDescription, appendRulesToPropertyDescription);
				}
			}
		}

		// Set required properties on the schema
		if (requiredProperties.Count > 0)
		{
			schema.Required = new HashSet<string>(requiredProperties);
		}
	}

	internal static OpenApiSchema CreateInlineSchemaWithAllValidation(IOpenApiSchema originalSchema, List<Validation.ValidationRule> rules, bool typeAppendRulesToPropertyDescription, bool appendRulesToPropertyDescription)
	{
		// Check for per-property AppendRulesToPropertyDescription setting (takes precedence over global setting)
		bool? perPropertySetting = rules.FirstOrDefault(r => r.AppendRuleToPropertyDescription.HasValue)?.AppendRuleToPropertyDescription;
		bool effectiveListRulesInDescription = perPropertySetting ?? typeAppendRulesToPropertyDescription;

		// Cast to OpenApiSchema to access properties
		OpenApiSchema? originalOpenApiSchema = originalSchema as OpenApiSchema;

		// Check if this is a complex object (has AllOf for schema references or has Properties for inline object definitions)
		// Complex objects should preserve their structure and only add validation descriptions
		bool isComplexObject = (originalOpenApiSchema?.AllOf?.Count > 0) ||
							   (originalOpenApiSchema?.Properties?.Count > 0 && originalOpenApiSchema.Type == JsonSchemaType.Object);

		// For complex objects (nested types with AllOf or object definitions), preserve the original schema structure
		if (isComplexObject && originalOpenApiSchema is not null)
		{
			// For complex objects, we don't want to lose the AllOf or Properties structure
			// Just return the original schema with an added description for validation rules

			// Extract custom description from DescriptionRule if present
			string? customDescription = rules
				.OfType<Validation.DescriptionRule>()
				.FirstOrDefault()?.Description;

			// Collect only applicable rule descriptions (RequiredRule for complex objects)
			List<string> ruleDescriptions = [];
			if (effectiveListRulesInDescription)
			{
				foreach (Validation.RequiredRule _ in rules.OfType<Validation.RequiredRule>())
				{
					ruleDescriptions.Add("Required");
				}
			}

			// Build the complete description
			List<string> descriptionParts = [];

			if (!string.IsNullOrEmpty(customDescription))
			{
				descriptionParts.Add(customDescription);
			}

			if (appendRulesToPropertyDescription && ruleDescriptions.Count > 0 && effectiveListRulesInDescription)
			{
				string rulesSection = "Validation rules:\n" + string.Join("\n", ruleDescriptions.Distinct().Select(msg => $"- {msg}"));
				descriptionParts.Add(rulesSection);
			}

			// Create a new schema that preserves the original structure but adds the description
			OpenApiSchema complexSchema = new()
			{
				AllOf = originalOpenApiSchema.AllOf,
				Properties = originalOpenApiSchema.Properties,
				Type = originalOpenApiSchema.Type,
				Format = originalOpenApiSchema.Format,
				Items = originalOpenApiSchema.Items,
				AdditionalProperties = originalOpenApiSchema.AdditionalProperties,
				Description = descriptionParts.Count > 0 ? string.Join("\n\n", descriptionParts) : originalOpenApiSchema.Description
			};

			return complexSchema;
		}

		// For simple properties, create a new inline schema with all validation rules
		// Get the type from the first rule (all rules for same property should have same type)
		JsonSchemaType? schemaType = rules.Select(GetSchemaType).FirstOrDefault(t => t is not null);
		string? format = rules.Select(GetSchemaFormat).FirstOrDefault(f => f is not null);

		// Create inline schema - set properties after creation to avoid initialization issues
		OpenApiSchema newInlineSchema = new();

		// Set type and format separately
		if (schemaType.HasValue)
		{
			newInlineSchema.Type = schemaType.Value;
		}
		if (format is not null)
		{
			newInlineSchema.Format = format;
		}

		// Extract custom description from DescriptionRule if present
		string? customDescription2 = rules
			.OfType<Validation.DescriptionRule>()
			.FirstOrDefault()?.Description;

		// Collect all rule descriptions (excluding DescriptionRule)
		List<string> ruleDescriptions2 = [];

		// Apply all rules to this schema
		foreach (Validation.ValidationRule rule in rules)
		{
			// Skip DescriptionRule - it's handled separately
			if (rule is Validation.DescriptionRule)
			{
				continue;
			}

			// Get human-readable description for this rule (only if effectiveListRulesInDescription is true)
			if (effectiveListRulesInDescription)
			{
				string? ruleDescription = rule.ErrorMessage;
				if (!string.IsNullOrEmpty(ruleDescription))
				{
					ruleDescriptions2.Add(ruleDescription);
				}
			}

			// Apply rule to schema (for non-custom and non-description rules)
			if (!IsCustomRule(rule))
			{
				ApplyRuleToSchema(rule, newInlineSchema);
			}
		}

		// Build the complete description: custom description + validation rules
		List<string> descriptionParts2 = [];

		// Add custom description first if present
		if (!string.IsNullOrEmpty(customDescription2))
		{
			descriptionParts2.Add(customDescription2);
		}

		// Add validation rules section if any exist (and if effectiveListRulesInDescription is true)
		if (appendRulesToPropertyDescription && ruleDescriptions2.Count > 0 && effectiveListRulesInDescription)
		{
			string rulesSection = "Validation rules:\n" + string.Join("\n", ruleDescriptions2.Distinct().Select(msg => $"- {msg}"));
			descriptionParts2.Add(rulesSection);
		}

		// Set the final description
		if (descriptionParts2.Count > 0)
		{
			newInlineSchema.Description = string.Join("\n\n", descriptionParts2);
		}

		return newInlineSchema;
	}

	static bool IsCustomRule(Validation.ValidationRule rule)
	{
		Type ruleType = rule.GetType();
		return ruleType.IsGenericType && ruleType.GetGenericTypeDefinition() == typeof(Validation.CustomRule<>);
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
				}
				if (intRangeRule.Maximum.HasValue)
				{
					schema.Maximum = intRangeRule.Maximum.Value.ToString();
					schema.ExclusiveMaximum = intRangeRule.ExclusiveMaximum.ToString().ToLowerInvariant();
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
}
