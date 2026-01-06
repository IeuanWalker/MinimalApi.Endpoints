namespace ExampleApi.IntegrationTests.Infrastructure;

public partial class OpenApiTests : IClassFixture<ExampleApiWebApplicationFactory>
{
	[Fact]
	public async Task OpenApiJson_ValidTypeStructure()
	{
		// Act
		HttpResponseMessage response = await _client.GetAsync("/openapi/v1.json");
		string actualContent = await response.Content.ReadAsStringAsync();
		string normalizedActual = RemoveNewLinesAndWhitespace(NormalizeOpenApiJson(actualContent, GetOptions()));

		// Assert
		normalizedActual.ShouldContain(RemoveNewLinesAndWhitespace(expectedTypeEndpoints));
		normalizedActual.ShouldContain(RemoveNewLinesAndWhitespace(expectedTypeComponenets));
	}

	static readonly string expectedTypeEndpoints = """
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

	static readonly string expectedTypeComponenets = """
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
}
