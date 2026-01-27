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
		/// <summary>
		/// Marks the endpoint request as being bound from the request body. [FromBody]
		/// </summary>
		/// <returns>The same <see cref="RouteHandlerBuilder"/> instance for chaining.</returns>
		public RouteHandlerBuilder RequestFromBody()
		{
			return source;
		}

		/// <summary>
		/// Marks the endpoint request as being bound from a request header. [FromHeader]
		/// </summary>
		/// <param name="name">
		/// The header name to bind from
		/// </param>
		/// <returns>The same <see cref="RouteHandlerBuilder"/> instance for chaining.</returns>
		public RouteHandlerBuilder RequestFromHeader(string? name = null)
		{
			return source;
		}

		/// <summary>
		/// Marks the endpoint request as being bound from the route values. [FromRoute]
		/// </summary>
		/// <param name="name">
		/// The route parameter name to bind from
		/// </param>
		/// <returns>The same <see cref="RouteHandlerBuilder"/> instance for chaining.</returns>
		public RouteHandlerBuilder RequestFromRoute(string? name = null)
		{
			return source;
		}

		/// <summary>
		/// Marks the endpoint request as being bound from the query string. [FromQuery]
		/// </summary>
		/// <param name="name">
		/// The query parameter name to bind from
		/// </param>
		/// <returns>The same <see cref="RouteHandlerBuilder"/> instance for chaining.</returns>
		public RouteHandlerBuilder RequestFromQuery(string? name = null)
		{
			return source;
		}

		/// <summary>
		/// Marks the endpoint request as being bound from form data. [FromForm]
		/// </summary>
		/// <param name="name">
		/// The form field name to bind from
		/// </param>
		/// <returns>The same <see cref="RouteHandlerBuilder"/> instance for chaining.</returns>
		public RouteHandlerBuilder RequestFromForm(string? name = null)
		{
			return source;
		}

		/// <summary>
		/// Marks the endpoint request as being bound from multiple parameters. [AsParameters]
		/// </summary>
		/// <returns>The same <see cref="RouteHandlerBuilder"/> instance for chaining.</returns>
		public RouteHandlerBuilder RequestAsParameters()
		{
			return source;
		}
	}
}
