using System.Diagnostics.CodeAnalysis;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Validation;

public class ValidationEndpointGroup : IEndpointGroup
{
	[ExcludeFromCodeCoverage]
	public static RouteGroupBuilder Configure(WebApplication app)
	{
		return app.MapGroup("api/v{version:apiVersion}/validation");
	}
}
