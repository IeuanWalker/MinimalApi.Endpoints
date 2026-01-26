using System.ComponentModel.DataAnnotations;
using System.Reflection;
using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.PropertyEnhancer.Core;
using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.PropertyEnhancer.Validation;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer;

partial class ValidationDocumentTransformer
{
	static readonly Lock dataAnnotationCacheLock = new();
	static Dictionary<Type, List<ValidationRule>>? dataAnnotationRuleCache;

	static void DiscoverDataAnnotationValidationRules(OpenApiDocumentTransformerContext context, Dictionary<Type, (List<ValidationRule> rules, bool appendRulesToPropertyDescription)> allValidationRules)
	{
		ILogger logger = context.ApplicationServices.GetService(typeof(ILogger<ValidationDocumentTransformer>)) as ILogger ?? NullLogger.Instance;

		Dictionary<Type, List<ValidationRule>> cachedRules = GetCachedDataAnnotationRules(logger);

		foreach (KeyValuePair<Type, List<ValidationRule>> kvp in cachedRules)
		{
			if (kvp.Value.Count == 0)
			{
				continue;
			}

			if (!allValidationRules.TryGetValue(kvp.Key, out (List<ValidationRule> rules, bool appendRulesToPropertyDescription) value))
			{
				value.rules = [];
				value.appendRulesToPropertyDescription = true;
			}

			value.rules.AddRange(kvp.Value);
			allValidationRules[kvp.Key] = value;
		}
	}

	static Dictionary<Type, List<ValidationRule>> GetCachedDataAnnotationRules(ILogger logger)
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

	static Dictionary<Type, List<ValidationRule>> BuildDataAnnotationRuleCache(ILogger logger)
	{
		Dictionary<Type, (List<ValidationRule> rules, bool appendRulesToPropertyDescription)> discoveredValidationRules = [];
		HashSet<Type> processedTypes = [];

		foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			if (!SchemaTypeResolver.ShouldInspectAssembly(assembly))
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
#pragma warning disable ASP0029 // Type is for evaluation purposes only
				bool hasValidatableTypeAttribute = type.GetCustomAttribute<Microsoft.Extensions.Validation.ValidatableTypeAttribute>() is not null;
#pragma warning restore ASP0029

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

	static void ProcessTypeRecursively(Type type, Dictionary<Type, (List<ValidationRule> rules, bool appendRulesToPropertyDescription)> allValidationRules, HashSet<Type> processedTypes, ILogger logger)
	{
		if (processedTypes.Contains(type))
		{
			return;
		}

		processedTypes.Add(type);

		List<ValidationRule> rules = ExtractDataAnnotationRules(type, logger);

		if (rules.Count > 0)
		{
			if (!allValidationRules.TryGetValue(type, out (List<ValidationRule> rules, bool appendRulesToPropertyDescription) value))
			{
				value.rules = [];
				value.appendRulesToPropertyDescription = true;
				allValidationRules[type] = value;
			}

			value.rules.AddRange(rules);
			allValidationRules[type] = value;
		}

		PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

		foreach (PropertyInfo property in properties)
		{
			Type propertyType = property.PropertyType;
			Type actualType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

			if (actualType != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(actualType))
			{
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
				continue;
			}

			PropertyInfo[] nestedProperties = actualType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
			if (nestedProperties.Any(np => np.GetCustomAttributes<ValidationAttribute>(inherit: true).Any()))
			{
				ProcessTypeRecursively(actualType, allValidationRules, processedTypes, logger);
			}
		}
	}

	static List<ValidationRule> ExtractDataAnnotationRules(Type type, ILogger logger)
	{
		List<ValidationRule> rules = [];

		PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

		foreach (PropertyInfo property in properties)
		{
			ValidationAttribute[] validationAttributes = [.. property.GetCustomAttributes<ValidationAttribute>(inherit: true)];

			foreach (ValidationAttribute attribute in validationAttributes)
			{
				ValidationRule? rule = ConvertDataAnnotationToValidationRule(property, type, attribute, logger);
				if (rule is not null)
				{
					rules.Add(rule);
				}
			}
		}

		return rules;
	}

	static ValidationRule? ConvertDataAnnotationToValidationRule(PropertyInfo property, Type propertyParent, ValidationAttribute attribute, ILogger logger)
	{
		string propertyName = property.Name;
		bool isCollection = IsCollectionType(property.PropertyType);

		return attribute switch
		{
			RequiredAttribute => new RequiredRule(propertyName),

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

			RegularExpressionAttribute regex => new PatternRule(propertyName, regex.Pattern),

			EmailAddressAttribute => new EmailRule(propertyName),

			UrlAttribute => new UrlRule(propertyName),

			PhoneAttribute => new CustomRule<object>(propertyName, "Must be a valid phone number"),

			CreditCardAttribute => new CustomRule<object>(propertyName, "Must be a valid credit card number"),

			CompareAttribute compare => new CustomRule<object>(propertyName, $"Must match {compare.OtherProperty}"),

			FileExtensionsAttribute fileExtensions => new CustomRule<object>(propertyName, $"Must be a file with extension: {fileExtensions.Extensions}"),

			CustomValidationAttribute customValidation => new CustomRule<object>(
				propertyName,
				GetFormattedErrorMessage(customValidation, logger, propertyParent, propertyName, "Custom validation failed") ?? "Custom validation failed"),

			_ => CreateCustomRuleFromValidationAttribute(propertyName, propertyParent, attribute, logger)
		};
	}

	static bool IsCollectionType(Type type)
	{
		Type actualType = Nullable.GetUnderlyingType(type) ?? type;

		if (actualType == typeof(string))
		{
			return false;
		}

		return actualType.IsArray ||
			   (actualType != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(actualType));
	}

	static StringLengthRule CreateStringLengthRule(string propertyName, int? minLength, int? maxLength, bool isCollection)
	{
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

		return new StringLengthRule(propertyName, minLength, maxLength, errorMessage);
	}

	static string? GetFormattedErrorMessage(ValidationAttribute attribute, ILogger logger, Type propertyValidator, string propertyName, string? defaultMessage = null)
	{
		if (!string.IsNullOrEmpty(attribute.ErrorMessage))
		{
			return attribute.ErrorMessage;
		}

		try
		{
			string formatted = attribute.FormatErrorMessage(propertyName);
			if (formatted.Contains("{0}") || formatted.Contains("{1}"))
			{
				return defaultMessage;
			}
			return formatted;
		}
#pragma warning disable CA1031
		catch
		{
			LogUnableToDetermineCustomErrorMessage(logger, propertyValidator.FullName ?? string.Empty, propertyName);
			return defaultMessage;
		}
#pragma warning restore CA1031
	}

	static ValidationRule? CreateRangeRuleFromAttribute(string propertyName, Type propertyType, RangeAttribute range)
	{
		Type actualType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

		if (actualType == typeof(int))
		{
			int min = Convert.ToInt32(range.Minimum);
			int max = Convert.ToInt32(range.Maximum);
			return new RangeRule<int>(propertyName, minimum: min, maximum: max);
		}
		else if (actualType == typeof(long))
		{
			long min = Convert.ToInt64(range.Minimum);
			long max = Convert.ToInt64(range.Maximum);
			return new RangeRule<long>(propertyName, minimum: min, maximum: max);
		}
		else if (actualType == typeof(double))
		{
			double min = Convert.ToDouble(range.Minimum);
			double max = Convert.ToDouble(range.Maximum);
			return new RangeRule<double>(propertyName, minimum: min, maximum: max);
		}
		else if (actualType == typeof(float))
		{
			float min = Convert.ToSingle(range.Minimum);
			float max = Convert.ToSingle(range.Maximum);
			return new RangeRule<float>(propertyName, minimum: min, maximum: max);
		}
		else if (actualType == typeof(decimal))
		{
			decimal min = Convert.ToDecimal(range.Minimum);
			decimal max = Convert.ToDecimal(range.Maximum);
			return new RangeRule<decimal>(propertyName, minimum: min, maximum: max);
		}
		else if (actualType == typeof(DateTime))
		{
			return new CustomRule<object>(propertyName, $"Must be between {range.Minimum} and {range.Maximum}");
		}

		return new CustomRule<object>(propertyName, $"Must be between {range.Minimum} and {range.Maximum}");
	}

	static CustomRule<object>? CreateCustomRuleFromValidationAttribute(string propertyName, Type propertyValidator, ValidationAttribute attribute, ILogger logger)
	{
		string? errorMessage = attribute.ErrorMessage;

		if (string.IsNullOrEmpty(errorMessage))
		{
			try
			{
				errorMessage = attribute.FormatErrorMessage(propertyName);

				if (errorMessage.Equals($"The field {propertyName} is invalid."))
				{
					LogVagueErrorMessageDataAnnotation(logger, propertyValidator.FullName ?? string.Empty, propertyName);
				}
			}
#pragma warning disable CA1031
			catch
			{
				LogUnableToDetermineCustomErrorMessage(logger, propertyValidator.FullName ?? string.Empty, propertyName);
				errorMessage = $"{propertyName} validation failed";
			}
#pragma warning restore CA1031
		}

		if (string.IsNullOrEmpty(errorMessage))
		{
			LogUnableToDetermineCustomErrorMessage(logger, propertyValidator.FullName ?? string.Empty, propertyName);
			errorMessage = $"{propertyName} validation failed";
		}

		return new CustomRule<object>(propertyName, errorMessage);
	}

	[LoggerMessage(Level = LogLevel.Warning, Message = "Vague error message given to property {PropertyName} from the object {ValidatorType}. Consider manually setting the error message.")]
	static partial void LogVagueErrorMessageDataAnnotation(ILogger logger, string validatorType, string propertyName);
}
