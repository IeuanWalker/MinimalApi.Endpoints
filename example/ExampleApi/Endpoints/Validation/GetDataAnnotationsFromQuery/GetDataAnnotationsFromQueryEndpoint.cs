using System.Diagnostics.CodeAnalysis;
using ExampleApi.Infrastructure;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Validation.GetDataAnnotationsFromQuery;

public class GetFluentValidationFromQuery : IEndpointWithoutResponse<RequestModel>
{
	[ExcludeFromCodeCoverage]
	public static void Configure(RouteHandlerBuilder builder)
	{
		builder
			.Group<ValidationEndpointGroup>()
			.Get("/DataAnnotations/FromQuery")
			.Version(1)
			.RequestAsParameters()
			.WithSummary("DataAnnotationsFromQuery");
	}

	public async Task Handle(RequestModel request, CancellationToken ct)
	{
	}
}
