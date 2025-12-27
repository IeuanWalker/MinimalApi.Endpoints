using FluentValidation;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Validation.PostFluentValidation;

public class RequestModel
{
	public string StringMin { get; set; } = string.Empty;
	public string StringMax { get; set; } = string.Empty;
	public string StringRange { get; set; } = string.Empty;
	public string StringPattern { get; set; } = string.Empty;

	public int IntMin { get; set; }
	public int IntMax { get; set; }
	public int IntRange { get; set; }

	public double DoubleMin { get; set; }
	public double DoubleMax { get; set; }
	public double DoubleRange { get; set; }

	public List<string> ListStringMinCount { get; set; } = [];
	public List<string> ListStringMaxCount { get; set; } = [];
	public List<string> ListStringRangeCount { get; set; } = [];

	public List<int> ListIntMinCount { get; set; } = [];
	public List<int> ListIntMaxCount { get; set; } = [];
	public List<int> ListIntRangeCount { get; set; } = [];

	public required NestedObjectModel NestedObject { get; set; }
	public List<NestedObjectModel>? ListNestedObject { get; set; }

	// Built in fleunt validators
	public string? AllBuiltInStringValidators { get; set; }
	public decimal? AllBuiltInNumberValidators { get; set; }
	public int MaxNumberTest { get; set; }
	public int MinNumberTest { get; set; }
	public required string EnumStringValidator { get; set; }
	public required int EnumIntValidator { get; set; }
	public required StatusEnum EnumTest { get; set; }
}

public class NestedObjectModel
{
	public string StringMin { get; set; } = string.Empty;
	public string StringMax { get; set; } = string.Empty;
	public string StringRange { get; set; } = string.Empty;
	public string StringPattern { get; set; } = string.Empty;

	public int IntMin { get; set; }
	public int IntMax { get; set; }
	public int IntRange { get; set; }

	public double DoubleMin { get; set; }
	public double DoubleMax { get; set; }
	public double DoubleRange { get; set; }

	public List<string> ListStringMinCount { get; set; } = [];
	public List<string> ListStringMaxCount { get; set; } = [];
	public List<string> ListStringRangeCount { get; set; } = [];

	public List<int> ListIntMinCount { get; set; } = [];
	public List<int> ListIntMaxCount { get; set; } = [];
	public List<int> ListIntRangeCount { get; set; } = [];
}

public enum StatusEnum
{
	Success,
	Failure
}

sealed class RequestModelValidator : Validator<RequestModel>
{
	public RequestModelValidator()
	{
		// String validation
		RuleFor(x => x.StringMin).MinimumLength(3);
		RuleFor(x => x.StringMax).MaximumLength(50);
		RuleFor(x => x.StringRange).Length(3, 50);
		RuleFor(x => x.StringPattern).Matches(@"^[a-zA-Z0-9]+$");

		// Integer validation
		RuleFor(x => x.IntMin).GreaterThanOrEqualTo(1);
		RuleFor(x => x.IntMax).LessThanOrEqualTo(100);
		RuleFor(x => x.IntRange).InclusiveBetween(1, 100);

		// Double validation
		RuleFor(x => x.DoubleMin).GreaterThanOrEqualTo(0.1);
		RuleFor(x => x.DoubleMax).LessThanOrEqualTo(99.9);
		RuleFor(x => x.DoubleRange).InclusiveBetween(0.1, 99.9);

		// List count validation (string lists)
		RuleFor(x => x.ListStringMinCount).Must(list => list.Count >= 1).WithMessage("Must contain at least 1 item.");
		RuleFor(x => x.ListStringMaxCount).Must(list => list.Count <= 10).WithMessage("Must contain at most 10 items.");
		RuleFor(x => x.ListStringRangeCount).Must(list => list.Count >= 1 && list.Count <= 10).WithMessage("Must contain between 1 and 10 items.");

		// List count validation (int lists)
		RuleFor(x => x.ListIntMinCount).Must(list => list.Count >= 1).WithMessage("Must contain at least 1 item.");
		RuleFor(x => x.ListIntMaxCount).Must(list => list.Count <= 10).WithMessage("Must contain at most 10 items.");
		RuleFor(x => x.ListIntRangeCount).Must(list => list.Count >= 1 && list.Count <= 10).WithMessage("Must contain between 1 and 10 items.");

		// Nested object validation
		RuleFor(x => x.NestedObject).NotNull().SetValidator(new NestedObjectModelValidator());

		// List of nested objects validation
		RuleForEach(x => x.ListNestedObject).SetValidator(new NestedObjectModelValidator());

		RuleFor(x => x.AllBuiltInStringValidators)
			.NotEmpty()
			.NotEqual("TestNotEqual")
			.Equal("TestEqual")
			.Length(2, 250)
			.MaximumLength(250)
			.MinimumLength(2)
			.Must(x => x == "TestEqual")
			.Matches(@"^[a-zA-Z0-9]+$")
			.EmailAddress()
			.CreditCard()
			.Empty()
			.Null();

		RuleFor(x => x.AllBuiltInNumberValidators)
			.NotEmpty()
			.NotEqual(10)
			.Equal(10)
			.LessThan(100)
			.LessThan(x => x.MaxNumberTest)
			.LessThanOrEqualTo(100)
			.LessThanOrEqualTo(x => x.MaxNumberTest)
			.GreaterThan(0)
			.GreaterThan(x => x.MinNumberTest)
			.GreaterThanOrEqualTo(1)
			.GreaterThanOrEqualTo(x => x.MinNumberTest)
			.Empty()
			.Null()
			.ExclusiveBetween(1, 10)
			.InclusiveBetween(1, 10)
			.PrecisionScale(4, 2, false);

		RuleFor(x => x.EnumStringValidator).IsEnumName(typeof(StatusEnum), caseSensitive: false);
		RuleFor(x => x.EnumIntValidator).IsInEnum();
		RuleFor(x => x.EnumTest).IsInEnum();
	}
}

sealed class NestedObjectModelValidator : Validator<NestedObjectModel>
{
	public NestedObjectModelValidator()
	{
		// String validation
		RuleFor(x => x.StringMin).MinimumLength(3);
		RuleFor(x => x.StringMax).MaximumLength(50);
		RuleFor(x => x.StringRange).Length(3, 50);
		RuleFor(x => x.StringPattern).Matches(@"^[a-zA-Z0-9]+$");

		// Integer validation
		RuleFor(x => x.IntMin).GreaterThanOrEqualTo(1);
		RuleFor(x => x.IntMax).LessThanOrEqualTo(100);
		RuleFor(x => x.IntRange).InclusiveBetween(1, 100);

		// Double validation
		RuleFor(x => x.DoubleMin).GreaterThanOrEqualTo(0.1);
		RuleFor(x => x.DoubleMax).LessThanOrEqualTo(99.9);
		RuleFor(x => x.DoubleRange).InclusiveBetween(0.1, 99.9);

		// List count validation (string lists)
		RuleFor(x => x.ListStringMinCount).Must(list => list.Count >= 1).WithMessage("Must contain at least 1 item.");
		RuleFor(x => x.ListStringMaxCount).Must(list => list.Count <= 10).WithMessage("Must contain at most 10 items.");
		RuleFor(x => x.ListStringRangeCount).Must(list => list.Count >= 1 && list.Count <= 10).WithMessage("Must contain between 1 and 10 items.");

		// List count validation (int lists)
		RuleFor(x => x.ListIntMinCount).Must(list => list.Count >= 1).WithMessage("Must contain at least 1 item.");
		RuleFor(x => x.ListIntMaxCount).Must(list => list.Count <= 10).WithMessage("Must contain at most 10 items.");
		RuleFor(x => x.ListIntRangeCount).Must(list => list.Count >= 1 && list.Count <= 10).WithMessage("Must contain between 1 and 10 items.");
	}
}
