using System.Diagnostics.CodeAnalysis;
using ExampleApi.Infrastructure;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Validation.PostDataValidation;

public class PostTodoEndpoint : IEndpointWithoutResponse<RequestModel>
{
	[ExcludeFromCodeCoverage]
	public static void Configure(RouteHandlerBuilder builder)
	{
		builder
			.Group<ValidationEndpointGroup>()
			.Post("/DataValidation")
			.Version(1)
			.RequestFromBody();
	}

	public async Task Handle(RequestModel request, CancellationToken ct)
	{
	}
}
