namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer.Core;

/// <summary>
/// Provides utility methods for matching OpenAPI paths to ASP.NET route patterns.
/// </summary>
static class OpenApiPathMatcher
{
	/// <summary>
	/// Determines if two path patterns match, accounting for different format variations.
	/// Handles OpenAPI path format (/api/v1/endpoint/{param}) vs ASP.NET route patterns (/api/v{version:apiVersion}/endpoint/{param}).
	/// </summary>
	/// <param name="openApiPath">The OpenAPI path pattern (e.g., /api/v1/todos/{id}).</param>
	/// <param name="routePattern">The ASP.NET route pattern (e.g., /api/v{version:apiVersion}/todos/{id}).</param>
	/// <returns>True if the paths match, false otherwise.</returns>
	public static bool PathsMatch(string openApiPath, string routePattern)
	{
		// Direct match
		if (string.Equals(openApiPath, routePattern, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		// Normalize both paths for comparison
		// Remove leading/trailing slashes and convert to lowercase
		string normalizedOpenApi = openApiPath.Trim('/').ToLowerInvariant();
		string normalizedRoute = routePattern.Trim('/').ToLowerInvariant();

		// Check if they match after normalization
		if (normalizedOpenApi == normalizedRoute)
		{
			return true;
		}

		// Split by '/' and compare segments
		string[] openApiSegments = normalizedOpenApi.Split('/');
		string[] routeSegments = normalizedRoute.Split('/');

		if (openApiSegments.Length != routeSegments.Length)
		{
			return false;
		}

		// Compare each segment
		for (int i = 0; i < openApiSegments.Length; i++)
		{
			string openApiSeg = openApiSegments[i];
			string routeSeg = routeSegments[i];

			// Exact match
			if (openApiSeg == routeSeg)
			{
				continue;
			}

			// Both are parameters (enclosed in {})
			if (openApiSeg.StartsWith('{') && openApiSeg.EndsWith('}') &&
				routeSeg.StartsWith('{') && routeSeg.EndsWith('}'))
			{
				continue;
			}

			// Check for version parameter matching (e.g., "v1" matches "v{version:apiversion}")
			// This handles the case where OpenAPI has "v1" but route pattern has "v{version:apiVersion}"
			// Ensure the route segment is a version placeholder and the OpenAPI segment matches the expected format
			if (routeSeg.StartsWith("v{") &&
				routeSeg.Contains("version", StringComparison.OrdinalIgnoreCase) &&
				routeSeg.EndsWith('}') &&
				openApiSeg.StartsWith('v') &&
				openApiSeg.Length > 1 &&
				openApiSeg[1..].All(char.IsDigit))
			{
				// OpenAPI segment should be exactly in format "v{digit}+" (e.g., "v1", "v2", "v10")
				// All characters after 'v' must be digits
				continue; // Version placeholder matches versioned path
			}

			// No match
			return false;
		}

		return true;
	}
}
