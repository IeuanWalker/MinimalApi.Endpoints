using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.TypeExamples;

public class TypeExamplesEndpointGroup : IEndpointGroup
{
	public static RouteGroupBuilder Configure(WebApplication app)
	{
		return app.MapGroup("api/v{version:apiVersion}/TypeExamples")
			.WithTags("Type Examples")
			.WithDescription("Endpoints demonstrating all types handled by TypeDocumentTransformer");
	}
}
