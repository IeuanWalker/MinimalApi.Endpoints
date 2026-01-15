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
		// Arrange + Act
		bool result = OpenApiPathMatcher.PathsMatch(openApiPath, routePattern);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("/api/v1/endpoint", "/api/v{version:apiVersion}/endpoint", true)]
	[InlineData("/api/v10/endpoint", "/api/v{version:apiVersion}/endpoint", true)]
	[InlineData("/api/v1/endpoint/", "api/v{version:apiVersion}/endpoint", true)]
	public void PathsMatch_ReturnsTrue_ForVersionPlaceholderMatches(string openApiPath, string routePattern, bool expected)
	{
		// Arrange + Act
		bool result = OpenApiPathMatcher.PathsMatch(openApiPath, routePattern);

		// Assert
		result.ShouldBe(expected);
	}

	[Fact]
	public void PathsMatch_ReturnsFalse_When_SegmentCountDiffers()
	{
		// Arrange
		string openApi = "/api/v1/endpoint";
		string route = "/api/v{version:apiVersion}/endpoint/extra";

		// Act
		bool result = OpenApiPathMatcher.PathsMatch(openApi, route);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void PathsMatch_ReturnsFalse_ForNonDigitVersionInOpenApi()
	{
		// Arrange
		string openApi = "/api/vx/endpoint"; // 'vx' is not a digit-only version
		string route = "/api/v{version:apiVersion}/endpoint";

		// Act
		bool result = OpenApiPathMatcher.PathsMatch(openApi, route);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void PathsMatch_ReturnsFalse_ForLiteralSegmentAgainstParameter()
	{
		// Arrange
		string openApi = "/api/todos/1";
		string route = "/api/todos/{id}";

		// Act
		bool result = OpenApiPathMatcher.PathsMatch(openApi, route);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void PathsMatch_ReturnsFalse_When_RouteVersionPlaceholderDoesNotContainVersionKeyword()
	{
		// Arrange
		string openApi = "/api/v1/endpoint";
		string route = "/api/v{ver:api}/endpoint"; // does not contain 'version' substring

		// Act
		bool result = OpenApiPathMatcher.PathsMatch(openApi, route);

		// Assert
		result.ShouldBeFalse();
	}
}
