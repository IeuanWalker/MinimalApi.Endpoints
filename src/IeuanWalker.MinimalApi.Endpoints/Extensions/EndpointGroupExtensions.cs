using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace IeuanWalker.MinimalApi.Endpoints;
#pragma warning restore IDE0130 // Namespace does not match folder structure

[ExcludeFromCodeCoverage]
public static class EndpointGroupExtensions
{
	extension(RouteHandlerBuilder source)
	{
		/// <summary>
		/// Marks the current endpoint as belonging to the specified endpoint group.
		/// </summary>
		/// <typeparam name="TEndpointGroup">
		/// The endpoint group type to associate with the endpoint.
		/// </typeparam>
		/// <returns>The same <see cref="RouteHandlerBuilder"/> instance for chaining.</returns>
		public RouteHandlerBuilder Group<TEndpointGroup>() where TEndpointGroup : IEndpointGroup
		{
			return source;
		}
	}
}
