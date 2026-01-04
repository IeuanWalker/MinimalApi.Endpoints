using FluentValidation;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Validation.PostWithValidationAlterRemove;

public class RequestModel
{
	public string Alter { get; set; } = string.Empty;
	public string Remove1 { get; set; } = string.Empty;
	public string RemoveAll { get; set; } = string.Empty;
}


sealed class RequestModelValidator : Validator<RequestModel>
{
	public RequestModelValidator()
	{
		RuleFor(x => x.Alter)
			.Matches(@"^[a-zA-Z0-9]+$")
			.MinimumLength(10);

		RuleFor(x => x.Remove1)
			.MinimumLength(10)
			.MaximumLength(100)
			.Matches(@"^[a-zA-Z0-9]+$");

		RuleFor(x => x.Remove1)
			.MinimumLength(10)
			.MaximumLength(100)
			.Matches(@"^[a-zA-Z0-9]+$");
	}
}
