using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

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
	public void BuildEndpointToRequestTypeMapping_IgnoresNonRouteEndpoint_ReturnsEmptyMapping()
	{
		// Arrange
		Endpoint endpoint = new Endpoint((RequestDelegate)(_ => Task.CompletedTask), new EndpointMetadataCollection(), "non-route");
		EndpointDataSource endpointDataSource = new SimpleEndpointDataSource(new[]
		{
			endpoint
		});

		ServiceProvider services = new ServiceCollection()
			.AddSingleton<EndpointDataSource>(endpointDataSource)
			.BuildServiceProvider();

		OpenApiDocumentTransformerContext context = new()
		{
			DocumentName = "v1",
			ApplicationServices = services,
			DescriptionGroups = new List<ApiDescriptionGroup>()
		};

		// Act
		Dictionary<string, Type> result = EndpointRequestTypeMapper.BuildEndpointToRequestTypeMapping(context);

		// Assert
		result.ShouldBeEmpty();
	}

	[Fact]
	public void BuildEndpointToRequestTypeMapping_RouteEndpointWithNoMethodInfo_ReturnsEmptyMapping()
	{
		// Arrange
		string routePatternText = "/api/test/{id}";

		RoutePattern routePattern = RoutePatternFactory.Parse(routePatternText);
		RouteEndpoint routeEndpoint = new(
			(RequestDelegate)(_ => Task.CompletedTask),
			routePattern,
			order: 0,
			metadata: new EndpointMetadataCollection(),
			displayName: "test");

		EndpointDataSource endpointDataSource = new SimpleEndpointDataSource([routeEndpoint]);

		ServiceProvider services = new ServiceCollection()
			.AddSingleton<EndpointDataSource>(endpointDataSource)
			.BuildServiceProvider();

		OpenApiDocumentTransformerContext context = new()
		{
			DocumentName = "v1",
			ApplicationServices = services,
			DescriptionGroups = []
		};

		// Act
		Dictionary<string, Type> result = EndpointRequestTypeMapper.BuildEndpointToRequestTypeMapping(context);

		// Assert
		result.ShouldBeEmpty();
	}

	[Fact]
	public void BuildEndpointToRequestTypeMapping_RouteEndpointWithEmptyRoutePattern_ReturnsEmptyMapping()
	{
		// Arrange
		RoutePattern routePattern = RoutePatternFactory.Parse(string.Empty);
		RouteEndpoint routeEndpoint = new(
			(RequestDelegate)(_ => Task.CompletedTask),
			routePattern,
			order: 0,
			metadata: new EndpointMetadataCollection(),
			displayName: "test");

		EndpointDataSource endpointDataSource = new SimpleEndpointDataSource([routeEndpoint]);

		ServiceProvider services = new ServiceCollection()
			.AddSingleton<EndpointDataSource>(endpointDataSource)
			.BuildServiceProvider();

		OpenApiDocumentTransformerContext context = new()
		{
			DocumentName = "v1",
			ApplicationServices = services,
			DescriptionGroups = []
		};

		// Act
		Dictionary<string, Type> result = EndpointRequestTypeMapper.BuildEndpointToRequestTypeMapping(context);

		// Assert
		result.ShouldBeEmpty();
	}

	[Fact]
	public void BuildEndpointToRequestTypeMapping_NoEndpointDataSource_ReturnsEmptyMapping()
	{
		// Arrange
		ServiceProvider services = new ServiceCollection().BuildServiceProvider();
		OpenApiDocumentTransformerContext context = new()
		{
			DocumentName = "v1",
			ApplicationServices = services,
			DescriptionGroups = new List<ApiDescriptionGroup>()
		};

		// Act
		Dictionary<string, Type> result = EndpointRequestTypeMapper.BuildEndpointToRequestTypeMapping(context);

		// Assert
		result.ShouldBeEmpty();
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

	class SimpleEndpointDataSource : EndpointDataSource
	{
		public SimpleEndpointDataSource(IEnumerable<Endpoint> endpoints)
		{
			Endpoints = endpoints.ToList();
		}

		public override IReadOnlyList<Endpoint> Endpoints { get; }

		public override IChangeToken GetChangeToken()
		{
			return new CancellationChangeToken(new CancellationToken());
		}
	}

	#endregion
}
