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
		/// <summary>
		/// Marks the endpoint as handling HTTP GET requests, optionally specifying a route pattern.
		/// </summary>
		/// <returns>The same <see cref="RouteHandlerBuilder"/> instance for chaining.</returns>
		public RouteHandlerBuilder Get([StringSyntax("Route")] string? pattern = null)
		{
			return source;
		}

		/// <summary>
		/// Marks the endpoint as handling HTTP POST requests, optionally specifying a route pattern.
		/// </summary>
		/// <returns>The same <see cref="RouteHandlerBuilder"/> instance for chaining.</returns>
		public RouteHandlerBuilder Post([StringSyntax("Route")] string? pattern = null)
		{
			return source;
		}

		/// <summary>
		/// Marks the endpoint as handling HTTP PUT requests, optionally specifying a route pattern.
		/// </summary>
		/// <returns>The same <see cref="RouteHandlerBuilder"/> instance for chaining.</returns>
		public RouteHandlerBuilder Put([StringSyntax("Route")] string? pattern = null)
		{
			return source;
		}

		/// <summary>
		/// Marks the endpoint as handling HTTP PATCH requests, optionally specifying a route pattern.
		/// </summary>
		/// <returns>The same <see cref="RouteHandlerBuilder"/> instance for chaining.</returns>
		public RouteHandlerBuilder Patch([StringSyntax("Route")] string? pattern = null)
		{
			return source;
		}

		/// <summary>
		/// Marks the endpoint as handling HTTP DELETE requests, optionally specifying a route pattern.
		/// </summary>
		/// <returns>The same <see cref="RouteHandlerBuilder"/> instance for chaining.</returns>
		public RouteHandlerBuilder Delete([StringSyntax("Route")] string? pattern = null)
		{
			return source;
		}
	}
}
