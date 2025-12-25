using FluentValidation;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Todos.PostFluentValidation;

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

		RuleFor(x => x.Email)
			.EmailAddress();

		RuleFor(x => x.Pattern)
			.Matches(@"^[A-Z][a-z]+$")
			.WithMessage("Must start with uppercase letter followed by lowercase letters");

		RuleFor(x => x.LengthRange)
			.Length(5, 15);

		RuleFor(x => x.CreditCard)
			.CreditCard();

		RuleFor(x => x.Url)
			.Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
			.WithMessage("Must be a valid URL");

		// Complex type validation
		RuleFor(x => x.NestedObject)
			.NotNull()
			.SetValidator(new NestedObjectModelValidator());

		RuleForEach(x => x.NestedObject2)
			.NotEmpty()
			.SetValidator(new NestedObject2ModelValidator());

		// Null/empty validation
		RuleFor(x => x.NullableString)
			.NotNull();

		RuleFor(x => x.NonEmptyString)
			.NotEmpty();
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

