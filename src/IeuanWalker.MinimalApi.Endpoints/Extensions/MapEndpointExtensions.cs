using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace IeuanWalker.MinimalApi.Endpoints;
#pragma warning restore IDE0130 // Namespace does not match folder structure

[ExcludeFromCodeCoverage]
[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
public static class MapEndpointExtensions
{
	extension(RouteHandlerBuilder source)
	{
		public RouteHandlerBuilder Get([StringSyntax("Route")] string? pattern = null)
		{
			return source;
		}
		public RouteHandlerBuilder Post([StringSyntax("Route")] string? pattern = null)
		{
			return source;
		}
		public RouteHandlerBuilder Put([StringSyntax("Route")] string? pattern = null)
		{
			return source;
		}
		public RouteHandlerBuilder Patch([StringSyntax("Route")] string? pattern = null)
		{
			return source;
		}
		public RouteHandlerBuilder Delete([StringSyntax("Route")] string? pattern = null)
		{
			return source;
		}
	}
}
