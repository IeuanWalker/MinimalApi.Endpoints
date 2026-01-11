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
		          "oneOf": [
		            {
		              "type": "integer",
		              "format": "int32"
		            },
		            {
		              "nullable": true
		            }
		          ]
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
		          "oneOf": [
		            {
		              "type": "integer",
		              "format": "int64"
		            },
		            {
		              "nullable": true
		            }
		          ]
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
		          "oneOf": [
		            {
		              "type": "number"
		            },
		            {
		              "nullable": true
		            }
		          ]
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
		          "oneOf": [
		            {
		              "type": "number",
		              "format": "double"
		            },
		            {
		              "nullable": true
		            }
		          ]
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
		          "oneOf": [
		            {
		              "type": "number",
		              "format": "float"
		            },
		            {
		              "nullable": true
		            }
		          ]
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
		          "oneOf": [
		            {
		              "type": "boolean"
		            },
		            {
		              "nullable": true
		            }
		          ]
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
		          "oneOf": [
		            {
		              "type": "string",
		              "format": "date-time"
		            },
		            {
		              "nullable": true
		            }
		          ]
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
		          "oneOf": [
		            {
		              "type": "string",
		              "format": "date"
		            },
		            {
		              "nullable": true
		            }
		          ]
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
		          "oneOf": [
		            {
		              "type": "string",
		              "format": "time"
		            },
		            {
		              "nullable": true
		            }
		          ]
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
		          "oneOf": [
		            {
		              "type": "string",
		              "format": "uuid"
		            },
		            {
		              "nullable": true
		            }
		          ]
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
		            "type": "string",
		            "format": "uri"
		          }
		        }
		      },
		      {
		        "name": "UriArrayNullable",
		        "in": "query",
		        "schema": {
		          "type": "array",
		          "items": {
		            "type": "string",
		            "format": "uri"
		          }
		        }
		      },
		      {
		        "name": "UriValue",
		        "in": "query",
		        "required": true,
		        "schema": {
		          "type": "string",
		          "format": "uri"
		        }
		      },
		      {
		        "name": "UriValueNullable",
		        "in": "query",
		        "schema": {
		          "type": "string",
		          "format": "uri"
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
		"ExampleApi.Endpoints.TypeExamples.GetFromQuery.ResponseModel": {
		  "type": "object",
		  "properties": {
		    "message": {
		      "type": "string"
		    },
		    "providedParametersCount": {
		      "type": "integer",
		      "format": "int32"
		    },
		    "receivedValues": {
		      "type": "string"
		    }
		  }
		},
		"ExampleApi.Endpoints.TypeExamples.PostFromBody.BaseTests": {
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
		          "type": "string"
		        },
		        {
		          "nullable": true
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
		          "type": "integer",
		          "format": "int32"
		        },
		        {
		          "nullable": true
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
		          "type": "integer",
		          "format": "int64"
		        },
		        {
		          "nullable": true
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
		          "type": "integer",
		          "format": "int32"
		        },
		        {
		          "nullable": true
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
		          "type": "integer",
		          "format": "int32"
		        },
		        {
		          "nullable": true
		        }
		      ]
		    },
		    "decimalValue": {
		      "type": "number"
		    },
		    "nullableDecimalValue": {
		      "oneOf": [
		        {
		          "type": "number"
		        },
		        {
		          "nullable": true
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
		          "type": "number",
		          "format": "double"
		        },
		        {
		          "nullable": true
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
		          "type": "number",
		          "format": "float"
		        },
		        {
		          "nullable": true
		        }
		      ]
		    },
		    "boolValue": {
		      "type": "boolean"
		    },
		    "nullableBoolValue": {
		      "oneOf": [
		        {
		          "type": "boolean"
		        },
		        {
		          "nullable": true
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
		          "type": "string",
		          "format": "date-time"
		        },
		        {
		          "nullable": true
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
		          "type": "string",
		          "format": "date-time"
		        },
		        {
		          "nullable": true
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
		          "type": "string",
		          "format": "date"
		        },
		        {
		          "nullable": true
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
		          "type": "string",
		          "format": "time"
		        },
		        {
		          "nullable": true
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
		          "type": "string",
		          "format": "uuid"
		        },
		        {
		          "nullable": true
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
		          "type": "string",
		          "format": "binary"
		        },
		        {
		          "nullable": true
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
		          "$ref": "#/components/schemas/ExampleApi.Endpoints.TypeExamples.PostFromBody.NestedObject"
		        },
		        {
		          "nullable": true
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
		        "type": "string",
		        "format": "uri"
		      }
		    },
		    "uriArrayNullable": {
		      "oneOf": [
		        {
		          "type": "array",
		          "items": {
		            "type": "string",
		            "format": "uri"
		          }
		        },
		        {
		          "nullable": true
		        }
		      ]
		    },
		    "uriValue": {
		      "type": "string",
		      "format": "uri"
		    },
		    "uriValueNullable": {
		      "oneOf": [
		        {
		          "type": "string",
		          "format": "uri"
		        },
		        {
		          "nullable": true
		        }
		      ]
		    }
		  }
		},
		"ExampleApi.Endpoints.TypeExamples.PostFromBody.NestedObject": {
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
		          "type": "string"
		        },
		        {
		          "nullable": true
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
		          "type": "integer",
		          "format": "int32"
		        },
		        {
		          "nullable": true
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
		          "type": "integer",
		          "format": "int64"
		        },
		        {
		          "nullable": true
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
		          "type": "integer",
		          "format": "int32"
		        },
		        {
		          "nullable": true
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
		          "type": "integer",
		          "format": "int32"
		        },
		        {
		          "nullable": true
		        }
		      ]
		    },
		    "decimalValue": {
		      "type": "number"
		    },
		    "nullableDecimalValue": {
		      "oneOf": [
		        {
		          "type": "number"
		        },
		        {
		          "nullable": true
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
		          "type": "number",
		          "format": "double"
		        },
		        {
		          "nullable": true
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
		          "type": "number",
		          "format": "float"
		        },
		        {
		          "nullable": true
		        }
		      ]
		    },
		    "boolValue": {
		      "type": "boolean"
		    },
		    "nullableBoolValue": {
		      "oneOf": [
		        {
		          "type": "boolean"
		        },
		        {
		          "nullable": true
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
		          "type": "string",
		          "format": "date-time"
		        },
		        {
		          "nullable": true
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
		          "type": "string",
		          "format": "date-time"
		        },
		        {
		          "nullable": true
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
		          "type": "string",
		          "format": "date"
		        },
		        {
		          "nullable": true
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
		          "type": "string",
		          "format": "time"
		        },
		        {
		          "nullable": true
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
		          "type": "string",
		          "format": "uuid"
		        },
		        {
		          "nullable": true
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
		          "type": "string",
		          "format": "binary"
		        },
		        {
		          "nullable": true
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
		          "$ref": "#/components/schemas/ExampleApi.Endpoints.TypeExamples.PostFromBody.NestedObject"
		        },
		        {
		          "nullable": true
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
		        "type": "string",
		        "format": "uri"
		      }
		    },
		    "uriArrayNullable": {
		      "oneOf": [
		        {
		          "type": "array",
		          "items": {
		            "type": "string",
		            "format": "uri"
		          }
		        },
		        {
		          "nullable": true
		        }
		      ]
		    },
		    "uriValue": {
		      "type": "string",
		      "format": "uri"
		    },
		    "uriValueNullable": {
		      "oneOf": [
		        {
		          "type": "string",
		          "format": "uri"
		        },
		        {
		          "nullable": true
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
		          "type": "string"
		        },
		        {
		          "nullable": true
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
		          "type": "integer",
		          "format": "int32"
		        },
		        {
		          "nullable": true
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
		          "type": "integer",
		          "format": "int64"
		        },
		        {
		          "nullable": true
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
		          "type": "integer",
		          "format": "int32"
		        },
		        {
		          "nullable": true
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
		          "type": "integer",
		          "format": "int32"
		        },
		        {
		          "nullable": true
		        }
		      ]
		    },
		    "decimalValue": {
		      "type": "number"
		    },
		    "nullableDecimalValue": {
		      "oneOf": [
		        {
		          "type": "number"
		        },
		        {
		          "nullable": true
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
		          "type": "number",
		          "format": "double"
		        },
		        {
		          "nullable": true
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
		          "type": "number",
		          "format": "float"
		        },
		        {
		          "nullable": true
		        }
		      ]
		    },
		    "boolValue": {
		      "type": "boolean"
		    },
		    "nullableBoolValue": {
		      "oneOf": [
		        {
		          "type": "boolean"
		        },
		        {
		          "nullable": true
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
		          "type": "string",
		          "format": "date-time"
		        },
		        {
		          "nullable": true
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
		          "type": "string",
		          "format": "date-time"
		        },
		        {
		          "nullable": true
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
		          "type": "string",
		          "format": "date"
		        },
		        {
		          "nullable": true
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
		          "type": "string",
		          "format": "time"
		        },
		        {
		          "nullable": true
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
		          "type": "string",
		          "format": "uuid"
		        },
		        {
		          "nullable": true
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
		          "type": "string",
		          "format": "binary"
		        },
		        {
		          "nullable": true
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
		          "$ref": "#/components/schemas/ExampleApi.Endpoints.TypeExamples.PostFromForm.NestedObject"
		        },
		        {
		          "nullable": true
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
		        "type": "string",
		        "format": "uri"
		      }
		    },
		    "uriArrayNullable": {
		      "oneOf": [
		        {
		          "type": "array",
		          "items": {
		            "type": "string",
		            "format": "uri"
		          }
		        },
		        {
		          "nullable": true
		        }
		      ]
		    },
		    "uriValue": {
		      "type": "string",
		      "format": "uri"
		    },
		    "uriValueNullable": {
		      "oneOf": [
		        {
		          "type": "string",
		          "format": "uri"
		        },
		        {
		          "nullable": true
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
		          "type": "string"
		        },
		        {
		          "nullable": true
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
		          "type": "integer",
		          "format": "int32"
		        },
		        {
		          "nullable": true
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
		          "type": "integer",
		          "format": "int64"
		        },
		        {
		          "nullable": true
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
		          "type": "integer",
		          "format": "int32"
		        },
		        {
		          "nullable": true
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
		          "type": "integer",
		          "format": "int32"
		        },
		        {
		          "nullable": true
		        }
		      ]
		    },
		    "decimalValue": {
		      "type": "number"
		    },
		    "nullableDecimalValue": {
		      "oneOf": [
		        {
		          "type": "number"
		        },
		        {
		          "nullable": true
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
		          "type": "number",
		          "format": "double"
		        },
		        {
		          "nullable": true
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
		          "type": "number",
		          "format": "float"
		        },
		        {
		          "nullable": true
		        }
		      ]
		    },
		    "boolValue": {
		      "type": "boolean"
		    },
		    "nullableBoolValue": {
		      "oneOf": [
		        {
		          "type": "boolean"
		        },
		        {
		          "nullable": true
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
		          "type": "string",
		          "format": "date-time"
		        },
		        {
		          "nullable": true
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
		          "type": "string",
		          "format": "date-time"
		        },
		        {
		          "nullable": true
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
		          "type": "string",
		          "format": "date"
		        },
		        {
		          "nullable": true
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
		          "type": "string",
		          "format": "time"
		        },
		        {
		          "nullable": true
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
		          "type": "string",
		          "format": "uuid"
		        },
		        {
		          "nullable": true
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
		          "type": "string",
		          "format": "binary"
		        },
		        {
		          "nullable": true
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
		          "$ref": "#/components/schemas/ExampleApi.Endpoints.TypeExamples.PostFromForm.NestedObject"
		        },
		        {
		          "nullable": true
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
		        "type": "string",
		        "format": "uri"
		      }
		    },
		    "uriArrayNullable": {
		      "oneOf": [
		        {
		          "type": "array",
		          "items": {
		            "type": "string",
		            "format": "uri"
		          }
		        },
		        {
		          "nullable": true
		        }
		      ]
		    },
		    "uriValue": {
		      "type": "string",
		      "format": "uri"
		    },
		    "uriValueNullable": {
		      "oneOf": [
		        {
		          "type": "string",
		          "format": "uri"
		        },
		        {
		          "nullable": true
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
