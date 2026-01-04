using System.Diagnostics.CodeAnalysis;
using ExampleApi.Infrastructure;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Validation.GetEndpointA;

public class GetEndpointAEndpoint : IEndpointWithoutResponse<RequestModel>
{
	[ExcludeFromCodeCoverage]
	public static void Configure(RouteHandlerBuilder builder)
	{
		builder
			.Group<ValidationEndpointGroup>()
			.Get("/EndpointA")
			.Version(1)
			.RequestAsParameters()
			.WithSummary("Test Endpoint A with Name property validation");
	}

	public async Task Handle(RequestModel request, CancellationToken ct)
	{
		// Just a test endpoint
		await Task.CompletedTask;
	}
}
