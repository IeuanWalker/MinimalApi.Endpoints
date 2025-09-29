using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace IeuanWalker.MinimalApi.Endpoints;

public interface IEndpointGroup
{
	static abstract RouteGroupBuilder Configure(WebApplication app);
}

public static class EndpointGroupExtensions
{
	[SuppressMessage("Major Code Smell", "S2326:Unused type parameters should be removed", Justification = "<Pending>")]
	public static RouteHandlerBuilder Group<TEndpointGroup>(this RouteHandlerBuilder builder) where TEndpointGroup : IEndpointGroup
	{
		return builder;
	}
}
