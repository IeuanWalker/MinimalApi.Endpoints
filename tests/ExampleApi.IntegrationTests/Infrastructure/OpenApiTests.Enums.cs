namespace ExampleApi.IntegrationTests.Infrastructure;

public partial class OpenApiTests : IClassFixture<ExampleApiWebApplicationFactory>
{
	[Fact]
	public async Task OpenApiJson_ValidEnumStructure()
	{
		// Act
		HttpResponseMessage response = await _client.GetAsync("/openapi/v1.json");
		string actualContent = await response.Content.ReadAsStringAsync();
		string normalizedActual = RemoveNewLinesAndWhitespace(NormalizeOpenApiJson(actualContent, GetOptions()));

		// Assert
		normalizedActual.ShouldContain(RemoveNewLinesAndWhitespace(expectedEnumEndpoints));
		normalizedActual.ShouldContain(RemoveNewLinesAndWhitespace(expectedEnumComponenets));
	}

	static readonly string expectedEnumEndpoints = """
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
								{
									"nullable": true
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
		},
		""";

	static readonly string expectedEnumComponenets = """
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
							"$ref": "#/components/schemas/ExampleApi.Endpoints.Enum.PostFromBody.PriorityEnum"
						},
						{
							"nullable": true
						}
					]
				},
				"plainEnumWithoutFluentValidation": {
					"$ref": "#/components/schemas/ExampleApi.Endpoints.Enum.PostFromBody.PriorityEnum"
				},
				"nullableEnumWithoutFluentValidation": {
					"oneOf": [
						{
							"$ref": "#/components/schemas/ExampleApi.Endpoints.Enum.PostFromBody.PriorityEnum"
						},
						{
							"nullable": true
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
							"$ref": "#/components/schemas/ExampleApi.Endpoints.Enum.PostFromBody.TestEnum"
						},
						{
							"nullable": true
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
}
