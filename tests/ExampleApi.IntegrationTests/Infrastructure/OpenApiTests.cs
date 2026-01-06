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

	[Fact]
	public async Task OpenApiJson_ValidTypeStructure()
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
			"/api/v1/TypeExamples/FromQuery": {
				"get": {
					"tags": [
						"Type Examples"
					],
					"summary": "Type Examples From Query",
					"description": "Demonstrates all primitive types handled by TypeDocumentTransformer as query parameters: string, int, long, decimal, double, float, bool, DateTime, DateOnly, TimeOnly, Guid, and arrays",
					"operationId": "get_TypeExamplesFromQuery_14",
					"parameters": [
						{
							"name": "StringValue",
							"in": "query",
							"required": true,
							"schema": {
								"type": "string"
							}
						},
						{
							"name": "NullableStringValue",
							"in": "query",
							"schema": {
								"type": "string"
							}
						},
						{
							"name": "IntValue",
							"in": "query",
							"required": true,
							"schema": {
								"type": "integer",
								"format": "int32"
							}
						},
						{
							"name": "NullableIntValue",
							"in": "query",
							"schema": {
								"type": "integer",
								"format": "int32"
							}
						},
						{
							"name": "LongValue",
							"in": "query",
							"required": true,
							"schema": {
								"type": "integer",
								"format": "int64"
							}
						},
						{
							"name": "NullableLongValue",
							"in": "query",
							"schema": {
								"type": "integer",
								"format": "int64"
							}
						},
						{
							"name": "DecimalValue",
							"in": "query",
							"required": true,
							"schema": {
								"type": "number"
							}
						},
						{
							"name": "NullableDecimalValue",
							"in": "query",
							"schema": {
								"type": "number"
							}
						},
						{
							"name": "DoubleValue",
							"in": "query",
							"required": true,
							"schema": {
								"type": "number",
								"format": "double"
							}
						},
						{
							"name": "NullableDoubleValue",
							"in": "query",
							"schema": {
								"type": "number",
								"format": "double"
							}
						},
						{
							"name": "FloatValue",
							"in": "query",
							"required": true,
							"schema": {
								"type": "number",
								"format": "float"
							}
						},
						{
							"name": "NullableFloatValue",
							"in": "query",
							"schema": {
								"type": "number",
								"format": "float"
							}
						},
						{
							"name": "BoolValue",
							"in": "query",
							"required": true,
							"schema": {
								"type": "boolean"
							}
						},
						{
							"name": "NullableBoolValue",
							"in": "query",
							"schema": {
								"type": "boolean"
							}
						},
						{
							"name": "DateTimeValue",
							"in": "query",
							"required": true,
							"schema": {
								"type": "string",
								"format": "date-time"
							}
						},
						{
							"name": "NullableDateTimeValue",
							"in": "query",
							"schema": {
								"type": "string",
								"format": "date-time"
							}
						},
						{
							"name": "DateOnlyValue",
							"in": "query",
							"required": true,
							"schema": {
								"type": "string",
								"format": "date"
							}
						},
						{
							"name": "NullableDateOnlyValue",
							"in": "query",
							"schema": {
								"type": "string",
								"format": "date"
							}
						},
						{
							"name": "TimeOnlyValue",
							"in": "query",
							"required": true,
							"schema": {
								"type": "string",
								"format": "time"
							}
						},
						{
							"name": "NullableTimeOnlyValue",
							"in": "query",
							"schema": {
								"type": "string",
								"format": "time"
							}
						},
						{
							"name": "GuidValue",
							"in": "query",
							"required": true,
							"schema": {
								"type": "string",
								"format": "uuid"
							}
						},
						{
							"name": "NullableGuidValue",
							"in": "query",
							"schema": {
								"type": "string",
								"format": "uuid"
							}
						},
						{
							"name": "StringArray",
							"in": "query",
							"schema": {
								"type": "array",
								"items": {
									"type": "string"
								}
							}
						},
						{
							"name": "IntArray",
							"in": "query",
							"schema": {
								"type": "array",
								"items": {
									"type": "integer",
									"format": "int32"
								}
							}
						},
						{
							"name": "DoubleArray",
							"in": "query",
							"schema": {
								"type": "array",
								"items": {
									"type": "number",
									"format": "double"
								}
							}
						},
						{
							"name": "UriArray",
							"in": "query",
							"required": true,
							"schema": {
								"type": "array",
								"items": {
									"$ref": "#/components/schemas/System.Uri"
								}
							}
						},
						{
							"name": "UriArrayNullable",
							"in": "query",
							"schema": {
								"type": "array",
								"items": {
									"$ref": "#/components/schemas/System.Uri"
								}
							}
						},
						{
							"name": "UriValue",
							"in": "query",
							"required": true,
							"schema": {
								"$ref": "#/components/schemas/System.Uri"
							}
						},
						{
							"name": "UriValueNullable",
							"in": "query",
							"schema": {
								"$ref": "#/components/schemas/System.Uri"
							}
						}
					],
					"responses": {
						"200": {
							"description": "OK",
							"content": {
								"application/json": {
									"schema": {
										"$ref": "#/components/schemas/ExampleApi.Endpoints.TypeExamples.GetFromQuery.ResponseModel"
									}
								}
							}
						}
					}
				}
			},
			"/api/v1/TypeExamples/FromBody": {
				"post": {
					"tags": [
						"Type Examples"
					],
					"summary": "Type Examples From Body",
					"description": "Demonstrates all primitive types handled by TypeDocumentTransformer: string, int, long, short, byte, decimal, double, float, bool, DateTime, DateTimeOffset, DateOnly, TimeOnly, Guid, arrays, and nested objects",
					"operationId": "post_TypeExamplesFromBody_15",
					"requestBody": {
						"content": {
							"application/json": {
								"schema": {
									"$ref": "#/components/schemas/ExampleApi.Endpoints.TypeExamples.PostFromBody.RequestModel"
								}
							}
						}
					},
					"responses": {
						"200": {
							"description": "OK",
							"content": {
								"application/json": {
									"schema": {
										"$ref": "#/components/schemas/ExampleApi.Endpoints.TypeExamples.PostFromBody.ResponseModel"
									}
								}
							}
						}
					}
				}
			},
			"/api/v1/TypeExamples/FromForm": {
				"post": {
					"tags": [
						"Type Examples"
					],
					"summary": "Type Examples From Form",
					"description": "Demonstrates all primitive types handled by TypeDocumentTransformer as form data: string, int, long, short, byte, decimal, double, float, bool, DateTime, DateTimeOffset, DateOnly, TimeOnly, Guid, arrays, nested objects, and file uploads (IFormFile, IFormFileCollection)",
					"operationId": "post_TypeExamplesFromForm_16",
					"requestBody": {
						"content": {
							"multipart/form-data": {
								"schema": {
									"$ref": "#/components/schemas/ExampleApi.Endpoints.TypeExamples.PostFromForm.RequestModel"
								}
							},
							"application/x-www-form-urlencoded": {
								"schema": {
									"$ref": "#/components/schemas/ExampleApi.Endpoints.TypeExamples.PostFromForm.RequestModel"
								}
							}
						},
						"required": true
					},
					"responses": {
						"200": {
							"description": "OK",
							"content": {
								"application/json": {
									"schema": {
										"$ref": "#/components/schemas/ExampleApi.Endpoints.TypeExamples.PostFromForm.ResponseModel"
									}
								}
							}
						}
					}
				}
			},
			""";

		RemoveNewLinesAndWhitespace(normalizedActual).ShouldContain(RemoveNewLinesAndWhitespace(expectedEndpoint));

		string expectedComponents = """
			"ExampleApi.Endpoints.TypeExamples.PostFromBody.RequestModel": {
				"required": [
					"nestedTests",
					"uriValue"
				],
				"type": "object",
				"properties": {
					"nestedTests": {
						"type": "array",
						"items": {
							"$ref": "#/components/schemas/ExampleApi.Endpoints.TypeExamples.PostFromBody.BaseTests"
						}
					},
					"stringValue": {
						"type": "string"
					},
					"nullableStringValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "string"
							}
						]
					},
					"intValue": {
						"type": "integer",
						"format": "int32"
					},
					"nullableIntValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "integer",
								"format": "int32"
							}
						]
					},
					"longValue": {
						"type": "integer",
						"format": "int64"
					},
					"nullableLongValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "integer",
								"format": "int64"
							}
						]
					},
					"shortValue": {
						"type": "integer",
						"format": "int32"
					},
					"nullableShortValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "integer",
								"format": "int32"
							}
						]
					},
					"byteValue": {
						"type": "integer",
						"format": "int32"
					},
					"nullableByteValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "integer",
								"format": "int32"
							}
						]
					},
					"decimalValue": {
						"type": "number"
					},
					"nullableDecimalValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "number"
							}
						]
					},
					"doubleValue": {
						"type": "number",
						"format": "double"
					},
					"nullableDoubleValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "number",
								"format": "double"
							}
						]
					},
					"floatValue": {
						"type": "number",
						"format": "float"
					},
					"nullableFloatValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "number",
								"format": "float"
							}
						]
					},
					"boolValue": {
						"type": "boolean"
					},
					"nullableBoolValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "boolean"
							}
						]
					},
					"dateTimeValue": {
						"type": "string",
						"format": "date-time"
					},
					"nullableDateTimeValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "string",
								"format": "date-time"
							}
						]
					},
					"dateTimeOffsetValue": {
						"type": "string",
						"format": "date-time"
					},
					"nullableDateTimeOffsetValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "string",
								"format": "date-time"
							}
						]
					},
					"dateOnlyValue": {
						"type": "string",
						"format": "date"
					},
					"nullableDateOnlyValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "string",
								"format": "date"
							}
						]
					},
					"timeOnlyValue": {
						"type": "string",
						"format": "time"
					},
					"nullableTimeOnlyValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "string",
								"format": "time"
							}
						]
					},
					"guidValue": {
						"type": "string",
						"format": "uuid"
					},
					"nullableGuidValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "string",
								"format": "uuid"
							}
						]
					},
					"stringList": {
						"type": "array",
						"items": {
							"type": "string"
						}
					},
					"nullableStringList": {
						"oneOf": [
							{
								"type": "array",
								"items": {
									"type": "string"
								}
							},
							{
								"nullable": true
							}
						]
					},
					"intList": {
						"type": "array",
						"items": {
							"type": "integer",
							"format": "int32"
						}
					},
					"nullableIntList": {
						"oneOf": [
							{
								"type": "array",
								"items": {
									"type": "integer",
									"format": "int32"
								}
							},
							{
								"nullable": true
							}
						]
					},
					"doubleList": {
						"type": "array",
						"items": {
							"type": "number",
							"format": "double"
						}
					},
					"nullableDoubleList": {
						"oneOf": [
							{
								"type": "array",
								"items": {
									"type": "number",
									"format": "double"
								}
							},
							{
								"nullable": true
							}
						]
					},
					"intArray": {
						"type": "array",
						"items": {
							"type": "integer",
							"format": "int32"
						}
					},
					"nullableIntArray": {
						"oneOf": [
							{
								"type": "array",
								"items": {
									"type": "integer",
									"format": "int32"
								}
							},
							{
								"nullable": true
							}
						]
					},
					"readOnlyStringList": {
						"type": "array",
						"items": {
							"type": "string"
						}
					},
					"nullableReadOnlyStringList": {
						"oneOf": [
							{
								"type": "array",
								"items": {
									"type": "string"
								}
							},
							{
								"nullable": true
							}
						]
					},
					"intCollection": {
						"type": "array",
						"items": {
							"type": "integer",
							"format": "int32"
						}
					},
					"nullableIntCollection": {
						"oneOf": [
							{
								"type": "array",
								"items": {
									"type": "integer",
									"format": "int32"
								}
							},
							{
								"nullable": true
							}
						]
					},
					"singleFile": {
						"type": "string",
						"format": "binary"
					},
					"readOnlyList1": {
						"type": "array",
						"items": {
							"type": "string",
							"format": "binary"
						}
					},
					"readOnlyList2": {
						"type": "array",
						"items": {
							"type": "string",
							"format": "binary"
						}
					},
					"singleFileNullable": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "string",
								"format": "binary"
							}
						]
					},
					"readOnlyList1Nullable": {
						"oneOf": [
							{
								"type": "array",
								"items": {
									"type": "string",
									"format": "binary"
								}
							},
							{
								"nullable": true
							}
						]
					},
					"readOnlyList2Nullable": {
						"oneOf": [
							{
								"type": "array",
								"items": {
									"type": "string",
									"format": "binary"
								}
							},
							{
								"nullable": true
							}
						]
					},
					"fileCollectionList": {
						"type": "array",
						"items": {
							"type": "string",
							"format": "binary"
						}
					},
					"nestedObjectValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"$ref": "#/components/schemas/ExampleApi.Endpoints.TypeExamples.PostFromBody.NestedObject"
							}
						]
					},
					"nestedObjectList": {
						"oneOf": [
							{
								"type": "array",
								"items": {
									"$ref": "#/components/schemas/ExampleApi.Endpoints.TypeExamples.PostFromBody.NestedObject"
								}
							},
							{
								"nullable": true
							}
						]
					},
					"uriArray": {
						"type": "array",
						"items": {
							"$ref": "#/components/schemas/System.Uri"
						}
					},
					"uriArrayNullable": {
						"oneOf": [
							{
								"type": "array",
								"items": {
									"$ref": "#/components/schemas/System.Uri"
								}
							},
							{
								"nullable": true
							}
						]
					},
					"uriValue": {
						"$ref": "#/components/schemas/System.Uri"
					},
					"uriValueNullable": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"$ref": "#/components/schemas/System.Uri"
							}
						]
					}
				}
			},
			"ExampleApi.Endpoints.TypeExamples.PostFromBody.ResponseModel": {
				"type": "object",
				"properties": {
					"message": {
						"type": "string"
					},
					"processedPropertiesCount": {
						"type": "integer",
						"format": "int32"
					}
				}
			},
			"ExampleApi.Endpoints.TypeExamples.PostFromForm.BaseTests": {
				"required": [
					"uriValue"
				],
				"type": "object",
				"properties": {
					"stringValue": {
						"type": "string"
					},
					"nullableStringValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "string"
							}
						]
					},
					"intValue": {
						"type": "integer",
						"format": "int32"
					},
					"nullableIntValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "integer",
								"format": "int32"
							}
						]
					},
					"longValue": {
						"type": "integer",
						"format": "int64"
					},
					"nullableLongValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "integer",
								"format": "int64"
							}
						]
					},
					"shortValue": {
						"type": "integer",
						"format": "int32"
					},
					"nullableShortValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "integer",
								"format": "int32"
							}
						]
					},
					"byteValue": {
						"type": "integer",
						"format": "int32"
					},
					"nullableByteValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "integer",
								"format": "int32"
							}
						]
					},
					"decimalValue": {
						"type": "number"
					},
					"nullableDecimalValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "number"
							}
						]
					},
					"doubleValue": {
						"type": "number",
						"format": "double"
					},
					"nullableDoubleValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "number",
								"format": "double"
							}
						]
					},
					"floatValue": {
						"type": "number",
						"format": "float"
					},
					"nullableFloatValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "number",
								"format": "float"
							}
						]
					},
					"boolValue": {
						"type": "boolean"
					},
					"nullableBoolValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "boolean"
							}
						]
					},
					"dateTimeValue": {
						"type": "string",
						"format": "date-time"
					},
					"nullableDateTimeValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "string",
								"format": "date-time"
							}
						]
					},
					"dateTimeOffsetValue": {
						"type": "string",
						"format": "date-time"
					},
					"nullableDateTimeOffsetValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "string",
								"format": "date-time"
							}
						]
					},
					"dateOnlyValue": {
						"type": "string",
						"format": "date"
					},
					"nullableDateOnlyValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "string",
								"format": "date"
							}
						]
					},
					"timeOnlyValue": {
						"type": "string",
						"format": "time"
					},
					"nullableTimeOnlyValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "string",
								"format": "time"
							}
						]
					},
					"guidValue": {
						"type": "string",
						"format": "uuid"
					},
					"nullableGuidValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "string",
								"format": "uuid"
							}
						]
					},
					"stringList": {
						"type": "array",
						"items": {
							"type": "string"
						}
					},
					"nullableStringList": {
						"oneOf": [
							{
								"type": "array",
								"items": {
									"type": "string"
								}
							},
							{
								"nullable": true
							}
						]
					},
					"intList": {
						"type": "array",
						"items": {
							"type": "integer",
							"format": "int32"
						}
					},
					"nullableIntList": {
						"oneOf": [
							{
								"type": "array",
								"items": {
									"type": "integer",
									"format": "int32"
								}
							},
							{
								"nullable": true
							}
						]
					},
					"doubleList": {
						"type": "array",
						"items": {
							"type": "number",
							"format": "double"
						}
					},
					"nullableDoubleList": {
						"oneOf": [
							{
								"type": "array",
								"items": {
									"type": "number",
									"format": "double"
								}
							},
							{
								"nullable": true
							}
						]
					},
					"intArray": {
						"type": "array",
						"items": {
							"type": "integer",
							"format": "int32"
						}
					},
					"nullableIntArray": {
						"oneOf": [
							{
								"type": "array",
								"items": {
									"type": "integer",
									"format": "int32"
								}
							},
							{
								"nullable": true
							}
						]
					},
					"readOnlyStringList": {
						"type": "array",
						"items": {
							"type": "string"
						}
					},
					"nullableReadOnlyStringList": {
						"oneOf": [
							{
								"type": "array",
								"items": {
									"type": "string"
								}
							},
							{
								"nullable": true
							}
						]
					},
					"intCollection": {
						"type": "array",
						"items": {
							"type": "integer",
							"format": "int32"
						}
					},
					"nullableIntCollection": {
						"oneOf": [
							{
								"type": "array",
								"items": {
									"type": "integer",
									"format": "int32"
								}
							},
							{
								"nullable": true
							}
						]
					},
					"singleFile": {
						"type": "string",
						"format": "binary"
					},
					"readOnlyList1": {
						"type": "array",
						"items": {
							"type": "string",
							"format": "binary"
						}
					},
					"readOnlyList2": {
						"type": "array",
						"items": {
							"type": "string",
							"format": "binary"
						}
					},
					"singleFileNullable": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "string",
								"format": "binary"
							}
						]
					},
					"readOnlyList1Nullable": {
						"oneOf": [
							{
								"type": "array",
								"items": {
									"type": "string",
									"format": "binary"
								}
							},
							{
								"nullable": true
							}
						]
					},
					"readOnlyList2Nullable": {
						"oneOf": [
							{
								"type": "array",
								"items": {
									"type": "string",
									"format": "binary"
								}
							},
							{
								"nullable": true
							}
						]
					},
					"fileCollectionList": {
						"type": "array",
						"items": {
							"type": "string",
							"format": "binary"
						}
					},
					"nestedObjectValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"$ref": "#/components/schemas/ExampleApi.Endpoints.TypeExamples.PostFromForm.NestedObject"
							}
						]
					},
					"nestedObjectList": {
						"oneOf": [
							{
								"type": "array",
								"items": {
									"$ref": "#/components/schemas/ExampleApi.Endpoints.TypeExamples.PostFromForm.NestedObject"
								}
							},
							{
								"nullable": true
							}
						]
					},
					"uriArray": {
						"type": "array",
						"items": {
							"$ref": "#/components/schemas/System.Uri"
						}
					},
					"uriArrayNullable": {
						"oneOf": [
							{
								"type": "array",
								"items": {
									"$ref": "#/components/schemas/System.Uri"
								}
							},
							{
								"nullable": true
							}
						]
					},
					"uriValue": {
						"$ref": "#/components/schemas/System.Uri"
					},
					"uriValueNullable": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"$ref": "#/components/schemas/System.Uri"
							}
						]
					}
				}
			},
			"ExampleApi.Endpoints.TypeExamples.PostFromForm.NestedObject": {
				"type": "object",
				"properties": {
					"name": {
						"type": "string"
					},
					"value": {
						"type": "integer",
						"format": "int32"
					},
					"createdAt": {
						"type": "string",
						"format": "date-time"
					}
				}
			},
			"ExampleApi.Endpoints.TypeExamples.PostFromForm.RequestModel": {
				"required": [
					"nestedTests",
					"uriValue"
				],
				"type": "object",
				"properties": {
					"nestedTests": {
						"type": "array",
						"items": {
							"$ref": "#/components/schemas/ExampleApi.Endpoints.TypeExamples.PostFromForm.BaseTests"
						}
					},
					"stringValue": {
						"type": "string"
					},
					"nullableStringValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "string"
							}
						]
					},
					"intValue": {
						"type": "integer",
						"format": "int32"
					},
					"nullableIntValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "integer",
								"format": "int32"
							}
						]
					},
					"longValue": {
						"type": "integer",
						"format": "int64"
					},
					"nullableLongValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "integer",
								"format": "int64"
							}
						]
					},
					"shortValue": {
						"type": "integer",
						"format": "int32"
					},
					"nullableShortValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "integer",
								"format": "int32"
							}
						]
					},
					"byteValue": {
						"type": "integer",
						"format": "int32"
					},
					"nullableByteValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "integer",
								"format": "int32"
							}
						]
					},
					"decimalValue": {
						"type": "number"
					},
					"nullableDecimalValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "number"
							}
						]
					},
					"doubleValue": {
						"type": "number",
						"format": "double"
					},
					"nullableDoubleValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "number",
								"format": "double"
							}
						]
					},
					"floatValue": {
						"type": "number",
						"format": "float"
					},
					"nullableFloatValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "number",
								"format": "float"
							}
						]
					},
					"boolValue": {
						"type": "boolean"
					},
					"nullableBoolValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "boolean"
							}
						]
					},
					"dateTimeValue": {
						"type": "string",
						"format": "date-time"
					},
					"nullableDateTimeValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "string",
								"format": "date-time"
							}
						]
					},
					"dateTimeOffsetValue": {
						"type": "string",
						"format": "date-time"
					},
					"nullableDateTimeOffsetValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "string",
								"format": "date-time"
							}
						]
					},
					"dateOnlyValue": {
						"type": "string",
						"format": "date"
					},
					"nullableDateOnlyValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "string",
								"format": "date"
							}
						]
					},
					"timeOnlyValue": {
						"type": "string",
						"format": "time"
					},
					"nullableTimeOnlyValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "string",
								"format": "time"
							}
						]
					},
					"guidValue": {
						"type": "string",
						"format": "uuid"
					},
					"nullableGuidValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "string",
								"format": "uuid"
							}
						]
					},
					"stringList": {
						"type": "array",
						"items": {
							"type": "string"
						}
					},
					"nullableStringList": {
						"oneOf": [
							{
								"type": "array",
								"items": {
									"type": "string"
								}
							},
							{
								"nullable": true
							}
						]
					},
					"intList": {
						"type": "array",
						"items": {
							"type": "integer",
							"format": "int32"
						}
					},
					"nullableIntList": {
						"oneOf": [
							{
								"type": "array",
								"items": {
									"type": "integer",
									"format": "int32"
								}
							},
							{
								"nullable": true
							}
						]
					},
					"doubleList": {
						"type": "array",
						"items": {
							"type": "number",
							"format": "double"
						}
					},
					"nullableDoubleList": {
						"oneOf": [
							{
								"type": "array",
								"items": {
									"type": "number",
									"format": "double"
								}
							},
							{
								"nullable": true
							}
						]
					},
					"intArray": {
						"type": "array",
						"items": {
							"type": "integer",
							"format": "int32"
						}
					},
					"nullableIntArray": {
						"oneOf": [
							{
								"type": "array",
								"items": {
									"type": "integer",
									"format": "int32"
								}
							},
							{
								"nullable": true
							}
						]
					},
					"readOnlyStringList": {
						"type": "array",
						"items": {
							"type": "string"
						}
					},
					"nullableReadOnlyStringList": {
						"oneOf": [
							{
								"type": "array",
								"items": {
									"type": "string"
								}
							},
							{
								"nullable": true
							}
						]
					},
					"intCollection": {
						"type": "array",
						"items": {
							"type": "integer",
							"format": "int32"
						}
					},
					"nullableIntCollection": {
						"oneOf": [
							{
								"type": "array",
								"items": {
									"type": "integer",
									"format": "int32"
								}
							},
							{
								"nullable": true
							}
						]
					},
					"singleFile": {
						"type": "string",
						"format": "binary"
					},
					"readOnlyList1": {
						"type": "array",
						"items": {
							"type": "string",
							"format": "binary"
						}
					},
					"readOnlyList2": {
						"type": "array",
						"items": {
							"type": "string",
							"format": "binary"
						}
					},
					"singleFileNullable": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"type": "string",
								"format": "binary"
							}
						]
					},
					"readOnlyList1Nullable": {
						"oneOf": [
							{
								"type": "array",
								"items": {
									"type": "string",
									"format": "binary"
								}
							},
							{
								"nullable": true
							}
						]
					},
					"readOnlyList2Nullable": {
						"oneOf": [
							{
								"type": "array",
								"items": {
									"type": "string",
									"format": "binary"
								}
							},
							{
								"nullable": true
							}
						]
					},
					"fileCollectionList": {
						"type": "array",
						"items": {
							"type": "string",
							"format": "binary"
						}
					},
					"nestedObjectValue": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"$ref": "#/components/schemas/ExampleApi.Endpoints.TypeExamples.PostFromForm.NestedObject"
							}
						]
					},
					"nestedObjectList": {
						"oneOf": [
							{
								"type": "array",
								"items": {
									"$ref": "#/components/schemas/ExampleApi.Endpoints.TypeExamples.PostFromForm.NestedObject"
								}
							},
							{
								"nullable": true
							}
						]
					},
					"uriArray": {
						"type": "array",
						"items": {
							"$ref": "#/components/schemas/System.Uri"
						}
					},
					"uriArrayNullable": {
						"oneOf": [
							{
								"type": "array",
								"items": {
									"$ref": "#/components/schemas/System.Uri"
								}
							},
							{
								"nullable": true
							}
						]
					},
					"uriValue": {
						"$ref": "#/components/schemas/System.Uri"
					},
					"uriValueNullable": {
						"oneOf": [
							{
								"nullable": true
							},
							{
								"$ref": "#/components/schemas/System.Uri"
							}
						]
					}
				}
			},
			"ExampleApi.Endpoints.TypeExamples.PostFromForm.ResponseModel": {
				"type": "object",
				"properties": {
					"message": {
						"type": "string"
					},
					"processedPropertiesCount": {
						"type": "integer",
						"format": "int32"
					},
					"uploadedFilesCount": {
						"type": "integer",
						"format": "int32"
					}
				}
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
