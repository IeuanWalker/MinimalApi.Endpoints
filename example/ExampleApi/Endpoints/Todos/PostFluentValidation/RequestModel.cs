using FluentValidation;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Todos.PostFluentValidation;

public class RequestModel
{
	public string Title { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	public bool IsCompleted { get; set; }
	public bool IsTest { get; set; }
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
	}
}
