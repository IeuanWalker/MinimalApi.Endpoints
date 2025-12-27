namespace IeuanWalker.MinimalApi.Endpoints.Validation;

/// <summary>
/// Base class for validation rules that define OpenAPI schema constraints
/// </summary>
public abstract record ValidationRule
{
	/// <summary>
	/// Name of the property this rule applies to
	/// </summary>
	public required string PropertyName { get; init; }

	/// <summary>
	/// Error message to display when validation fails
	/// </summary>
	public required string ErrorMessage { get; set; }

	/// <summary>
	/// Whether to list validation rules in the property description field in OpenAPI documentation.
	/// Null means use the global configuration setting.
	/// </summary>
	public bool? ListRulesInDescription { get; init; }
}

/// <summary>
/// Validation rule that marks a property as required in OpenAPI schema
/// </summary>
public sealed record RequiredRule : ValidationRule;

/// <summary>
/// Validation rule for string length constraints (minLength/maxLength in OpenAPI)
/// </summary>
public sealed record StringLengthRule : ValidationRule
{
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
	/// <summary>
	/// Regular expression pattern to match
	/// </summary>
	public required string Pattern { get; init; }
}

/// <summary>
/// Validation rule for email addresses (format: email in OpenAPI)
/// </summary>
public sealed record EmailRule : ValidationRule;

/// <summary>
/// Validation rule for URLs (format: uri in OpenAPI)
/// </summary>
public sealed record UrlRule : ValidationRule;

/// <summary>
/// Validation rule for numeric range constraints (minimum/maximum in OpenAPI)
/// </summary>
public sealed record RangeRule<T> : ValidationRule where T : struct, IComparable<T>
{
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
public sealed record CustomRule<TProperty> : ValidationRule;

/// <summary>
/// Description rule for adding custom descriptions to properties in OpenAPI documentation
/// </summary>
public sealed record DescriptionRule : ValidationRule
{
	/// <summary>
	/// Custom description to display for the property
	/// </summary>
	public required string Description { get; init; }
}
