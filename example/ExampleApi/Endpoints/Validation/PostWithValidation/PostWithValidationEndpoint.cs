using System.Diagnostics.CodeAnalysis;
using ExampleApi.Infrastructure;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Validation.PostWithValidation;

[ExcludeFromCodeCoverage]
public class PostWithValidationEndpoint : IEndpointWithoutResponse<RequestModel>
{
	public static void Configure(RouteHandlerBuilder builder)
	{
		builder
			.Group<ValidationEndpointGroup>()
			.Post("/WithValidation")
			.Version(1)
			.RequestFromBody()
			.WithValidationRules<RequestModel>(config =>
			{
				// String validation
				config.Property(x => x.StringMin).MinLength(3);
				config.Property(x => x.StringMax).MaxLength(50);
				config.Property(x => x.StringRange).MinLength(3).MaxLength(50);
				config.Property(x => x.StringPattern).Pattern(@"^[a-zA-Z0-9]+$");

				// Integer validation - using custom rules
				config.Property(x => x.IntMin).Custom("Must be >= 1");
				config.Property(x => x.IntMax).Custom("Must be <= 100");
				config.Property(x => x.IntRange).Custom("Must be between 1 and 100");

				// Double validation - using custom rules
				config.Property(x => x.DoubleMin).Custom("Must be >= 0.1");
				config.Property(x => x.DoubleMax).Custom("Must be <= 99.9");
				config.Property(x => x.DoubleRange).Custom("Must be between 0.1 and 99.9");

				// List count validation (custom rules for demonstration)
				config.Property(x => x.ListStringMinCount).Custom("Must contain at least 1 item.");
				config.Property(x => x.ListStringMaxCount).Custom("Must contain at most 10 items.");
				config.Property(x => x.ListStringRangeCount).Custom("Must contain between 1 and 10 items.");

				config.Property(x => x.ListIntMinCount).Custom("Must contain at least 1 item.");
				config.Property(x => x.ListIntMaxCount).Custom("Must contain at most 10 items.");
				config.Property(x => x.ListIntRangeCount).Custom("Must contain between 1 and 10 items.");

				// Nested object validation
				config.Property(x => x.NestedObject).Required();

				// Note: Nested object properties would need their own validation rules
				// or would be validated by a separate validator for the NestedObjectModel type
			});
	}

	public async Task Handle(RequestModel request, CancellationToken ct)
	{
	}
}
