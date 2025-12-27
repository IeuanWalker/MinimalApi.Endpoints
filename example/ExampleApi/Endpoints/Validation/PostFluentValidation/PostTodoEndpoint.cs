using System.Diagnostics.CodeAnalysis;
using ExampleApi.Infrastructure;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Validation.PostFluentValidation;

public class PostTodoEndpoint : IEndpointWithoutResponse<RequestModel>
{
	[ExcludeFromCodeCoverage]
	public static void Configure(RouteHandlerBuilder builder)
	{
		builder
			.Group<ValidationEndpointGroup>()
			.Post("/FluentValidation")
			.RequestFromBody()
			.Version(1.0);
	}

	public async Task Handle(RequestModel request, CancellationToken ct)
	{
	}
}
