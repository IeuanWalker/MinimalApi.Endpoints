using System.Diagnostics.CodeAnalysis;
using ExampleApi.Infrastructure;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Validation.PostFluentValidationEdgeCases;

[ExcludeFromCodeCoverage]
public class PostFluentValidationEdgeCasesEndpoint : IEndpointWithoutResponse<RequestModel>
{
	public static void Configure(RouteHandlerBuilder builder)
	{
		builder
			.Group<ValidationEndpointGroup>()
			.Post("/FluentValidationEdgeCases")
			.Version(1)
			.RequestFromBody()
			.WithSummary("FluentValidationEdgeCases");
	}

	public async Task Handle(RequestModel request, CancellationToken ct)
	{
	}
}
