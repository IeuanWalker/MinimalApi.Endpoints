namespace IeuanWalker.MinimalApi.Endpoints.Validation;

/// <summary>
/// Base class for validation rules that define OpenAPI schema constraints
/// </summary>
public abstract record ValidationRule
{
	/// <summary>
	/// Name of the property this rule applies to
	/// </summary>
	public string PropertyName { get; init; } = string.Empty;

	/// <summary>
	/// Error message to display when validation fails
	/// </summary>
	public string ErrorMessage { get; set; } = string.Empty;

	/// <summary>
	/// Whether to list validation rules in the property description field in OpenAPI documentation.
	/// Null means use the global configuration setting.
	/// </summary>
	public bool? AppendRulesToPropertyDescription { get; init; }
}

/// <summary>
/// Validation rule that marks a property as required in OpenAPI schema
/// </summary>
public sealed record RequiredRule : ValidationRule
{
	public RequiredRule(string propertyName, string? errorMessage = null)
	{
		ArgumentNullException.ThrowIfNullOrWhiteSpace(propertyName);

		PropertyName = propertyName;
		ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? "Is required" : errorMessage;
	}
}

/// <summary>
/// Validation rule for string length constraints (minLength/maxLength in OpenAPI)
/// </summary>
public sealed record StringLengthRule : ValidationRule
{
	public StringLengthRule(string propertyName, int? minLength, int? maxLength, string? errorMessage = null)
	{
		ArgumentNullException.ThrowIfNullOrWhiteSpace(propertyName);

		if (minLength is null && maxLength is null)
		{
			throw new ArgumentException("Minimum or Maximum must be set");
		}

		PropertyName = propertyName;
		MinLength = minLength;
		MaxLength = maxLength;

		if (!string.IsNullOrWhiteSpace(errorMessage))
		{
			ErrorMessage = errorMessage;
			return;
		}

		if (MinLength.HasValue && MaxLength.HasValue)
		{
			ErrorMessage = $"Length must be between {MinLength.Value} and {MaxLength.Value} characters";
		}
		else if (MinLength.HasValue)
		{
			ErrorMessage = $"Minimum length: {MinLength.Value} characters";
		}
		else if (MaxLength.HasValue)
		{
			ErrorMessage = $"Maximum length: {MaxLength.Value} characters";
		}
	}
	/// <summary>
	/// Minimum allowed length (inclusive)
	/// </summary>
	public int? MinLength { get; init; }

	/// <summary>
	/// Maximum allowed length (inclusive)
	/// </summary>
	public int? MaxLength { get; init; }
}

/// <summary>
/// Validation rule for regex pattern matching (pattern in OpenAPI)
/// </summary>
public sealed record PatternRule : ValidationRule
{
	public PatternRule(string propertyName, string pattern, string? errorMessage = null)
	{
		ArgumentNullException.ThrowIfNullOrWhiteSpace(propertyName);
		ArgumentNullException.ThrowIfNullOrWhiteSpace(pattern);

		PropertyName = propertyName;
		Pattern = pattern;
		ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? $"Must match pattern: {Pattern}" : errorMessage;
	}
	/// <summary>
	/// Regular expression pattern to match
	/// </summary>
	public string Pattern { get; init; }
}

/// <summary>
/// Validation rule for email addresses (format: email in OpenAPI)
/// </summary>
public sealed record EmailRule : ValidationRule
{
	public EmailRule(string propertyName, string? errorMessage = null)
	{
		ArgumentNullException.ThrowIfNullOrWhiteSpace(propertyName);

		PropertyName = propertyName;
		ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? "Must be a valid email address" : errorMessage;
	}
}

/// <summary>
/// Validation rule for URLs (format: uri in OpenAPI)
/// </summary>
public sealed record UrlRule : ValidationRule
{
	public UrlRule(string propertyName, string? errorMessage = null)
	{
		ArgumentNullException.ThrowIfNullOrWhiteSpace(propertyName);

		PropertyName = propertyName;
		ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? "Must be a valid URL" : errorMessage;
	}
}

/// <summary>
/// Validation rule for numeric range constraints (minimum/maximum in OpenAPI)
/// </summary>
public sealed record RangeRule<T> : ValidationRule where T : struct, IComparable<T>
{
	public RangeRule(
		string propertyName,
		T? minimum = null,
		T? maximum = null,
		bool exclusiveMinimum = false,
		bool exclusiveMaximum = false,
		string? errorMessage = null)
	{
		ArgumentNullException.ThrowIfNullOrWhiteSpace(propertyName);

		if (minimum is null && maximum is null)
		{
			throw new ArgumentException("Minimum or Maximum must be set");
		}

		PropertyName = propertyName;
		Minimum = minimum;
		Maximum = maximum;
		ExclusiveMinimum = exclusiveMinimum;
		ExclusiveMaximum = exclusiveMaximum;

		if (!string.IsNullOrWhiteSpace(errorMessage))
		{
			ErrorMessage = errorMessage;
			return;
		}

		if (Minimum.HasValue && Maximum.HasValue)
		{
			string minOperator = ExclusiveMinimum ? ">" : ">=";
			string maxOperator = ExclusiveMaximum ? "<" : "<=";
			ErrorMessage = $"Must be {minOperator} {Minimum.Value} and {maxOperator} {Maximum.Value}";
		}
		else if (Minimum.HasValue)
		{
			string minOperator = ExclusiveMinimum ? ">" : ">=";
			ErrorMessage = $"Must be {minOperator} {Minimum.Value}";
		}
		else if (Maximum.HasValue)
		{
			string maxOperator = ExclusiveMaximum ? "<" : "<=";
			ErrorMessage = $"Must be {maxOperator} {Maximum.Value}";
		}
	}
	/// <summary>
	/// Minimum allowed value
	/// </summary>
	public T? Minimum { get; init; }

	/// <summary>
	/// Maximum allowed value
	/// </summary>
	public T? Maximum { get; init; }

	/// <summary>
	/// Whether the minimum is exclusive (value must be greater than minimum)
	/// </summary>
	public bool ExclusiveMinimum { get; init; }

	/// <summary>
	/// Whether the maximum is exclusive (value must be less than maximum)
	/// </summary>
	public bool ExclusiveMaximum { get; init; }
}

/// <summary>
/// Custom validation rule (note: custom rules cannot be represented in OpenAPI schema and are ignored)
/// </summary>
public sealed record CustomRule<TProperty> : ValidationRule
{
	public CustomRule(string propertyName, string errorMessage)
	{
		ArgumentNullException.ThrowIfNullOrWhiteSpace(propertyName);
		ArgumentNullException.ThrowIfNullOrWhiteSpace(errorMessage);

		PropertyName = propertyName;
		ErrorMessage = errorMessage;
	}
}

/// <summary>
/// Description rule for adding custom descriptions to properties in OpenAPI documentation
/// </summary>
public sealed record DescriptionRule : ValidationRule
{
	public DescriptionRule(string propertyName, string description)
	{
		ArgumentNullException.ThrowIfNullOrWhiteSpace(propertyName);

		PropertyName = propertyName;
		Description = description;
	}
	/// <summary>
	/// Custom description to display for the property
	/// </summary>
	public string Description { get; init; }
}
