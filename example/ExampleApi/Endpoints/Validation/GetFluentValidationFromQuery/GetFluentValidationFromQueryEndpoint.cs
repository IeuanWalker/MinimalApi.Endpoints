using System.Diagnostics.CodeAnalysis;
using ExampleApi.Infrastructure;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Validation.GetFluentValidationFromQuery;

public class GetFluentValidationFromQuery : IEndpointWithoutResponse<RequestModel>
{
	[ExcludeFromCodeCoverage]
	public static void Configure(RouteHandlerBuilder builder)
	{
		builder
			.Group<ValidationEndpointGroup>()
			.Get("/FluentValidation/FromQuery")
			.Version(1)
			.RequestAsParameters()
			.WithSummary("FluentValidationFromQuery");
	}

	public async Task Handle(RequestModel request, CancellationToken ct)
	{
	}
}
