using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Routing;

namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer.Core;

/// <summary>
/// Provides utilities for mapping endpoints to their request types.
/// This is used by both TypeDocumentTransformer and ValidationDocumentTransformer to avoid duplicate lookups.
/// </summary>
static class EndpointRequestTypeMapper
{
	/// <summary>
	/// Builds a mapping from route patterns to their request types.
	/// </summary>
	/// <param name="context">The OpenAPI document transformer context.</param>
	/// <param name="filterPredicate">Optional predicate to filter which types to include.</param>
	/// <returns>A dictionary mapping route patterns to request types.</returns>
	public static Dictionary<string, Type> BuildEndpointToRequestTypeMapping(
		OpenApiDocumentTransformerContext context,
		Func<Type, bool>? filterPredicate = null)
	{
		Dictionary<string, Type> mapping = new(StringComparer.OrdinalIgnoreCase);

		if (context.ApplicationServices.GetService(typeof(EndpointDataSource)) is not EndpointDataSource endpointDataSource)
		{
			return mapping;
		}

		foreach (Endpoint endpoint in endpointDataSource.Endpoints)
		{
			if (endpoint is not RouteEndpoint routeEndpoint)
			{
				continue;
			}

			string? routePattern = routeEndpoint.RoutePattern.RawText;
			if (string.IsNullOrEmpty(routePattern))
			{
				continue;
			}

			MethodInfo? handlerMethod = routeEndpoint.Metadata.OfType<MethodInfo>().FirstOrDefault();
			if (handlerMethod is null)
			{
				continue;
			}

			ParameterInfo[] parameters = handlerMethod.GetParameters();

			Type? requestType = parameters
				.Select(param => param.ParameterType)
				.FirstOrDefault(paramType =>
					!paramType.IsPrimitive &&
					paramType != typeof(string) &&
					paramType != typeof(CancellationToken) &&
					(filterPredicate is null || filterPredicate(paramType)));

			if (requestType is not null && !mapping.ContainsKey(routePattern))
			{
				mapping[routePattern] = requestType;
			}
		}

		return mapping;
	}

	/// <summary>
	/// Resolves the request type for a given OpenAPI path pattern.
	/// </summary>
	/// <param name="pathPattern">The OpenAPI path pattern to resolve.</param>
	/// <param name="endpointToRequestType">The endpoint-to-request-type mapping.</param>
	/// <returns>The request type for the path, or null if not found.</returns>
	public static Type? ResolveRequestTypeForPath(string pathPattern, Dictionary<string, Type> endpointToRequestType)
	{
		return endpointToRequestType
			.Where(mapping => OpenApiPathMatcher.PathsMatch(pathPattern, mapping.Key))
			.Select(mapping => mapping.Value)
			.FirstOrDefault();
	}
}
