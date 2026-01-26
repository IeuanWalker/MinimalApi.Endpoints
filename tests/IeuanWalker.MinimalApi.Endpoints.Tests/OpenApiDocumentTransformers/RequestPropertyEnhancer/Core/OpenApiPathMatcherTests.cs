using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer.Core;

namespace IeuanWalker.MinimalApi.Endpoints.Tests.OpenApiDocumentTransformers.RequestPropertyEnhancer.Core;

public class OpenApiPathMatcherTests
{
	#region Direct Match Tests

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

	[Fact]
	public void PathsMatch_ExactSamePath_ReturnsTrue()
	{
		// Arrange
		string path = "/api/users";

		// Act
		bool result = OpenApiPathMatcher.PathsMatch(path, path);

		// Assert
		result.ShouldBeTrue();
	}

	#endregion

	#region Version Placeholder Tests

	[Theory]
	[InlineData("/api/v1/endpoint", "/api/v{version:apiVersion}/endpoint", true)]
	[InlineData("/api/v10/endpoint", "/api/v{version:apiVersion}/endpoint", true)]
	[InlineData("/api/v1/endpoint/", "api/v{version:apiVersion}/endpoint", true)]
	[InlineData("/api/v99/items", "/api/v{version:apiVersion}/items", true)]
	[InlineData("/api/v2/users/{id}", "/api/v{version:apiVersion}/users/{id}", true)]
	public void PathsMatch_ReturnsTrue_ForVersionPlaceholderMatches(string openApiPath, string routePattern, bool expected)
	{
		// Arrange + Act
		bool result = OpenApiPathMatcher.PathsMatch(openApiPath, routePattern);

		// Assert
		result.ShouldBe(expected);
	}

	#endregion

	#region Segment Count Tests

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
	public void PathsMatch_ReturnsFalse_When_RouteHasFewerSegments()
	{
		// Arrange
		string openApi = "/api/v1/endpoint/extra";
		string route = "/api/v1/endpoint";

		// Act
		bool result = OpenApiPathMatcher.PathsMatch(openApi, route);

		// Assert
		result.ShouldBeFalse();
	}

	#endregion

	#region Invalid Version Formats

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
	public void PathsMatch_ReturnsFalse_ForAlphanumericVersionInOpenApi()
	{
		// Arrange
		string openApi = "/api/v1a/endpoint"; // 'v1a' contains non-digits after v
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

	#endregion

	#region Empty and Single Segment Paths

	[Theory]
	[InlineData("", "", true)]
	[InlineData("/", "/", true)]
	[InlineData("", "/", true)]
	public void PathsMatch_EmptyPaths_ReturnsTrue(string openApi, string route, bool expected)
	{
		// Act
		bool result = OpenApiPathMatcher.PathsMatch(openApi, route);

		// Assert
		result.ShouldBe(expected);
	}

	[Fact]
	public void PathsMatch_SingleSegmentPaths_ReturnsTrue()
	{
		// Arrange
		string openApi = "/users";
		string route = "/users";

		// Act
		bool result = OpenApiPathMatcher.PathsMatch(openApi, route);

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public void PathsMatch_SingleSegmentParameter_ReturnsTrue()
	{
		// Arrange
		string openApi = "/{id}";
		string route = "/{userId}";

		// Act
		bool result = OpenApiPathMatcher.PathsMatch(openApi, route);

		// Assert
		result.ShouldBeTrue();
	}

	#endregion

	#region Multiple Parameters

	[Fact]
	public void PathsMatch_MultipleParameters_ReturnsTrue()
	{
		// Arrange
		string openApi = "/api/users/{userId}/orders/{orderId}";
		string route = "/api/users/{id}/orders/{orderNumber}";

		// Act
		bool result = OpenApiPathMatcher.PathsMatch(openApi, route);

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public void PathsMatch_MultipleParametersWithVersion_ReturnsTrue()
	{
		// Arrange
		string openApi = "/api/v1/users/{userId}/items/{itemId}";
		string route = "/api/v{version:apiVersion}/users/{id}/items/{iid}";

		// Act
		bool result = OpenApiPathMatcher.PathsMatch(openApi, route);

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public void PathsMatch_ConsecutiveParameters_ReturnsTrue()
	{
		// Arrange
		string openApi = "/{a}/{b}/{c}";
		string route = "/{x}/{y}/{z}";

		// Act
		bool result = OpenApiPathMatcher.PathsMatch(openApi, route);

		// Assert
		result.ShouldBeTrue();
	}

	#endregion

	#region Case Sensitivity

	[Fact]
	public void PathsMatch_CaseInsensitive_ReturnsTrue()
	{
		// Arrange
		string openApi = "/API/USERS";
		string route = "/api/users";

		// Act
		bool result = OpenApiPathMatcher.PathsMatch(openApi, route);

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public void PathsMatch_MixedCase_ReturnsTrue()
	{
		// Arrange
		string openApi = "/Api/UsErS/{Id}";
		string route = "/api/USERS/{id}";

		// Act
		bool result = OpenApiPathMatcher.PathsMatch(openApi, route);

		// Assert
		result.ShouldBeTrue();
	}

	#endregion

	#region Trailing Slash Normalization

	[Theory]
	[InlineData("/api/users/", "/api/users", true)]
	[InlineData("/api/users", "/api/users/", true)]
	[InlineData("/api/users/", "/api/users/", true)]
	public void PathsMatch_TrailingSlashNormalization_ReturnsTrue(string openApi, string route, bool expected)
	{
		// Act
		bool result = OpenApiPathMatcher.PathsMatch(openApi, route);

		// Assert
		result.ShouldBe(expected);
	}

	#endregion

	#region Complex Scenarios

	[Fact]
	public void PathsMatch_ComplexRealWorldPath_ReturnsTrue()
	{
		// Arrange
		string openApi = "/api/v1/organizations/{orgId}/projects/{projId}/tasks/{taskId}";
		string route = "/api/v{version:apiVersion}/organizations/{organizationId}/projects/{projectId}/tasks/{id}";

		// Act
		bool result = OpenApiPathMatcher.PathsMatch(openApi, route);

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public void PathsMatch_DifferentLiteralSegments_ReturnsFalse()
	{
		// Arrange
		string openApi = "/api/users/{id}";
		string route = "/api/customers/{id}";

		// Act
		bool result = OpenApiPathMatcher.PathsMatch(openApi, route);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void PathsMatch_ParameterConstraints_ReturnsTrue()
	{
		// Arrange - route constraints like {id:int} should still match {id}
		string openApi = "/api/users/{id}";
		string route = "/api/users/{id:int}";

		// Act
		bool result = OpenApiPathMatcher.PathsMatch(openApi, route);

		// Assert - both are parameter segments so they should match
		result.ShouldBeTrue();
	}

	#endregion
}
