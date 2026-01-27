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
		public OutputCachePolicyBuilder AllowCachingAuthenticatedResponses()
		{
			return source.AddPolicy<AllowAuthenticatedCachePolicy>();
		}
	}
}
