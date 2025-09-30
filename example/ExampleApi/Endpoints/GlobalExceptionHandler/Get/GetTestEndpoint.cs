using ExampleApi.Infrastructure;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.GlobalExceptionHandler.Get;

public class GetTestEndpoint : IEndpoint
{
	public static void Configure(RouteHandlerBuilder builder)
	{
		builder
			.Get("api/v{version:apiVersion}/GlobalExceptionHandler/test")
			.Version(1.0);
	}

	public async Task Handle(CancellationToken ct)
	{
		throw new NotImplementedException();
	}
}
