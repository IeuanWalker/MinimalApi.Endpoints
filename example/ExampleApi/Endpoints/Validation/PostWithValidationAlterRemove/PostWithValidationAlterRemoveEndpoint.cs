using System.Diagnostics.CodeAnalysis;
using ExampleApi.Infrastructure;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Validation.PostWithValidationAlterRemove;

[ExcludeFromCodeCoverage]
public class PostWithValidationAlterRemoveEndpoint : IEndpointWithoutResponse<RequestModel>
{
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
				// Demonstrate Alter: Change the FluentValidation pattern error message
				x.Property(p => p.Alter).Alter("Must match pattern: ^[a-zA-Z0-9]+$", "Must be alphanumeric");

				// Demonstrate Remove: Remove the MaxLength rule from FluentValidation
				x.Property(p => p.Remove1).Remove("Must be 100 characters or fewer");

				// Demonstrate RemoveAll: Remove all FluentValidation rules for this property
				x.Property(p => p.RemoveAll).RemoveAll();
			});
	}

	public async Task Handle(RequestModel request, CancellationToken ct)
	{
	}
}
