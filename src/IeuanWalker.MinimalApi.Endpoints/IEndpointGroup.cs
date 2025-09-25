using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace IeuanWalker.MinimalApi.Endpoints;

public interface IEndpointGroup
{
	static abstract RouteGroupBuilder Configure(WebApplication app);
}

public static class EndpointGroupExtensions
{
	public static RouteHandlerBuilder Group<TEndpointGroup>(this RouteHandlerBuilder builder) where TEndpointGroup : IEndpointGroup
	{
		return builder;
	}
}
