using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.OutputCaching;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace IeuanWalker.MinimalApi.Endpoints;
#pragma warning restore IDE0130 // Namespace does not match folder structure

[ExcludeFromCodeCoverage]
public static class OutputCachePolicyBuilderExtensions
{
	extension(OutputCachePolicyBuilder source)
	{
		/// <summary>
		/// Adds an output caching policy that allows caching responses for authenticated users.
		/// </summary>
		/// <remarks>
		/// By default, output caching typically avoids caching authenticated responses to prevent accidentally serving
		/// user-specific content to other users. This policy can be used when the response is safe to cache
		/// </remarks>
		/// <returns>The <see cref="OutputCachePolicyBuilder"/> instance for chaining.</returns>
		public OutputCachePolicyBuilder AllowCachingAuthenticatedResponses()
		{
			return source.AddPolicy<AllowAuthenticatedCachePolicy>();
		}
	}
}
