using ExampleApi.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace ExampleApi.IntegrationTests.Endpoints.Validation;

public class DebugEndpointMetadata : IClassFixture<ExampleApiWebApplicationFactory>
{
	readonly ExampleApiWebApplicationFactory _factory;

	public DebugEndpointMetadata(ExampleApiWebApplicationFactory factory)
	{
		_factory = factory;
	}

	[Fact]
	public void PrintEndpointMetadata()
	{
		// Get the endpoint data source
		EndpointDataSource? endpointDataSource = _factory.Services.GetService<EndpointDataSource>();
		Assert.NotNull(endpointDataSource);

		Console.WriteLine("=== All Endpoints ===");
		
		int count = 0;
		foreach (var endpoint in endpointDataSource.Endpoints)
		{
			if (endpoint is RouteEndpoint routeEndpoint)
			{
				string? pattern = routeEndpoint.RoutePattern.RawText;
				
				if (pattern?.Contains("Endpoint") == true)
				{
					count++;
					Console.WriteLine($"\n#{count} Route: {pattern}");
					Console.WriteLine($"  Display Name: {routeEndpoint.DisplayName}");
					
					Console.WriteLine($"  Metadata ({routeEndpoint.Metadata.Count} items):");
					foreach (var metadata in routeEndpoint.Metadata)
					{
						Type metadataType = metadata.GetType();
						Console.WriteLine($"    - {metadataType.FullName}");
						
						if (metadataType.IsGenericType)
						{
							Console.WriteLine($"      Generic: {string.Join(", ", metadataType.GetGenericArguments().Select(t => t.FullName))}");
						}
						
						// Check if this is a FluentValidationFilter
						if (metadataType.Name.Contains("FluentValidationFilter"))
						{
							Console.WriteLine($"      *** FOUND FluentValidationFilter! ***");
						}
					}
				}
			}
		}
		
		Console.WriteLine($"\n=== Found {count} endpoints containing 'Endpoint' ===");
	}
}
