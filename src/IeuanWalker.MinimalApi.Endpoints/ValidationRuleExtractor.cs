using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Validators;
using System.Reflection;

namespace IeuanWalker.MinimalApi.Endpoints;

public static class ValidationRuleExtractor
{
	public static List<PropertyValidationRule> ExtractRules<T>(AbstractValidator<T> validator)
	{
		List<PropertyValidationRule> rules = new List<PropertyValidationRule>();

		try
		{
			// Get the descriptor to access validation rules
			IValidatorDescriptor descriptor = validator.CreateDescriptor();

			// Get all properties of the type
			PropertyInfo[] properties = typeof(T).GetProperties();

			foreach (PropertyInfo property in properties)
			{
				// Get validation rules for each property
				IEnumerable<IValidationRule> propertyRules = descriptor.GetRulesForMember(property.Name);

				foreach (IValidationRule? rule in propertyRules)
				{
					foreach (IRuleComponent? component in rule.Components)
					{
						Type validatorType = component.Validator.GetType();
						string ruleName = GetRuleName(validatorType);
						string errorMessage = component.GetUnformattedErrorMessage();

						// Extract additional parameters from validator
						Dictionary<string, object> parameters = ExtractValidatorParameters(component.Validator);

						rules.Add(new PropertyValidationRule
						{
							PropertyName = property.Name,
							RuleName = ruleName,
							ErrorMessage = errorMessage,
							ValidatorType = validatorType.Name,
							IsNestedProperty = false,
							Parameters = parameters
						});
					}
				}
			}

			// Also extract nested validator rules
			ExtractNestedValidatorRules<T>(validator, rules, string.Empty);
		}
		catch (Exception)
		{
			// If we can't extract rules for any reason, return empty list
			// This ensures the extension doesn't break the application
		}

		return rules;
	}

	static void ExtractNestedValidatorRules<T>(AbstractValidator<T> validator, List<PropertyValidationRule> rules, string prefix)
	{
		try
		{
			IValidatorDescriptor descriptor = validator.CreateDescriptor();
			PropertyInfo[] properties = typeof(T).GetProperties();

			foreach (PropertyInfo property in properties)
			{
				IEnumerable<IValidationRule> propertyRules = descriptor.GetRulesForMember(property.Name);

				foreach (IValidationRule? rule in propertyRules)
				{
					foreach (IRuleComponent? component in rule.Components)
					{
						// Handle child validators - check the actual type name since interface might not be available
						if (component.Validator.GetType().Name.Contains("ChildValidatorAdaptor"))
						{
							try
							{
								// Use reflection to get the validator type
								PropertyInfo? validatorTypeProperty = component.Validator.GetType().GetProperty("ValidatorType");
								if (validatorTypeProperty != null)
								{
									Type? childValidatorType = validatorTypeProperty.GetValue(component.Validator) as Type;
									if (childValidatorType != null)
									{
										object? childValidatorInstance = Activator.CreateInstance(childValidatorType);
										if (childValidatorInstance != null)
										{
											string childPropertyName = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";

											// Extract rules from child validator
											List<PropertyValidationRule> childRules = ExtractChildValidatorRules(childValidatorInstance, childPropertyName);
											rules.AddRange(childRules);
										}
									}
								}
							}
							catch
							{
								// Skip if we can't instantiate the child validator
							}
						}

						// Handle collection validators
						if (component.Validator.GetType().Name.Contains("CollectionValidator"))
						{
							// Mark as collection validator
							rules.Add(new PropertyValidationRule
							{
								PropertyName = property.Name,
								RuleName = "Collection",
								ErrorMessage = "Collection validation",
								ValidatorType = "CollectionValidator",
								IsNestedProperty = false,
								Parameters = []
							});
						}
					}
				}
			}
		}
		catch
		{
			// Ignore errors during nested extraction
		}
	}

	static List<PropertyValidationRule> ExtractChildValidatorRules(object validatorInstance, string parentPropertyName)
	{
		List<PropertyValidationRule> rules = new List<PropertyValidationRule>();

		try
		{
			// Use reflection to call CreateDescriptor method
			MethodInfo? descriptorMethod = validatorInstance.GetType().GetMethod("CreateDescriptor");
			if (descriptorMethod != null)
			{
				IValidatorDescriptor? descriptor = descriptorMethod.Invoke(validatorInstance, null) as IValidatorDescriptor;
				if (descriptor != null)
				{
					Type? validatedType = validatorInstance.GetType().BaseType?.GetGenericArguments().FirstOrDefault();
					if (validatedType != null)
					{
						PropertyInfo[] properties = validatedType.GetProperties();

						foreach (PropertyInfo property in properties)
						{
							IEnumerable<IValidationRule> propertyRules = descriptor.GetRulesForMember(property.Name);

							foreach (IValidationRule? rule in propertyRules)
							{
								foreach (IRuleComponent? component in rule.Components)
								{
									Type validatorType = component.Validator.GetType();
									string ruleName = GetRuleName(validatorType);
									string errorMessage = component.GetUnformattedErrorMessage();
									Dictionary<string, object> parameters = ExtractValidatorParameters(component.Validator);

									rules.Add(new PropertyValidationRule
									{
										PropertyName = $"{parentPropertyName}.{property.Name}",
										RuleName = ruleName,
										ErrorMessage = errorMessage,
										ValidatorType = validatorType.Name,
										IsNestedProperty = true,
										Parameters = parameters
									});
								}
							}
						}
					}
				}
			}
		}
		catch
		{
			// Ignore errors during child validator extraction
		}

		return rules;
	}

	static Dictionary<string, object> ExtractValidatorParameters(FluentValidation.Validators.IPropertyValidator validator)
	{
		Dictionary<string, object> parameters = new Dictionary<string, object>();

		try
		{
			Type validatorType = validator.GetType();

			var name = validatorType.Name;

			if (name.StartsWith("LengthValidator"))
			{
				if (TryGetPropertyValue(validator, "Min", out object? minLength))
				{
					parameters["MinLength"] = minLength;
				}
				if (TryGetPropertyValue(validator, "Max", out object? maxLength))
				{
					parameters["MaxLength"] = maxLength;
				}
			}
			else if (validator is IMinimumLengthValidator minimumLengthValidator)
			{
				parameters["MinLength"] = minimumLengthValidator.Min;
			}
			else if (name.StartsWith("MaximumLengthValidator"))
			{
				if (TryGetPropertyValue(validator, "MaxLength", out object? max))
				{
					parameters["MaxLength"] = max;
				}
			}
			else if (name.StartsWith("RegularExpressionValidator"))
			{
				if (TryGetPropertyValue(validator, "Expression", out object? regex))
				{
					parameters["Pattern"] = regex?.ToString() ?? string.Empty;
				}
			}
			else if (name.StartsWith("GreaterThanValidator"))
			{
				if (TryGetPropertyValue(validator, "ValueToCompare", out object? gtValue))
				{
					parameters["Minimum"] = gtValue;
				}
				parameters["ExclusiveMinimum"] = true;
			}
			else if (name.StartsWith("GreaterThanOrEqualValidator"))
			{
				if (TryGetPropertyValue(validator, "ValueToCompare", out object? gteValue))
				{
					parameters["Minimum"] = gteValue;
				}
				parameters["ExclusiveMinimum"] = false;
			}
			else if (name.StartsWith("LessThanValidator"))
			{
				if (TryGetPropertyValue(validator, "ValueToCompare", out object? ltValue))
				{
					parameters["Maximum"] = ltValue;
				}
				parameters["ExclusiveMaximum"] = true;
			}
			else if (name.StartsWith("LessThanOrEqualValidator"))
			{
				if (TryGetPropertyValue(validator, "ValueToCompare", out object? lteValue))
				{
					parameters["Maximum"] = lteValue;
				}
				parameters["ExclusiveMaximum"] = false;
			}
			else if (name.StartsWith("InclusiveBetweenValidator"))
			{
				if (TryGetPropertyValue(validator, "From", out object? fromValue))
				{
					parameters["Minimum"] = fromValue;
				}
				if (TryGetPropertyValue(validator, "To", out object? toValue))
				{
					parameters["Maximum"] = toValue;
				}
				parameters["ExclusiveMinimum"] = false;
				parameters["ExclusiveMaximum"] = false;
			}
		}
		catch
		{
			// Ignore parameter extraction errors
		}

		return parameters;
	}

	static bool TryGetPropertyValue(object obj, string propertyName, out object? value)
	{
		value = null;
		try
		{
			PropertyInfo? property = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (property != null)
			{
				value = property.GetValue(obj);
				return value != null;
			}
		}
		catch
		{
			// Ignore reflection errors
		}
		return false;
	}

	static string GetRuleName(Type validatorType)
	{
		var name = validatorType.Name;
		if (name.StartsWith("NotEmptyValidator")) return "NotEmpty";
		if (name.StartsWith("LengthValidator")) return "Length";
		if (name.StartsWith("MinimumLengthValidator")) return "MinimumLength";
		if (name.StartsWith("MaximumLengthValidator")) return "MaximumLength";
		if (name.StartsWith("NotNullValidator")) return "NotNull";
		if (name.StartsWith("EmailValidator")) return "EmailAddress";
		if (name.StartsWith("RegularExpressionValidator")) return "Matches";
		if (name.StartsWith("GreaterThanValidator")) return "GreaterThan";
		if (name.StartsWith("GreaterThanOrEqualValidator")) return "GreaterThanOrEqualTo";
		if (name.StartsWith("LessThanValidator")) return "LessThan";
		if (name.StartsWith("LessThanOrEqualValidator")) return "LessThanOrEqualTo";
		if (name.StartsWith("InclusiveBetweenValidator")) return "InclusiveBetween";
		if (name.StartsWith("ExclusiveBetweenValidator")) return "ExclusiveBetween";
		if (name.StartsWith("ExactLengthValidator")) return "Length";
		if (name.StartsWith("ChildValidatorAdaptor")) return "ChildValidator";
		if (name.StartsWith("CollectionValidatorAdaptor")) return "CollectionValidator";
		return name.Replace("Validator", "");
	}
}

public class PropertyValidationRule
{
	public string PropertyName { get; set; } = string.Empty;
	public string RuleName { get; set; } = string.Empty;
	public string ErrorMessage { get; set; } = string.Empty;
	public string ValidatorType { get; set; } = string.Empty;
	public bool IsNestedProperty { get; set; }
	public Dictionary<string, object> Parameters { get; set; } = [];
}
