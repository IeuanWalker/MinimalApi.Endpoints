using System.Reflection;
using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace IeuanWalker.MinimalApi.Endpoints.Tests.OpenApiDocumentTransformers.RequestPropertyEnhancer.Core;

public class EndpointRequestTypeMapperTests
{
	#region BuildEndpointToRequestTypeMapping - Basic Tests

	[Fact]
	public void BuildEndpointToRequestTypeMapping_NoEndpointDataSource_ReturnsEmpty()
	{
		// Arrange
		OpenApiDocumentTransformerContext ctx = new()
		{
			DocumentName = "v1",
			DescriptionGroups = [],
			ApplicationServices = new ServiceCollection().BuildServiceProvider()
		};

		// Act
		Dictionary<string, Type> mapping = EndpointRequestTypeMapper.BuildEndpointToRequestTypeMapping(ctx);

		// Assert
		mapping.ShouldNotBeNull();
		mapping.Count.ShouldBe(0);
	}

	[Fact]
	public void BuildEndpointToRequestTypeMapping_WithEndpoint_ReturnsRequestType()
	{
		// Arrange
		MethodInfo handler = ((Action<TestRequest, CancellationToken>)HandleWithRequest).Method;
		RouteEndpoint ep = CreateRouteEndpoint("/api/todos", handler);
		TestEndpointDataSource ds = new([ep]);
		OpenApiDocumentTransformerContext ctx = CreateContextWithEndpointDataSource(ds);

		// Act
		Dictionary<string, Type> mapping = EndpointRequestTypeMapper.BuildEndpointToRequestTypeMapping(ctx);

		// Assert
		ep.RoutePattern.RawText.ShouldNotBeNull();
		mapping.Count.ShouldBe(1);
		mapping.ContainsKey(ep.RoutePattern.RawText).ShouldBeTrue();
		mapping[ep.RoutePattern.RawText].ShouldBe(typeof(TestRequest));
	}

	[Fact]
	public void BuildEndpointToRequestTypeMapping_FilterPredicate_Applies()
	{
		// Arrange
		MethodInfo handler = ((Action<TestRequest, CancellationToken>)HandleWithRequest).Method;
		RouteEndpoint ep = CreateRouteEndpoint("/api/todos", handler);
		TestEndpointDataSource ds = new([ep]);
		OpenApiDocumentTransformerContext ctx = CreateContextWithEndpointDataSource(ds);

		// Act
		Dictionary<string, Type> mapping = EndpointRequestTypeMapper.BuildEndpointToRequestTypeMapping(ctx, t => false);

		// Assert
		mapping.Count.ShouldBe(0);
	}

	#endregion

	#region BuildEndpointToRequestTypeMapping - Multiple Endpoints Tests

	[Fact]
	public void BuildEndpointToRequestTypeMapping_MultipleEndpoints_ReturnsMappingForEach()
	{
		// Arrange
		MethodInfo handler1 = ((Action<TestRequest, CancellationToken>)HandleWithRequest).Method;
		MethodInfo handler2 = ((Action<AnotherRequest, CancellationToken>)HandleWithAnotherRequest).Method;

		RouteEndpoint ep1 = CreateRouteEndpoint("/api/todos", handler1);
		RouteEndpoint ep2 = CreateRouteEndpoint("/api/users", handler2);

		TestEndpointDataSource ds = new([ep1, ep2]);
		OpenApiDocumentTransformerContext ctx = CreateContextWithEndpointDataSource(ds);

		// Act
		Dictionary<string, Type> mapping = EndpointRequestTypeMapper.BuildEndpointToRequestTypeMapping(ctx);

		// Assert
		mapping.Count.ShouldBe(2);
		mapping["/api/todos"].ShouldBe(typeof(TestRequest));
		mapping["/api/users"].ShouldBe(typeof(AnotherRequest));
	}

	[Fact]
	public void BuildEndpointToRequestTypeMapping_DuplicateRoutePattern_KeepsFirstMapping()
	{
		// Arrange
		MethodInfo handler1 = ((Action<TestRequest, CancellationToken>)HandleWithRequest).Method;
		MethodInfo handler2 = ((Action<AnotherRequest, CancellationToken>)HandleWithAnotherRequest).Method;

		RouteEndpoint ep1 = CreateRouteEndpoint("/api/items", handler1);
		RouteEndpoint ep2 = CreateRouteEndpoint("/api/items", handler2); // Same route pattern

		TestEndpointDataSource ds = new([ep1, ep2]);
		OpenApiDocumentTransformerContext ctx = CreateContextWithEndpointDataSource(ds);

		// Act
		Dictionary<string, Type> mapping = EndpointRequestTypeMapper.BuildEndpointToRequestTypeMapping(ctx);

		// Assert
		mapping.Count.ShouldBe(1);
		mapping["/api/items"].ShouldBe(typeof(TestRequest)); // First one wins
	}

	#endregion

	#region BuildEndpointToRequestTypeMapping - Primitive Parameters Tests

	[Fact]
	public void BuildEndpointToRequestTypeMapping_OnlyPrimitiveParameters_ReturnsEmpty()
	{
		// Arrange
		MethodInfo handler = ((Action<int, string, CancellationToken>)HandleWithPrimitives).Method;
		RouteEndpoint ep = CreateRouteEndpoint("/api/items/{id}", handler);
		TestEndpointDataSource ds = new([ep]);
		OpenApiDocumentTransformerContext ctx = CreateContextWithEndpointDataSource(ds);

		// Act
		Dictionary<string, Type> mapping = EndpointRequestTypeMapper.BuildEndpointToRequestTypeMapping(ctx);

		// Assert
		mapping.Count.ShouldBe(0);
	}

	[Fact]
	public void BuildEndpointToRequestTypeMapping_MixedPrimitiveAndComplexParameters_ReturnsComplexType()
	{
		// Arrange
		MethodInfo handler = ((Action<int, TestRequest, string, CancellationToken>)HandleWithMixedParams).Method;
		RouteEndpoint ep = CreateRouteEndpoint("/api/items/{id}", handler);
		TestEndpointDataSource ds = new([ep]);
		OpenApiDocumentTransformerContext ctx = CreateContextWithEndpointDataSource(ds);

		// Act
		Dictionary<string, Type> mapping = EndpointRequestTypeMapper.BuildEndpointToRequestTypeMapping(ctx);

		// Assert
		mapping.Count.ShouldBe(1);
		mapping["/api/items/{id}"].ShouldBe(typeof(TestRequest));
	}

	#endregion

	#region BuildEndpointToRequestTypeMapping - Filter Predicate Tests

	[Fact]
	public void BuildEndpointToRequestTypeMapping_FilterPredicateSelectsSpecificType_ReturnsFiltered()
	{
		// Arrange
		MethodInfo handler = ((Action<TestRequest, CancellationToken>)HandleWithRequest).Method;
		RouteEndpoint ep = CreateRouteEndpoint("/api/todos", handler);
		TestEndpointDataSource ds = new([ep]);
		OpenApiDocumentTransformerContext ctx = CreateContextWithEndpointDataSource(ds);

		// Act - filter to only include types with Name property
		Dictionary<string, Type> mapping = EndpointRequestTypeMapper.BuildEndpointToRequestTypeMapping(
			ctx,
			t => t.GetProperty("Name") is not null);

		// Assert
		mapping.Count.ShouldBe(1);
		mapping["/api/todos"].ShouldBe(typeof(TestRequest));
	}

	[Fact]
	public void BuildEndpointToRequestTypeMapping_FilterPredicateExcludesAllTypes_ReturnsEmpty()
	{
		// Arrange
		MethodInfo handler = ((Action<TestRequest, CancellationToken>)HandleWithRequest).Method;
		RouteEndpoint ep = CreateRouteEndpoint("/api/todos", handler);
		TestEndpointDataSource ds = new([ep]);
		OpenApiDocumentTransformerContext ctx = CreateContextWithEndpointDataSource(ds);

		// Act - filter that excludes everything
		Dictionary<string, Type> mapping = EndpointRequestTypeMapper.BuildEndpointToRequestTypeMapping(
			ctx,
			t => t.Name == "NonExistentType");

		// Assert
		mapping.Count.ShouldBe(0);
	}

	#endregion

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

	#region Handler Methods

	static void HandleWithRequest(TestRequest _, CancellationToken __) { }
	static void HandleWithAnotherRequest(AnotherRequest _, CancellationToken __) { }
	static void HandleWithPrimitives(int id, string name, CancellationToken _) { }
	static void HandleWithMixedParams(int id, TestRequest request, string name, CancellationToken _) { }

	#endregion

	#region Test Types

	class TestRequest { public string? Name { get; set; } }
	class AnotherRequest { public int Id { get; set; } }

	#endregion

	#region Helper Methods

	static RouteEndpoint CreateRouteEndpoint(string routePattern, MethodInfo handlerMethod)
	{
		RoutePattern parsed = RoutePatternFactory.Parse(routePattern);
		EndpointMetadataCollection metadata = new(handlerMethod);

		return new RouteEndpoint(_ => Task.CompletedTask, parsed, 0, metadata, displayName: null);
	}

	class TestEndpointDataSource : EndpointDataSource
	{
		public TestEndpointDataSource(IReadOnlyList<Endpoint> endpoints)
		{
			Endpoints = endpoints;
		}

		public override IReadOnlyList<Endpoint> Endpoints { get; }

		public override IChangeToken GetChangeToken() => new CancellationChangeToken(CancellationToken.None);
	}

	static OpenApiDocumentTransformerContext CreateContextWithEndpointDataSource(EndpointDataSource ds)
	{
		return new OpenApiDocumentTransformerContext
		{
			DocumentName = "v1",
			DescriptionGroups = [],
			ApplicationServices = new ServiceCollection().AddSingleton(ds).BuildServiceProvider()
		};
	}

	#endregion
}
