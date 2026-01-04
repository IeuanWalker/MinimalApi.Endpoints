namespace IeuanWalker.MinimalApi.Endpoints.Validation;

/// <summary>
/// Fluent builder for configuring validation rules for a specific property
/// </summary>
public sealed class PropertyValidationBuilder<TRequest, TProperty>
{
	readonly string _propertyName;
	readonly List<ValidationRule> _rules = [];
	readonly List<ValidationRuleOperation> _operations = [];
	bool? _appendRulesToPropertyDescription;

	internal PropertyValidationBuilder(string propertyName)
	{
		ArgumentNullException.ThrowIfNullOrWhiteSpace(propertyName, nameof(propertyName));

		_propertyName = propertyName;
	}

	internal IEnumerable<ValidationRule> Build()
	{
		// Don't apply operations here - they will be applied later in ValidationDocumentTransformer
		// after FluentValidation rules are merged

		// Apply the per-property AppendRulesToPropertyDescription setting to all rules
		if (_appendRulesToPropertyDescription.HasValue)
		{
			return _rules.Select(rule => rule with
			{
				AppendRuleToPropertyDescription = _appendRulesToPropertyDescription.Value
			});
		}

		return _rules;
	}

	internal IReadOnlyList<ValidationRuleOperation> GetOperations() => _operations.AsReadOnly();

	/// <summary>
	/// Replaces the error message of an existing validation rule with a new message.
	/// The operation is applied after all rules are collected, including FluentValidation auto-discovered rules.
	/// </summary>
	/// <param name="oldRule">The error message of the validation rule to be replaced. Must match an existing rule's error message.</param>
	/// <param name="newRule">The new error message to assign to the specified validation rule.</param>
	/// <returns>The current <see cref="PropertyValidationBuilder{TRequest, TProperty}"/> instance for method chaining.</returns>
	/// <exception cref="ArgumentException">Thrown if no validation rule exists with an error message equal to <paramref name="oldRule"/>.</exception>
	public PropertyValidationBuilder<TRequest, TProperty> Alter(string oldRule, string newRule)
	{
		_operations.Add(new AlterOperation(oldRule, newRule));
		return this;
	}

	/// <summary>
	/// Removes the validation rule with the specified error message from the property validation builder.
	/// The operation is applied after all rules are collected, including FluentValidation auto-discovered rules.
	/// </summary>
	/// <param name="rule">The error message of the validation rule to remove. Cannot be null or empty.</param>
	/// <returns>The current <see cref="PropertyValidationBuilder{TRequest, TProperty}"/> instance for method chaining.</returns>
	/// <exception cref="ArgumentException">Thrown if no validation rule exists with an error message equal to <paramref name="rule"/>.</exception>
	public PropertyValidationBuilder<TRequest, TProperty> Remove(string rule)
	{
		_operations.Add(new RemoveOperation(rule));
		return this;
	}

	/// <summary>
	/// Removes all validation rules from the property validation builder.
	/// The operation is applied after all rules are collected, including FluentValidation auto-discovered rules.
	/// </summary>
	/// <returns>The current <see cref="PropertyValidationBuilder{TRequest, TProperty}"/> instance for method chaining.</returns>
	/// <exception cref="InvalidOperationException">Thrown if there are no validation rules to remove.</exception>
	public PropertyValidationBuilder<TRequest, TProperty> RemoveAll()
	{
		_operations.Add(new RemoveAllOperation());
		return this;
	}

	/// <summary>
	/// Adds a custom description to the property in OpenAPI documentation.
	/// The description will be prepended before validation rules.
	/// </summary>
	/// <param name="description">Custom description for the property</param>
	public PropertyValidationBuilder<TRequest, TProperty> Description(string description)
	{
		_rules.Add(new DescriptionRule(_propertyName, description));
		return this;
	}

	/// <summary>
	/// Controls whether validation rules should be listed in the property description field in OpenAPI documentation.
	/// If not called, uses the global configuration setting (default: true).
	/// </summary>
	/// <param name="appendRules">True to list validation rules in description, false to hide them</param>
	public PropertyValidationBuilder<TRequest, TProperty> AppendRulesToPropertyDescription(bool appendRules)
	{
		_appendRulesToPropertyDescription = appendRules;
		return this;
	}

	/// <summary>
	/// Applies a custom validation note (not represented in OpenAPI schema - use for documentation only)
	/// </summary>
	/// <param name="errorMessage">Description of the validation rule</param>
	public PropertyValidationBuilder<TRequest, TProperty> Custom(string errorMessage)
	{
		_rules.Add(new CustomRule<TProperty>(_propertyName, errorMessage));
		return this;
	}

	/// <summary>
	/// Requires the property to have a non-null value
	/// </summary>
	public PropertyValidationBuilder<TRequest, TProperty> Required(string? errorMessage = null)
	{
		_rules.Add(new RequiredRule(_propertyName, errorMessage));
		return this;
	}

	// ----------------
	// String-based rules
	// ----------------

	/// <summary>
	/// Requires string length to be between min and max (inclusive)
	/// </summary>
	/// <exception cref="ArgumentException">Min must be less than or equal to max</exception>
	/// <exception cref="ArgumentException">Min must be greater than or equal to 0</exception>
	public PropertyValidationBuilder<TRequest, TProperty> Length(int min, int max, string? errorMessage = null)
	{
		if (min > max)
		{
			throw new ArgumentException("Min must be less than or equal to max");
		}

		if (min < 0)
		{
			throw new ArgumentException("Min must be greater than or equal to 0", nameof(min));
		}

		_rules.Add(new StringLengthRule(_propertyName, min, max, errorMessage));
		return this;
	}

	/// <summary>
	/// Requires string to have at least the specified minimum length
	/// </summary>
	/// <exception cref="ArgumentException">Min must be greater than or equal to 0</exception>
	public PropertyValidationBuilder<TRequest, TProperty> MinLength(int min, string? errorMessage = null)
	{
		if (min < 0)
		{
			throw new ArgumentException("Min must be greater than or equal to 0", nameof(min));
		}

		_rules.Add(new StringLengthRule(_propertyName, min, null, errorMessage));
		return this;
	}

	/// <summary>
	/// Requires string to not exceed the specified maximum length
	/// </summary>
	/// <exception cref="ArgumentException">Min must be greater than or equal to 0</exception>
	public PropertyValidationBuilder<TRequest, TProperty> MaxLength(int max, string? errorMessage = null)
	{
		if (max < 0)
		{
			throw new ArgumentException("Max must be greater than or equal to 0", nameof(max));
		}

		_rules.Add(new StringLengthRule(_propertyName, null, max, errorMessage));
		return this;
	}

	/// <summary>
	/// Requires string to match the specified regex pattern
	/// </summary>
	/// <exception cref="ArgumentNullException">Pattern is null or whitespace</exception>
	public PropertyValidationBuilder<TRequest, TProperty> Pattern(string regex, string? errorMessage = null)
	{
		ArgumentNullException.ThrowIfNullOrWhiteSpace(regex, nameof(regex));

		_rules.Add(new PatternRule(_propertyName, regex, errorMessage));
		return this;
	}

	/// <summary>
	/// Requires string to be a valid email address
	/// </summary>
	public PropertyValidationBuilder<TRequest, TProperty> Email(string? errorMessage = null)
	{
		_rules.Add(new EmailRule(_propertyName, errorMessage));
		return this;
	}

	/// <summary>
	/// Requires string to be a valid URL
	/// </summary>
	public PropertyValidationBuilder<TRequest, TProperty> Url(string? errorMessage = null)
	{
		_rules.Add(new UrlRule(_propertyName, errorMessage));
		return this;
	}

	// ----------------
	// Number / range-based rules
	// ----------------

	/// <summary>
	/// Requires value to be between min and max (inclusive)
	/// </summary>
	/// <exception cref="ArgumentException">Min must be less than or equal to max</exception>
	public PropertyValidationBuilder<TRequest, TProperty> Between<TValue>(TValue min, TValue max, string? errorMessage = null) where TValue : struct, IComparable<TValue>
	{
		if (min.CompareTo(max) > 0)
		{
			throw new ArgumentException("Min must be less than or equal to max");
		}

		_rules.Add(new RangeRule<TValue>(_propertyName, min, max, false, false, errorMessage));
		return this;
	}

	/// <summary>
	/// Requires value to be greater than the specified minimum (exclusive)
	/// </summary>
	public PropertyValidationBuilder<TRequest, TProperty> GreaterThan<TValue>(TValue value, string? errorMessage = null) where TValue : struct, IComparable<TValue>
	{
		_rules.Add(new RangeRule<TValue>(_propertyName, value, null, true, false, errorMessage));
		return this;
	}

	/// <summary>
	/// Requires value to be greater than or equal to the specified minimum
	/// </summary>
	public PropertyValidationBuilder<TRequest, TProperty> GreaterThanOrEqual<TValue>(TValue value, string? errorMessage = null) where TValue : struct, IComparable<TValue>
	{
		_rules.Add(new RangeRule<TValue>(_propertyName, value, null, false, false, errorMessage));
		return this;
	}

	/// <summary>
	/// Requires value to be less than the specified maximum (exclusive)
	/// </summary>
	public PropertyValidationBuilder<TRequest, TProperty> LessThan<TValue>(TValue value, string? errorMessage = null) where TValue : struct, IComparable<TValue>
	{
		_rules.Add(new RangeRule<TValue>(_propertyName, null, value, false, true, errorMessage));
		return this;
	}

	/// <summary>
	/// Requires value to be less than or equal to the specified maximum
	/// </summary>
	public PropertyValidationBuilder<TRequest, TProperty> LessThanOrEqual<TValue>(TValue value, string? errorMessage = null) where TValue : struct, IComparable<TValue>
	{
		_rules.Add(new RangeRule<TValue>(_propertyName, null, value, false, false, errorMessage));
		return this;
	}
}
