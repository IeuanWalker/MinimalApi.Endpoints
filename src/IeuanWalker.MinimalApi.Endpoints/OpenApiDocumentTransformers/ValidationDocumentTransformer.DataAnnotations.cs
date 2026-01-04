using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.OpenApi;

namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers;

partial class ValidationDocumentTransformer
{
	static void DiscoverDataAnnotationValidationRules(OpenApiDocumentTransformerContext context, Dictionary<Type, (List<Validation.ValidationRule> rules, bool appendRulesToPropertyDescription)> allValidationRules)
	{
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
				// Find all types in this assembly
				Type[] types = assembly.GetTypes();
				foreach (Type type in types)
				{
					// Check if this type has the [ValidatableType] attribute
					// This attribute is used to opt-in to DataAnnotations validation in .NET 10
#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
					bool hasValidatableTypeAttribute = type.GetCustomAttribute<Microsoft.Extensions.Validation.ValidatableTypeAttribute>() is not null;
#pragma warning restore ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

					if (!hasValidatableTypeAttribute)
					{
						continue;
					}

					// Extract validation rules from this type
					List<Validation.ValidationRule> rules = ExtractDataAnnotationRules(type);

					if (rules.Count <= 0)
					{
						continue;
					}

					if (!allValidationRules.TryGetValue(type, out (List<Validation.ValidationRule> rules, bool appendRulesToPropertyDescription) value))
					{
						value.rules = [];
						value.appendRulesToPropertyDescription = true;
						allValidationRules[type] = value;
					}

					value.rules.AddRange(rules);
				}
			}
			catch (ReflectionTypeLoadException)
			{
				// Skip assemblies that can't be reflected
				continue;
			}
		}
	}

	static List<Validation.ValidationRule> ExtractDataAnnotationRules(Type type)
	{
		List<Validation.ValidationRule> rules = [];

		// Get all properties with validation attributes
		PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

		foreach (PropertyInfo property in properties)
		{
			// Get all validation attributes on this property
			ValidationAttribute[] validationAttributes = property.GetCustomAttributes<ValidationAttribute>(inherit: true).ToArray();

			foreach (ValidationAttribute attribute in validationAttributes)
			{
				Validation.ValidationRule? rule = ConvertDataAnnotationToValidationRule(property.Name, property.PropertyType, attribute);
				if (rule is not null)
				{
					rules.Add(rule);
				}
			}
		}

		return rules;
	}

	static Validation.ValidationRule? ConvertDataAnnotationToValidationRule(string propertyName, Type propertyType, ValidationAttribute attribute)
	{
		// Map DataAnnotation attributes to internal ValidationRule types
		return attribute switch
		{
			RequiredAttribute required => new Validation.RequiredRule(propertyName, required.ErrorMessage),

			StringLengthAttribute stringLength => new Validation.StringLengthRule(
				propertyName,
				minLength: stringLength.MinimumLength > 0 ? stringLength.MinimumLength : null,
				maxLength: stringLength.MaximumLength,
				errorMessage: stringLength.ErrorMessage),

			MinLengthAttribute minLength => new Validation.StringLengthRule(
				propertyName,
				minLength: minLength.Length,
				maxLength: null,
				errorMessage: minLength.ErrorMessage),

			MaxLengthAttribute maxLength => new Validation.StringLengthRule(
				propertyName,
				minLength: null,
				maxLength: maxLength.Length,
				errorMessage: maxLength.ErrorMessage),

			RangeAttribute range => CreateRangeRuleFromAttribute(propertyName, propertyType, range),

			RegularExpressionAttribute regex => new Validation.PatternRule(
				propertyName,
				regex.Pattern,
				errorMessage: regex.ErrorMessage),

			EmailAddressAttribute email => new Validation.EmailRule(propertyName, email.ErrorMessage),

			UrlAttribute url => new Validation.UrlRule(propertyName, url.ErrorMessage),

			PhoneAttribute phone => new Validation.CustomRule<object>(
				propertyName,
				phone.ErrorMessage ?? "Must be a valid phone number"),

			CreditCardAttribute creditCard => new Validation.CustomRule<object>(
				propertyName,
				creditCard.ErrorMessage ?? "Must be a valid credit card number"),

			CompareAttribute compare => new Validation.CustomRule<object>(
				propertyName,
				compare.ErrorMessage ?? $"Must match {compare.OtherProperty}"),

			FileExtensionsAttribute fileExtensions => new Validation.CustomRule<object>(
				propertyName,
				fileExtensions.ErrorMessage ?? $"Must be a file with extension: {fileExtensions.Extensions}"),

			CustomValidationAttribute customValidation => new Validation.CustomRule<object>(
				propertyName,
				customValidation.ErrorMessage ?? $"Custom validation failed"),

			// For any other custom ValidationAttribute subclass
			_ => CreateCustomRuleFromValidationAttribute(propertyName, attribute)
		};
	}

	static Validation.ValidationRule? CreateRangeRuleFromAttribute(string propertyName, Type propertyType, RangeAttribute range)
	{
		// Get the underlying type if it's nullable
		Type actualType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

		// Handle type-specific ranges
		if (actualType == typeof(int))
		{
			int min = Convert.ToInt32(range.Minimum);
			int max = Convert.ToInt32(range.Maximum);
			return new Validation.RangeRule<int>(propertyName, minimum: min, maximum: max, errorMessage: range.ErrorMessage);
		}
		else if (actualType == typeof(long))
		{
			long min = Convert.ToInt64(range.Minimum);
			long max = Convert.ToInt64(range.Maximum);
			return new Validation.RangeRule<long>(propertyName, minimum: min, maximum: max, errorMessage: range.ErrorMessage);
		}
		else if (actualType == typeof(double))
		{
			double min = Convert.ToDouble(range.Minimum);
			double max = Convert.ToDouble(range.Maximum);
			return new Validation.RangeRule<double>(propertyName, minimum: min, maximum: max, errorMessage: range.ErrorMessage);
		}
		else if (actualType == typeof(float))
		{
			float min = Convert.ToSingle(range.Minimum);
			float max = Convert.ToSingle(range.Maximum);
			return new Validation.RangeRule<float>(propertyName, minimum: min, maximum: max, errorMessage: range.ErrorMessage);
		}
		else if (actualType == typeof(decimal))
		{
			decimal min = Convert.ToDecimal(range.Minimum);
			decimal max = Convert.ToDecimal(range.Maximum);
			return new Validation.RangeRule<decimal>(propertyName, minimum: min, maximum: max, errorMessage: range.ErrorMessage);
		}
		else if (actualType == typeof(DateTime))
		{
			// DateTime ranges cannot be represented in OpenAPI schemas, so create a custom rule
			return new Validation.CustomRule<object>(
				propertyName,
				range.ErrorMessage ?? $"Must be between {range.Minimum} and {range.Maximum}");
		}

		// For other types, create a custom rule with the error message
		return new Validation.CustomRule<object>(
			propertyName,
			range.ErrorMessage ?? $"Must be between {range.Minimum} and {range.Maximum}");
	}

	static Validation.ValidationRule? CreateCustomRuleFromValidationAttribute(string propertyName, ValidationAttribute attribute)
	{
		// For custom ValidationAttribute subclasses, we need to get the error message
		// The error message might be set in the ErrorMessage property, or might be generated
		// by the FormatErrorMessage method

		string? errorMessage = attribute.ErrorMessage;

		// If no error message is set, try to get it from FormatErrorMessage
		if (string.IsNullOrEmpty(errorMessage))
		{
			try
			{
				// FormatErrorMessage needs a display name, we'll use the property name
				errorMessage = attribute.FormatErrorMessage(propertyName);
			}
#pragma warning disable CA1031 // Do not catch general exception types
			catch
			{
				// If FormatErrorMessage fails, use a default message
				errorMessage = $"{propertyName} validation failed";
			}
#pragma warning restore CA1031 // Do not catch general exception types
		}

		// If the error message is still empty or null, use a default
		if (string.IsNullOrEmpty(errorMessage))
		{
			errorMessage = $"{propertyName} validation failed";
		}

		return new Validation.CustomRule<object>(propertyName, errorMessage);
	}
}
