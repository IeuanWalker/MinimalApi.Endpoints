using System.Reflection;
using System.Runtime.CompilerServices;
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
	class TestRequest { public string? Name { get; set; } }

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
		// Create uninitialized context and set ApplicationServices
		Type contextType = typeof(OpenApiDocumentTransformerContext);
		object context = RuntimeHelpers.GetUninitializedObject(contextType);

		PropertyInfo? prop = contextType.GetProperty("ApplicationServices", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		prop?.SetValue(context, new ServiceCollection().AddSingleton(ds).BuildServiceProvider());

		return (OpenApiDocumentTransformerContext)context;
	}

	[Fact]
	public void BuildEndpointToRequestTypeMapping_NoEndpointDataSource_ReturnsEmpty()
	{
		// Arrange - context with no EndpointDataSource registered
		Type contextType = typeof(OpenApiDocumentTransformerContext);
		object context = RuntimeHelpers.GetUninitializedObject(contextType);
		// Ensure ApplicationServices is an empty provider to avoid null reference when accessing GetService
		PropertyInfo? prop = contextType.GetProperty("ApplicationServices", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		prop?.SetValue(context, new ServiceCollection().BuildServiceProvider());

		OpenApiDocumentTransformerContext ctx = (OpenApiDocumentTransformerContext)context;

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
		MethodInfo handler = typeof(EndpointRequestTypeMapperTests).GetMethod(nameof(HandleWithRequest), BindingFlags.Static | BindingFlags.NonPublic)!;
		RouteEndpoint ep = CreateRouteEndpoint("/api/todos", handler);
		TestEndpointDataSource ds = new(new[] { ep });
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
		MethodInfo handler = typeof(EndpointRequestTypeMapperTests).GetMethod(nameof(HandleWithRequest), BindingFlags.Static | BindingFlags.NonPublic)!;
		RouteEndpoint ep = CreateRouteEndpoint("/api/todos", handler);
		TestEndpointDataSource ds = new(new[] { ep });
		OpenApiDocumentTransformerContext ctx = CreateContextWithEndpointDataSource(ds);

		// Act - filter that rejects all types
		Dictionary<string, Type> mapping = EndpointRequestTypeMapper.BuildEndpointToRequestTypeMapping(ctx, t => false);

		// Assert
		mapping.Count.ShouldBe(0);
	}

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

	static void HandleWithRequest(TestRequest req, CancellationToken ct) { }
}
