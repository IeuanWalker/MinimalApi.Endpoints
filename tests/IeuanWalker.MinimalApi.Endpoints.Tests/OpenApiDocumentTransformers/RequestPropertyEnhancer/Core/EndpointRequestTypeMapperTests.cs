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

	static void HandleWithRequest(TestRequest _, CancellationToken __) { }

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
		return new OpenApiDocumentTransformerContext
		{
			DocumentName = "v1",
			DescriptionGroups = [],
			ApplicationServices = new ServiceCollection().AddSingleton(ds).BuildServiceProvider()
		};
	}

}
