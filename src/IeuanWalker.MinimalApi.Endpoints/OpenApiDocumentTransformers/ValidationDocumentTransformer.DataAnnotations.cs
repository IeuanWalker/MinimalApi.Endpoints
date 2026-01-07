using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers;

partial class ValidationDocumentTransformer
{
	static readonly object dataAnnotationCacheLock = new();
	static Dictionary<Type, List<Validation.ValidationRule>>? dataAnnotationRuleCache;

	static void DiscoverDataAnnotationValidationRules(OpenApiDocumentTransformerContext context, Dictionary<Type, (List<Validation.ValidationRule> rules, bool appendRulesToPropertyDescription)> allValidationRules)
	{
		ILogger logger = context.ApplicationServices.GetService(typeof(ILogger<ValidationDocumentTransformer>)) as ILogger ?? NullLogger.Instance;

		Dictionary<Type, List<Validation.ValidationRule>> cachedRules = GetCachedDataAnnotationRules(logger);

		foreach (KeyValuePair<Type, List<Validation.ValidationRule>> kvp in cachedRules)
		{
			if (kvp.Value.Count == 0)
			{
				continue;
			}

			if (!allValidationRules.TryGetValue(kvp.Key, out (List<Validation.ValidationRule> rules, bool appendRulesToPropertyDescription) value))
			{
				value.rules = [];
				value.appendRulesToPropertyDescription = true;
			}

			value.rules.AddRange(kvp.Value);
			allValidationRules[kvp.Key] = value;
		}
	}

	static Dictionary<Type, List<Validation.ValidationRule>> GetCachedDataAnnotationRules(ILogger logger)
	{
		if (dataAnnotationRuleCache is not null)
		{
			return dataAnnotationRuleCache;
		}

		lock (dataAnnotationCacheLock)
		{
			dataAnnotationRuleCache ??= BuildDataAnnotationRuleCache(logger);
			return dataAnnotationRuleCache;
		}
	}

	static Dictionary<Type, List<Validation.ValidationRule>> BuildDataAnnotationRuleCache(ILogger logger)
	{
		Dictionary<Type, (List<Validation.ValidationRule> rules, bool appendRulesToPropertyDescription)> discoveredValidationRules = [];
		HashSet<Type> processedTypes = [];

		foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			if (assembly.IsDynamic)
			{
				continue;
			}

			if (assembly.FullName?.StartsWith("System.") == true ||
				assembly.FullName?.StartsWith("Microsoft.") == true ||
				assembly.FullName?.StartsWith("netstandard") == true)
			{
				continue;
			}

			Type[] types;
			try
			{
				types = assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException)
			{
				continue;
			}

			foreach (Type type in types)
			{
#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
				bool hasValidatableTypeAttribute = type.GetCustomAttribute<Microsoft.Extensions.Validation.ValidatableTypeAttribute>() is not null;
#pragma warning restore ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

				if (!hasValidatableTypeAttribute)
				{
					continue;
				}

				ProcessTypeRecursively(type, discoveredValidationRules, processedTypes, logger);
			}
		}

		return discoveredValidationRules
			.Where(kvp => kvp.Value.rules.Count > 0)
			.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.rules);
	}

	static void ProcessTypeRecursively(Type type, Dictionary<Type, (List<Validation.ValidationRule> rules, bool appendRulesToPropertyDescription)> allValidationRules, HashSet<Type> processedTypes, ILogger logger)
	{
		// Avoid processing the same type multiple times
		if (processedTypes.Contains(type))
		{
			return;
		}

		processedTypes.Add(type);

		// Extract validation rules from this type
		List<Validation.ValidationRule> rules = ExtractDataAnnotationRules(type, logger);

		if (rules.Count > 0)
		{
			if (!allValidationRules.TryGetValue(type, out (List<Validation.ValidationRule> rules, bool appendRulesToPropertyDescription) value))
			{
				value.rules = [];
				value.appendRulesToPropertyDescription = true;
				allValidationRules[type] = value;
			}

			value.rules.AddRange(rules);
			allValidationRules[type] = value;
		}

		// Get all properties to find nested objects
		PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

		foreach (Type actualType in properties.Select(p =>
		{
			Type propertyType = p.PropertyType;
			Type actualType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

			// Check if it's a collection type
			if (actualType != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(actualType))
			{
				// Get the element type for collections
				if (actualType.IsGenericType)
				{
					Type[] genericArgs = actualType.GetGenericArguments();
					if (genericArgs.Length > 0)
					{
						actualType = genericArgs[0];
					}
				}
				else if (actualType.IsArray)
				{
					actualType = actualType.GetElementType() ?? actualType;
				}
			}

			return actualType;
		})
		.Where(actualType =>
		{
			// Skip primitive types, system types, and types we've already processed
			if (actualType.IsPrimitive ||
				actualType == typeof(string) ||
				actualType == typeof(decimal) ||
				actualType == typeof(DateTime) ||
				actualType == typeof(DateTimeOffset) ||
				actualType == typeof(TimeSpan) ||
				actualType == typeof(Guid) ||
				actualType.Namespace?.StartsWith("System.") == true ||
				actualType.Namespace?.StartsWith("Microsoft.") == true ||
				processedTypes.Contains(actualType))
			{
				return false;
			}

			// Check if this is a complex type with validation attributes
			PropertyInfo[] nestedProperties = actualType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
			return nestedProperties.Any(np => np.GetCustomAttributes<ValidationAttribute>(inherit: true).Any());
		}))
		{
			// Recursively process this nested type
			ProcessTypeRecursively(actualType, allValidationRules, processedTypes, logger);
		}
	}

	static List<Validation.ValidationRule> ExtractDataAnnotationRules(Type type, ILogger logger)
	{
		List<Validation.ValidationRule> rules = [];

		// Get all properties with validation attributes
		PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

		foreach (PropertyInfo property in properties)
		{
			// Get all validation attributes on this property
			ValidationAttribute[] validationAttributes = [.. property.GetCustomAttributes<ValidationAttribute>(inherit: true)];

			foreach (ValidationAttribute attribute in validationAttributes)
			{
				Validation.ValidationRule? rule = ConvertDataAnnotationToValidationRule(property, type, attribute, logger);
				if (rule is not null)
				{
					rules.Add(rule);
				}
			}
		}

		return rules;
	}

	static Validation.ValidationRule? ConvertDataAnnotationToValidationRule(PropertyInfo property, Type propertyParent, ValidationAttribute attribute, ILogger logger)
	{
		string propertyName = property.Name;
		bool isCollection = IsCollectionType(property.PropertyType);

		// Map DataAnnotation attributes to internal ValidationRule types
		return attribute switch
		{
			RequiredAttribute => new Validation.RequiredRule(propertyName),

			StringLengthAttribute stringLength => CreateStringLengthRule(
				propertyName,
				minLength: stringLength.MinimumLength > 0 ? stringLength.MinimumLength : null,
				maxLength: stringLength.MaximumLength,
				isCollection),

			MinLengthAttribute minLength => CreateStringLengthRule(
				propertyName,
				minLength: minLength.Length,
				maxLength: null,
				isCollection),

			MaxLengthAttribute maxLength => CreateStringLengthRule(
				propertyName,
				minLength: null,
				maxLength: maxLength.Length,
				isCollection),

			RangeAttribute range => CreateRangeRuleFromAttribute(propertyName, property.PropertyType, range),

			RegularExpressionAttribute regex => new Validation.PatternRule(propertyName, regex.Pattern),

			EmailAddressAttribute => new Validation.EmailRule(propertyName),

			UrlAttribute => new Validation.UrlRule(propertyName),

			PhoneAttribute => new Validation.CustomRule<object>(propertyName, "Must be a valid phone number"),

			CreditCardAttribute => new Validation.CustomRule<object>(propertyName, "Must be a valid credit card number"),

			CompareAttribute compare => new Validation.CustomRule<object>(propertyName, $"Must match {compare.OtherProperty}"),

			FileExtensionsAttribute fileExtensions => new Validation.CustomRule<object>(propertyName, $"Must be a file with extension: {fileExtensions.Extensions}"),

			CustomValidationAttribute customValidation => new Validation.CustomRule<object>(
				propertyName,
				GetFormattedErrorMessage(customValidation, logger, propertyParent, propertyName, "Custom validation failed") ?? "Custom validation failed"),

			// For any other custom ValidationAttribute subclass
			_ => CreateCustomRuleFromValidationAttribute(propertyName, propertyParent, attribute, logger)
		};
	}

	static bool IsCollectionType(Type type)
	{
		// Get the underlying type if nullable
		Type actualType = Nullable.GetUnderlyingType(type) ?? type;

		// String is not considered a collection for our purposes
		if (actualType == typeof(string))
		{
			return false;
		}

		// Check if it's an array or implements IEnumerable
		return actualType.IsArray ||
			   (actualType != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(actualType));
	}

	static Validation.StringLengthRule CreateStringLengthRule(string propertyName, int? minLength, int? maxLength, bool isCollection)
	{
		// Generate appropriate error message based on whether it's a collection or string
		string? errorMessage = null;

		if (minLength.HasValue && maxLength.HasValue)
		{
			errorMessage = isCollection
				? $"Must be at least {minLength.Value} items and less than {maxLength.Value} items"
				: $"Must be at least {minLength.Value} characters and less than {maxLength.Value} characters";
		}
		else if (minLength.HasValue)
		{
			errorMessage = isCollection
				? $"Must be {minLength.Value} items or more"
				: $"Must be {minLength.Value} characters or more";
		}
		else if (maxLength.HasValue)
		{
			errorMessage = isCollection
				? $"Must be {maxLength.Value} items or fewer"
				: $"Must be {maxLength.Value} characters or fewer";
		}

		return new Validation.StringLengthRule(propertyName, minLength, maxLength, errorMessage);
	}

	static string? GetFormattedErrorMessage(ValidationAttribute attribute, ILogger logger, Type propertyValidator, string propertyName, string? defaultMessage = null)
	{
		// If a custom error message is set, use it
		if (!string.IsNullOrEmpty(attribute.ErrorMessage))
		{
			return attribute.ErrorMessage;
		}

		// Otherwise, try to format the default message
		try
		{
			string formatted = attribute.FormatErrorMessage(propertyName);
			// Check if the formatted message still has placeholders
			if (formatted.Contains("{0}") || formatted.Contains("{1}"))
			{
				// Use the default message if provided
				return defaultMessage;
			}
			return formatted;
		}
#pragma warning disable CA1031 // Do not catch general exception types
		catch
		{
			// Log a warning when we have to fall back to a generic message
			LogUnableToDetermineCustomErrorMessage(logger, propertyValidator.FullName ?? string.Empty, propertyName);
			// If formatting fails, use the default message
			return defaultMessage;
		}
#pragma warning restore CA1031 // Do not catch general exception types
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
			return new Validation.RangeRule<int>(propertyName, minimum: min, maximum: max);
		}
		else if (actualType == typeof(long))
		{
			long min = Convert.ToInt64(range.Minimum);
			long max = Convert.ToInt64(range.Maximum);
			return new Validation.RangeRule<long>(propertyName, minimum: min, maximum: max);
		}
		else if (actualType == typeof(double))
		{
			double min = Convert.ToDouble(range.Minimum);
			double max = Convert.ToDouble(range.Maximum);
			return new Validation.RangeRule<double>(propertyName, minimum: min, maximum: max);
		}
		else if (actualType == typeof(float))
		{
			float min = Convert.ToSingle(range.Minimum);
			float max = Convert.ToSingle(range.Maximum);
			return new Validation.RangeRule<float>(propertyName, minimum: min, maximum: max);
		}
		else if (actualType == typeof(decimal))
		{
			decimal min = Convert.ToDecimal(range.Minimum);
			decimal max = Convert.ToDecimal(range.Maximum);
			return new Validation.RangeRule<decimal>(propertyName, minimum: min, maximum: max);
		}
		else if (actualType == typeof(DateTime))
		{
			// DateTime ranges cannot be represented in OpenAPI schemas, so create a custom rule
			return new Validation.CustomRule<object>(
				propertyName, $"Must be between {range.Minimum} and {range.Maximum}");
		}

		// For other types, create a custom rule with the error message
		return new Validation.CustomRule<object>(
			propertyName, $"Must be between {range.Minimum} and {range.Maximum}");
	}

	static Validation.CustomRule<object>? CreateCustomRuleFromValidationAttribute(string propertyName, Type propertyValidator, ValidationAttribute attribute, ILogger logger)
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

				if (errorMessage.Equals($"The field {propertyName} is invalid."))
				{
					LogVagueErrorMessageDataAnnotation(logger, propertyValidator.FullName ?? string.Empty, propertyName);
				}
			}
#pragma warning disable CA1031 // Do not catch general exception types
			catch
			{
				// If FormatErrorMessage fails, use a default message
				// Log a warning when we have to fall back to a generic message
				LogUnableToDetermineCustomErrorMessage(logger, propertyValidator.FullName ?? string.Empty, propertyName);
				errorMessage = $"{propertyName} validation failed";
			}
#pragma warning restore CA1031 // Do not catch general exception types
		}

		// If the error message is still empty or null, use a default
		if (string.IsNullOrEmpty(errorMessage))
		{
			LogUnableToDetermineCustomErrorMessage(logger, propertyValidator.FullName ?? string.Empty, propertyName);
			errorMessage = $"{propertyName} validation failed";
		}

		return new Validation.CustomRule<object>(propertyName, errorMessage);
	}

	[LoggerMessage(Level = LogLevel.Warning, Message = "Vague error message given to property {PropertyName} from the object {ValidatorType}. Consider manually setting the error message.")]
	static partial void LogVagueErrorMessageDataAnnotation(ILogger logger, string validatorType, string propertyName);
}
