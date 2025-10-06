using System.Diagnostics.CodeAnalysis;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Todos;

public class TodoEndpointGroup : IEndpointGroup
{
	[ExcludeFromCodeCoverage]
	public static RouteGroupBuilder Configure(WebApplication app)
	{
		return app.MapGroup("api/v{version:apiVersion}/todos");
	}
}
