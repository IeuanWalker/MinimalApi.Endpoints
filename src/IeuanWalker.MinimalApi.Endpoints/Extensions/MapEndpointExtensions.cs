using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace IeuanWalker.MinimalApi.Endpoints;
#pragma warning restore IDE0130 // Namespace does not match folder structure

[ExcludeFromCodeCoverage]
public static class MapEndpointExtensions
{
	[ExcludeFromCodeCoverage]
	[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
	public static RouteHandlerBuilder Get(this RouteHandlerBuilder builder, [StringSyntax("Route")] string pattern)
	{
		return builder;
	}
	[ExcludeFromCodeCoverage]
	[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
	public static RouteHandlerBuilder Post(this RouteHandlerBuilder builder, [StringSyntax("Route")] string pattern)
	{
		return builder;
	}
	[ExcludeFromCodeCoverage]
	[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
	public static RouteHandlerBuilder Put(this RouteHandlerBuilder builder, [StringSyntax("Route")] string pattern)
	{
		return builder;
	}
	[ExcludeFromCodeCoverage]
	[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
	public static RouteHandlerBuilder Patch(this RouteHandlerBuilder builder, [StringSyntax("Route")] string pattern)
	{
		return builder;
	}
	[ExcludeFromCodeCoverage]
	[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
	public static RouteHandlerBuilder Delete(this RouteHandlerBuilder builder, [StringSyntax("Route")] string pattern)
	{
		return builder;
	}
}
