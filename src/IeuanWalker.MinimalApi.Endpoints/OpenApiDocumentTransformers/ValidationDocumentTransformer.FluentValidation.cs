using System.Reflection;
using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Validators;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers;

partial class ValidationDocumentTransformer
{
	static void DiscoverFluentValidationRules(OpenApiDocumentTransformerContext context, Dictionary<Type, (List<Validation.ValidationRule> rules, bool appendRulesToPropertyDescription)> allValidationRules)
	{
		// FluentValidation validators are registered as IValidator<T>, not IValidator
		// We need to scan assemblies to find all types that implement IValidator<T>
		List<IValidator> validators = [];

		// Get logger from DI if available
		ILogger logger = context.ApplicationServices.GetService(typeof(ILogger<ValidationDocumentTransformer>)) as ILogger ?? NullLogger.Instance;

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
					Type? validatorInterface = type
						.GetInterfaces()
						.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>));

					if (validatorInterface is not null && !type.IsAbstract && !type.IsInterface)
					{
						// Try to get this validator from DI
						object? validatorInstance = context.ApplicationServices.GetService(validatorInterface);
						if (validatorInstance is IValidator validator)
						{
							validators.Add(validator);
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

		foreach (IValidator validator in validators)
		{
			// Find the validated type (T in IValidator<T>)
			Type? validatedType = validator
				.GetType()
				.GetInterfaces()
				.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>))
				?.GetGenericArguments()
				.FirstOrDefault();

			if (validatedType is null)
			{
				continue;
			}

			// Extract validation rules from the validator
			List<Validation.ValidationRule> rules = ExtractFluentValidationRules(validator, logger);

			if (rules.Count <= 0)
			{
				continue;
			}

			if (!allValidationRules.TryGetValue(validatedType, out (List<Validation.ValidationRule> rules, bool appendRulesToPropertyDescription) value))
			{
				value.rules = [];
				value.appendRulesToPropertyDescription = true;
				allValidationRules[validatedType] = value;
			}

			value.rules.AddRange(rules);
		}
	}

	static List<Validation.ValidationRule> ExtractFluentValidationRules(IValidator validator, ILogger logger)
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
				Validation.ValidationRule? rule = ConvertToValidationRule(propertyName, propertyValidator, ruleComponent, logger);
				if (rule is not null)
				{
					rules.Add(rule);
				}
			}
		}

		return rules;
	}

	static Validation.ValidationRule? ConvertToValidationRule(string propertyName, IPropertyValidator propertyValidator, IRuleComponent ruleComponent, ILogger logger)
	{
		// Skip ChildValidatorAdaptor (SetValidator) - nested object validators
		// These cannot be represented in OpenAPI schema constraints
		// The nested object's properties will have their own validators that will be processed separately
		// TODO: Use type rather than string
		string validatorTypeName = propertyValidator.GetType().Name;
		if (validatorTypeName.Contains(nameof(ChildValidatorAdaptor<,>)) || validatorTypeName.Contains("SetValidator"))
		{
			return null;
		}

		// Map FluentValidation validators to our internal ValidationRule types
		Validation.ValidationRule? rule = propertyValidator switch
		{
			INotNullValidator or INotEmptyValidator => new Validation.RequiredRule(propertyName),
			ILengthValidator lengthValidator => CreateStringLengthRule(propertyName, lengthValidator),
			IRegularExpressionValidator regexValidator => CreatePatternRule(propertyName, regexValidator),
			IEmailValidator => new Validation.EmailRule(propertyName),
			IComparisonValidator comparisonValidator => CreateComparisonRule(propertyName, comparisonValidator),
			IBetweenValidator betweenValidator => CreateBetweenRule(propertyName, betweenValidator),
			_ => null
		};

		if (rule is not null)
		{
			return rule;
		}

		// Check if this is an enum validator (IsEnumName for string, IsInEnum for int/enum)
		(Type? enumType, Type? propertyType) = ExtractEnumAndPropertyTypes(propertyValidator);
		if (enumType is not null && propertyType is not null)
		{
			string enumErrorMessage = GetValidatorErrorMessage(propertyValidator, ruleComponent, propertyName, logger);
			return new Validation.EnumRule(propertyName, enumType, propertyType, enumErrorMessage);
		}

		// If we couldn't map to a specific rule type, create a CustomRule with the error message
		string customErrorMessage = GetValidatorErrorMessage(propertyValidator, ruleComponent, propertyName, logger);
		if (!string.IsNullOrEmpty(customErrorMessage))
		{
			// Create a CustomRule<object> to hold the unsupported validator's error message
			rule = new Validation.CustomRule<object>(propertyName, customErrorMessage);

			rule.ErrorMessage = rule.ErrorMessage.Replace($"'{propertyName}' m", "M");

			return rule;
		}

		return null;
	}

	static string GetValidatorErrorMessage(IPropertyValidator propertyValidator, IRuleComponent ruleComponent, string propertyName, ILogger logger)
	{
		try
		{
			// Strategy 1: Try GetUnformattedErrorMessage() method on RuleComponent
			// This is the most reliable way to get the custom message set via .WithMessage()
			MethodInfo? getUnformattedMethod = ruleComponent.GetType().GetMethod(nameof(IRuleComponent.GetUnformattedErrorMessage), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (getUnformattedMethod is not null)
			{
				try
				{
					object? result = getUnformattedMethod.Invoke(ruleComponent, null);
					if (result is string message && !string.IsNullOrEmpty(message))
					{
						// Replace placeholders with actual values from the validator
						if (message.Equals("The specified condition was not met for '{PropertyName}'."))
						{
							if (logger.IsEnabled(LogLevel.Warning))
							{
#pragma warning disable CA1873 // Avoid potentially expensive logging
								LogVagueErrorMessageFluentValidation(logger, propertyValidator.GetType().FullName ?? string.Empty, propertyName);
#pragma warning restore CA1873 // Avoid potentially expensive logging
							}
						}

						if (message.Equals("'{PropertyName}' is not a valid credit card number."))
						{
							message = "Must be a valid credit card number";
						}

						return ReplacePlaceholders(message, propertyName, propertyValidator);
					}
				}
#pragma warning disable CA1031 // Do not catch general exception types
				catch
				{
					// Fall through to next strategy
				}
#pragma warning restore CA1031 // Do not catch general exception types
			}

			// Strategy 2: Try to get the error message template from the rule component's ErrorMessageSource
			PropertyInfo? errorMessageProp = ruleComponent.GetType().GetProperty("ErrorMessageSource");
			if (errorMessageProp is not null)
			{
				object? errorMessageSource = errorMessageProp.GetValue(ruleComponent);
				if (errorMessageSource is not null)
				{
					// Try to get the Message property directly (StaticStringSource)
					PropertyInfo? messageProp = errorMessageSource.GetType().GetProperty("Message", BindingFlags.Public | BindingFlags.Instance);
					if (messageProp is not null)
					{
						string? message = messageProp.GetValue(errorMessageSource) as string;
						if (!string.IsNullOrEmpty(message))
						{
							// Replace placeholders with actual values
							return ReplacePlaceholders(message, propertyName, propertyValidator);
						}
					}

					// Try private message field
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

			// Fallback: try to construct a basic message from the validator type
			string validatorTypeName = propertyValidator.GetType().Name;
			// Remove "Validator" suffix if present
			if (validatorTypeName.EndsWith("Validator"))
			{
				validatorTypeName = validatorTypeName[..^9];
			}
			// Remove generic type parameters like `2
			int backtickIndex = validatorTypeName.IndexOf('`');
			if (backtickIndex > 0)
			{
				validatorTypeName = validatorTypeName[..backtickIndex];
			}

			// Log a warning when we have to fall back to a generic message
#pragma warning disable CA1873 // Avoid potentially expensive logging
			LogUnableToDetermineCustomErrorMessage(logger, propertyValidator.GetType().FullName ?? string.Empty, propertyName);
#pragma warning restore CA1873 // Avoid potentially expensive logging

			return $"{propertyName} {validatorTypeName} validation";
		}
#pragma warning disable CA1031 // Do not catch general exception types
		catch (Exception ex)
		{
			// Log the exception and fall back to a generic message
#pragma warning disable CA1873 // Avoid potentially expensive logging
			LogExceptionWhileObtainingValidatorErrorMessage(logger, propertyName, propertyValidator.GetType().FullName ?? string.Empty, ex);
#pragma warning restore CA1873 // Avoid potentially expensive logging

			// If all else fails, return a generic message
			return $"{propertyName} custom validation";
		}
#pragma warning restore CA1031 // Do not catch general exception types
	}

	static string ReplacePlaceholders(string message, string propertyName, IPropertyValidator propertyValidator)
	{
		// Replace common placeholders
		message = message
			.Replace("{PropertyName}", propertyName)
			.Replace("{PropertyValue}", "{value}");

		// Extract and replace validator-specific placeholders
		try
		{
			// For ComparisonValidator (Equal, NotEqual, GreaterThan, LessThan, etc.)
			if (propertyValidator is IComparisonValidator comparisonValidator)
			{
				// Check for MemberToCompare first (property comparisons like x => x.MaxValue)
				PropertyInfo? memberProp = comparisonValidator.GetType().GetProperty(nameof(IComparisonValidator.MemberToCompare));
				object? memberValue = memberProp?.GetValue(comparisonValidator);

				if (memberValue is not null)
				{
					// MemberToCompare is a MemberInfo (typically PropertyInfo or FieldInfo)
					// Extract just the member name from the MemberInfo object
					string memberName = memberValue switch
					{
						PropertyInfo propInfo => propInfo.Name,
						FieldInfo fieldInfo => fieldInfo.Name,
						MemberInfo memberInfo => memberInfo.Name,
						_ => memberValue.ToString() ?? string.Empty
					};

					// For property comparisons, replace {ComparisonValue} with the property name
					message = message.Replace("{ComparisonValue}", memberName);
				}
				else
				{
					// For constant value comparisons, use ValueToCompare
					PropertyInfo? valueProp = comparisonValidator.GetType().GetProperty(nameof(IComparisonValidator.ValueToCompare));
					object? value = valueProp?.GetValue(comparisonValidator);
					if (value is not null)
					{
						message = message.Replace("{ComparisonValue}", value.ToString());
					}
				}
			}

			// For BetweenValidator
			if (propertyValidator is IBetweenValidator betweenValidator)
			{
				PropertyInfo? fromProp = betweenValidator.GetType().GetProperty(nameof(IBetweenValidator.From));
				PropertyInfo? toProp = betweenValidator.GetType().GetProperty(nameof(IBetweenValidator.To));

				object? from = fromProp?.GetValue(betweenValidator);
				object? to = toProp?.GetValue(betweenValidator);

				if (from is not null)
				{
					message = message.Replace("{From}", from.ToString());
				}
				if (to is not null)
				{
					message = message.Replace("{To}", to.ToString());
				}
			}

			// For LengthValidator
			if (propertyValidator is ILengthValidator lengthValidator)
			{
				PropertyInfo? minProp = lengthValidator.GetType().GetProperty(nameof(ILengthValidator.Min));
				PropertyInfo? maxProp = lengthValidator.GetType().GetProperty(nameof(ILengthValidator.Max));

				object? min = minProp?.GetValue(lengthValidator);
				object? max = maxProp?.GetValue(lengthValidator);

				if (min is not null)
				{
					message = message
						.Replace("{MinLength}", min.ToString())
						.Replace("{min}", min.ToString());
				}
				if (max is not null)
				{
					message = message
						.Replace("{MaxLength}", max.ToString())
						.Replace("{max}", max.ToString());
				}
			}

			// For PrecisionScale/ScalePrecision Validator
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

			// For regex/pattern validators
			if (propertyValidator is IRegularExpressionValidator regexValidator)
			{
				PropertyInfo? expressionProp = regexValidator.GetType().GetProperty(nameof(IRegularExpressionValidator.Expression));
				object? expression = expressionProp?.GetValue(regexValidator);
				if (expression is not null)
				{
					message = message.Replace("{RegularExpression}", expression.ToString());
				}
			}

			// For enum validators - clean up quotes around property names
			if (validatorType.Name.Contains("EnumValidator") || validatorType.Name.Contains("IsEnum"))
			{
				// Remove single quotes around property name at the beginning of the message
				if (message.StartsWith('\'') && message.Contains('\'', StringComparison.Ordinal))
				{
					int secondQuoteIndex = message.IndexOf('\'', 1);
					if (secondQuoteIndex > 0)
					{
						message = string.Concat(message.AsSpan(1, secondQuoteIndex - 1), message.AsSpan(secondQuoteIndex + 1));
					}
				}
			}
		}
#pragma warning disable CA1031 // Do not catch general exception types - we want to fallback gracefully for any reflection errors
		catch
		{
			// If any reflection fails, just return the message as-is with basic replacements
		}
#pragma warning restore CA1031

		return message;
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

	static Validation.StringLengthRule? CreateStringLengthRule(string propertyName, ILengthValidator lengthValidator)
	{
		// Get the Min and Max properties using reflection
		PropertyInfo? minProp = lengthValidator.GetType().GetProperty(nameof(ILengthValidator.Min));
		PropertyInfo? maxProp = lengthValidator.GetType().GetProperty(nameof(ILengthValidator.Max));

		int? min = minProp?.GetValue(lengthValidator) as int?;
		int? max = maxProp?.GetValue(lengthValidator) as int?;

		if (min is null && max is null)
		{
			return null;
		}

		return new Validation.StringLengthRule(propertyName, minLength: min > 0 ? min : null, maxLength: max > 0 ? max : null);
	}

	static Validation.PatternRule? CreatePatternRule(string propertyName, IRegularExpressionValidator regexValidator)
	{
		PropertyInfo? expressionProp = regexValidator.GetType().GetProperty(nameof(IRegularExpressionValidator.Expression));
		string? pattern = expressionProp?.GetValue(regexValidator) as string;

		if (string.IsNullOrEmpty(pattern))
		{
			return null;
		}

		return new Validation.PatternRule(propertyName, pattern);
	}

	static Validation.ValidationRule? CreateComparisonRule(string propertyName, IComparisonValidator comparisonValidator)
	{
		// Get the ValueToCompare, MemberToCompare, and Comparison properties
		PropertyInfo? valueProp = comparisonValidator.GetType().GetProperty(nameof(IComparisonValidator.ValueToCompare));
		PropertyInfo? memberProp = comparisonValidator.GetType().GetProperty(nameof(IComparisonValidator.MemberToCompare));
		PropertyInfo? comparisonProp = comparisonValidator.GetType().GetProperty(nameof(IComparisonValidator.Comparison));

		object? value = valueProp?.GetValue(comparisonValidator);
		object? memberToCompare = memberProp?.GetValue(comparisonValidator);
		object? comparison = comparisonProp?.GetValue(comparisonValidator);

		if (comparison is null)
		{
			return null;
		}

		// If MemberToCompare is set (property comparison), we cannot create a RangeRule
		// because we don't have a compile-time constant value. Return null to let the
		// error message be handled as a custom rule with proper property name substitution.
		if (memberToCompare is not null)
		{
			return null;
		}

		if (value is null)
		{
			return null;
		}

		string comparisonName = comparison.ToString() ?? string.Empty;

		// Create appropriate range rule based on value type
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

	static Validation.RangeRule<T>? CreateTypedRangeRule<T>(string propertyName, T value, string comparisonName) where T : struct, IComparable<T>
	{
		return comparisonName switch
		{
			"GreaterThan" => new Validation.RangeRule<T>(propertyName, minimum: value, exclusiveMinimum: true),
			"GreaterThanOrEqual" => new Validation.RangeRule<T>(propertyName, minimum: value, exclusiveMinimum: false),
			"LessThan" => new Validation.RangeRule<T>(propertyName, maximum: value, exclusiveMaximum: true),
			"LessThanOrEqual" => new Validation.RangeRule<T>(propertyName, maximum: value, exclusiveMaximum: false),
			_ => null
		};
	}

	static Validation.ValidationRule? CreateBetweenRule(string propertyName, IBetweenValidator betweenValidator)
	{
		// Get the From and To properties
		PropertyInfo? fromProp = betweenValidator.GetType().GetProperty(nameof(IBetweenValidator.From));
		PropertyInfo? toProp = betweenValidator.GetType().GetProperty(nameof(IBetweenValidator.To));

		object? from = fromProp?.GetValue(betweenValidator);
		object? to = toProp?.GetValue(betweenValidator);

		if (from is null || to is null)
		{
			return null;
		}

		// Create appropriate range rule based on value type
		return from switch
		{
			int intFrom when to is int intTo => new Validation.RangeRule<int>(propertyName, minimum: intFrom, maximum: intTo),
			long longFrom when to is long longTo => new Validation.RangeRule<long>(propertyName, minimum: longFrom, maximum: longTo),
			decimal decimalFrom when to is decimal decimalTo => new Validation.RangeRule<decimal>(propertyName, minimum: decimalFrom, maximum: decimalTo),
			double doubleFrom when to is double doubleTo => new Validation.RangeRule<double>(propertyName, minimum: doubleFrom, maximum: doubleTo),
			float floatFrom when to is float floatTo => new Validation.RangeRule<float>(propertyName, minimum: floatFrom, maximum: floatTo),
			_ => null
		};
	}

	[LoggerMessage(Level = LogLevel.Warning, Message = "Unable to determine custom error message for validator '{ValidatorType}' on property '{PropertyName}'.Falling back to generic message.")]
	static partial void LogUnableToDetermineCustomErrorMessage(ILogger logger, string validatorType, string propertyName);

	[LoggerMessage(Level = LogLevel.Warning, Message = "Exception while obtaining validator error message for property '{PropertyName}' using validator '{ValidatorType}'. Using generic message.")]
	static partial void LogExceptionWhileObtainingValidatorErrorMessage(ILogger logger, string propertyName, string validatorType, Exception exception);

	[LoggerMessage(Level = LogLevel.Warning, Message = "Vague error message given to propery {PropertyName} from the object {ValidatorType}. Consider using .WithMessage().")]
	static partial void LogVagueErrorMessageFluentValidation(ILogger logger, string validatorType, string propertyName);

	/// <summary>
	/// Extracts both the enum type and the property type from a FluentValidation validator
	/// Returns (enumType, propertyType) where:
	/// - enumType is the enum type being validated against
	/// - propertyType is the actual property type (string for IsEnumName, int for IsInEnum, TEnum for IsInEnum on enum)
	/// </summary>
	static (Type? enumType, Type? propertyType) ExtractEnumAndPropertyTypes(IPropertyValidator propertyValidator)
	{
		Type validatorType = propertyValidator.GetType();

		// For built-in FluentValidation EnumValidator (used with .IsInEnum() on enum properties)
		// This validator is used when calling .IsInEnum() on TEnum or TEnum? properties
		if (validatorType.Name.Contains("EnumValidator"))
		{
			// Try to get the enum type from the generic type argument
			// EnumValidator<TModel, TProperty> where TProperty is the enum type or Nullable<TEnum>
			if (validatorType.IsGenericType)
			{
				Type[] genericArgs = validatorType.GetGenericArguments();
				// EnumValidator has 2 generic arguments: TModel and TProperty
				// We need the second one (TProperty)
				if (genericArgs.Length >= 2)
				{
					Type propertyType = genericArgs[1];

					// Check if it's directly an enum
					if (propertyType.IsEnum)
					{
						return (propertyType, propertyType);
					}

					// Check if it's a nullable enum (Nullable<TEnum>)
					if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
					{
						Type? underlyingType = Nullable.GetUnderlyingType(propertyType);
						if (underlyingType is not null && underlyingType.IsEnum)
						{
							return (underlyingType, propertyType);  // Return enum type and nullable property type
						}
					}
				}
			}

			// Fallback: Try to infer from _enumType field
			FieldInfo? enumTypeField = validatorType.GetField("_enumType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			if (enumTypeField is not null)
			{
				Type? enumType = enumTypeField.GetValue(propertyValidator) as Type;
				if (enumType is not null && enumType.IsEnum)
				{
					// Assume property type is the enum type for built-in EnumValidator
					return (enumType, enumType);
				}
			}
		}

		// For IsEnumName validators (StringEnumValidator) - property type is string
		FieldInfo? enumNamesField = validatorType.GetField("_enumNames", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
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
			PropertyInfo? errorMessageSource = validatorType.GetProperty("ErrorMessageSource", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
			if (errorMessageSource is not null)
			{
				object? messageSource = errorMessageSource.GetValue(propertyValidator);
				if (messageSource is not null)
				{
					PropertyInfo? messageProperty = messageSource.GetType().GetProperty("Message", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
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
			FieldInfo? predicateField = validatorType.GetField("_predicate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
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

		FieldInfo[] fields = target.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

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
}
