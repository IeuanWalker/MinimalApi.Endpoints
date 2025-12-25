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
/// OpenAPI document transformer that automatically extracts validation rules from FluentValidation validators
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class FluentValidationDocumentTransformer : IOpenApiDocumentTransformer
{
	public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
	{
		// Get all registered validators from DI
		var validators = context.ApplicationServices.GetServices<IValidator>();
		
		foreach (var validator in validators)
		{
			Type validatorType = validator.GetType();
			
			// Find the validated type (T in IValidator<T>)
			Type? validatedType = GetValidatedType(validatorType);
			if (validatedType == null)
			{
				continue;
			}
			
			// Extract validation rules from the validator
			List<Validation.ValidationRule> rules = ExtractValidationRules(validator, validatedType);
			
			if (rules.Count > 0)
			{
				// Check if there's manual WithValidation metadata for this type
				List<Validation.ValidationRule>? manualRules = GetManualValidationRules(context, validatedType);
				
				// Merge: manual rules take precedence over auto-generated rules
				List<Validation.ValidationRule> mergedRules = MergeValidationRules(rules, manualRules);
				
				// Apply to OpenAPI schema
				ApplyValidationToSchemas(document, validatedType, mergedRules);
			}
		}
		
		return Task.CompletedTask;
	}
	
	static Type? GetValidatedType(Type validatorType)
	{
		// Look for IValidator<T> interface
		Type? validatorInterface = validatorType.GetInterfaces()
			.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>));
		
		return validatorInterface?.GetGenericArguments().FirstOrDefault();
	}
	
	static List<Validation.ValidationRule> ExtractValidationRules(IValidator validator, Type validatedType)
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
			
			IComparisonValidator comparisonValidator => CreateRangeRule(propertyName, comparisonValidator),
			
			IBetweenValidator betweenValidator => CreateBetweenRule(propertyName, betweenValidator),
			
			_ => null // Unsupported validator types are skipped
		};
	}
	
	static Validation.ValidationRule? CreateStringLengthRule(string propertyName, ILengthValidator lengthValidator)
	{
		// Use reflection to get Min and Max properties
		PropertyInfo? minProp = lengthValidator.GetType().GetProperty("Min");
		PropertyInfo? maxProp = lengthValidator.GetType().GetProperty("Max");
		
		int? min = minProp?.GetValue(lengthValidator) as int?;
		int? max = maxProp?.GetValue(lengthValidator) as int?;
		
		return new Validation.StringLengthRule
		{
			PropertyName = propertyName,
			MinLength = min,
			MaxLength = max,
			ErrorMessage = $"{propertyName} length must be between {min} and {max} characters"
		};
	}
	
	static Validation.ValidationRule? CreatePatternRule(string propertyName, IRegularExpressionValidator regexValidator)
	{
		// Use reflection to get Expression property
		PropertyInfo? exprProp = regexValidator.GetType().GetProperty("Expression");
		string? pattern = exprProp?.GetValue(regexValidator) as string;
		
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
	
	static Validation.ValidationRule? CreateRangeRule(string propertyName, IComparisonValidator comparisonValidator)
	{
		// Get the ValueToCompare and Comparison properties
		PropertyInfo? valueProp = comparisonValidator.GetType().GetProperty("ValueToCompare");
		PropertyInfo? comparisonProp = comparisonValidator.GetType().GetProperty("Comparison");
		
		object? valueToCompare = valueProp?.GetValue(comparisonValidator);
		object? comparison = comparisonProp?.GetValue(comparisonValidator);
		
		if (valueToCompare == null || comparison == null)
		{
			return null;
		}
		
		// Determine the comparison type (GreaterThan, GreaterThanOrEqual, LessThan, LessThanOrEqual)
		string comparisonName = comparison.ToString() ?? "";
		
		// Create appropriate range rule based on value type
		return valueToCompare switch
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
	
	static List<Validation.ValidationRule>? GetManualValidationRules(OpenApiDocumentTransformerContext context, Type validatedType)
	{
		// Check all endpoints for WithValidation metadata matching this type
		EndpointDataSource? endpointDataSource = context.ApplicationServices.GetService(typeof(EndpointDataSource)) as EndpointDataSource;
		if (endpointDataSource == null)
		{
			return null;
		}
		
		foreach (var operation in endpointDataSource.Endpoints)
		{
			if (operation is RouteEndpoint routeEndpoint)
			{
				var metadataItems = routeEndpoint.Metadata.GetOrderedMetadata<object>();
				foreach (var metadata in metadataItems)
				{
					Type metadataType = metadata.GetType();
					if (metadataType.IsGenericType && 
					    metadataType.GetGenericTypeDefinition().Name.Contains("ValidationMetadata") &&
					    metadataType.GetGenericArguments()[0] == validatedType)
					{
						// Extract the configuration
						PropertyInfo? configProp = metadataType.GetProperty("Configuration");
						if (configProp?.GetValue(metadata) is object config)
						{
							PropertyInfo? rulesProp = config.GetType().GetProperty("Rules");
							if (rulesProp?.GetValue(config) is IEnumerable<Validation.ValidationRule> rules)
							{
								return rules.ToList();
							}
						}
					}
				}
			}
		}
		
		return null;
	}
	
	static List<Validation.ValidationRule> MergeValidationRules(List<Validation.ValidationRule> autoRules, List<Validation.ValidationRule>? manualRules)
	{
		if (manualRules == null || manualRules.Count == 0)
		{
			return autoRules;
		}
		
		// Group rules by property name
		var autoRulesByProperty = autoRules.GroupBy(r => r.PropertyName).ToDictionary(g => g.Key, g => g.ToList());
		var manualRulesByProperty = manualRules.GroupBy(r => r.PropertyName).ToDictionary(g => g.Key, g => g.ToList());
		
		List<Validation.ValidationRule> mergedRules = [];
		
		// Start with all manual rules (they take precedence)
		foreach (var kvp in manualRulesByProperty)
		{
			mergedRules.AddRange(kvp.Value);
		}
		
		// Add auto rules for properties that don't have manual rules
		foreach (var kvp in autoRulesByProperty)
		{
			if (!manualRulesByProperty.ContainsKey(kvp.Key))
			{
				mergedRules.AddRange(kvp.Value);
			}
		}
		
		return mergedRules;
	}
	
	static void ApplyValidationToSchemas(OpenApiDocument document, Type requestType, List<Validation.ValidationRule> rules)
	{
		// Try to find the schema using different naming strategies
		// The schema name could be: FullName, FullName with + replaced by ., or just Name
		if (document.Components?.Schemas == null)
		{
			return;
		}
		
		IOpenApiSchema? schemaInterface = null;
		string? schemaKey = null;
		
		// Try full name as-is
		string? fullName = requestType.FullName;
		if (fullName != null && document.Components.Schemas.TryGetValue(fullName, out schemaInterface))
		{
			schemaKey = fullName;
		}
		// Try full name with + replaced by . (common convention for nested types)
		else if (fullName != null && fullName.Contains('+'))
		{
			string modifiedName = fullName.Replace('+', '.');
			if (document.Components.Schemas.TryGetValue(modifiedName, out schemaInterface))
			{
				schemaKey = modifiedName;
			}
		}
		// Try just the type name
		else if (document.Components.Schemas.TryGetValue(requestType.Name, out schemaInterface))
		{
			schemaKey = requestType.Name;
		}
		
		if (schemaInterface == null || schemaKey == null)
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
				schema.Properties[propertyKey] = ValidationDocumentTransformer.CreateInlineSchemaWithAllValidation(propertySchemaInterface, propertyRules.ToList());
			}
		}
		
		// Set required properties on the schema
		if (requiredProperties.Count > 0)
		{
			schema.Required = new HashSet<string>(requiredProperties);
		}
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
