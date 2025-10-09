using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace IeuanWalker.MinimalApi.Endpoints;
#pragma warning restore IDE0130 // Namespace does not match folder structure

[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
[SuppressMessage("Roslynator", "RCS1163:Unused parameter", Justification = "<Pending>")]
public static class RequestBindingTypeExtensions
{
	[ExcludeFromCodeCoverage]
	public static RouteHandlerBuilder RequestFromBody(this RouteHandlerBuilder builder)
	{
		return builder;
	}

	[ExcludeFromCodeCoverage]
	public static RouteHandlerBuilder RequestFromHeader(this RouteHandlerBuilder builder, string? name = null)
	{
		return builder;
	}
	[ExcludeFromCodeCoverage]
	public static RouteHandlerBuilder RequestFromRoute(this RouteHandlerBuilder builder, string? name = null)
	{
		return builder;
	}

	[ExcludeFromCodeCoverage]
	public static RouteHandlerBuilder RequestFromQuery(this RouteHandlerBuilder builder, string? name = null)
	{
		return builder;
	}

	[ExcludeFromCodeCoverage]
	public static RouteHandlerBuilder RequestFromForm(this RouteHandlerBuilder builder, string? name = null)
	{
		return builder;
	}

	[ExcludeFromCodeCoverage]
	public static RouteHandlerBuilder RequestAsParameters(this RouteHandlerBuilder builder)
	{
		return builder;
	}
}
