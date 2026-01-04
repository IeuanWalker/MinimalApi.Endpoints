using ExampleApi.IntegrationTests.Infrastructure;
using System.Text.Json;

namespace ExampleApi.IntegrationTests.Endpoints.Validation;

public class DebugOpenApiPaths : IClassFixture<ExampleApiWebApplicationFactory>
{
	readonly ExampleApiWebApplicationFactory _factory;
	readonly HttpClient _client;

	public DebugOpenApiPaths(ExampleApiWebApplicationFactory factory)
	{
		_factory = factory;
		_client = _factory.CreateClient();
	}

	[Fact]
	public async Task PrintOpenApiPaths()
	{
		// Act - Get OpenAPI document as JSON
		HttpResponseMessage response = await _client.GetAsync("/openapi/v1.json");
		response.EnsureSuccessStatusCode();
		
		string content = await response.Content.ReadAsStringAsync();
		using JsonDocument document = JsonDocument.Parse(content);

		// Find all paths
		JsonElement paths = document.RootElement.GetProperty("paths");
		
		Console.WriteLine("=== OpenAPI Paths ===");
		foreach (JsonProperty path in paths.EnumerateObject())
		{
			Console.WriteLine($"Path: {path.Name}");
			
			if (path.Name.Contains("Endpoint"))
			{
				Console.WriteLine($"  Contains 'Endpoint': YES");
				
				// Check if it has GET method
				if (path.Value.TryGetProperty("get", out JsonElement getOp))
				{
					Console.WriteLine($"  Has GET: YES");
					
					// Check parameters
					if (getOp.TryGetProperty("parameters", out JsonElement params_))
					{
						Console.WriteLine($"  Parameters:");
						foreach (JsonElement param in params_.EnumerateArray())
						{
							string? paramName = param.GetProperty("name").GetString();
							Console.WriteLine($"    - {paramName}");
							
							// Get schema
							if (param.TryGetProperty("schema", out JsonElement schema))
							{
								if (schema.TryGetProperty("minLength", out JsonElement minLen))
								{
									Console.WriteLine($"      minLength: {minLen.GetInt32()}");
								}
								else
								{
									Console.WriteLine($"      NO minLength");
								}
							}
						}
					}
				}
			}
		}
	}
}
