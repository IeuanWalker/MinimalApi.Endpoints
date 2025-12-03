using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace IeuanWalker.MinimalApi.Endpoints;
#pragma warning restore IDE0130 // Namespace does not match folder structure

[ExcludeFromCodeCoverage]
public static class RequestBindingTypeExtensions
{
	[ExcludeFromCodeCoverage]
	public static RouteHandlerBuilder RequestFromBody(this RouteHandlerBuilder builder)
	{
		return builder;
	}

	[ExcludeFromCodeCoverage]
	[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
	public static RouteHandlerBuilder RequestFromHeader(this RouteHandlerBuilder builder, string? name = null)
	{
		return builder;
	}
	[ExcludeFromCodeCoverage]
	[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
	public static RouteHandlerBuilder RequestFromRoute(this RouteHandlerBuilder builder, string? name = null)
	{
		return builder;
	}

	[ExcludeFromCodeCoverage]
	[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
	public static RouteHandlerBuilder RequestFromQuery(this RouteHandlerBuilder builder, string? name = null)
	{
		return builder;
	}

	[ExcludeFromCodeCoverage]
	[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
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
