namespace IeuanWalker.MinimalApi.Endpoints.Validation;

/// <summary>
/// Fluent builder for configuring validation rules for a specific property
/// </summary>
public class PropertyValidationBuilder<TRequest, TProperty>
{
	readonly string _propertyName;
	readonly List<ValidationRule> _rules = [];

	internal PropertyValidationBuilder(string propertyName)
	{
		_propertyName = propertyName;
	}

	/// <summary>
	/// Requires the property to have a non-null value
	/// </summary>
	public PropertyValidationBuilder<TRequest, TProperty> Required(string? errorMessage = null)
	{
		_rules.Add(new RequiredRule
		{
			PropertyName = _propertyName,
			ErrorMessage = errorMessage ?? $"{_propertyName} is required"
		});
		return this;
	}

	/// <summary>
	/// Requires string to have at least the specified minimum length
	/// </summary>
	public PropertyValidationBuilder<TRequest, TProperty> MinLength(int min, string? errorMessage = null)
	{
		_rules.Add(new StringLengthRule
		{
			PropertyName = _propertyName,
			MinLength = min,
			ErrorMessage = errorMessage ?? $"{_propertyName} must be at least {min} characters"
		});
		return this;
	}

	/// <summary>
	/// Requires string to not exceed the specified maximum length
	/// </summary>
	public PropertyValidationBuilder<TRequest, TProperty> MaxLength(int max, string? errorMessage = null)
	{
		_rules.Add(new StringLengthRule
		{
			PropertyName = _propertyName,
			MaxLength = max,
			ErrorMessage = errorMessage ?? $"{_propertyName} must not exceed {max} characters"
		});
		return this;
	}

	/// <summary>
	/// Requires string length to be between min and max (inclusive)
	/// </summary>
	public PropertyValidationBuilder<TRequest, TProperty> Length(int min, int max, string? errorMessage = null)
	{
		_rules.Add(new StringLengthRule
		{
			PropertyName = _propertyName,
			MinLength = min,
			MaxLength = max,
			ErrorMessage = errorMessage ?? $"{_propertyName} must be between {min} and {max} characters"
		});
		return this;
	}

	/// <summary>
	/// Requires string to match the specified regex pattern
	/// </summary>
	public PropertyValidationBuilder<TRequest, TProperty> Pattern(string regex, string? errorMessage = null)
	{
		_rules.Add(new PatternRule
		{
			PropertyName = _propertyName,
			Pattern = regex,
			ErrorMessage = errorMessage ?? $"{_propertyName} does not match required pattern"
		});
		return this;
	}

	/// <summary>
	/// Requires string to be a valid email address
	/// </summary>
	public PropertyValidationBuilder<TRequest, TProperty> Email(string? errorMessage = null)
	{
		_rules.Add(new EmailRule
		{
			PropertyName = _propertyName,
			ErrorMessage = errorMessage ?? $"{_propertyName} must be a valid email address"
		});
		return this;
	}

	/// <summary>
	/// Applies a custom validation function
	/// </summary>
	public PropertyValidationBuilder<TRequest, TProperty> Custom(
		Func<TProperty?, bool> validator,
		string errorMessage)
	{
		_rules.Add(new CustomRule<TProperty>
		{
			PropertyName = _propertyName,
			Validator = validator,
			ErrorMessage = errorMessage
		});
		return this;
	}

	internal IEnumerable<ValidationRule> Build() => _rules;
}

/// <summary>
/// Extension methods for PropertyValidationBuilder with comparable types
/// </summary>
public static class PropertyValidationBuilderExtensions
{
	/// <summary>
	/// Requires value to be greater than the specified minimum (exclusive)
	/// </summary>
	public static PropertyValidationBuilder<TRequest, TProperty> GreaterThan<TRequest, TProperty>(
		this PropertyValidationBuilder<TRequest, TProperty> builder,
		TProperty value,
		string? errorMessage = null)
		where TProperty : struct, IComparable<TProperty>
	{
		// Use reflection to access the private members
		string propertyName = GetPropertyName(builder);
		List<ValidationRule> rules = GetRules(builder);

		rules.Add(new RangeRule<TProperty>
		{
			PropertyName = propertyName,
			Minimum = value,
			ExclusiveMinimum = true,
			ErrorMessage = errorMessage ?? $"{propertyName} must be greater than {value}"
		});
		return builder;
	}

	/// <summary>
	/// Requires value to be greater than or equal to the specified minimum
	/// </summary>
	public static PropertyValidationBuilder<TRequest, TProperty> GreaterThanOrEqual<TRequest, TProperty>(
		this PropertyValidationBuilder<TRequest, TProperty> builder,
		TProperty value,
		string? errorMessage = null)
		where TProperty : struct, IComparable<TProperty>
	{
		string propertyName = GetPropertyName(builder);
		List<ValidationRule> rules = GetRules(builder);

		rules.Add(new RangeRule<TProperty>
		{
			PropertyName = propertyName,
			Minimum = value,
			ExclusiveMinimum = false,
			ErrorMessage = errorMessage ?? $"{propertyName} must be greater than or equal to {value}"
		});
		return builder;
	}

	/// <summary>
	/// Requires value to be less than the specified maximum (exclusive)
	/// </summary>
	public static PropertyValidationBuilder<TRequest, TProperty> LessThan<TRequest, TProperty>(
		this PropertyValidationBuilder<TRequest, TProperty> builder,
		TProperty value,
		string? errorMessage = null)
		where TProperty : struct, IComparable<TProperty>
	{
		string propertyName = GetPropertyName(builder);
		List<ValidationRule> rules = GetRules(builder);

		rules.Add(new RangeRule<TProperty>
		{
			PropertyName = propertyName,
			Maximum = value,
			ExclusiveMaximum = true,
			ErrorMessage = errorMessage ?? $"{propertyName} must be less than {value}"
		});
		return builder;
	}

	/// <summary>
	/// Requires value to be less than or equal to the specified maximum
	/// </summary>
	public static PropertyValidationBuilder<TRequest, TProperty> LessThanOrEqual<TRequest, TProperty>(
		this PropertyValidationBuilder<TRequest, TProperty> builder,
		TProperty value,
		string? errorMessage = null)
		where TProperty : struct, IComparable<TProperty>
	{
		string propertyName = GetPropertyName(builder);
		List<ValidationRule> rules = GetRules(builder);

		rules.Add(new RangeRule<TProperty>
		{
			PropertyName = propertyName,
			Maximum = value,
			ExclusiveMaximum = false,
			ErrorMessage = errorMessage ?? $"{propertyName} must be less than or equal to {value}"
		});
		return builder;
	}

	/// <summary>
	/// Requires value to be between min and max (inclusive)
	/// </summary>
	public static PropertyValidationBuilder<TRequest, TProperty> Between<TRequest, TProperty>(
		this PropertyValidationBuilder<TRequest, TProperty> builder,
		TProperty min,
		TProperty max,
		string? errorMessage = null)
		where TProperty : struct, IComparable<TProperty>
	{
		string propertyName = GetPropertyName(builder);
		List<ValidationRule> rules = GetRules(builder);

		rules.Add(new RangeRule<TProperty>
		{
			PropertyName = propertyName,
			Minimum = min,
			Maximum = max,
			ExclusiveMinimum = false,
			ExclusiveMaximum = false,
			ErrorMessage = errorMessage ?? $"{propertyName} must be between {min} and {max}"
		});
		return builder;
	}

	static string GetPropertyName<TRequest, TProperty>(PropertyValidationBuilder<TRequest, TProperty> builder)
	{
		System.Reflection.FieldInfo? field = typeof(PropertyValidationBuilder<TRequest, TProperty>)
			.GetField("_propertyName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		return (string)(field?.GetValue(builder) ?? throw new InvalidOperationException("Unable to access property name"));
	}

	static List<ValidationRule> GetRules<TRequest, TProperty>(PropertyValidationBuilder<TRequest, TProperty> builder)
	{
		System.Reflection.FieldInfo? field = typeof(PropertyValidationBuilder<TRequest, TProperty>)
			.GetField("_rules", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		return (List<ValidationRule>)(field?.GetValue(builder) ?? throw new InvalidOperationException("Unable to access rules"));
	}
}
