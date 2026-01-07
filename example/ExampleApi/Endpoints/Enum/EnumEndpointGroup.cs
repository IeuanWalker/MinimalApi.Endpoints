using System.Diagnostics.CodeAnalysis;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Enum;

public class EnumEndpointGroup : IEndpointGroup
{
	[ExcludeFromCodeCoverage]
	public static RouteGroupBuilder Configure(WebApplication app)
	{
		return app.MapGroup("api/v{version:apiVersion}/enum");
	}
}
