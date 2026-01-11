using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace IeuanWalker.MinimalApi.Endpoints;
#pragma warning restore IDE0130 // Namespace does not match folder structure

[ExcludeFromCodeCoverage]
[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
public static class RequestBindingTypeExtensions
{
	extension(RouteHandlerBuilder source)
	{
		public RouteHandlerBuilder RequestFromBody()
		{
			return source;
		}

		public RouteHandlerBuilder RequestFromHeader(string? name = null)
		{
			return source;
		}

		public RouteHandlerBuilder RequestFromRoute(string? name = null)
		{
			return source;
		}

		public RouteHandlerBuilder RequestFromQuery(string? name = null)
		{
			return source;
		}

		public RouteHandlerBuilder RequestFromForm(string? name = null)
		{
			return source;
		}

		public RouteHandlerBuilder RequestAsParameters()
		{
			return source;
		}
	}
}
