using System.Diagnostics.CodeAnalysis;
using ExampleApi.Infrastructure;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Validation.PostWithValidationAlterRemove;

public class PostWithValidationAlterRemoveEndpoint : IEndpointWithoutResponse<RequestModel>
{
	[ExcludeFromCodeCoverage]
	public static void Configure(RouteHandlerBuilder builder)
	{
		builder
			.Group<ValidationEndpointGroup>()
			.Post("/WithValidation/AlterAndRemove")
			.Version(1)
			.RequestFromForm()
			.WithSummary("WithValidationAlterAndRemove")
			.WithValidationRules<RequestModel>(x =>
			{
				// Demonstrate Alter: Add a pattern rule then change its error message
				x.Property(p => p.Alter)
					.Pattern(@"^[a-zA-Z0-9]+$")
					.Alter("Must match pattern: ^[a-zA-Z0-9]+$", "Must be alphanumeric");

				// Demonstrate Remove: Add multiple rules then remove one
				x.Property(p => p.Remove1)
					.MinLength(10)
					.MaxLength(100)
					.Remove("Must be 100 characters or fewer");

				// Demonstrate RemoveAll: Add multiple rules then remove all
				x.Property(p => p.RemoveAll)
					.MinLength(10)
					.MaxLength(100)
					.Pattern(@"^[a-zA-Z0-9]+$")
					.RemoveAll();
			});
	}

	public async Task Handle(RequestModel request, CancellationToken ct)
	{
	}
}
