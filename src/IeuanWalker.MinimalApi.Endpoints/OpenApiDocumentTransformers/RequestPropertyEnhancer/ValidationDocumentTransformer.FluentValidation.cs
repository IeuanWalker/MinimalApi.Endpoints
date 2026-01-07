using System.Reflection;
using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Validators;
using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer.Core;
using IeuanWalker.MinimalApi.Endpoints.Validation;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer;

partial class ValidationDocumentTransformer
{
	static void DiscoverFluentValidationRules(OpenApiDocumentTransformerContext context, Dictionary<Type, (List<ValidationRule> rules, bool appendRulesToPropertyDescription)> allValidationRules)
	{
		List<IValidator> validators = [];
		HashSet<Type> discoveredValidatorTypes = [];

		ILogger logger = context.ApplicationServices.GetService(typeof(ILogger<ValidationDocumentTransformer>)) as ILogger ?? NullLogger.Instance;

		foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			if (!SchemaTypeResolver.ShouldInspectAssembly(assembly))
			{
				continue;
			}

			foreach (Type type in SchemaTypeResolver.GetLoadableTypes(assembly))
			{
				if (!discoveredValidatorTypes.Add(type))
				{
					continue;
				}

				if (!TryGetValidatorInterface(type, out Type? validatorInterface))
				{
					continue;
				}

				object? validatorInstance = context.ApplicationServices.GetService(validatorInterface);
				if (validatorInstance is IValidator validator)
				{
					validators.Add(validator);
				}
			}
		}

		foreach (IValidator validator in validators)
		{
			Type? validatedType = validator
				.GetType()
				.GetInterfaces()
				.FirstOrDefault(IsValidatorInterface)
				?.GetGenericArguments()
				.FirstOrDefault();

			if (validatedType is null)
			{
				continue;
			}

			List<ValidationRule> rules = ExtractFluentValidationRules(validator, logger);

			if (rules.Count <= 0)
			{
				continue;
			}

			if (!allValidationRules.TryGetValue(validatedType, out (List<ValidationRule> rules, bool appendRulesToPropertyDescription) value))
			{
				value.rules = [];
				value.appendRulesToPropertyDescription = true;
				allValidationRules[validatedType] = value;
			}

			value.rules.AddRange(rules);
			allValidationRules[validatedType] = value;
		}
	}

	static bool TryGetValidatorInterface(Type type, out Type? validatorInterface)
	{
		validatorInterface = null;

		if (type.IsAbstract || type.IsInterface)
		{
			return false;
		}

		validatorInterface = type
			.GetInterfaces()
			.FirstOrDefault(IsValidatorInterface);

		return validatorInterface is not null;
	}

	static bool IsValidatorInterface(Type @interface) =>
		@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IValidator<>);

	static List<ValidationRule> ExtractFluentValidationRules(IValidator validator, ILogger logger)
	{
		List<ValidationRule> rules = [];

		IValidatorDescriptor descriptor = validator.CreateDescriptor();

		foreach (IGrouping<string, (IPropertyValidator Validator, IRuleComponent Options)> memberValidators in descriptor.GetMembersWithValidators())
		{
			string propertyName = memberValidators.Key;

			foreach ((IPropertyValidator Validator, IRuleComponent Options) validatorTuple in memberValidators)
			{
				IPropertyValidator propertyValidator = validatorTuple.Validator;
				IRuleComponent ruleComponent = validatorTuple.Options;

				ValidationRule? rule = ConvertToValidationRule(propertyName, propertyValidator, ruleComponent, logger);
				if (rule is not null)
				{
					rules.Add(rule);
				}
			}
		}

		return rules;
	}

	static ValidationRule? ConvertToValidationRule(string propertyName, IPropertyValidator propertyValidator, IRuleComponent ruleComponent, ILogger logger)
	{
		string validatorTypeName = propertyValidator.GetType().Name;
		if (validatorTypeName.Contains(nameof(ChildValidatorAdaptor<,>)) || validatorTypeName.Contains("SetValidator"))
		{
			return null;
		}

		ValidationRule? rule = propertyValidator switch
		{
			INotNullValidator or INotEmptyValidator => new RequiredRule(propertyName),
			ILengthValidator lengthValidator => CreateStringLengthRule(propertyName, lengthValidator),
			IRegularExpressionValidator regexValidator => CreatePatternRule(propertyName, regexValidator),
			IEmailValidator => new EmailRule(propertyName),
			IComparisonValidator comparisonValidator => CreateComparisonRule(propertyName, comparisonValidator),
			IBetweenValidator betweenValidator => CreateBetweenRule(propertyName, betweenValidator),
			_ => null
		};

		if (rule is not null)
		{
			return rule;
		}

		(Type? enumType, Type? propertyType) = ExtractEnumAndPropertyTypes(propertyValidator);
		if (enumType is not null && propertyType is not null)
		{
			string enumErrorMessage = GetValidatorErrorMessage(propertyValidator, ruleComponent, propertyName, logger);
			return new EnumRule(propertyName, enumType, propertyType, enumErrorMessage);
		}

		string customErrorMessage = GetValidatorErrorMessage(propertyValidator, ruleComponent, propertyName, logger);
		if (!string.IsNullOrEmpty(customErrorMessage))
		{
			rule = new CustomRule<object>(propertyName, customErrorMessage);
			rule.ErrorMessage = rule.ErrorMessage.Replace($"'{propertyName}' m", "M");
			return rule;
		}

		return null;
	}

	static string GetValidatorErrorMessage(IPropertyValidator propertyValidator, IRuleComponent ruleComponent, string propertyName, ILogger logger)
	{
		try
		{
			MethodInfo? getUnformattedMethod = ruleComponent.GetType().GetMethod(nameof(IRuleComponent.GetUnformattedErrorMessage), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (getUnformattedMethod is not null)
			{
				try
				{
					object? result = getUnformattedMethod.Invoke(ruleComponent, null);
					if (result is string message && !string.IsNullOrEmpty(message))
					{
						if (message.Equals("The specified condition was not met for '{PropertyName}'.") && logger.IsEnabled(LogLevel.Warning))
						{
							LogVagueErrorMessageFluentValidation(logger, propertyValidator.GetType().FullName ?? string.Empty, propertyName);
						}

						if (message.Equals("'{PropertyName}' is not a valid credit card number."))
						{
							message = "Must be a valid credit card number";
						}

						return ReplacePlaceholders(message, propertyName, propertyValidator);
					}
				}
#pragma warning disable CA1031
				catch { }
#pragma warning restore CA1031
			}

			PropertyInfo? errorMessageProp = ruleComponent.GetType().GetProperty("ErrorMessageSource");
			if (errorMessageProp is not null)
			{
				object? errorMessageSource = errorMessageProp.GetValue(ruleComponent);
				if (errorMessageSource is not null)
				{
					PropertyInfo? messageProp = errorMessageSource.GetType().GetProperty("Message", BindingFlags.Public | BindingFlags.Instance);
					if (messageProp is not null)
					{
						string? message = messageProp.GetValue(errorMessageSource) as string;
						if (!string.IsNullOrEmpty(message))
						{
							return ReplacePlaceholders(message, propertyName, propertyValidator);
						}
					}

					FieldInfo? messageField = errorMessageSource.GetType().GetField("message", BindingFlags.NonPublic | BindingFlags.Instance);
					if (messageField is not null)
					{
						string? message = messageField.GetValue(errorMessageSource) as string;
						if (!string.IsNullOrEmpty(message))
						{
							return ReplacePlaceholders(message, propertyName, propertyValidator);
						}
					}
				}
			}

			string vTypeName = propertyValidator.GetType().Name;
			if (vTypeName.EndsWith("Validator"))
			{
				vTypeName = vTypeName[..^9];
			}
			int backtickIndex = vTypeName.IndexOf('`');
			if (backtickIndex > 0)
			{
				vTypeName = vTypeName[..backtickIndex];
			}

			LogUnableToDetermineCustomErrorMessage(logger, propertyValidator.GetType().FullName ?? string.Empty, propertyName);

			return $"{propertyName} {vTypeName} validation";
		}
#pragma warning disable CA1031
		catch (Exception ex)
		{
			LogExceptionWhileObtainingValidatorErrorMessage(logger, propertyName, propertyValidator.GetType().FullName ?? string.Empty, ex);
			return $"{propertyName} custom validation";
		}
#pragma warning restore CA1031
	}

	static string ReplacePlaceholders(string message, string propertyName, IPropertyValidator propertyValidator)
	{
		message = message
			.Replace("{PropertyName}", propertyName)
			.Replace("{PropertyValue}", "{value}");

		try
		{
			if (propertyValidator is IComparisonValidator comparisonValidator)
			{
				MemberInfo? memberValue = comparisonValidator.MemberToCompare;
				if (memberValue is not null)
				{
					message = message.Replace("{ComparisonValue}", memberValue.Name);
				}
				else
				{
					object? value = comparisonValidator.ValueToCompare;
					if (value is not null)
					{
						message = message.Replace("{ComparisonValue}", value.ToString());
					}
				}
			}

			if (propertyValidator is IBetweenValidator betweenValidator)
			{
				if (betweenValidator.From is not null)
				{
					message = message.Replace("{From}", betweenValidator.From.ToString());
				}
				if (betweenValidator.To is not null)
				{
					message = message.Replace("{To}", betweenValidator.To.ToString());
				}
			}

			if (propertyValidator is ILengthValidator lengthValidator)
			{
				message = message
					.Replace("{MinLength}", lengthValidator.Min.ToString())
					.Replace("{min}", lengthValidator.Min.ToString())
					.Replace("{MaxLength}", lengthValidator.Max.ToString())
					.Replace("{max}", lengthValidator.Max.ToString());
			}

			if (propertyValidator is IRegularExpressionValidator regexValidator && regexValidator.Expression is not null)
			{
				message = message.Replace("{RegularExpression}", regexValidator.Expression);
			}
		}
#pragma warning disable CA1031
		catch { }
#pragma warning restore CA1031

		return message;
	}

	static StringLengthRule? CreateStringLengthRule(string propertyName, ILengthValidator lengthValidator)
	{
		int min = lengthValidator.Min;
		int max = lengthValidator.Max;

		bool hasMinLength = min > 0;
		bool hasMaxLength = max is > 0 and not int.MaxValue;

		if (!hasMinLength && !hasMaxLength)
		{
			return null;
		}

		return new StringLengthRule(propertyName, minLength: hasMinLength ? min : null, maxLength: hasMaxLength ? max : null);
	}

	static PatternRule? CreatePatternRule(string propertyName, IRegularExpressionValidator regexValidator)
	{
		string? pattern = regexValidator.Expression;
		return string.IsNullOrEmpty(pattern) ? null : new PatternRule(propertyName, pattern);
	}

	static ValidationRule? CreateComparisonRule(string propertyName, IComparisonValidator comparisonValidator)
	{
		object? value = comparisonValidator.ValueToCompare;
		MemberInfo? memberToCompare = comparisonValidator.MemberToCompare;
		Comparison comparison = comparisonValidator.Comparison;

		if (memberToCompare is not null || value is null)
		{
			return null;
		}

		string comparisonName = comparison.ToString();

		return value switch
		{
			int intValue => CreateTypedRangeRule(propertyName, intValue, comparisonName),
			long longValue => CreateTypedRangeRule(propertyName, longValue, comparisonName),
			decimal decimalValue => CreateTypedRangeRule(propertyName, decimalValue, comparisonName),
			double doubleValue => CreateTypedRangeRule(propertyName, doubleValue, comparisonName),
			float floatValue => CreateTypedRangeRule(propertyName, floatValue, comparisonName),
			_ => null
		};
	}

	static RangeRule<T>? CreateTypedRangeRule<T>(string propertyName, T value, string comparisonName) where T : struct, IComparable<T>
	{
		return comparisonName switch
		{
			"GreaterThan" => new RangeRule<T>(propertyName, minimum: value, exclusiveMinimum: true),
			"GreaterThanOrEqual" => new RangeRule<T>(propertyName, minimum: value, exclusiveMinimum: false),
			"LessThan" => new RangeRule<T>(propertyName, maximum: value, exclusiveMaximum: true),
			"LessThanOrEqual" => new RangeRule<T>(propertyName, maximum: value, exclusiveMaximum: false),
			_ => null
		};
	}

	static ValidationRule? CreateBetweenRule(string propertyName, IBetweenValidator betweenValidator)
	{
		object? from = betweenValidator.From;
		object? to = betweenValidator.To;

		if (from is null || to is null)
		{
			return null;
		}

		return from switch
		{
			int intFrom when to is int intTo => new RangeRule<int>(propertyName, minimum: intFrom, maximum: intTo),
			long longFrom when to is long longTo => new RangeRule<long>(propertyName, minimum: longFrom, maximum: longTo),
			decimal decimalFrom when to is decimal decimalTo => new RangeRule<decimal>(propertyName, minimum: decimalFrom, maximum: decimalTo),
			double doubleFrom when to is double doubleTo => new RangeRule<double>(propertyName, minimum: doubleFrom, maximum: doubleTo),
			float floatFrom when to is float floatTo => new RangeRule<float>(propertyName, minimum: floatFrom, maximum: floatTo),
			_ => null
		};
	}

	[LoggerMessage(Level = LogLevel.Warning, Message = "Unable to determine custom error message for validator '{ValidatorType}' on property '{PropertyName}'. Falling back to generic message.")]
	static partial void LogUnableToDetermineCustomErrorMessage(ILogger logger, string validatorType, string propertyName);

	[LoggerMessage(Level = LogLevel.Warning, Message = "Exception while obtaining validator error message for property '{PropertyName}' using validator '{ValidatorType}'. Using generic message.")]
	static partial void LogExceptionWhileObtainingValidatorErrorMessage(ILogger logger, string propertyName, string validatorType, Exception exception);

	[LoggerMessage(Level = LogLevel.Warning, Message = "Vague error message given to property {PropertyName} from the object {ValidatorType}. Consider using .WithMessage().")]
	static partial void LogVagueErrorMessageFluentValidation(ILogger logger, string validatorType, string propertyName);

	static (Type? enumType, Type? propertyType) ExtractEnumAndPropertyTypes(IPropertyValidator propertyValidator)
	{
		Type validatorType = propertyValidator.GetType();
		string validatorTypeName = validatorType.Name;

		// Handle StringEnumValidator (used by .IsEnumName())
		if (validatorTypeName.Contains("StringEnumValidator") && validatorType.IsGenericType)
		{
			Type[] genericArgs = validatorType.GetGenericArguments();
			// StringEnumValidator<T, TEnum> - TEnum is the enum type
			foreach (Type arg in genericArgs)
			{
				if (arg.IsEnum)
				{
					return (arg, typeof(string));
				}
			}

			// If not found in generic args, try the _enumType field (may be passed via constructor)
			Type? enumTypeFromField = GetEnumTypeFromValidatorFields(propertyValidator, validatorType);
			if (enumTypeFromField is not null)
			{
				return (enumTypeFromField, typeof(string));
			}
		}

		// Handle EnumValidator (used by .IsInEnum())
		if (validatorTypeName.Contains("EnumValidator"))
		{
			// First try to get enum type from generic arguments
			if (validatorType.IsGenericType)
			{
				Type[] genericArgs = validatorType.GetGenericArguments();

				// Find the enum type among the generic arguments
				Type? enumType = null;
				Type? propertyType = null;

				foreach (Type arg in genericArgs)
				{
					if (arg.IsEnum)
					{
						enumType = arg;
					}
					else if (arg.IsGenericType && arg.GetGenericTypeDefinition() == typeof(Nullable<>))
					{
						Type? underlyingType = Nullable.GetUnderlyingType(arg);
						if (underlyingType?.IsEnum is true)
						{
							enumType = underlyingType;
							propertyType = arg;
						}
					}
					else if (arg == typeof(string) || arg == typeof(int) || arg == typeof(long) || arg == typeof(int?) || arg == typeof(long?))
					{
						propertyType = arg;
					}
				}

				if (enumType is not null)
				{
					propertyType ??= enumType;
					return (enumType, propertyType);
				}

				// Enum type not in generic args, it was likely passed via .IsInEnum(typeof(Enum))
				// The property type is in the generic args (the second one after the model type)
				if (genericArgs.Length >= 2)
				{
					propertyType = genericArgs[1]; // Second generic arg is the property type
				}

				// Try to get enum type from fields
				Type? enumTypeFromField = GetEnumTypeFromValidatorFields(propertyValidator, validatorType);
				if (enumTypeFromField is not null)
				{
					propertyType ??= typeof(int);
					return (enumTypeFromField, propertyType);
				}
			}
		}

		return (null, null);
	}

	static Type? GetEnumTypeFromValidatorFields(IPropertyValidator propertyValidator, Type validatorType)
	{
		// Try common field names used by FluentValidation validators
		string[] fieldNames = ["_enumType", "_type", "enumType", "EnumType", "EnumType", "_EnumType", "enumTypeInfo", "_enumTypeInfo"];
		BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

		foreach (string fieldName in fieldNames)
		{
			FieldInfo? field = validatorType.GetField(fieldName, bindingFlags);
			if (field is not null)
			{
				object? fieldValue = field.GetValue(propertyValidator);
				
				// Check if the field value is directly a Type
				if (fieldValue is Type enumType && enumType.IsEnum)
				{
					return enumType;
				}
				
				// Check if the field value has a Type property (e.g., TypeInfo)
				if (fieldValue is not null)
				{
					Type fieldValueType = fieldValue.GetType();
					PropertyInfo? typeProperty = fieldValueType.GetProperty("Type", BindingFlags.Public | BindingFlags.Instance);
					if (typeProperty is not null)
					{
						Type? extractedType = typeProperty.GetValue(fieldValue) as Type;
						if (extractedType?.IsEnum is true)
						{
							return extractedType;
						}
					}
					
					// Check if field value has AsType() method (for TypeInfo)
					MethodInfo? asTypeMethod = fieldValueType.GetMethod("AsType", BindingFlags.Public | BindingFlags.Instance);
					if (asTypeMethod is not null)
					{
						Type? extractedType = asTypeMethod.Invoke(fieldValue, null) as Type;
						if (extractedType?.IsEnum is true)
						{
							return extractedType;
						}
					}
				}
			}
		}

		// Also try properties
		string[] propNames = ["EnumType", "Type"];
		foreach (string propName in propNames)
		{
			PropertyInfo? prop = validatorType.GetProperty(propName, bindingFlags);
			if (prop is not null && prop.CanRead)
			{
				Type? enumType = prop.GetValue(propertyValidator) as Type;
				if (enumType?.IsEnum is true)
				{
					return enumType;
				}
			}
		}

		// Walk up the type hierarchy to find the field in base classes
		Type? currentType = validatorType.BaseType;
		while (currentType is not null && currentType != typeof(object))
		{
			foreach (string fieldName in fieldNames)
			{
				FieldInfo? field = currentType.GetField(fieldName, bindingFlags | BindingFlags.DeclaredOnly);
				if (field is not null)
				{
					Type? enumType = field.GetValue(propertyValidator) as Type;
					if (enumType?.IsEnum is true)
					{
						return enumType;
					}
				}
			}
			currentType = currentType.BaseType;
		}

		return null;
	}
}
