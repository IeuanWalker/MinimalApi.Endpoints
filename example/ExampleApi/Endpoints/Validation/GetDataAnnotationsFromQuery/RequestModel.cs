using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Validation;

namespace ExampleApi.Endpoints.Validation.GetDataAnnotationsFromQuery;
#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
[ValidatableType]// Required for DataAnnotations validation
#pragma warning restore ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
public class RequestModel
{
	[FromQuery]
	[Required]
	[StringLength(50, MinimumLength = 3)]
	[MinLength(3)]
	[MaxLength(100)]
	[RegularExpression(@"^[a-zA-Z0-9]+$")]
	[EmailAddress]
	[Phone]
	[Url]
	[CreditCard]
	public string? AllBuiltInStringValidators { get; set; }

	[FromQuery]
	[Required]
	[Range(10, 25)]
	[MinLength(3)]
	[MaxLength(100)]
	public decimal? AllBuiltInNumberValidators { get; set; }

	[FromQuery]
	[Required]
	public string RequiredString { get; set; } = string.Empty;

	[FromQuery]
	[Required]
	public int RequiredInt { get; set; }

	[FromQuery]
	[StringLength(50, MinimumLength = 3)]
	public string StringLength { get; set; } = string.Empty;

	[FromQuery]
	[MinLength(3)]
	public string StringMin { get; set; } = string.Empty;

	[FromQuery]
	[MinLength(3)]
	public string[] ListMin { get; set; } = [];

	[FromQuery]
	[MaxLength(100)]
	public string StringMax { get; set; } = string.Empty;

	[FromQuery]
	[MaxLength(10)]
	public string[] ListMax { get; set; } = [];

	[FromQuery]
	[Range(10, 25)]
	public int RangeInt { get; set; }

	[FromQuery]
	[Range(typeof(DateTime), "1996-06-08", "2026-01-04")]
	public DateTime RangeDateTime { get; set; }

	[FromQuery]
	[RegularExpression(@"^[a-zA-Z0-9]+$")]
	public string StringPattern { get; set; } = string.Empty;

	[FromQuery]
	[EmailAddress]
	public string StringEmail { get; set; } = string.Empty;

	[FromQuery]
	[Phone]
	public string StringPhoneNumber { get; set; } = string.Empty;

	[FromQuery]
	[Url]
	public string StringUrl { get; set; } = string.Empty;

	[FromQuery]
	public Uri? ActualUri { get; set; }

	[FromQuery]
	[Compare(nameof(Compare2))]
	public string Compare1 { get; set; } = string.Empty;

	[FromQuery]
	[Compare(nameof(Compare1))]
	public string Compare2 { get; set; } = string.Empty;

	[FromQuery]
	[CreditCard]
	public string StringCreditCard { get; set; } = string.Empty;

	[FromQuery]
	[Range(1, int.MaxValue)]
	public int IntMin { get; set; }

	[FromQuery]
	[Range(int.MinValue, 100)]
	public int IntMax { get; set; }

	[FromQuery]
	[Range(1, 100)]
	public int IntRange { get; set; }

	[FromQuery]
	[Range(0.1, double.MaxValue)]
	public double DoubleMin { get; set; }

	[FromQuery]
	[Range(double.MinValue, 99.9)]
	public double DoubleMax { get; set; }

	[FromQuery]
	[Range(0.1, 99.9)]
	public double DoubleRange { get; set; }

	[FromQuery]
	[MinLength(1)]
	public string[] ListStringMinCount { get; set; } = [];

	[FromQuery]
	[MaxLength(10)]
	public string[] ListStringMaxCount { get; set; } = [];

	[FromQuery]
	[MinLength(1)]
	[MaxLength(10)]
	public string[] ListStringRangeCount { get; set; } = [];

	[FromQuery]
	[MinLength(1)]
	public int[] ListIntMinCount { get; set; } = [];

	[FromQuery]
	[MaxLength(10)]
	public int[] ListIntMaxCount { get; set; } = [];

	[FromQuery]
	[MinLength(1)]
	[MaxLength(10)]
	public int[] ListIntRangeCount { get; set; } = [];

	// CustomValidationAttribute test property
	[FromQuery]
	[CustomValidation(typeof(RequestModelValidators), nameof(RequestModelValidators.ValidateCustomProperty))]
	public string CustomValidatedProperty { get; set; } = string.Empty;

	// Custom ValidationAttribute that throws a custom exception when validated
	[FromQuery]
	[WithDefaultErrorMessage]
	public string ThrowsCustomValidationProperty { get; set; } = string.Empty;

	[FromQuery]
	[WithDefaultErrorMessage]
	public string CustomValidationWithDefaultMessage { get; set; } = string.Empty;

	[FromQuery]
	[WithoutDefaultErrorMessage]
	public string CustomValidationWithoutDefaultMessage { get; set; } = string.Empty;

	[FromQuery]
	[WithoutDefaultErrorMessage(ErrorMessage = "Setting error message manually")]
	public string CustomValidationWithoutDefaultMessageSetManually { get; set; } = string.Empty;

	[FromQuery]
	[WithDefaultErrorMessage(ErrorMessage = "Override error message")]
	public string CustomValidationWithDefaultMessageOverrideMessage { get; set; } = string.Empty;
}

// Helper validator used by CustomValidationAttribute
public static class RequestModelValidators
{
	public static ValidationResult? ValidateCustomProperty(object? value, ValidationContext context)
	{
		string? s = value as string;
		if (string.IsNullOrWhiteSpace(s) || !s.StartsWith("ok"))
		{
			return new ValidationResult("Value must be a non-empty string starting with 'ok'.", new[] { context.MemberName ?? string.Empty });
		}
		return ValidationResult.Success;
	}
}

public class WithDefaultErrorMessageAttribute : ValidationAttribute
{
	public WithDefaultErrorMessageAttribute()
	{
		ErrorMessage = "Default error message";
	}

	public WithDefaultErrorMessageAttribute(string errorMessage)
	{
		ErrorMessage = errorMessage;
	}

	public override bool IsValid(object? value)
	{
		if (value is string valueString && valueString.Equals("Ieuan"))
		{
			return false;
		}

		return true;
	}
}

public class WithoutDefaultErrorMessageAttribute : ValidationAttribute
{
	public WithoutDefaultErrorMessageAttribute()
	{

	}
	public WithoutDefaultErrorMessageAttribute(string errorMessage)
	{
		ErrorMessage = errorMessage;
	}

	public override bool IsValid(object? value)
	{
		if (value is string valueString && valueString.Equals("Ieuan"))
		{
			return false;
		}

		return true;
	}
}

