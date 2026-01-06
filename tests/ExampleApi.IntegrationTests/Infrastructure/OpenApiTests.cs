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

	[Fact]
	public async Task OpenApiJson_ValidEnumStructure()
	{
		// Act
		HttpResponseMessage response = await _client.GetAsync("/openapi/v1.json");

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");

		string actualContent = await response.Content.ReadAsStringAsync();
		actualContent.ShouldNotBeNullOrWhiteSpace();
		string normalizedActual = NormalizeOpenApiJson(actualContent, GetOptions());

		// Compare the normalized JSON strings

		// Validate endpoints
		string expectedEndpoint = """
				"/api/v1/enum": {
					"get": {
						"tags": [
							"Enum"
						],
						"summary": "FromQuery",
						"operationId": "get_Enum_0",
						"parameters": [
							{
								"name": "PlainEnum",
								"in": "query",
								"required": true,
								"schema": {
									"$ref": "#/components/schemas/ExampleApi.Endpoints.Enum.GetFromQuery.PriorityEnum"
								}
							},
							{
								"name": "NullableEnum",
								"in": "query",
								"schema": {
									"$ref": "#/components/schemas/ExampleApi.Endpoints.Enum.GetFromQuery.PriorityEnum"
								}
							},
							{
								"name": "PlainEnumWithoutFluentValidation",
								"in": "query",
								"required": true,
								"schema": {
									"$ref": "#/components/schemas/ExampleApi.Endpoints.Enum.GetFromQuery.PriorityEnum"
								}
							},
							{
								"name": "NullableEnumWithoutFluentValidation",
								"in": "query",
								"schema": {
									"$ref": "#/components/schemas/ExampleApi.Endpoints.Enum.GetFromQuery.PriorityEnum"
								}
							},
							{
								"name": "EnumAsString",
								"in": "query",
								"required": true,
								"schema": {
									"type": "string",
									"description": "Enum: Low, Medium, High, Critical",
									"enum": [
										"Low",
										"Medium",
										"High",
										"Critical"
									],
									"x-enum-descriptions": {
										"Low": "Low priority task",
										"Medium": "Medium priority task",
										"High": "High priority task",
										"Critical": "Critical priority task requiring immediate attention"
									}
								}
							},
							{
								"name": "NullableEnumAsString",
								"in": "query",
								"schema": {
									"type": "string",
									"description": "Enum: Low, Medium, High, Critical",
									"enum": [
										"Low",
										"Medium",
										"High",
										"Critical"
									],
									"x-enum-descriptions": {
										"Low": "Low priority task",
										"Medium": "Medium priority task",
										"High": "High priority task",
										"Critical": "Critical priority task requiring immediate attention"
									}
								}
							},
							{
								"name": "EnumAsInt",
								"in": "query",
								"required": true,
								"schema": {
									"type": "integer",
									"description": "Enum: Low, Medium, High, Critical",
									"format": "int32",
									"enum": [
										0,
										1,
										2,
										3
									],
									"x-enum-varnames": [
										"Low",
										"Medium",
										"High",
										"Critical"
									],
									"x-enum-descriptions": {
										"Low": "Low priority task",
										"Medium": "Medium priority task",
										"High": "High priority task",
										"Critical": "Critical priority task requiring immediate attention"
									}
								}
							},
							{
								"name": "NullableEnumAsInt",
								"in": "query",
								"schema": {
									"oneOf": [
										{
											"nullable": true
										},
										{
											"type": "integer",
											"description": "Enum: Low, Medium, High, Critical",
											"format": "int32",
											"enum": [
												0,
												1,
												2,
												3
											],
											"x-enum-varnames": [
												"Low",
												"Medium",
												"High",
												"Critical"
											],
											"x-enum-descriptions": {
												"Low": "Low priority task",
												"Medium": "Medium priority task",
												"High": "High priority task",
												"Critical": "Critical priority task requiring immediate attention"
											}
										}
									]
								}
							}
						],
						"responses": {
							"400": {
								"description": "Bad Request",
								"content": {
									"application/problem+json": {
										"schema": {
											"$ref": "#/components/schemas/Microsoft.AspNetCore.Http.HttpValidationProblemDetails"
										}
									}
								}
							}
						}
					},
					"post": {
						"tags": [
							"Enum"
						],
						"summary": "FromBody",
						"operationId": "post_Enum_1",
						"requestBody": {
							"content": {
								"application/json": {
									"schema": {
										"$ref": "#/components/schemas/ExampleApi.Endpoints.Enum.PostFromBody.RequestModel"
									}
								}
							}
						},
						"responses": {
							"400": {
								"description": "Bad Request",
								"content": {
									"application/problem+json": {
										"schema": {
											"$ref": "#/components/schemas/Microsoft.AspNetCore.Http.HttpValidationProblemDetails"
										}
									}
								}
							}
						}
					}
				}
			""";

		RemoveNewLinesAndWhitespace(normalizedActual).ShouldContain(RemoveNewLinesAndWhitespace(expectedEndpoint));

		string expectedComponents = """
			   			"ExampleApi.Endpoints.Enum.GetFromQuery.PriorityEnum": {
							"type": "integer",
							"description": "Enum: Low, Medium, High, Critical",
							"enum": [
								0,
								1,
								2,
								3
							],
							"x-enum-varnames": [
								"Low",
								"Medium",
								"High",
								"Critical"
							],
							"x-enum-descriptions": {
								"Low": "Low priority task",
								"Medium": "Medium priority task",
								"High": "High priority task",
								"Critical": "Critical priority task requiring immediate attention"
							}
						},
						"ExampleApi.Endpoints.Enum.PostFromBody.PriorityEnum": {
							"type": "integer",
							"description": "Enum: Low, Medium, High, Critical",
							"enum": [
								0,
								1,
								2,
								3
							],
							"x-enum-varnames": [
								"Low",
								"Medium",
								"High",
								"Critical"
							],
							"x-enum-descriptions": {
								"Low": "Low priority task",
								"Medium": "Medium priority task",
								"High": "High priority task",
								"Critical": "Critical priority task requiring immediate attention"
							}
						},
						"ExampleApi.Endpoints.Enum.PostFromBody.RequestModel": {
							"type": "object",
							"properties": {
								"plainEnum": {
									"$ref": "#/components/schemas/ExampleApi.Endpoints.Enum.PostFromBody.PriorityEnum"
								},
								"nullableEnum": {
									"oneOf": [
										{
											"nullable": true
										},
										{
											"$ref": "#/components/schemas/ExampleApi.Endpoints.Enum.PostFromBody.PriorityEnum"
										}
									]
								},
								"plainEnumWithoutFluentValidation": {
									"$ref": "#/components/schemas/ExampleApi.Endpoints.Enum.PostFromBody.PriorityEnum"
								},
								"nullableEnumWithoutFluentValidation": {
									"oneOf": [
										{
											"nullable": true
										},
										{
											"$ref": "#/components/schemas/ExampleApi.Endpoints.Enum.PostFromBody.PriorityEnum"
										}
									]
								},
								"enumAsString": {
									"type": "string",
									"description": "Enum: Low, Medium, High, Critical",
									"enum": [
										"Low",
										"Medium",
										"High",
										"Critical"
									],
									"x-enum-descriptions": {
										"Low": "Low priority task",
										"Medium": "Medium priority task",
										"High": "High priority task",
										"Critical": "Critical priority task requiring immediate attention"
									}
								},
								"nullableEnumAsString": {
									"oneOf": [
										{
											"type": "string",
											"description": "Enum: Low, Medium, High, Critical",
											"enum": [
												"Low",
												"Medium",
												"High",
												"Critical"
											],
											"x-enum-descriptions": {
												"Low": "Low priority task",
												"Medium": "Medium priority task",
												"High": "High priority task",
												"Critical": "Critical priority task requiring immediate attention"
											}
										},
										{
											"nullable": true
										}
									]
								},
								"enumAsInt": {
									"type": "integer",
									"description": "Enum: Low, Medium, High, Critical",
									"format": "int32",
									"enum": [
										0,
										1,
										2,
										3
									],
									"x-enum-varnames": [
										"Low",
										"Medium",
										"High",
										"Critical"
									],
									"x-enum-descriptions": {
										"Low": "Low priority task",
										"Medium": "Medium priority task",
										"High": "High priority task",
										"Critical": "Critical priority task requiring immediate attention"
									}
								},
								"nullableEnumAsInt": {
									"oneOf": [
										{
											"type": "integer",
											"description": "Enum: Low, Medium, High, Critical",
											"enum": [
												0,
												1,
												2,
												3
											],
											"x-enum-varnames": [
												"Low",
												"Medium",
												"High",
												"Critical"
											],
											"x-enum-descriptions": {
												"Low": "Low priority task",
												"Medium": "Medium priority task",
												"High": "High priority task",
												"Critical": "Critical priority task requiring immediate attention"
											}
										},
										{
											"nullable": true
										}
									]
								},
								"nullableOnlyEnumTest": {
									"oneOf": [
										{
											"nullable": true
										},
										{
											"$ref": "#/components/schemas/ExampleApi.Endpoints.Enum.PostFromBody.TestEnum"
										}
									]
								}
							}
						},
						"ExampleApi.Endpoints.Enum.PostFromBody.TestEnum": {
							"type": "integer",
							"description": "Enum: Test1, Test2",
							"enum": [
								0,
								1
							],
							"x-enum-varnames": [
								"Test1",
								"Test2"
							]
						},
			""";

		RemoveNewLinesAndWhitespace(normalizedActual).ShouldContain(RemoveNewLinesAndWhitespace(expectedComponents));
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
		value = Regex.Replace(value, "\r\n|\r|\n", string.Empty);

		// Replace encoded plus
		value = value.Replace("\\u002B", "+", StringComparison.OrdinalIgnoreCase);

		// Remove all whitespace characters (spaces, tabs, newlines)
		return Regex.Replace(value, @"\s+", string.Empty);
	}
}
