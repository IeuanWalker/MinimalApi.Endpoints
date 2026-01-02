using FluentValidation;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Todos.Post;

public class RequestModel
{
	// String validation examples
	public string Title { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string Pattern { get; set; } = string.Empty;
	public string LengthRange { get; set; } = string.Empty;
	public string CreditCard { get; set; } = string.Empty;
	public string Url { get; set; } = string.Empty;

	// Boolean validation
	public bool IsCompleted { get; set; }
	public bool IsTest { get; set; }

	// Complex type validation
	public NestedObjectModel NestedObject { get; set; } = new();
	public List<NestedObject2Model> NestedObject2 { get; set; } = [];

	// Null/empty validation
	public string? NullableString { get; set; }
	public string NonEmptyString { get; set; } = string.Empty;
}

public class NestedObjectModel
{
	public string Name { get; set; } = string.Empty;
	public int Age { get; set; }
}

public class NestedObject2Model
{
	public string Note { get; set; } = string.Empty;
}


sealed class RequestModelValidator : Validator<RequestModel>
{
	public RequestModelValidator()
	{
		// String validation examples
		RuleFor(x => x.Title)
			.NotEmpty()
			.MinimumLength(1)
			.MaximumLength(200);

		RuleFor(x => x.Description)
			.NotEmpty()
			.MaximumLength(1000);

		// Optional fields - only validate if provided
		RuleFor(x => x.Email)
			.EmailAddress()
			.When(x => !string.IsNullOrWhiteSpace(x.Email));

		RuleFor(x => x.Pattern)
			.Matches(@"^[A-Z][a-z]+$")
			.WithMessage("Must start with uppercase letter followed by lowercase letters")
			.When(x => !string.IsNullOrWhiteSpace(x.Pattern));

		RuleFor(x => x.LengthRange)
			.Length(5, 15)
			.When(x => !string.IsNullOrWhiteSpace(x.LengthRange));

		RuleFor(x => x.CreditCard)
			.CreditCard()
			.When(x => !string.IsNullOrWhiteSpace(x.CreditCard));

		RuleFor(x => x.Url)
			.Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
			.WithMessage("Must be a valid URL")
			.When(x => !string.IsNullOrWhiteSpace(x.Url));

		// Complex type validation
		RuleFor(x => x.NestedObject)
			.NotNull()
			.SetValidator(new NestedObjectModelValidator());

		RuleForEach(x => x.NestedObject2)
			.NotEmpty()
			.SetValidator(new NestedObject2ModelValidator())
			.When(x => x.NestedObject2 != null && x.NestedObject2.Count > 0);
	}
}

sealed class NestedObjectModelValidator : Validator<NestedObjectModel>
{
	public NestedObjectModelValidator()
	{
		RuleFor(x => x.Name)
			.NotEmpty()
			.MinimumLength(1)
			.MaximumLength(200);

		RuleFor(x => x.Age)
			.GreaterThan(0)
			.LessThan(80);
	}
}

sealed class NestedObject2ModelValidator : Validator<NestedObject2Model>
{
	public NestedObject2ModelValidator()
	{
		RuleFor(x => x.Note)
			.NotEmpty()
			.MinimumLength(1)
			.MaximumLength(200);
	}
}

