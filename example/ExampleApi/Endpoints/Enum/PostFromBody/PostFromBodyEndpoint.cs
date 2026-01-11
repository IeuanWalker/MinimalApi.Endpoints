using System.Diagnostics.CodeAnalysis;
using ExampleApi.Infrastructure;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Enum.PostFromBody;

[ExcludeFromCodeCoverage]
public class PostFromBodyEndpoint : IEndpointWithoutResponse<RequestModel>
{
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
