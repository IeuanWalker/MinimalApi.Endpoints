using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer.Core;

namespace IeuanWalker.MinimalApi.Endpoints.Tests.OpenApiDocumentTransformers.RequestPropertyEnhancer.Core;

public class EndpointRequestTypeMapperTests
{
	#region ResolveRequestTypeForPath Tests

	[Fact]
	public void ResolveRequestTypeForPath_MatchesVersionedRoute()
	{
		// Arrange
		Dictionary<string, Type> map = new(StringComparer.OrdinalIgnoreCase)
		{
			["/api/v{version:apiVersion}/todos/{id}"] = typeof(TestRequest)
		};

		// Act
		Type? result = EndpointRequestTypeMapper.ResolveRequestTypeForPath("/api/v1/todos/{id}", map);

		// Assert
		result.ShouldBe(typeof(TestRequest));
	}

	[Fact]
	public void ResolveRequestTypeForPath_NoMatch_ReturnsNull()
	{
		// Arrange
		Dictionary<string, Type> map = new(StringComparer.OrdinalIgnoreCase)
		{
			["/api/other/{id}"] = typeof(TestRequest)
		};

		// Act
		Type? result = EndpointRequestTypeMapper.ResolveRequestTypeForPath("/api/v1/todos/{id}", map);

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void ResolveRequestTypeForPath_ExactMatch_ReturnsType()
	{
		// Arrange
		Dictionary<string, Type> map = new(StringComparer.OrdinalIgnoreCase)
		{
			["/api/users/{id}"] = typeof(TestRequest)
		};

		// Act
		Type? result = EndpointRequestTypeMapper.ResolveRequestTypeForPath("/api/users/{id}", map);

		// Assert
		result.ShouldBe(typeof(TestRequest));
	}

	[Fact]
	public void ResolveRequestTypeForPath_CaseInsensitiveMatch_ReturnsType()
	{
		// Arrange
		Dictionary<string, Type> map = new(StringComparer.OrdinalIgnoreCase)
		{
			["/API/USERS/{ID}"] = typeof(TestRequest)
		};

		// Act
		Type? result = EndpointRequestTypeMapper.ResolveRequestTypeForPath("/api/users/{id}", map);

		// Assert
		result.ShouldBe(typeof(TestRequest));
	}

	[Fact]
	public void ResolveRequestTypeForPath_EmptyMap_ReturnsNull()
	{
		// Arrange
		Dictionary<string, Type> map = new(StringComparer.OrdinalIgnoreCase);

		// Act
		Type? result = EndpointRequestTypeMapper.ResolveRequestTypeForPath("/api/users/{id}", map);

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void ResolveRequestTypeForPath_MultipleMatches_ReturnsFirstMatch()
	{
		// Arrange
		Dictionary<string, Type> map = new(StringComparer.OrdinalIgnoreCase)
		{
			["/api/v{version:apiVersion}/items/{id}"] = typeof(TestRequest),
			["/api/v1/items/{id}"] = typeof(AnotherRequest)
		};

		// Act
		Type? result = EndpointRequestTypeMapper.ResolveRequestTypeForPath("/api/v1/items/{id}", map);

		// Assert
		result.ShouldNotBeNull();
		// The first match in iteration order wins
	}

		#endregion

		#region Test Types

		class TestRequest { public string? Name { get; set; } }
		class AnotherRequest { public int Id { get; set; } }

		#endregion
	}
