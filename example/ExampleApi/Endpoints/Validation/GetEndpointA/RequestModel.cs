using FluentValidation;
using IeuanWalker.MinimalApi.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace ExampleApi.Endpoints.Validation.GetEndpointA;

public class RequestModel
{
	[FromQuery]
	public string Name { get; set; } = string.Empty;
}

sealed class RequestModelValidator : Validator<RequestModel>
{
	public RequestModelValidator()
	{
		// Name must be at least 5 characters for Endpoint A
		RuleFor(x => x.Name).MinimumLength(5).WithMessage("Must be at least 5 characters");
	}
}
