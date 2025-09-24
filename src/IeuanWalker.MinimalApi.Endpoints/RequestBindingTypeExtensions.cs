using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;

namespace IeuanWalker.MinimalApi.Endpoints;

[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
[SuppressMessage("Roslynator", "RCS1163:Unused parameter", Justification = "<Pending>")]
public static class RequestBindingTypeExtensions
{
	public static RouteHandlerBuilder RequestFromBody(this RouteHandlerBuilder builder)
	{
		return builder;
	}

	public static RouteHandlerBuilder RequestFromHeader(this RouteHandlerBuilder builder, string? name = null)
	{
		return builder;
	}
	public static RouteHandlerBuilder RequestFromRoute(this RouteHandlerBuilder builder, string? name = null)
	{
		return builder;
	}

	public static RouteHandlerBuilder RequestFromQuery(this RouteHandlerBuilder builder, string? name = null)
	{
		return builder;
	}

	public static RouteHandlerBuilder RequestFromForm(this RouteHandlerBuilder builder, string? name = null)
	{
		return builder;
	}

	public static RouteHandlerBuilder RequestAsParameters(this RouteHandlerBuilder builder)
	{
		return builder;
	}
}