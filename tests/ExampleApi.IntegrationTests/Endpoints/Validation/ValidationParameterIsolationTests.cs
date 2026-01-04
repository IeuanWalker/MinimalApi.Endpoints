using ExampleApi.IntegrationTests.Infrastructure;
using Shouldly;
using System.Text.Json;

namespace ExampleApi.IntegrationTests.Endpoints.Validation;

public class ValidationParameterIsolationTests : IClassFixture<ExampleApiWebApplicationFactory>
{
	readonly ExampleApiWebApplicationFactory _factory;
	readonly HttpClient _client;

	public ValidationParameterIsolationTests(ExampleApiWebApplicationFactory factory)
	{
		_factory = factory;
		_client = _factory.CreateClient();
	}

	[Fact]
	public async Task BothEndpoints_ShouldHaveDifferentValidationRules()
	{
		// Act - Get OpenAPI document as JSON
		HttpResponseMessage response = await _client.GetAsync("/openapi/v1.json");
		response.EnsureSuccessStatusCode();
		
		string content = await response.Content.ReadAsStringAsync();
		using JsonDocument document = JsonDocument.Parse(content);

		// Find both endpoints in the paths
		JsonElement paths = document.RootElement.GetProperty("paths");
		
		JsonElement? endpointA = null;
		JsonElement? endpointB = null;
		
		foreach (JsonProperty path in paths.EnumerateObject())
		{
			if (path.Name.Contains("/EndpointA"))
			{
				endpointA = path.Value;
			}
			else if (path.Name.Contains("/EndpointB"))
			{
				endpointB = path.Value;
			}
		}

		endpointA.HasValue.ShouldBeTrue("Endpoint A should exist");
		endpointB.HasValue.ShouldBeTrue("Endpoint B should exist");

		// Get the GET operation for each endpoint
		JsonElement opA = endpointA.Value.GetProperty("get");
		JsonElement opB = endpointB.Value.GetProperty("get");

		// Find the Name parameter in each endpoint
		JsonElement? paramA = null;
		JsonElement? paramB = null;

		if (opA.TryGetProperty("parameters", out JsonElement paramsA))
		{
			paramA = paramsA.EnumerateArray()
				.Where(param => param.TryGetProperty("name", out JsonElement nameElem) && 
				                nameElem.GetString()?.Equals("name", StringComparison.OrdinalIgnoreCase) == true)
				.Cast<JsonElement?>()
				.FirstOrDefault();
		}

		if (opB.TryGetProperty("parameters", out JsonElement paramsB))
		{
			paramB = paramsB.EnumerateArray()
				.Where(param => param.TryGetProperty("name", out JsonElement nameElem) && 
				                nameElem.GetString()?.Equals("name", StringComparison.OrdinalIgnoreCase) == true)
				.Cast<JsonElement?>()
				.FirstOrDefault();
		}

		paramA.HasValue.ShouldBeTrue("Name parameter should exist in Endpoint A");
		paramB.HasValue.ShouldBeTrue("Name parameter should exist in Endpoint B");

		// Get the schema for each parameter
		JsonElement schemaA = paramA.Value.GetProperty("schema");
		JsonElement schemaB = paramB.Value.GetProperty("schema");

		// Extract minLength from each schema
		int? minLengthA = schemaA.TryGetProperty("minLength", out JsonElement minA) ? minA.GetInt32() : (int?)null;
		int? minLengthB = schemaB.TryGetProperty("minLength", out JsonElement minB) ? minB.GetInt32() : (int?)null;

		// This is the key assertion - they should be different!
		minLengthA.ShouldNotBe(minLengthB, 
			$"Endpoint A and B should have different validation rules. " +
			$"A has minLength={minLengthA}, B has minLength={minLengthB}");
		
		minLengthA.ShouldBe(5, "Endpoint A should have minLength=5");
		minLengthB.ShouldBe(10, "Endpoint B should have minLength=10");
	}
}
