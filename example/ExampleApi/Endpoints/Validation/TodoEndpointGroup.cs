using System.Diagnostics.CodeAnalysis;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Validation;

[ExcludeFromCodeCoverage]
public class ValidationEndpointGroup : IEndpointGroup
{
	public static RouteGroupBuilder Configure(WebApplication app)
	{
		return app.MapGroup("api/v{version:apiVersion}/validation");
	}
}
