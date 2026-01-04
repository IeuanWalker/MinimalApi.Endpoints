using FluentValidation;
using IeuanWalker.MinimalApi.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace ExampleApi.Endpoints.Validation.GetEndpointB;

public class RequestModel
{
	[FromQuery]
	public string Name { get; set; } = string.Empty;
}

sealed class RequestModelValidator : Validator<RequestModel>
{
	public RequestModelValidator()
	{
		// Name must be at least 10 characters for Endpoint B (different from A)
		RuleFor(x => x.Name).MinimumLength(10).WithMessage("Must be at least 10 characters");
	}
}
