using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Validators;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Routing;
using Microsoft.OpenApi;

namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers;

/// <summary>
/// OpenAPI document transformer that applies validation rules from both WithValidation and FluentValidation to schemas
/// </summary>
[ExcludeFromCodeCoverage]
sealed class ValidationDocumentTransformer : IOpenApiDocumentTransformer
{
	public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
	{
		// Dictionary to track all request types and their validation rules (from both manual and FluentValidation)
		Dictionary<Type, List<Validation.ValidationRule>> allValidationRules = [];

		// Dictionary to track whether to list rules in description for each request type
		Dictionary<Type, bool> listRulesInDescription = [];

		// Step 1: Discover FluentValidation rules
		DiscoverFluentValidationRules(context, allValidationRules, listRulesInDescription);

		// Step 2: Discover manual WithValidation rules (these override FluentValidation per property)
		DiscoverManualValidationRules(context, allValidationRules, listRulesInDescription);

		// Step 3: Apply all collected rules to OpenAPI schemas
		foreach (KeyValuePair<Type, List<Validation.ValidationRule>> kvp in allValidationRules)
		{
			Type requestType = kvp.Key;
			List<Validation.ValidationRule> rules = kvp.Value;
			bool listInDescription = listRulesInDescription.GetValueOrDefault(requestType, true); // Default to true
			ApplyValidationToSchemas(document, requestType, rules, listInDescription);
		}

		return Task.CompletedTask;
	}

	static void DiscoverFluentValidationRules(OpenApiDocumentTransformerContext context, Dictionary<Type, List<Validation.ValidationRule>> allValidationRules, Dictionary<Type, bool> listRulesInDescription)
	{
		// FluentValidation validators are registered as IValidator<T>, not IValidator
		// We need to scan assemblies to find all types that implement IValidator<T>

		List<IValidator> validators = [];

		// Get all loaded assemblies
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

		foreach (Assembly assembly in assemblies)
		{
			// Skip system assemblies
			if (assembly.FullName?.StartsWith("System.") == true ||
				assembly.FullName?.StartsWith("Microsoft.") == true ||
				assembly.FullName?.StartsWith("netstandard") == true)
			{
				continue;
			}

			try
			{
				// Find all validator types in this assembly
				Type[] types = assembly.GetTypes();
				foreach (Type type in types)
				{
					// Check if this type implements IValidator<T>
					Type? validatorInterface = type.GetInterfaces()
						.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>));

					if (validatorInterface != null && !type.IsAbstract && !type.IsInterface)
					{
						// Try to get this validator from DI
						object? validatorInstance = context.ApplicationServices.GetService(validatorInterface);
						if (validatorInstance is IValidator validator)
						{
							validators.Add(validator);
							Console.WriteLine($"[DEBUG-FV] Found validator: {type.Name} for {validatorInterface.GetGenericArguments()[0].Name}");
						}
					}
				}
			}
			catch (ReflectionTypeLoadException)
			{
				// Skip assemblies that can't be reflected
				continue;
			}
		}

		Console.WriteLine($"[DEBUG-FV] Found {validators.Count} FluentValidation validators");

		foreach (IValidator validator in validators)
		{
			Type validatorType = validator.GetType();

			// Find the validated type (T in IValidator<T>)
			Type? validatedType = GetValidatedType(validatorType);
			if (validatedType == null)
			{
				continue;
			}

			Console.WriteLine($"[DEBUG-FV] Processing validator for type: {validatedType.FullName}");

			// Extract validation rules from the validator
			List<Validation.ValidationRule> rules = ExtractFluentValidationRules(validator, validatedType);

			Console.WriteLine($"[DEBUG-FV] Extracted {rules.Count} rules for {validatedType.Name}");

			if (rules.Count > 0)
			{
				if (!allValidationRules.ContainsKey(validatedType))
				{
					allValidationRules[validatedType] = [];
				}

				// FluentValidation rules default to listing in description (true)
				if (!listRulesInDescription.ContainsKey(validatedType))
				{
					listRulesInDescription[validatedType] = true;
				}

				allValidationRules[validatedType].AddRange(rules);
			}
		}
	}

	static void DiscoverManualValidationRules(OpenApiDocumentTransformerContext context, Dictionary<Type, List<Validation.ValidationRule>> allValidationRules, Dictionary<Type, bool> listRulesInDescription)
	{
		// Iterate through all endpoints to find WithValidation metadata
		EndpointDataSource? endpointDataSource = context.ApplicationServices.GetService(typeof(EndpointDataSource)) as EndpointDataSource;
		if (endpointDataSource == null)
		{
			return;
		}

		foreach (Endpoint operation in endpointDataSource.Endpoints)
		{
			if (operation is RouteEndpoint routeEndpoint)
			{
				// Check for validation metadata
				IReadOnlyList<object> metadataItems = routeEndpoint.Metadata.GetOrderedMetadata<object>();
				foreach (object metadata in metadataItems)
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

							// Extract ListRulesInDescription setting from the configuration object
							PropertyInfo? listRulesInDescriptionProp = config.GetType().GetProperty("ListRulesInDescription");
							if (listRulesInDescriptionProp?.GetValue(config) is bool listInDesc)
							{
								listRulesInDescription[requestType] = listInDesc;
							}

							// Extract rules from the configuration object
							PropertyInfo? rulesProp = config.GetType().GetProperty("Rules");
							if (rulesProp?.GetValue(config) is IEnumerable<Validation.ValidationRule> manualRules)
							{
								// Manual rules override auto-discovered rules per property
								if (!allValidationRules.ContainsKey(requestType))
								{
									allValidationRules[requestType] = [];
								}

								// Remove auto-discovered rules for properties that have manual rules
								HashSet<string> manualPropertyNames = manualRules.Select(r => r.PropertyName).Distinct().ToHashSet();
								allValidationRules[requestType].RemoveAll(r => manualPropertyNames.Contains(r.PropertyName));

								// Add manual rules
								allValidationRules[requestType].AddRange(manualRules);
							}
						}
					}
				}
			}
		}
	}

	static Type? GetValidatedType(Type validatorType)
	{
		// Look for IValidator<T> interface
		Type? validatorInterface = validatorType.GetInterfaces()
			.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>));

		return validatorInterface?.GetGenericArguments().FirstOrDefault();
	}

	static List<Validation.ValidationRule> ExtractFluentValidationRules(IValidator validator, Type validatedType)
	{
		List<Validation.ValidationRule> rules = [];

		// Get the validator descriptor which contains all rules
		IValidatorDescriptor descriptor = validator.CreateDescriptor();

		foreach (IGrouping<string, (IPropertyValidator Validator, IRuleComponent Options)> memberValidators in descriptor.GetMembersWithValidators())
		{
			string propertyName = memberValidators.Key;

			// Each member returns a collection of (IPropertyValidator Validator, IRuleComponent Options) tuples
			foreach ((IPropertyValidator Validator, IRuleComponent Options) validatorTuple in memberValidators)
			{
				IPropertyValidator propertyValidator = validatorTuple.Validator;
				IRuleComponent ruleComponent = validatorTuple.Options;

				// Convert FluentValidation validators to our ValidationRule format
				Validation.ValidationRule? rule = ConvertToValidationRule(propertyName, propertyValidator, ruleComponent, validator, validatedType);
				if (rule != null)
				{
					rules.Add(rule);
				}
			}
		}

		return rules;
	}

	static Validation.ValidationRule? ConvertToValidationRule(string propertyName, IPropertyValidator propertyValidator, IRuleComponent ruleComponent, IValidator validator, Type validatedType)
	{
		// Skip ChildValidatorAdaptor (SetValidator) - nested object validators
		// These cannot be represented in OpenAPI schema constraints
		// The nested object's properties will have their own validators that will be processed separately
		string validatorTypeName = propertyValidator.GetType().Name;
		if (validatorTypeName.Contains("ChildValidatorAdaptor") || validatorTypeName.Contains("SetValidator"))
		{
			return null;
		}

		// Map FluentValidation validators to our internal ValidationRule types
		Validation.ValidationRule? rule = propertyValidator switch
		{
			INotNullValidator or INotEmptyValidator => new Validation.RequiredRule
			{
				PropertyName = propertyName,
				ErrorMessage = $"{propertyName} is required"
			},
			ILengthValidator lengthValidator => CreateStringLengthRule(propertyName, lengthValidator),
			IRegularExpressionValidator regexValidator => CreatePatternRule(propertyName, regexValidator),
			IEmailValidator => new Validation.EmailRule
			{
				PropertyName = propertyName,
				ErrorMessage = $"{propertyName} must be a valid email address"
			},
			IComparisonValidator comparisonValidator => CreateComparisonRule(propertyName, comparisonValidator),
			IBetweenValidator betweenValidator => CreateBetweenRule(propertyName, betweenValidator),
			_ => null
		};

		// If we couldn't map to a specific rule type, create a CustomRule with the error message
		if (rule == null)
		{
			string errorMessage = GetValidatorErrorMessage(propertyValidator, ruleComponent, propertyName, validator, validatedType);
			if (!string.IsNullOrEmpty(errorMessage))
			{
				// Create a CustomRule<object> to hold the unsupported validator's error message
				rule = new Validation.CustomRule<object>
				{
					PropertyName = propertyName,
					ErrorMessage = errorMessage
				};
			}
		}

		return rule;
	}

	static string GetValidatorErrorMessage(IPropertyValidator propertyValidator, IRuleComponent ruleComponent, string propertyName, IValidator validator, Type validatedType)
	{
#pragma warning disable CA1031 // Do not catch general exception types - we want to fallback gracefully for any reflection errors
		try
		{
			// Try to get the error message template from the rule component
			PropertyInfo? errorMessageProp = ruleComponent.GetType().GetProperty("ErrorMessageSource");
			if (errorMessageProp != null)
			{
				object? errorMessageSource = errorMessageProp.GetValue(ruleComponent);
				if (errorMessageSource != null)
				{
					// Try to get the message from the error message source
					PropertyInfo? getMessageProp = errorMessageSource.GetType().GetProperty("Message");
					if (getMessageProp != null)
					{
						string? message = getMessageProp.GetValue(errorMessageSource) as string;
						if (!string.IsNullOrEmpty(message))
						{
							// Replace placeholders with property name if needed
							return message.Replace("{PropertyName}", propertyName);
						}
					}
				}
			}

			// Fallback: try to construct a basic message from the validator type
			string validatorTypeName = propertyValidator.GetType().Name;
			// Remove "Validator" suffix if present
			if (validatorTypeName.EndsWith("Validator"))
			{
				validatorTypeName = validatorTypeName[..^9];
			}

			return $"{propertyName} {validatorTypeName} validation";
		}
		catch
		{
			// If all else fails, return a generic message
			return $"{propertyName} custom validation";
		}
#pragma warning restore CA1031
	}

	static Validation.ValidationRule? CreateStringLengthRule(string propertyName, ILengthValidator lengthValidator)
	{
		// Get the Min and Max properties using reflection
		PropertyInfo? minProp = lengthValidator.GetType().GetProperty("Min");
		PropertyInfo? maxProp = lengthValidator.GetType().GetProperty("Max");

		int? min = minProp?.GetValue(lengthValidator) as int?;
		int? max = maxProp?.GetValue(lengthValidator) as int?;

		if (min == null && max == null)
		{
			return null;
		}

		return new Validation.StringLengthRule
		{
			PropertyName = propertyName,
			MinLength = min > 0 ? min : null,
			MaxLength = max > 0 ? max : null,
			ErrorMessage = min.HasValue && max.HasValue
				? $"{propertyName} must be between {min} and {max} characters"
				: min.HasValue
					? $"{propertyName} must be at least {min} characters"
					: $"{propertyName} must not exceed {max} characters"
		};
	}

	static Validation.ValidationRule? CreatePatternRule(string propertyName, IRegularExpressionValidator regexValidator)
	{
		PropertyInfo? expressionProp = regexValidator.GetType().GetProperty("Expression");
		string? pattern = expressionProp?.GetValue(regexValidator) as string;

		if (string.IsNullOrEmpty(pattern))
		{
			return null;
		}

		return new Validation.PatternRule
		{
			PropertyName = propertyName,
			Pattern = pattern,
			ErrorMessage = $"{propertyName} does not match required pattern"
		};
	}

	static Validation.ValidationRule? CreateComparisonRule(string propertyName, IComparisonValidator comparisonValidator)
	{
		// Get the ValueToCompare and Comparison properties
		PropertyInfo? valueProp = comparisonValidator.GetType().GetProperty("ValueToCompare");
		PropertyInfo? comparisonProp = comparisonValidator.GetType().GetProperty("Comparison");

		object? value = valueProp?.GetValue(comparisonValidator);
		object? comparison = comparisonProp?.GetValue(comparisonValidator);

		if (value == null || comparison == null)
		{
			return null;
		}

		string comparisonName = comparison.ToString() ?? string.Empty;

		// Create appropriate range rule based on value type
		return value switch
		{
			int intValue => CreateTypedRangeRule<int>(propertyName, intValue, comparisonName),
			long longValue => CreateTypedRangeRule<long>(propertyName, longValue, comparisonName),
			decimal decimalValue => CreateTypedRangeRule<decimal>(propertyName, decimalValue, comparisonName),
			double doubleValue => CreateTypedRangeRule<double>(propertyName, doubleValue, comparisonName),
			float floatValue => CreateTypedRangeRule<float>(propertyName, floatValue, comparisonName),
			_ => null
		};
	}

	static Validation.ValidationRule? CreateTypedRangeRule<T>(string propertyName, T value, string comparisonName) where T : struct, IComparable<T>
	{
		return comparisonName switch
		{
			"GreaterThan" => new Validation.RangeRule<T>
			{
				PropertyName = propertyName,
				Minimum = value,
				ExclusiveMinimum = true,
				ErrorMessage = $"{propertyName} must be greater than {value}"
			},
			"GreaterThanOrEqual" => new Validation.RangeRule<T>
			{
				PropertyName = propertyName,
				Minimum = value,
				ExclusiveMinimum = false,
				ErrorMessage = $"{propertyName} must be greater than or equal to {value}"
			},
			"LessThan" => new Validation.RangeRule<T>
			{
				PropertyName = propertyName,
				Maximum = value,
				ExclusiveMaximum = true,
				ErrorMessage = $"{propertyName} must be less than {value}"
			},
			"LessThanOrEqual" => new Validation.RangeRule<T>
			{
				PropertyName = propertyName,
				Maximum = value,
				ExclusiveMaximum = false,
				ErrorMessage = $"{propertyName} must be less than or equal to {value}"
			},
			_ => null
		};
	}

	static Validation.ValidationRule? CreateBetweenRule(string propertyName, IBetweenValidator betweenValidator)
	{
		// Get the From and To properties
		PropertyInfo? fromProp = betweenValidator.GetType().GetProperty("From");
		PropertyInfo? toProp = betweenValidator.GetType().GetProperty("To");

		object? from = fromProp?.GetValue(betweenValidator);
		object? to = toProp?.GetValue(betweenValidator);

		if (from == null || to == null)
		{
			return null;
		}

		// Create appropriate range rule based on value type
		return from switch
		{
			int intFrom when to is int intTo => new Validation.RangeRule<int>
			{
				PropertyName = propertyName,
				Minimum = intFrom,
				Maximum = intTo,
				ExclusiveMinimum = false,
				ExclusiveMaximum = false,
				ErrorMessage = $"{propertyName} must be between {intFrom} and {intTo}"
			},
			long longFrom when to is long longTo => new Validation.RangeRule<long>
			{
				PropertyName = propertyName,
				Minimum = longFrom,
				Maximum = longTo,
				ExclusiveMinimum = false,
				ExclusiveMaximum = false,
				ErrorMessage = $"{propertyName} must be between {longFrom} and {longTo}"
			},
			decimal decimalFrom when to is decimal decimalTo => new Validation.RangeRule<decimal>
			{
				PropertyName = propertyName,
				Minimum = decimalFrom,
				Maximum = decimalTo,
				ExclusiveMinimum = false,
				ExclusiveMaximum = false,
				ErrorMessage = $"{propertyName} must be between {decimalFrom} and {decimalTo}"
			},
			double doubleFrom when to is double doubleTo => new Validation.RangeRule<double>
			{
				PropertyName = propertyName,
				Minimum = doubleFrom,
				Maximum = doubleTo,
				ExclusiveMinimum = false,
				ExclusiveMaximum = false,
				ErrorMessage = $"{propertyName} must be between {doubleFrom} and {doubleTo}"
			},
			float floatFrom when to is float floatTo => new Validation.RangeRule<float>
			{
				PropertyName = propertyName,
				Minimum = floatFrom,
				Maximum = floatTo,
				ExclusiveMinimum = false,
				ExclusiveMaximum = false,
				ErrorMessage = $"{propertyName} must be between {floatFrom} and {floatTo}"
			},
			_ => null
		};
	}

	static void ApplyValidationToSchemas(OpenApiDocument document, Type requestType, List<Validation.ValidationRule> rules, bool listRulesInDescription)
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

		// Group rules by property name
		IEnumerable<IGrouping<string, Validation.ValidationRule>> rulesByProperty = rules.GroupBy(r => r.PropertyName);

		// Track required properties
		List<string> requiredProperties = [];

		// Apply validation rules to properties
		foreach (IGrouping<string, Validation.ValidationRule> propertyRules in rulesByProperty)
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
				schema.Properties[propertyKey] = CreateInlineSchemaWithAllValidation(propertySchemaInterface, [.. propertyRules], listRulesInDescription);
			}
		}

		// Set required properties on the schema
		if (requiredProperties.Count > 0)
		{
			schema.Required = new HashSet<string>(requiredProperties);
		}
	}

	internal static OpenApiSchema CreateInlineSchemaWithAllValidation(IOpenApiSchema originalSchema, List<Validation.ValidationRule> rules, bool listRulesInDescription)
	{
		// Check for per-property ListRulesInDescription setting (takes precedence over global setting)
		bool? perPropertySetting = rules.FirstOrDefault(r => r.ListRulesInDescription.HasValue)?.ListRulesInDescription;
		bool effectiveListRulesInDescription = perPropertySetting ?? listRulesInDescription;

		// Cast to OpenApiSchema to access properties
		OpenApiSchema? originalOpenApiSchema = originalSchema as OpenApiSchema;

		// Check if this is a complex object (has AllOf for schema references or has Properties for inline object definitions)
		// Complex objects should preserve their structure and only add validation descriptions
		bool isComplexObject = (originalOpenApiSchema?.AllOf?.Count > 0) ||
							   (originalOpenApiSchema?.Properties?.Count > 0 && originalOpenApiSchema.Type == JsonSchemaType.Object);

		// For complex objects (nested types with AllOf or object definitions), preserve the original schema structure
		if (isComplexObject && originalOpenApiSchema != null)
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
				foreach (Validation.ValidationRule rule in rules)
				{
					if (rule is Validation.RequiredRule)
					{
						ruleDescriptions.Add("Required");
					}
				}
			}

			// Build the complete description
			List<string> descriptionParts = [];

			if (!string.IsNullOrEmpty(customDescription))
			{
				descriptionParts.Add(customDescription);
			}

			if (ruleDescriptions.Count > 0 && effectiveListRulesInDescription)
			{
				string rulesSection = "Validation rules:\n" + string.Join("\n", ruleDescriptions.Select(msg => $"- {msg}"));
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
		JsonSchemaType? schemaType = rules.Select(GetSchemaType).FirstOrDefault(t => t != null);
		string? format = rules.Select(GetSchemaFormat).FirstOrDefault(f => f != null);

		// Create inline schema - set properties after creation to avoid initialization issues
		OpenApiSchema newInlineSchema = new();

		// Set type and format separately
		if (schemaType.HasValue)
		{
			newInlineSchema.Type = schemaType.Value;
		}
		if (format != null)
		{
			newInlineSchema.Format = format;
		}

		Console.WriteLine($"[DEBUG] Created inline schema: Type={newInlineSchema.Type}, Format={newInlineSchema.Format}");

		// Extract custom description from DescriptionRule if present
		string? customDescription2 = rules
			.OfType<Validation.DescriptionRule>()
			.FirstOrDefault()?.Description;

		// Collect all rule descriptions (excluding DescriptionRule)
		List<string> ruleDescriptions2 = [];

		// Apply all rules to this schema
		foreach (Validation.ValidationRule rule in rules)
		{
			Console.WriteLine($"[DEBUG] Applying rule: {rule.GetType().Name} for property {rule.PropertyName}");

			// Skip DescriptionRule - it's handled separately
			if (rule is Validation.DescriptionRule)
			{
				continue;
			}

			// Get human-readable description for this rule (only if effectiveListRulesInDescription is true)
			if (effectiveListRulesInDescription)
			{
				string? ruleDescription = GetRuleDescription(rule);
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

			Console.WriteLine($"[DEBUG] After applying rule - Minimum='{newInlineSchema.Minimum}', Maximum='{newInlineSchema.Maximum}'");
		}

		// Build the complete description: custom description + validation rules
		List<string> descriptionParts2 = [];

		// Add custom description first if present
		if (!string.IsNullOrEmpty(customDescription2))
		{
			descriptionParts2.Add(customDescription2);
		}

		// Add validation rules section if any exist (and if effectiveListRulesInDescription is true)
		if (ruleDescriptions2.Count > 0 && effectiveListRulesInDescription)
		{
			string rulesSection = "Validation rules:\n" + string.Join("\n", ruleDescriptions2.Select(msg => $"- {msg}"));
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
