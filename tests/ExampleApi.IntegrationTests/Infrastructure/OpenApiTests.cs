using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ExampleApi.IntegrationTests.Infrastructure;

/// <summary>
/// Integration tests for infrastructure concerns like API documentation, health checks, etc.
/// </summary>
public partial class OpenApiTests : IClassFixture<ExampleApiWebApplicationFactory>
{
	readonly HttpClient _client;

	public OpenApiTests(ExampleApiWebApplicationFactory factory)
	{
		_client = factory.CreateClient();
	}

	[Fact]
	public async Task OpenApiJson_ReturnsValidResponse()
	{
		// Act
		HttpResponseMessage response = await _client.GetAsync("/openapi/v1.json");

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");

		string actualContent = await response.Content.ReadAsStringAsync();

		Debugger.Break();

		actualContent.ShouldNotBeNullOrWhiteSpace();

		// Load expected OpenAPI JSON
		string expectedContent = await File.ReadAllTextAsync("ExpectedOpenApi.json");

		// Normalize both JSON strings (URLs and formatting) before comparison
		string normalizedActual = NormalizeOpenApiJson(actualContent, GetOptions());
		string normalizedExpected = NormalizeOpenApiJson(expectedContent, GetOptions());

		// Compare the normalized JSON strings
		normalizedActual.ShouldBe(normalizedExpected, "The actual OpenAPI JSON should match the expected OpenAPI JSON exactly (ignoring server URL and formatting differences)");
	}

	static JsonSerializerOptions GetOptions()
	{
		return new() { WriteIndented = false };
	}

	[GeneratedRegex(@"""url""\s*:\s*""[^""]*""", RegexOptions.IgnoreCase, "en-GB")]
	private static partial Regex OpenApiServerUrl();

	static string NormalizeOpenApiJson(string jsonContent, JsonSerializerOptions options)
	{
		// Normalize server URLs
		string normalized = OpenApiServerUrl().Replace(jsonContent, @"""url"": ""http://localhost/""");

		// Parse and reserialize to normalize formatting (whitespace, indentation, line endings)
		JsonDocument doc = JsonDocument.Parse(normalized);
		return JsonSerializer.Serialize(doc, options);
	}

	public static string RemoveNewLinesAndWhitespace(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return string.Empty;
		}

		// Replace different newline sequences with a single space
		value = NewLinePattern().Replace(value, string.Empty);

		// Replace encoded plus
		value = value.Replace("\\u002B", "+", StringComparison.OrdinalIgnoreCase);

		// Remove all whitespace characters (spaces, tabs, newlines)
		return SpacePattern().Replace(value, string.Empty);
	}

	[GeneratedRegex("\r\n|\r|\n")]
	private static partial Regex NewLinePattern();
	[GeneratedRegex(@"\s+")]
	private static partial Regex SpacePattern();
}
