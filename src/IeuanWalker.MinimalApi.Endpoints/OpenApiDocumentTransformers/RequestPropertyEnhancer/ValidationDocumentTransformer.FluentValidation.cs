using System.Reflection;
using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Validators;
using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer.Core;
using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer.Validation;
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

				object? validatorInstance = context.ApplicationServices.GetService(validatorInterface!);
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
		// TODO: Use type rather than string
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
#pragma warning disable CA1873 // Avoid potentially expensive logging
							LogVagueErrorMessageFluentValidation(logger, propertyValidator.GetType().FullName ?? string.Empty, propertyName);
#pragma warning restore CA1873 // Avoid potentially expensive logging
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

			string validatorTypeName = propertyValidator.GetType().Name;
			if (validatorTypeName.EndsWith("Validator"))
			{
				validatorTypeName = validatorTypeName[..^9];
			}
			int backtickIndex = validatorTypeName.IndexOf('`');
			if (backtickIndex > 0)
			{
				validatorTypeName = validatorTypeName[..backtickIndex];
			}

#pragma warning disable CA1873 // Avoid potentially expensive logging
			LogUnableToDetermineCustomErrorMessage(logger, propertyValidator.GetType().FullName ?? string.Empty, propertyName);
#pragma warning restore CA1873 // Avoid potentially expensive logging

			return $"{propertyName} {validatorTypeName} validation";
		}
#pragma warning disable CA1031 // Do not catch general exception types
		catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
		{
#pragma warning disable CA1873 // Avoid potentially expensive logging
			LogExceptionWhileObtainingValidatorErrorMessage(logger, propertyName, propertyValidator.GetType().FullName ?? string.Empty, ex);
#pragma warning restore CA1873 // Avoid potentially expensive logging
			return $"{propertyName} custom validation";
		}
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

			Type validatorType = propertyValidator.GetType();
			if (validatorType.Name.Contains("PrecisionScale") || validatorType.Name.Contains("ScalePrecision"))
			{
				// Try multiple property access strategies (public/non-public)
				// Note: FluentValidation uses "Precision" and "Scale" as property names
				object? precision = TryGetPropertyOrFieldValue(propertyValidator, "Precision", "ExpectedPrecision", "precision");
				object? scale = TryGetPropertyOrFieldValue(propertyValidator, "Scale", "ExpectedScale", "scale");
				object? ignoreTrailing = TryGetPropertyOrFieldValue(propertyValidator, "IgnoreTrailingZeros", "ignoreTrailingZeros");

				if (precision is not null)
				{
					message = message.Replace("{ExpectedPrecision}", precision.ToString());
				}
				if (scale is not null)
				{
					message = message.Replace("{ExpectedScale}", scale.ToString());
				}
				if (ignoreTrailing is not null)
				{
					message = message.Replace("{IgnoreTrailingZeros}", ignoreTrailing.ToString());
				}

				// These are dynamic values that can't be determined at design time
				message = message
					.Replace("{Digits}", "X")
					.Replace("{ActualScale}", "Y");
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

		// For IsEnumName validators (StringEnumValidator) - property type is string
		FieldInfo? enumNamesField = validatorType.GetField("_enumNames", BindingFlags.NonPublic | BindingFlags.Instance);
		if (enumNamesField is not null)
		{
			string[]? enumNames = enumNamesField.GetValue(propertyValidator) as string[];
			if (enumNames is not null && enumNames.Length > 0)
			{
				// Find the enum type by matching the names
				Type? enumType = FindEnumTypeByNames(enumNames);
				if (enumType is not null)
				{
					return (enumType, typeof(string));  // Property type is string for IsEnumName
				}
			}
		}

		// For IsInEnum validators on int properties (PredicateValidator created by Must())
		if (validatorType.Name.StartsWith("PredicateValidator"))
		{
			// First, try to extract from error message
			PropertyInfo? errorMessageSource = validatorType.GetProperty("ErrorMessageSource", BindingFlags.Public | BindingFlags.Instance);
			if (errorMessageSource is not null)
			{
				object? messageSource = errorMessageSource.GetValue(propertyValidator);
				if (messageSource is not null)
				{
					PropertyInfo? messageProperty = messageSource.GetType().GetProperty("Message", BindingFlags.Public | BindingFlags.Instance);
					if (messageProperty is not null)
					{
						string? message = messageProperty.GetValue(messageSource) as string;
						if (message is not null && (message.Contains("must be a valid value of enum") || message.Contains("must be empty or a valid value of enum")))
						{
							Type? enumType = ExtractEnumTypeFromMessage(message);
							if (enumType is not null)
							{
								// Property type is int for IsInEnum on int properties
								return (enumType, typeof(int));
							}
						}
					}
				}
			}

			// Fallback: Try to extract from predicate closure
			FieldInfo? predicateField = validatorType.GetField("_predicate", BindingFlags.NonPublic | BindingFlags.Instance);
			if (predicateField is not null)
			{
				object? predicate = predicateField.GetValue(propertyValidator);
				if (predicate is Delegate predicateDelegate)
				{
					// Recursively search through nested closures
					Type? enumType = FindEnumTypeInClosure(predicateDelegate.Target);
					if (enumType is not null)
					{
						// Property type is int for IsInEnum on int properties
						return (enumType, typeof(int));
					}
				}
			}
		}

		return (null, null);
	}

	static Type? FindEnumTypeByNames(string[] names)
	{
		if (names is null || names.Length == 0)
		{
			return null;
		}

		if (!enumTypeByNamesCacheInitialized)
		{
			lock (enumTypeByNamesCacheLock)
			{
				if (!enumTypeByNamesCacheInitialized)
				{
					enumTypesByNames = new(StringComparer.Ordinal);

					// Build cache: key is the sorted list of enum member names
					foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
					{
						try
						{
							foreach (Type type in assembly.GetTypes())
							{
								if (!type.IsEnum)
								{
									continue;
								}

								string[] typeEnumNames = Enum.GetNames(type);
								if (typeEnumNames.Length == 0)
								{
									continue;
								}

								string enumKey = CreateKey(typeEnumNames);

								// Keep first occurrence to avoid exceptions on duplicates.
								if (!enumTypesByNames.ContainsKey(enumKey))
								{
									enumTypesByNames[enumKey] = type;
								}
							}
						}
#pragma warning disable CA1031 // Do not catch general exception types
						catch
						{
							// Skip assemblies that can't be inspected
							continue;
						}
#pragma warning restore CA1031 // Do not catch general exception types
					}

					enumTypeByNamesCacheInitialized = true;
				}
			}
		}

		if (enumTypesByNames is null)
		{
			return null;
		}

		string key = CreateKey(names);
		return enumTypesByNames.TryGetValue(key, out Type? matchingType) ? matchingType : null;

		static string CreateKey(string[] values)
		{
			// Work on a copy so the original order is not modified.
			string[] copy = (string[])values.Clone();
			Array.Sort(copy, StringComparer.Ordinal);
			return string.Join("|", copy);
		}
	}

	static readonly Lock enumTypeCacheLock = new();
	static Dictionary<string, Type>? enumTypesByName;
	static volatile bool enumTypeCacheInitialized;

	static readonly Lock enumTypeByNamesCacheLock = new();
	static Dictionary<string, Type>? enumTypesByNames;
	static volatile bool enumTypeByNamesCacheInitialized;
	static Type? FindEnumTypeBySimpleName(string enumTypeName)
	{
		if (string.IsNullOrWhiteSpace(enumTypeName))
		{
			return null;
		}

		if (!enumTypeCacheInitialized)
		{
			lock (enumTypeCacheLock)
			{
				if (!enumTypeCacheInitialized)
				{
					enumTypesByName = new(StringComparer.OrdinalIgnoreCase);

					foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
					{
						try
						{
							foreach (Type type in assembly.GetTypes())
							{
								if (!type.IsEnum)
								{
									continue;
								}

								// Use simple name as key; keep first occurrence to avoid exceptions on duplicates.
								if (!enumTypesByName.ContainsKey(type.Name))
								{
									enumTypesByName[type.Name] = type;
								}
							}
						}
#pragma warning disable CA1031 // Do not catch general exception types
						catch
						{
							// Skip assemblies that can't be inspected
							continue;
						}
#pragma warning restore CA1031 // Do not catch general exception types
					}

					enumTypeCacheInitialized = true;
				}
			}
		}

		if (enumTypesByName is null)
		{
			return null;
		}

		return enumTypesByName.TryGetValue(enumTypeName, out Type? enumType) ? enumType : null;
	}

	static Type? ExtractEnumTypeFromMessage(string message)
	{
		// Extract enum type name from error message like "'{PropertyName}' must be a valid value of enum TodoPriority."
		// or "'{PropertyName}' must be empty or a valid value of enum TodoPriority."
		int enumIndex = message.IndexOf("enum ");
		if (enumIndex < 0)
		{
			return null;
		}

		string afterEnum = message[(enumIndex + 5)..]; // Skip "enum "
		int dotIndex = afterEnum.IndexOf('.');
		string enumTypeName = dotIndex > 0 ? afterEnum[..dotIndex].Trim() : afterEnum.Trim();

		// Use cached lookup instead of scanning all assemblies on every call.
		return FindEnumTypeBySimpleName(enumTypeName);
	}
	static Type? FindEnumTypeInClosure(object? target, int maxDepth = 5)
	{
		if (target is null || maxDepth <= 0)
		{
			return null;
		}

		FieldInfo[] fields = target.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

		// Check if any field is directly an enum type
		Type? directEnumType = fields
			.Select(field => field.GetValue(target))
			.OfType<Type>()
			.FirstOrDefault(type => type.IsEnum);

		if (directEnumType is not null)
		{
			return directEnumType;
		}

		// Recursively check delegate closures
		return fields
			.Select(field => field.GetValue(target))
			.OfType<Delegate>()
			.Where(del => del.Target is not null)
			.Select(del => FindEnumTypeInClosure(del.Target, maxDepth - 1))
			.FirstOrDefault(found => found is not null);
	}

	static object? TryGetPropertyOrFieldValue(object obj, params string[] names)
	{
		Type type = obj.GetType();

		// Try properties first (public and non-public)
		foreach (string name in names)
		{
			PropertyInfo? prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

			if (prop is null)
			{
				continue;
			}

			try
			{
				return prop.GetValue(obj);
			}
#pragma warning disable CA1031 // Do not catch general exception types - we want to catch all reflection exceptions
			catch
			{
				// Continue to next property name
			}
#pragma warning restore CA1031
		}

		// Try fields (public and non-public)
		foreach (string name in names)
		{
			FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

			if (field is null)
			{
				continue;
			}

			try
			{
				return field.GetValue(obj);
			}
#pragma warning disable CA1031 // Do not catch general exception types - we want to catch all reflection exceptions
			catch
			{
				// Continue to next field name
			}
#pragma warning restore CA1031
		}

		return null;
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
