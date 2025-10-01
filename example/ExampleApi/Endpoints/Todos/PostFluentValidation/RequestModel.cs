using FluentValidation;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Todos.PostFluentValidation;

public class RequestModel
{
	public string Title { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;
	public NestedObjectModel NestedObject { get; set; } = new();
	public List<NestedObject2Model> NestedObject2 { get; set; } = [];

	public bool IsCompleted { get; set; }
	public bool IsTest { get; set; }
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


sealed class ConfirmationEmailSettingsValidator : Validator<RequestModel>
{
	public ConfirmationEmailSettingsValidator()
	{
		RuleFor(x => x.Title)
			.NotEmpty()
			.MinimumLength(1)
			.MaximumLength(200);

		RuleFor(x => x.Description)
			.NotEmpty()
			.MaximumLength(1000);

		RuleFor(x => x.NestedObject)
			.NotNull()
			.SetValidator(new NestedObjectModelValidator());

		RuleForEach(x => x.NestedObject2)
			.NotEmpty()
			.SetValidator(new NestedObject2ModelValidator());

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

