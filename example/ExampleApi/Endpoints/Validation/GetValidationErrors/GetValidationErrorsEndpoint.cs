using System.Diagnostics.CodeAnalysis;
using ExampleApi.Infrastructure;
using IeuanWalker.MinimalApi.Endpoints;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ExampleApi.Endpoints.Validation.GetValidationErrors;

public class GetValidationErrorsEndpoint : IEndpointWithoutRequest<ProblemHttpResult>
{
	[ExcludeFromCodeCoverage]
	public static void Configure(RouteHandlerBuilder builder)
	{
		builder
			.Group<ValidationEndpointGroup>()
			.Get("/ValidationErrors")
			.Version(1)
			.WithSummary("ValidationErrors");
	}

	public Task<ProblemHttpResult> Handle(CancellationToken ct)
	{
		ProblemHttpResult result = new ValidationErrors<RequestModel>()
			.Add(x => x.Name, "Name is required")
			.Add(x => x.Nested.Description, "Description is required")
			.ToProblemResponse();

		return Task.FromResult(result);
	}
}
