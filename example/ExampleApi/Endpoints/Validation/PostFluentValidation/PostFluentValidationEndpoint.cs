using System.Diagnostics.CodeAnalysis;
using ExampleApi.Infrastructure;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Validation.PostFluentValidation;

public class PostFluentValidationEndpoint : IEndpointWithoutResponse<RequestModel>
{
	[ExcludeFromCodeCoverage]
	public static void Configure(RouteHandlerBuilder builder)
	{
		builder
			.Group<ValidationEndpointGroup>()
			.Post("/FluentValidation")
			.Version(1)
			.RequestFromBody();
	}

	public async Task Handle(RequestModel request, CancellationToken ct)
	{
	}
}
