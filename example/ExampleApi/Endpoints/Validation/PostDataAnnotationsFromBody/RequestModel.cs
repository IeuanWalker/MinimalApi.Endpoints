using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Validation;

namespace ExampleApi.Endpoints.Validation.PostDataAnnotationsFromBody;

#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
[ValidatableType]// Required for DataAnnotations validation
#pragma warning restore ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
public class RequestModel
{
	[Required]
	[StringLength(maximumLength: 50, MinimumLength = 3)]
	[MinLength(3)]
	[MaxLength(100)]
	[RegularExpression(@"^[a-zA-Z0-9]+$")]
	[EmailAddress]
	[Phone]
	[Url]
	[CreditCard]
	public string? AllBuiltInStringValidators { get; set; }

	[Required]
	[Range(10, 25)]
	[MinLength(3)]
	[MaxLength(100)]
	public decimal? AllBuiltInNumberValidators { get; set; }

	[Required]
	public string RequiredString { get; set; } = string.Empty;

	[Required]
	public int RequiredInt { get; set; }

	[StringLength(50, MinimumLength = 3)]
	public string StringLength { get; set; } = string.Empty;

	[MinLength(3)]
	public string StringMin { get; set; } = string.Empty;

	[MinLength(3)]
	public string[] ListMin { get; set; } = [];

	[MaxLength(100)]
	public string StringMax { get; set; } = string.Empty;

	[MaxLength(10)]
	public string[] ListMax { get; set; } = [];

	[Range(10, 25)]
	public int RangeInt { get; set; }

	[Range(typeof(DateTime), "1996-06-08", "2026-01-04")]
	public DateTime RangeDateTime { get; set; }

	[RegularExpression(@"^[a-zA-Z0-9]+$")]
	public string StringPattern { get; set; } = string.Empty;

	[EmailAddress]
	public string StringEmail { get; set; } = string.Empty;

	[Phone]
	public string StringPhoneNumber { get; set; } = string.Empty;

	[Url]
	public string StringUrl { get; set; } = string.Empty;

	public Uri? ActualUri { get; set; }

	[Compare(nameof(Compare2))]
	public string Compare1 { get; set; } = string.Empty;

	[Compare(nameof(Compare1))]
	public string Compare2 { get; set; } = string.Empty;

	[CreditCard]
	public string StringCreditCard { get; set; } = string.Empty;

	[FileExtensions(Extensions = "jpg,png")]
	public IFormFile? FileExtension { get; set; }

	[Range(1, int.MaxValue)]
	public int IntMin { get; set; }

	[Range(int.MinValue, 100)]
	public int IntMax { get; set; }

	[Range(1, 100)]
	public int IntRange { get; set; }

	[Range(0.1, double.MaxValue)]
	public double DoubleMin { get; set; }

	[Range(double.MinValue, 99.9)]
	public double DoubleMax { get; set; }

	[Range(0.1, 99.9)]
	public double DoubleRange { get; set; }

	[MinLength(1)]
	public List<string> ListStringMinCount { get; set; } = [];

	[MaxLength(10)]
	public List<string> ListStringMaxCount { get; set; } = [];

	[MinLength(1)]
	[MaxLength(10)]
	public List<string> ListStringRangeCount { get; set; } = [];

	[MinLength(1)]
	public List<int> ListIntMinCount { get; set; } = [];

	[MaxLength(10)]
	public List<int> ListIntMaxCount { get; set; } = [];

	[MinLength(1)]
	[MaxLength(10)]
	public List<int> ListIntRangeCount { get; set; } = [];

	// CustomValidationAttribute test property
	[CustomValidation(typeof(RequestModelValidators), nameof(RequestModelValidators.ValidateCustomProperty))]
	public string CustomValidatedProperty { get; set; } = string.Empty;

	// Custom ValidationAttribute that throws a custom exception when validated
	[WithDefaultErrorMessage]
	public string ThrowsCustomValidationProperty { get; set; } = string.Empty;

	[Required]
	public required NestedObjectModel NestedObject { get; set; }
	[MinLength(0)]
	[MaxLength(10)]
	public List<NestedObjectModel>? ListNestedObject { get; set; }

	[WithDefaultErrorMessage]
	public string CustomValidationWithDefaultMessage { get; set; } = string.Empty;

	[WithoutDefaultErrorMessage]
	public string CustomValidationWithoutDefaultMessage { get; set; } = string.Empty;

	[WithoutDefaultErrorMessage(ErrorMessage = "Setting error message manually")]
	public string CustomValidationWithoutDefaultMessageSetManually { get; set; } = string.Empty;

	[WithDefaultErrorMessage(ErrorMessage = "Override error message")]
	public string CustomValidationWithDefaultMessageOverrideMessage { get; set; } = string.Empty;
}

#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
[ValidatableType]
#pragma warning restore ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
public class NestedObjectModel
{
	[Required]
	[StringLength(50, MinimumLength = 3)]
	[MinLength(3)]
	[MaxLength(100)]
	[RegularExpression(@"^[a-zA-Z0-9]+$")]
	[EmailAddress]
	[Phone]
	[Url]
	[CreditCard]
	[CustomValidation(typeof(RequestModelValidators), nameof(RequestModelValidators.ValidateCustomProperty))]
	[WithDefaultErrorMessage]
	public string? AllBuiltInStringValidators { get; set; }

	[Required]
	[Range(10, 25)]
	[MinLength(3)]
	[MaxLength(100)]
	public decimal? AllBuiltInNumberValidators { get; set; }

	[Required]
	public string RequiredString { get; set; } = string.Empty;

	[Required]
	public int RequiredInt { get; set; }

	[StringLength(50, MinimumLength = 3)]
	public string StringLength { get; set; } = string.Empty;

	[MinLength(3)]
	public string StringMin { get; set; } = string.Empty;

	[MinLength(3)]
	public string[] ListMin { get; set; } = [];

	[MaxLength(100)]
	public string StringMax { get; set; } = string.Empty;

	[MaxLength(10)]
	public string[] ListMax { get; set; } = [];

	[Range(10, 25)]
	public int RangeInt { get; set; }

	[Range(typeof(DateTime), "1996-06-08", "2026-01-04")]
	public DateTime RangeDateTime { get; set; }

	[RegularExpression(@"^[a-zA-Z0-9]+$")]
	public string StringPattern { get; set; } = string.Empty;

	[EmailAddress]
	public string StringEmail { get; set; } = string.Empty;

	[Phone]
	public string StringPhoneNumber { get; set; } = string.Empty;

	[Url]
	public string StringUrl { get; set; } = string.Empty;

	public Uri? ActualUri { get; set; }

	[Compare(nameof(Compare2))]
	public string Compare1 { get; set; } = string.Empty;

	[Compare(nameof(Compare1))]
	public string Compare2 { get; set; } = string.Empty;

	[CreditCard]
	public string StringCreditCard { get; set; } = string.Empty;

	[FileExtensions(Extensions = "jpg,png")]
	public IFormFile? FileExtension { get; set; }

	[Range(1, int.MaxValue)]
	public int IntMin { get; set; }

	[Range(int.MinValue, 100)]
	public int IntMax { get; set; }

	[Range(1, 100)]
	public int IntRange { get; set; }

	[Range(0.1, double.MaxValue)]
	public double DoubleMin { get; set; }

	[Range(double.MinValue, 99.9)]
	public double DoubleMax { get; set; }

	[Range(0.1, 99.9)]
	public double DoubleRange { get; set; }

	[MinLength(1)]
	public List<string> ListStringMinCount { get; set; } = [];

	[MaxLength(10)]
	public List<string> ListStringMaxCount { get; set; } = [];

	[MinLength(1)]
	[MaxLength(10)]
	public List<string> ListStringRangeCount { get; set; } = [];

	[MinLength(1)]
	public List<int> ListIntMinCount { get; set; } = [];

	[MaxLength(10)]
	public List<int> ListIntMaxCount { get; set; } = [];

	[MinLength(1)]
	[MaxLength(10)]
	public List<int> ListIntRangeCount { get; set; } = [];

	[CustomValidation(typeof(RequestModelValidators), nameof(RequestModelValidators.ValidateCustomProperty))]
	public string CustomValidatedProperty { get; set; } = string.Empty;

	[WithDefaultErrorMessage]
	public string CustomValidationWithDefaultMessage { get; set; } = string.Empty;

	[WithoutDefaultErrorMessage]
	public string CustomValidationWithoutDefaultMessage { get; set; } = string.Empty;

	[WithoutDefaultErrorMessage(ErrorMessage = "Setting error message manually")]
	public string CustomValidationWithoutDefaultMessageSetManually { get; set; } = string.Empty;

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
