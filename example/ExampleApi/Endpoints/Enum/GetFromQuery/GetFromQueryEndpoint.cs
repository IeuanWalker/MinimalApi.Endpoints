using System.Diagnostics.CodeAnalysis;
using ExampleApi.Infrastructure;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Enum.GetFromQuery;

public class GetFromQueryEndpoint : IEndpointWithoutResponse<RequestModel>
{
	[ExcludeFromCodeCoverage]
	public static void Configure(RouteHandlerBuilder builder)
	{
		builder
			.Group<EnumEndpointGroup>()
			.Get()
			.Version(1.0)
			.RequestAsParameters()
			.WithSummary("FromQuery");
	}

	public async Task Handle(RequestModel request, CancellationToken ct)
	{

	}
}
