using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Validators;
using IeuanWalker.MinimalApi.Endpoints.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers;

/// <summary>
/// OpenAPI document transformer that applies validation rules from both WithValidation and FluentValidation to schemas
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class ValidationDocumentTransformer : IOpenApiDocumentTransformer
{
	public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
	{
		// Dictionary to track all request types and their validation rules (from both manual and FluentValidation)
		Dictionary<Type, List<Validation.ValidationRule>> allValidationRules = [];
		
		// Step 1: Discover FluentValidation rules
		DiscoverFluentValidationRules(context, allValidationRules);
		
		// Step 2: Discover manual WithValidation rules (these override FluentValidation per property)
		DiscoverManualValidationRules(context, allValidationRules);
		
		// Step 3: Apply all collected rules to OpenAPI schemas
		foreach (var kvp in allValidationRules)
		{
			Type requestType = kvp.Key;
			List<Validation.ValidationRule> rules = kvp.Value;
			ApplyValidationToSchemas(document, requestType, rules);
		}

		return Task.CompletedTask;
	}
	
	static void DiscoverFluentValidationRules(OpenApiDocumentTransformerContext context, Dictionary<Type, List<Validation.ValidationRule>> allValidationRules)
	{
		// Get all registered FluentValidation validators from DI
		var validators = context.ApplicationServices.GetServices<IValidator>();
		
		Console.WriteLine($"[DEBUG-FV] Found {validators.Count()} FluentValidation validators");
		
		foreach (var validator in validators)
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
				allValidationRules[validatedType].AddRange(rules);
			}
		}
	}
	
	static void DiscoverManualValidationRules(OpenApiDocumentTransformerContext context, Dictionary<Type, List<Validation.ValidationRule>> allValidationRules)
	{
		// Iterate through all endpoints to find WithValidation metadata
		EndpointDataSource? endpointDataSource = context.ApplicationServices.GetService(typeof(EndpointDataSource)) as EndpointDataSource;
		if (endpointDataSource == null)
		{
			return;
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
								var manualPropertyNames = manualRules.Select(r => r.PropertyName).Distinct().ToHashSet();
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
		
		foreach (var memberValidators in descriptor.GetMembersWithValidators())
		{
			string propertyName = memberValidators.Key;
			
			// Each member returns a collection of (IPropertyValidator Validator, IRuleComponent Options) tuples
			foreach (var validatorTuple in memberValidators)
			{
				IPropertyValidator propertyValidator = validatorTuple.Validator;
				
				// Convert FluentValidation validators to our ValidationRule format
				Validation.ValidationRule? rule = ConvertToValidationRule(propertyName, propertyValidator);
				if (rule != null)
				{
					rules.Add(rule);
				}
			}
		}
		
		return rules;
	}
	
	static Validation.ValidationRule? ConvertToValidationRule(string propertyName, IPropertyValidator propertyValidator)
	{
		// Map FluentValidation validators to our internal ValidationRule types
		return propertyValidator switch
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

	static void ApplyValidationToSchemas(OpenApiDocument document, Type requestType, List<Validation.ValidationRule> rules)
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

	internal static OpenApiSchema CreateInlineSchemaWithAllValidation(IOpenApiSchema originalSchema, List<Validation.ValidationRule> rules)
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
