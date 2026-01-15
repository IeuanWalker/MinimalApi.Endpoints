using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer.Core;

namespace IeuanWalker.MinimalApi.Endpoints.Tests.OpenApiDocumentTransformers.RequestPropertyEnhancer.Core;

public class OpenApiPathMatcherTests
{
	[Theory]
	[InlineData("/api/v1/todos/{id}", "/api/v1/todos/{id}", true)]
	[InlineData("/api/v1/todos/{id}/", "/API/V1/TODOS/{id}", true)]
	[InlineData("/api/todos/{id}", "/api/todos/{todoId}", true)]
	public void PathsMatch_ReturnsTrue_ForDirectAndNormalizedAndParameterMatches(string openApiPath, string routePattern, bool expected)
	{
		bool result = OpenApiPathMatcher.PathsMatch(openApiPath, routePattern);

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData("/api/v1/endpoint", "/api/v{version:apiVersion}/endpoint", true)]
	[InlineData("/api/v10/endpoint", "/api/v{version:apiVersion}/endpoint", true)]
	[InlineData("/api/v1/endpoint/", "api/v{version:apiVersion}/endpoint", true)]
	public void PathsMatch_ReturnsTrue_ForVersionPlaceholderMatches(string openApiPath, string routePattern, bool expected)
	{
		bool result = OpenApiPathMatcher.PathsMatch(openApiPath, routePattern);

		Assert.Equal(expected, result);
	}

	[Fact]
	public void PathsMatch_ReturnsFalse_When_SegmentCountDiffers()
	{
		string openApi = "/api/v1/endpoint";
		string route = "/api/v{version:apiVersion}/endpoint/extra";

		bool result = OpenApiPathMatcher.PathsMatch(openApi, route);

		Assert.False(result);
	}

	[Fact]
	public void PathsMatch_ReturnsFalse_ForNonDigitVersionInOpenApi()
	{
		string openApi = "/api/vx/endpoint"; // 'vx' is not a digit-only version
		string route = "/api/v{version:apiVersion}/endpoint";

		bool result = OpenApiPathMatcher.PathsMatch(openApi, route);

		Assert.False(result);
	}

	[Fact]
	public void PathsMatch_ReturnsFalse_ForLiteralSegmentAgainstParameter()
	{
		string openApi = "/api/todos/1";
		string route = "/api/todos/{id}";

		bool result = OpenApiPathMatcher.PathsMatch(openApi, route);

		Assert.False(result);
	}

	[Fact]
	public void PathsMatch_ReturnsFalse_When_RouteVersionPlaceholderDoesNotContainVersionKeyword()
	{
		string openApi = "/api/v1/endpoint";
		string route = "/api/v{ver:api}/endpoint"; // does not contain 'version' substring

		bool result = OpenApiPathMatcher.PathsMatch(openApi, route);

		Assert.False(result);
	}
}
