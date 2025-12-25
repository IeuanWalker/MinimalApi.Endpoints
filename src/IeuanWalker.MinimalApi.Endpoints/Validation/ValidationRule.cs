using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace IeuanWalker.MinimalApi.Endpoints.Validation;

/// <summary>
/// Base class for validation rules that can be applied to properties
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
	public required string ErrorMessage { get; init; }

	/// <summary>
	/// Validates the given value according to the rule
	/// </summary>
	public abstract bool IsValid(object? value);
}

/// <summary>
/// Validation rule that ensures a property is not null
/// </summary>
public sealed record RequiredRule : ValidationRule
{
	public override bool IsValid(object? value) => value is not null;
}

/// <summary>
/// Validation rule for string length constraints
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

	public override bool IsValid(object? value)
	{
		if (value is not string str)
		{
			return true; // Not a string, let other validators handle it
		}

		if (MinLength.HasValue && str.Length < MinLength.Value)
		{
			return false;
		}

		if (MaxLength.HasValue && str.Length > MaxLength.Value)
		{
			return false;
		}

		return true;
	}
}

/// <summary>
/// Validation rule for regex pattern matching
/// </summary>
public sealed record PatternRule : ValidationRule
{
	/// <summary>
	/// Regular expression pattern to match
	/// </summary>
	public required string Pattern { get; init; }

	Lazy<Regex>? _regex;

	public override bool IsValid(object? value)
	{
		if (value is not string str)
		{
			return true; // Not a string, let other validators handle it
		}

		_regex ??= new Lazy<Regex>(() => new Regex(Pattern, RegexOptions.Compiled));
		return _regex.Value.IsMatch(str);
	}
}

/// <summary>
/// Validation rule for email addresses
/// </summary>
public sealed record EmailRule : ValidationRule
{
	static readonly EmailAddressAttribute _emailValidator = new();

	public override bool IsValid(object? value)
	{
		if (value is not string str)
		{
			return true; // Not a string, let other validators handle it
		}

		return _emailValidator.IsValid(str);
	}
}

/// <summary>
/// Validation rule for numeric range constraints
/// </summary>
public sealed record RangeRule<T> : ValidationRule where T : IComparable<T>
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

	public override bool IsValid(object? value)
	{
		if (value is not T comparable)
		{
			return true; // Not the expected type, let other validators handle it
		}

		if (Minimum != null)
		{
			int minCompare = comparable.CompareTo(Minimum);
			if (ExclusiveMinimum && minCompare <= 0)
			{
				return false;
			}

			if (!ExclusiveMinimum && minCompare < 0)
			{
				return false;
			}
		}

		if (Maximum != null)
		{
			int maxCompare = comparable.CompareTo(Maximum);
			if (ExclusiveMaximum && maxCompare >= 0)
			{
				return false;
			}

			if (!ExclusiveMaximum && maxCompare > 0)
			{
				return false;
			}
		}

		return true;
	}
}

/// <summary>
/// Custom validation rule with a user-defined validator function
/// </summary>
public sealed record CustomRule<TProperty> : ValidationRule
{
	/// <summary>
	/// Custom validator function
	/// </summary>
	public required Func<TProperty?, bool> Validator { get; init; }

	public override bool IsValid(object? value)
	{
		if (value is not TProperty typedValue && value is not null)
		{
			return true; // Not the expected type
		}

		return Validator((TProperty?)value);
	}
}
