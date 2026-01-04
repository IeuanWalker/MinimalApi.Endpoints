using System.Diagnostics.CodeAnalysis;
using ExampleApi.Infrastructure;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Validation.PostDataAnnotationsFromBody;

public class PostDataValidationEndpoint : IEndpointWithoutResponse<RequestModel>
{
	[ExcludeFromCodeCoverage]
	public static void Configure(RouteHandlerBuilder builder)
	{
		builder
			.Group<ValidationEndpointGroup>()
			.Post("/DataValidation")
			.Version(1)
			.RequestFromBody()
			.WithSummary("DataAnnotationsFromBody");
	}

	public async Task Handle(RequestModel request, CancellationToken ct)
	{
	}
}
