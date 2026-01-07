using System.Diagnostics.CodeAnalysis;
using ExampleApi.Infrastructure;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Enum.PostFromBody;

public class PostFromBodyEndpoint : IEndpointWithoutResponse<RequestModel>
{
	[ExcludeFromCodeCoverage]
	public static void Configure(RouteHandlerBuilder builder)
	{
		builder
			.Group<EnumEndpointGroup>()
			.Post()
			.Version(1.0)
			.RequestFromBody()
			.WithSummary("FromBody");
	}

	public async Task Handle(RequestModel request, CancellationToken ct)
	{

	}
}
