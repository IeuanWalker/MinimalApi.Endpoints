using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Primitives;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace IeuanWalker.MinimalApi.Endpoints;
#pragma warning restore IDE0130 // Namespace does not match folder structure

[ExcludeFromCodeCoverage]
sealed class AllowAuthenticatedCachePolicy : IOutputCachePolicy
{
	public ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellation)
	{
		context.EnableOutputCaching = true;
		context.AllowCacheLookup = true;
		context.AllowCacheStorage = true;
		context.AllowLocking = true;

		return ValueTask.CompletedTask;
	}

	public ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellation)
	{
		// Nothing special to do when serving from cache for this policy.
		return ValueTask.CompletedTask;
	}

	public ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellation)
	{
		HttpResponse response = context.HttpContext.Response;

		// If the response sets cookies, don't store it in the cache.
		if (!StringValues.IsNullOrEmpty(response.Headers.SetCookie))
		{
			context.AllowCacheStorage = false;
			return ValueTask.CompletedTask;
		}

		// Only cache successful 200 OK responses.
		if (response.StatusCode != StatusCodes.Status200OK)
		{
			context.AllowCacheStorage = false;
			return ValueTask.CompletedTask;
		}

		return ValueTask.CompletedTask;
	}
}
