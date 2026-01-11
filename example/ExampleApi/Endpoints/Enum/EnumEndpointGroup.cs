using System.Diagnostics.CodeAnalysis;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Enum;

[ExcludeFromCodeCoverage]
public class EnumEndpointGroup : IEndpointGroup
{
	public static RouteGroupBuilder Configure(WebApplication app)
	{
		return app.MapGroup("api/v{version:apiVersion}/enum");
	}
}
