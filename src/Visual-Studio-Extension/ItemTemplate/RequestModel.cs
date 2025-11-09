using IeuanWalker.MinimalApi.Endpoints;

namespace $rootnamespace$;

public sealed class RequestModel
{
	// TODO: Add request properties
	// Example:
	// public int Id { get; set; }
	// public string Name { get; set; } = string.Empty;
}

sealed class RequestModelValidator : Validator<RequestModel>
{
	public RequestModelValidator()
	{
		// TODO: Add validation rules
		// Example:
		// RuleFor(x => x.Name)
		//     .NotEmpty()
		//     .MinimumLength(1)
		//     .MaximumLength(200);
	}
}
