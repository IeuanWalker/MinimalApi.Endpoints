using System.Diagnostics.CodeAnalysis;
using ExampleApi.Infrastructure;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Validation.PostFluentValidationFromFrom;

public class PostFluentValidationFromFromEndpoint : IEndpointWithoutResponse<RequestModel>
{
	[ExcludeFromCodeCoverage]
	public static void Configure(RouteHandlerBuilder builder)
	{
		builder
			.Group<ValidationEndpointGroup>()
			.Post("/FluentValidation/FromForm")
			.Version(1)
			.RequestFromForm();
	}

	public async Task Handle(RequestModel request, CancellationToken ct)
	{
	}
}
