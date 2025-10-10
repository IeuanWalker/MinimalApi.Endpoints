using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace IeuanWalker.MinimalApi.Endpoints;
#pragma warning restore IDE0130 // Namespace does not match folder structure

[ExcludeFromCodeCoverage]
public static class EndpointGroupExtensions
{
	[SuppressMessage("Major Code Smell", "S2326:Unused type parameters should be removed", Justification = "<Pending>")]
	[ExcludeFromCodeCoverage]
	public static RouteHandlerBuilder Group<TEndpointGroup>(this RouteHandlerBuilder builder) where TEndpointGroup : IEndpointGroup
	{
		return builder;
	}
}
