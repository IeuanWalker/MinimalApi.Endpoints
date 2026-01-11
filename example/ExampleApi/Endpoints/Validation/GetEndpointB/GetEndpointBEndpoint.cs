using System.Diagnostics.CodeAnalysis;
using ExampleApi.Infrastructure;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Validation.GetEndpointB;

[ExcludeFromCodeCoverage]
public class GetEndpointBEndpoint : IEndpointWithoutResponse<RequestModel>
{
	public static void Configure(RouteHandlerBuilder builder)
	{
		builder
			.Group<ValidationEndpointGroup>()
			.Get("/EndpointB")
			.Version(1)
			.RequestAsParameters()
			.WithSummary("Test Endpoint B with Name property validation");
	}

	public Task Handle(RequestModel request, CancellationToken ct)
	{
		// Just a test endpoint
		return Task.CompletedTask;
	}
}
