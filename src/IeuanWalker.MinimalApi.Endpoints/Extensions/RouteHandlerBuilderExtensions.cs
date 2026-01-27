using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace IeuanWalker.MinimalApi.Endpoints;
#pragma warning restore IDE0130 // Namespace does not match folder structure

[ExcludeFromCodeCoverage]
public static class RouteHandlerBuilderExtensions
{
	extension(RouteHandlerBuilder source)
	{
		/// <summary>
		/// Requires the current endpoint to be authorized with at least one of the specified roles.
		/// </summary>
		/// <param name="roles">The allowed roles for the endpoint.</param>
		/// <returns>The same <see cref="RouteHandlerBuilder"/> instance for chaining.</returns>
		public RouteHandlerBuilder RequireRoles(params string[] roles)
		{
			return source.RequireAuthorization(p =>
			{
				p.RequireRole(roles);
			});
		}
	}
}
