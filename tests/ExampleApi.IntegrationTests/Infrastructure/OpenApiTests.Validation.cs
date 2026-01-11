namespace ExampleApi.IntegrationTests.Infrastructure;

public partial class OpenApiTests : IClassFixture<ExampleApiWebApplicationFactory>
{
	[Fact]
	public async Task OpenApiJson_ValidValidationStructure()
	{
		// Act
		HttpResponseMessage response = await _client.GetAsync("/openapi/v1.json");
		string actualContent = await response.Content.ReadAsStringAsync();
		string normalizedActual = RemoveNewLinesAndWhitespace(NormalizeOpenApiJson(actualContent, GetOptions()));

		// Assert
		normalizedActual.ShouldContain(RemoveNewLinesAndWhitespace(expectedValidationEndpoints));
		normalizedActual.ShouldContain(RemoveNewLinesAndWhitespace(expectedValidationComponenets));
	}

	static readonly string expectedValidationEndpoints = """
		"/api/v1/validation/DataAnnotations/FromQuery": {
			"get": {
				"tags": [
					"Validation"
				],
				"summary": "DataAnnotationsFromQuery",
				"operationId": "get_ValidationDataAnnotationsFromQuery_17",
				"parameters": [
					{
						"name": "AllBuiltInStringValidators",
						"in": "query",
						"required": true,
						"schema": {
							"maxLength": 22,
							"minLength": 4,
							"pattern": "^[a-zA-Z0-9]+$",
							"type": "string",
							"description": "Validation rules:\n- Is required\n- Must be at least 4 characters and less than 22 characters\n- Must be 4 characters or more\n- Must be 22 characters or fewer\n- Must match pattern: ^[a-zA-Z0-9]+$\n- Must be a valid email address\n- Must be a valid phone number\n- Must be a valid URL\n- Must be a valid credit card number",
							"format": "uri"
						}
					},
					{
						"name": "AllBuiltInNumberValidators",
						"in": "query",
						"required": true,
						"schema": {
							"oneOf": [
								{
									"maximum": 25,
									"minimum": 10,
									"type": "number",
									"description": "Validation rules:\n- Is required\n- Must be >= 10 and <= 25"
								},
								{
									"nullable": true
								}
							]
						}
					},
					{
						"name": "RequiredString",
						"in": "query",
						"required": true,
						"schema": {
							"type": "string",
							"description": "Validation rules:\n- Is required"
						}
					},
					{
						"name": "RequiredInt",
						"in": "query",
						"required": true,
						"schema": {
							"type": "integer",
							"description": "Validation rules:\n- Is required",
							"format": "int32"
						}
					},
					{
						"name": "StringLength",
						"in": "query",
						"required": true,
						"schema": {
							"maxLength": 879,
							"minLength": 12,
							"type": "string",
							"description": "Validation rules:\n- Must be at least 12 characters and less than 879 characters"
						}
					},
					{
						"name": "StringMin",
						"in": "query",
						"required": true,
						"schema": {
							"minLength": 3,
							"type": "string",
							"description": "Validation rules:\n- Must be 3 characters or more"
						}
					},
					{
						"name": "StringMax",
						"in": "query",
						"required": true,
						"schema": {
							"maxLength": 100,
							"type": "string",
							"description": "Validation rules:\n- Must be 100 characters or fewer"
						}
					},
					{
						"name": "RangeInt",
						"in": "query",
						"required": true,
						"schema": {
							"maximum": 25,
							"minimum": 10,
							"type": "integer",
							"description": "Validation rules:\n- Must be >= 10 and <= 25",
							"format": "int32"
						}
					},
					{
						"name": "RangeDateTime",
						"in": "query",
						"required": true,
						"schema": {
							"type": "string",
							"description": "Validation rules:\n- Must be between 1996-06-08 and 2026-01-04",
							"format": "date-time"
						}
					},
					{
						"name": "StringPattern",
						"in": "query",
						"required": true,
						"schema": {
							"pattern": "^[a-zA-Z0-9]+$",
							"type": "string",
							"description": "Validation rules:\n- Must match pattern: ^[a-zA-Z0-9]+$"
						}
					},
					{
						"name": "StringEmail",
						"in": "query",
						"required": true,
						"schema": {
							"type": "string",
							"description": "Validation rules:\n- Must be a valid email address",
							"format": "email"
						}
					},
					{
						"name": "StringPhoneNumber",
						"in": "query",
						"required": true,
						"schema": {
							"type": "string",
							"description": "Validation rules:\n- Must be a valid phone number"
						}
					},
					{
						"name": "StringUrl",
						"in": "query",
						"required": true,
						"schema": {
							"type": "string",
							"description": "Validation rules:\n- Must be a valid URL",
							"format": "uri"
						}
					},
					{
						"name": "ActualUri",
						"in": "query",
						"schema": {
							"type": "string",
							"format": "uri"
						}
					},
					{
						"name": "Compare1",
						"in": "query",
						"required": true,
						"schema": {
							"type": "string",
							"description": "Validation rules:\n- Must match Compare2"
						}
					},
					{
						"name": "Compare2",
						"in": "query",
						"required": true,
						"schema": {
							"type": "string",
							"description": "Validation rules:\n- Must match Compare1"
						}
					},
					{
						"name": "StringCreditCard",
						"in": "query",
						"required": true,
						"schema": {
							"type": "string",
							"description": "Validation rules:\n- Must be a valid credit card number"
						}
					},
					{
						"name": "IntMinMax",
						"in": "query",
						"required": true,
						"schema": {
							"maximum": 2147483647,
							"minimum": -2147483648,
							"type": "integer",
							"description": "Validation rules:\n- Must be >= -2147483648 and <= 2147483647",
							"format": "int32"
						}
					},
					{
						"name": "IntRange",
						"in": "query",
						"required": true,
						"schema": {
							"maximum": 100,
							"minimum": 1,
							"type": "integer",
							"description": "Validation rules:\n- Must be >= 1 and <= 100",
							"format": "int32"
						}
					},
					{
						"name": "DoubleMinMax",
						"in": "query",
						"required": true,
						"schema": {
							"maximum": 1.7976931348623157E+308,
							"minimum": -1.7976931348623157E+308,
							"type": "number",
							"description": "Validation rules:\n- Must be >= -1.7976931348623157E+308 and <= 1.7976931348623157E+308",
							"format": "double"
						}
					},
					{
						"name": "DoubleRange",
						"in": "query",
						"required": true,
						"schema": {
							"maximum": 99.9,
							"minimum": 0.1,
							"type": "number",
							"description": "Validation rules:\n- Must be >= 0.1 and <= 99.9",
							"format": "double"
						}
					},
					{
						"name": "ListStringMinCount",
						"in": "query",
						"required": true,
						"schema": {
							"minLength": 1,
							"type": "array",
							"items": {
								"type": "string"
							},
							"description": "Validation rules:\n- Must be 1 items or more"
						}
					},
					{
						"name": "ListStringMaxCount",
						"in": "query",
						"required": true,
						"schema": {
							"maxLength": 10,
							"type": "array",
							"items": {
								"type": "string"
							},
							"description": "Validation rules:\n- Must be 10 items or fewer"
						}
					},
					{
						"name": "ListStringRangeCount",
						"in": "query",
						"required": true,
						"schema": {
							"maxLength": 10,
							"minLength": 1,
							"type": "array",
							"items": {
								"type": "string"
							},
							"description": "Validation rules:\n- Must be 1 items or more\n- Must be 10 items or fewer"
						}
					},
					{
						"name": "ListIntMinCount",
						"in": "query",
						"required": true,
						"schema": {
							"minLength": 1,
							"type": "array",
							"items": {
								"type": "integer",
								"format": "int32"
							},
							"description": "Validation rules:\n- Must be 1 items or more"
						}
					},
					{
						"name": "ListIntMaxCount",
						"in": "query",
						"required": true,
						"schema": {
							"maxLength": 10,
							"type": "array",
							"items": {
								"type": "integer",
								"format": "int32"
							},
							"description": "Validation rules:\n- Must be 10 items or fewer"
						}
					},
					{
						"name": "ListIntRangeCount",
						"in": "query",
						"required": true,
						"schema": {
							"maxLength": 10,
							"minLength": 1,
							"type": "array",
							"items": {
								"type": "integer",
								"format": "int32"
							},
							"description": "Validation rules:\n- Must be 1 items or more\n- Must be 10 items or fewer"
						}
					},
					{
						"name": "CustomValidatedProperty",
						"in": "query",
						"required": true,
						"schema": {
							"type": "string",
							"description": "Validation rules:\n- CustomValidatedProperty is not valid."
						}
					},
					{
						"name": "CustomValidationWithDefaultMessage",
						"in": "query",
						"required": true,
						"schema": {
							"type": "string",
							"description": "Validation rules:\n- Default error message"
						}
					},
					{
						"name": "CustomValidationWithoutDefaultMessage",
						"in": "query",
						"required": true,
						"schema": {
							"type": "string",
							"description": "Validation rules:\n- The field CustomValidationWithoutDefaultMessage is invalid."
						}
					},
					{
						"name": "CustomValidationWithoutDefaultMessageSetManually",
						"in": "query",
						"required": true,
						"schema": {
							"type": "string",
							"description": "Validation rules:\n- Setting error message manually"
						}
					},
					{
						"name": "CustomValidationWithDefaultMessageOverrideMessage",
						"in": "query",
						"required": true,
						"schema": {
							"type": "string",
							"description": "Validation rules:\n- Override error message"
						}
					}
				],
				"responses": {
					"200": {
						"description": "OK"
					}
				}
			}
		},
		"/api/v1/validation/EndpointA": {
			"get": {
				"tags": [
					"Validation"
				],
				"summary": "Test Endpoint A with Name property validation",
				"operationId": "get_ValidationEndpointA_18",
				"parameters": [
					{
						"name": "Name",
						"in": "query",
						"required": true,
						"schema": {
							"minLength": 5,
							"type": "string",
							"description": "Validation rules:\n- Must be 5 characters or more"
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
			}
		},
		"/api/v1/validation/EndpointB": {
			"get": {
				"tags": [
					"Validation"
				],
				"summary": "Test Endpoint B with Name property validation",
				"operationId": "get_ValidationEndpointB_19",
				"parameters": [
					{
						"name": "Name",
						"in": "query",
						"required": true,
						"schema": {
							"minLength": 10,
							"type": "string",
							"description": "Validation rules:\n- Must be 10 characters or more"
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
			}
		},
		"/api/v1/validation/FluentValidation/FromQuery": {
			"get": {
				"tags": [
					"Validation"
				],
				"summary": "FluentValidationFromQuery",
				"operationId": "get_ValidationFluentValidationFromQuery_20",
				"parameters": [
					{
						"name": "StringMin",
						"in": "query",
						"required": true,
						"schema": {
							"minLength": 3,
							"type": "string",
							"description": "Validation rules:\n- Must be 3 characters or more"
						}
					},
					{
						"name": "StringMax",
						"in": "query",
						"required": true,
						"schema": {
							"maxLength": 50,
							"type": "string",
							"description": "Validation rules:\n- Must be 50 characters or fewer"
						}
					},
					{
						"name": "StringRange",
						"in": "query",
						"required": true,
						"schema": {
							"maxLength": 50,
							"minLength": 3,
							"type": "string",
							"description": "Validation rules:\n- Must be at least 3 characters and less than 50 characters"
						}
					},
					{
						"name": "StringPattern",
						"in": "query",
						"required": true,
						"schema": {
							"pattern": "^[a-zA-Z0-9]+$",
							"type": "string",
							"description": "Validation rules:\n- Must match pattern: ^[a-zA-Z0-9]+$"
						}
					},
					{
						"name": "IntMin",
						"in": "query",
						"required": true,
						"schema": {
							"minimum": 1,
							"type": "integer",
							"description": "Validation rules:\n- Must be >= 1",
							"format": "int32"
						}
					},
					{
						"name": "IntMax",
						"in": "query",
						"required": true,
						"schema": {
							"maximum": 100,
							"type": "integer",
							"description": "Validation rules:\n- Must be <= 100",
							"format": "int32"
						}
					},
					{
						"name": "IntRange",
						"in": "query",
						"required": true,
						"schema": {
							"maximum": 100,
							"minimum": 1,
							"type": "integer",
							"description": "Validation rules:\n- Must be >= 1 and <= 100",
							"format": "int32"
						}
					},
					{
						"name": "DoubleMin",
						"in": "query",
						"required": true,
						"schema": {
							"minimum": 0.1,
							"type": "number",
							"description": "Validation rules:\n- Must be >= 0.1",
							"format": "double"
						}
					},
					{
						"name": "DoubleMax",
						"in": "query",
						"required": true,
						"schema": {
							"maximum": 99.9,
							"type": "number",
							"description": "Validation rules:\n- Must be <= 99.9",
							"format": "double"
						}
					},
					{
						"name": "DoubleRange",
						"in": "query",
						"required": true,
						"schema": {
							"maximum": 99.9,
							"minimum": 0.1,
							"type": "number",
							"description": "Validation rules:\n- Must be >= 0.1 and <= 99.9",
							"format": "double"
						}
					},
					{
						"name": "ListStringMinCount",
						"in": "query",
						"required": true,
						"schema": {
							"type": "array",
							"items": {
								"type": "string"
							},
							"description": "Validation rules:\n- Must contain at least 1 item."
						}
					},
					{
						"name": "ListStringMaxCount",
						"in": "query",
						"required": true,
						"schema": {
							"type": "array",
							"items": {
								"type": "string"
							},
							"description": "Validation rules:\n- Must contain at most 10 items."
						}
					},
					{
						"name": "ListStringRangeCount",
						"in": "query",
						"required": true,
						"schema": {
							"type": "array",
							"items": {
								"type": "string"
							},
							"description": "Validation rules:\n- Must contain between 1 and 10 items."
						}
					},
					{
						"name": "ListIntMinCount",
						"in": "query",
						"required": true,
						"schema": {
							"type": "array",
							"items": {
								"type": "integer",
								"format": "int32"
							},
							"description": "Validation rules:\n- Must contain at least 1 item."
						}
					},
					{
						"name": "ListIntMaxCount",
						"in": "query",
						"required": true,
						"schema": {
							"type": "array",
							"items": {
								"type": "integer",
								"format": "int32"
							},
							"description": "Validation rules:\n- Must contain at most 10 items."
						}
					},
					{
						"name": "ListIntRangeCount",
						"in": "query",
						"required": true,
						"schema": {
							"type": "array",
							"items": {
								"type": "integer",
								"format": "int32"
							},
							"description": "Validation rules:\n- Must contain between 1 and 10 items."
						}
					},
					{
						"name": "AllBuiltInStringValidators",
						"in": "query",
						"required": true,
						"schema": {
							"maxLength": 250,
							"minLength": 2,
							"pattern": "^[a-zA-Z0-9]+$",
							"type": "string",
							"description": "Validation rules:\n- Is required\n- Must not be equal to 'TestNotEqual'.\n- Must be equal to 'TestEqual'.\n- Must be at least 2 characters and less than 250 characters\n- Must be 250 characters or fewer\n- Must be 2 characters or more\n- The specified condition was not met for 'AllBuiltInStringValidators'.\n- Must match pattern: ^[a-zA-Z0-9]+$\n- Must be a valid email address\n- Must be a valid credit card number\n- Must be empty.",
							"format": "email"
						}
					},
					{
						"name": "AllBuiltInNumberValidators",
						"in": "query",
						"required": true,
						"schema": {
							"oneOf": [
								{
									"maximum": 100,
									"exclusiveMaximum": true,
									"minimum": 0,
									"exclusiveMinimum": true,
									"type": "number",
									"description": "Validation rules:\n- Is required\n- Must not be equal to '10'.\n- Must be equal to '10'.\n- Must be < 100\n- Must be less than 'MaxNumberTest'.\n- Must be <= 100\n- Must be less than or equal to 'MaxNumberTest'.\n- Must be > 0\n- Must be greater than 'MinNumberTest'.\n- Must be >= 1\n- Must be greater than or equal to 'MinNumberTest'.\n- Must be empty.\n- Must be >= 1 and <= 10\n- Must not be more than 4 digits in total, with allowance for 2 decimals. "
								},
								{
									"nullable": true
								}
							]
						}
					},
					{
						"name": "MaxNumberTest",
						"in": "query",
						"required": true,
						"schema": {
							"type": "integer",
							"format": "int32"
						}
					},
					{
						"name": "MinNumberTest",
						"in": "query",
						"required": true,
						"schema": {
							"type": "integer",
							"format": "int32"
						}
					},
					{
						"name": "EnumStringValidator",
						"in": "query",
						"required": true,
						"schema": {
							"type": "string",
							"description": "Enum: Success, Failure",
							"enum": [
								"Success",
								"Failure"
							]
						}
					},
					{
						"name": "EnumIntValidator",
						"in": "query",
						"required": true,
						"schema": {
							"type": "integer",
							"description": "Enum: Success, Failure",
							"format": "int32",
							"enum": [
								0,
								1
							],
							"x-enum-varnames": [
								"Success",
								"Failure"
							]
						}
					},
					{
						"name": "EnumTest",
						"in": "query",
						"required": true,
						"schema": {
							"$ref": "#/components/schemas/ExampleApi.Endpoints.Validation.GetFluentValidationFromQuery.StatusEnum"
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
			}
		},
		"/api/v1/validation/DataValidation": {
			"post": {
				"tags": [
					"Validation"
				],
				"summary": "DataAnnotationsFromBody",
				"operationId": "post_ValidationDataValidation_21",
				"requestBody": {
					"content": {
						"application/json": {
							"schema": {
								"$ref": "#/components/schemas/ExampleApi.Endpoints.Validation.PostDataAnnotationsFromBody.RequestModel"
							}
						}
					}
				},
				"responses": {
					"200": {
						"description": "OK"
					}
				}
			}
		},
		"/api/v1/validation/FluentValidation": {
			"post": {
				"tags": [
					"Validation"
				],
				"summary": "FluentValidationFromBody",
				"operationId": "post_ValidationFluentValidation_22",
				"requestBody": {
					"content": {
						"application/json": {
							"schema": {
								"$ref": "#/components/schemas/ExampleApi.Endpoints.Validation.PostFluentValidation.RequestModel"
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
		"/api/v1/validation/FluentValidation/FromForm": {
			"post": {
				"tags": [
					"Validation"
				],
				"summary": "FluentValidationFromForm",
				"operationId": "post_ValidationFluentValidationFromForm_23",
				"requestBody": {
					"content": {
						"multipart/form-data": {
							"schema": {
								"$ref": "#/components/schemas/ExampleApi.Endpoints.Validation.PostFluentValidationFromFrom.RequestModel"
							}
						},
						"application/x-www-form-urlencoded": {
							"schema": {
								"$ref": "#/components/schemas/ExampleApi.Endpoints.Validation.PostFluentValidationFromFrom.RequestModel"
							}
						}
					},
					"required": true
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
		"/api/v1/validation/WithValidation": {
			"post": {
				"tags": [
					"Validation"
				],
				"summary": "ManualWithValidation",
				"operationId": "post_ValidationWithValidation_24",
				"requestBody": {
					"content": {
						"application/json": {
							"schema": {
								"$ref": "#/components/schemas/ExampleApi.Endpoints.Validation.PostWithValidation.RequestModel"
							}
						}
					}
				},
				"responses": {
					"200": {
						"description": "OK"
					}
				}
			}
		},
		"/api/v1/validation/WithValidation/AlterAndRemove": {
			"post": {
				"tags": [
					"Validation"
				],
				"summary": "WithValidationAlterAndRemove",
				"operationId": "post_ValidationWithValidationAlterAndRemove_25",
				"requestBody": {
					"content": {
						"multipart/form-data": {
							"schema": {
								"$ref": "#/components/schemas/ExampleApi.Endpoints.Validation.PostWithValidationAlterRemove.RequestModel"
							}
						},
						"application/x-www-form-urlencoded": {
							"schema": {
								"$ref": "#/components/schemas/ExampleApi.Endpoints.Validation.PostWithValidationAlterRemove.RequestModel"
							}
						}
					},
					"required": true
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

	static readonly string expectedValidationComponenets = """
				"ExampleApi.Endpoints.Validation.GetFluentValidationFromQuery.StatusEnum": {
			"type": "integer",
			"description": "Enum: Success, Failure",
			"enum": [
				0,
				1
			],
			"x-enum-varnames": [
				"Success",
				"Failure"
			]
		},
		"ExampleApi.Endpoints.Validation.PostDataAnnotationsFromBody.NestedObjectModel": {
			"required": [
				"allBuiltInStringValidators",
				"allBuiltInNumberValidators",
				"requiredString",
				"requiredInt"
			],
			"type": "object",
			"properties": {
				"allBuiltInStringValidators": {
					"oneOf": [
						{
							"maxLength": 100,
							"minLength": 3,
							"pattern": "^[a-zA-Z0-9]+$",
							"type": "string",
							"description": "Validation rules:\n- Is required\n- Must be at least 3 characters and less than 50 characters\n- Must be 3 characters or more\n- Must be 100 characters or fewer\n- Must match pattern: ^[a-zA-Z0-9]+$\n- Must be a valid email address\n- Must be a valid phone number\n- Must be a valid URL\n- Must be a valid credit card number\n- AllBuiltInStringValidators is not valid.\n- Default error message",
							"format": "uri"
						},
						{
							"nullable": true
						}
					]
				},
				"allBuiltInNumberValidators": {
					"oneOf": [
						{
							"maximum": 25,
							"minimum": 10,
							"maxLength": 100,
							"minLength": 3,
							"type": "number",
							"description": "Validation rules:\n- Is required\n- Must be >= 10 and <= 25\n- Must be 3 characters or more\n- Must be 100 characters or fewer"
						},
						{
							"nullable": true
						}
					]
				},
				"requiredString": {
					"type": "string",
					"description": "Validation rules:\n- Is required"
				},
				"requiredInt": {
					"type": "integer",
					"description": "Validation rules:\n- Is required",
					"format": "int32"
				},
				"stringLength": {
					"maxLength": 50,
					"minLength": 3,
					"type": "string",
					"description": "Validation rules:\n- Must be at least 3 characters and less than 50 characters"
				},
				"stringMin": {
					"minLength": 3,
					"type": "string",
					"description": "Validation rules:\n- Must be 3 characters or more"
				},
				"listMin": {
					"minLength": 3,
					"type": "array",
					"items": {
						"type": "string"
					},
					"description": "Validation rules:\n- Must be 3 items or more"
				},
				"stringMax": {
					"maxLength": 100,
					"type": "string",
					"description": "Validation rules:\n- Must be 100 characters or fewer"
				},
				"listMax": {
					"maxLength": 10,
					"type": "array",
					"items": {
						"type": "string"
					},
					"description": "Validation rules:\n- Must be 10 items or fewer"
				},
				"rangeInt": {
					"maximum": 25,
					"minimum": 10,
					"type": "integer",
					"description": "Validation rules:\n- Must be >= 10 and <= 25",
					"format": "int32"
				},
				"rangeDateTime": {
					"type": "string",
					"description": "Validation rules:\n- Must be between 1996-06-08 and 2026-01-04",
					"format": "date-time"
				},
				"stringPattern": {
					"pattern": "^[a-zA-Z0-9]+$",
					"type": "string",
					"description": "Validation rules:\n- Must match pattern: ^[a-zA-Z0-9]+$"
				},
				"stringEmail": {
					"type": "string",
					"description": "Validation rules:\n- Must be a valid email address",
					"format": "email"
				},
				"stringPhoneNumber": {
					"type": "string",
					"description": "Validation rules:\n- Must be a valid phone number"
				},
				"stringUrl": {
					"type": "string",
					"description": "Validation rules:\n- Must be a valid URL",
					"format": "uri"
				},
				"actualUri": {
					"oneOf": [
						{
							"type": "string",
							"format": "uri"
						},
						{
							"nullable": true
						}
					]
				},
				"compare1": {
					"type": "string",
					"description": "Validation rules:\n- Must match Compare2"
				},
				"compare2": {
					"type": "string",
					"description": "Validation rules:\n- Must match Compare1"
				},
				"stringCreditCard": {
					"type": "string",
					"description": "Validation rules:\n- Must be a valid credit card number"
				},
				"fileExtension": {
					"oneOf": [
						{
							"nullable": true
						},
						{
							"description": "Validation rules:\n- Must be a file with extension: jpg,png",
							"nullable": true
						}
					]
				},
				"intMin": {
					"maximum": 2147483647,
					"minimum": 1,
					"type": "integer",
					"description": "Validation rules:\n- Must be >= 1 and <= 2147483647",
					"format": "int32"
				},
				"intMax": {
					"maximum": 100,
					"minimum": -2147483648,
					"type": "integer",
					"description": "Validation rules:\n- Must be >= -2147483648 and <= 100",
					"format": "int32"
				},
				"intRange": {
					"maximum": 100,
					"minimum": 1,
					"type": "integer",
					"description": "Validation rules:\n- Must be >= 1 and <= 100",
					"format": "int32"
				},
				"doubleMin": {
					"maximum": 1.7976931348623157E+308,
					"minimum": 0.1,
					"type": "number",
					"description": "Validation rules:\n- Must be >= 0.1 and <= 1.7976931348623157E+308",
					"format": "double"
				},
				"doubleMax": {
					"maximum": 99.9,
					"minimum": -1.7976931348623157E+308,
					"type": "number",
					"description": "Validation rules:\n- Must be >= -1.7976931348623157E+308 and <= 99.9",
					"format": "double"
				},
				"doubleRange": {
					"maximum": 99.9,
					"minimum": 0.1,
					"type": "number",
					"description": "Validation rules:\n- Must be >= 0.1 and <= 99.9",
					"format": "double"
				},
				"listStringMinCount": {
					"minLength": 1,
					"type": "array",
					"items": {
						"minLength": 1,
						"type": "string",
						"description": "Validation rules:\n- Must be 1 items or more"
					},
					"description": "Validation rules:\n- Must be 1 items or more"
				},
				"listStringMaxCount": {
					"maxLength": 10,
					"type": "array",
					"items": {
						"maxLength": 10,
						"type": "string",
						"description": "Validation rules:\n- Must be 10 items or fewer"
					},
					"description": "Validation rules:\n- Must be 10 items or fewer"
				},
				"listStringRangeCount": {
					"maxLength": 10,
					"minLength": 1,
					"type": "array",
					"items": {
						"maxLength": 10,
						"minLength": 1,
						"type": "string",
						"description": "Validation rules:\n- Must be 1 items or more\n- Must be 10 items or fewer"
					},
					"description": "Validation rules:\n- Must be 1 items or more\n- Must be 10 items or fewer"
				},
				"listIntMinCount": {
					"minLength": 1,
					"type": "array",
					"items": {
						"minLength": 1,
						"type": "integer",
						"description": "Validation rules:\n- Must be 1 items or more",
						"format": "int32"
					},
					"description": "Validation rules:\n- Must be 1 items or more"
				},
				"listIntMaxCount": {
					"maxLength": 10,
					"type": "array",
					"items": {
						"maxLength": 10,
						"type": "integer",
						"description": "Validation rules:\n- Must be 10 items or fewer",
						"format": "int32"
					},
					"description": "Validation rules:\n- Must be 10 items or fewer"
				},
				"listIntRangeCount": {
					"maxLength": 10,
					"minLength": 1,
					"type": "array",
					"items": {
						"maxLength": 10,
						"minLength": 1,
						"type": "integer",
						"description": "Validation rules:\n- Must be 1 items or more\n- Must be 10 items or fewer",
						"format": "int32"
					},
					"description": "Validation rules:\n- Must be 1 items or more\n- Must be 10 items or fewer"
				},
				"customValidatedProperty": {
					"type": "string",
					"description": "Validation rules:\n- CustomValidatedProperty is not valid."
				},
				"customValidationWithDefaultMessage": {
					"type": "string",
					"description": "Validation rules:\n- Default error message"
				},
				"customValidationWithoutDefaultMessage": {
					"type": "string",
					"description": "Validation rules:\n- The field CustomValidationWithoutDefaultMessage is invalid."
				},
				"customValidationWithoutDefaultMessageSetManually": {
					"type": "string",
					"description": "Validation rules:\n- Setting error message manually"
				},
				"customValidationWithDefaultMessageOverrideMessage": {
					"type": "string",
					"description": "Validation rules:\n- Override error message"
				}
			}
		},
		"ExampleApi.Endpoints.Validation.PostDataAnnotationsFromBody.RequestModel": {
			"required": [
				"allBuiltInStringValidators",
				"allBuiltInNumberValidators",
				"requiredString",
				"requiredInt",
				"nestedObject"
			],
			"type": "object",
			"properties": {
				"allBuiltInStringValidators": {
					"oneOf": [
						{
							"maxLength": 100,
							"minLength": 3,
							"pattern": "^[a-zA-Z0-9]+$",
							"type": "string",
							"description": "Validation rules:\n- Is required\n- Must be at least 3 characters and less than 50 characters\n- Must be 3 characters or more\n- Must be 100 characters or fewer\n- Must match pattern: ^[a-zA-Z0-9]+$\n- Must be a valid email address\n- Must be a valid phone number\n- Must be a valid URL\n- Must be a valid credit card number",
							"format": "uri"
						},
						{
							"nullable": true
						}
					]
				},
				"allBuiltInNumberValidators": {
					"oneOf": [
						{
							"maximum": 25,
							"minimum": 10,
							"maxLength": 100,
							"minLength": 3,
							"type": "number",
							"description": "Validation rules:\n- Is required\n- Must be >= 10 and <= 25\n- Must be 3 characters or more\n- Must be 100 characters or fewer"
						},
						{
							"nullable": true
						}
					]
				},
				"requiredString": {
					"type": "string",
					"description": "Validation rules:\n- Is required"
				},
				"requiredInt": {
					"type": "integer",
					"description": "Validation rules:\n- Is required",
					"format": "int32"
				},
				"stringLength": {
					"maxLength": 50,
					"minLength": 3,
					"type": "string",
					"description": "Validation rules:\n- Must be at least 3 characters and less than 50 characters"
				},
				"stringMin": {
					"minLength": 3,
					"type": "string",
					"description": "Validation rules:\n- Must be 3 characters or more"
				},
				"listMin": {
					"minLength": 3,
					"type": "array",
					"items": {
						"type": "string"
					},
					"description": "Validation rules:\n- Must be 3 items or more"
				},
				"stringMax": {
					"maxLength": 100,
					"type": "string",
					"description": "Validation rules:\n- Must be 100 characters or fewer"
				},
				"listMax": {
					"maxLength": 10,
					"type": "array",
					"items": {
						"type": "string"
					},
					"description": "Validation rules:\n- Must be 10 items or fewer"
				},
				"rangeInt": {
					"maximum": 25,
					"minimum": 10,
					"type": "integer",
					"description": "Validation rules:\n- Must be >= 10 and <= 25",
					"format": "int32"
				},
				"rangeDateTime": {
					"type": "string",
					"description": "Validation rules:\n- Must be between 1996-06-08 and 2026-01-04",
					"format": "date-time"
				},
				"stringPattern": {
					"pattern": "^[a-zA-Z0-9]+$",
					"type": "string",
					"description": "Validation rules:\n- Must match pattern: ^[a-zA-Z0-9]+$"
				},
				"stringEmail": {
					"type": "string",
					"description": "Validation rules:\n- Must be a valid email address",
					"format": "email"
				},
				"stringPhoneNumber": {
					"type": "string",
					"description": "Validation rules:\n- Must be a valid phone number"
				},
				"stringUrl": {
					"type": "string",
					"description": "Validation rules:\n- Must be a valid URL",
					"format": "uri"
				},
				"actualUri": {
					"oneOf": [
						{
							"type": "string",
							"format": "uri"
						},
						{
							"nullable": true
						}
					]
				},
				"compare1": {
					"type": "string",
					"description": "Validation rules:\n- Must match Compare2"
				},
				"compare2": {
					"type": "string",
					"description": "Validation rules:\n- Must match Compare1"
				},
				"stringCreditCard": {
					"type": "string",
					"description": "Validation rules:\n- Must be a valid credit card number"
				},
				"fileExtension": {
					"oneOf": [
						{
							"nullable": true
						},
						{
							"description": "Validation rules:\n- Must be a file with extension: jpg,png",
							"nullable": true
						}
					]
				},
				"intMin": {
					"maximum": 2147483647,
					"minimum": 1,
					"type": "integer",
					"description": "Validation rules:\n- Must be >= 1 and <= 2147483647",
					"format": "int32"
				},
				"intMax": {
					"maximum": 100,
					"minimum": -2147483648,
					"type": "integer",
					"description": "Validation rules:\n- Must be >= -2147483648 and <= 100",
					"format": "int32"
				},
				"intRange": {
					"maximum": 100,
					"minimum": 1,
					"type": "integer",
					"description": "Validation rules:\n- Must be >= 1 and <= 100",
					"format": "int32"
				},
				"doubleMin": {
					"maximum": 1.7976931348623157E+308,
					"minimum": 0.1,
					"type": "number",
					"description": "Validation rules:\n- Must be >= 0.1 and <= 1.7976931348623157E+308",
					"format": "double"
				},
				"doubleMax": {
					"maximum": 99.9,
					"minimum": -1.7976931348623157E+308,
					"type": "number",
					"description": "Validation rules:\n- Must be >= -1.7976931348623157E+308 and <= 99.9",
					"format": "double"
				},
				"doubleRange": {
					"maximum": 99.9,
					"minimum": 0.1,
					"type": "number",
					"description": "Validation rules:\n- Must be >= 0.1 and <= 99.9",
					"format": "double"
				},
				"listStringMinCount": {
					"minLength": 1,
					"type": "array",
					"items": {
						"minLength": 1,
						"type": "string",
						"description": "Validation rules:\n- Must be 1 items or more"
					},
					"description": "Validation rules:\n- Must be 1 items or more"
				},
				"listStringMaxCount": {
					"maxLength": 10,
					"type": "array",
					"items": {
						"maxLength": 10,
						"type": "string",
						"description": "Validation rules:\n- Must be 10 items or fewer"
					},
					"description": "Validation rules:\n- Must be 10 items or fewer"
				},
				"listStringRangeCount": {
					"maxLength": 10,
					"minLength": 1,
					"type": "array",
					"items": {
						"maxLength": 10,
						"minLength": 1,
						"type": "string",
						"description": "Validation rules:\n- Must be 1 items or more\n- Must be 10 items or fewer"
					},
					"description": "Validation rules:\n- Must be 1 items or more\n- Must be 10 items or fewer"
				},
				"listIntMinCount": {
					"minLength": 1,
					"type": "array",
					"items": {
						"minLength": 1,
						"type": "integer",
						"description": "Validation rules:\n- Must be 1 items or more",
						"format": "int32"
					},
					"description": "Validation rules:\n- Must be 1 items or more"
				},
				"listIntMaxCount": {
					"maxLength": 10,
					"type": "array",
					"items": {
						"maxLength": 10,
						"type": "integer",
						"description": "Validation rules:\n- Must be 10 items or fewer",
						"format": "int32"
					},
					"description": "Validation rules:\n- Must be 10 items or fewer"
				},
				"listIntRangeCount": {
					"maxLength": 10,
					"minLength": 1,
					"type": "array",
					"items": {
						"maxLength": 10,
						"minLength": 1,
						"type": "integer",
						"description": "Validation rules:\n- Must be 1 items or more\n- Must be 10 items or fewer",
						"format": "int32"
					},
					"description": "Validation rules:\n- Must be 1 items or more\n- Must be 10 items or fewer"
				},
				"customValidatedProperty": {
					"type": "string",
					"description": "Validation rules:\n- CustomValidatedProperty is not valid."
				},
				"throwsCustomValidationProperty": {
					"type": "string",
					"description": "Validation rules:\n- Default error message"
				},
				"nestedObject": {
					"allOf": [
						{
							"$ref": "#/components/schemas/ExampleApi.Endpoints.Validation.PostDataAnnotationsFromBody.NestedObjectModel"
						}
					],
					"description": "Validation rules:\n- Required"
				},
				"listNestedObject": {
					"oneOf": [
						{
							"maxLength": 10,
							"minLength": 0,
							"type": "array",
							"items": {
								"$ref": "#/components/schemas/ExampleApi.Endpoints.Validation.PostDataAnnotationsFromBody.NestedObjectModel"
							},
							"description": "Validation rules:\n- Must be 0 items or more\n- Must be 10 items or fewer"
						},
						{
							"nullable": true
						}
					]
				},
				"customValidationWithDefaultMessage": {
					"type": "string",
					"description": "Validation rules:\n- Default error message"
				},
				"customValidationWithoutDefaultMessage": {
					"type": "string",
					"description": "Validation rules:\n- The field CustomValidationWithoutDefaultMessage is invalid."
				},
				"customValidationWithoutDefaultMessageSetManually": {
					"type": "string",
					"description": "Validation rules:\n- Setting error message manually"
				},
				"customValidationWithDefaultMessageOverrideMessage": {
					"type": "string",
					"description": "Validation rules:\n- Override error message"
				}
			}
		},
		"ExampleApi.Endpoints.Validation.PostFluentValidation.NestedObjectModel": {
			"type": "object",
			"properties": {
				"stringMin": {
					"minLength": 3,
					"type": "string",
					"description": "Validation rules:\n- Must be 3 characters or more"
				},
				"stringMax": {
					"maxLength": 50,
					"type": "string",
					"description": "Validation rules:\n- Must be 50 characters or fewer"
				},
				"stringRange": {
					"maxLength": 50,
					"minLength": 3,
					"type": "string",
					"description": "Validation rules:\n- Must be at least 3 characters and less than 50 characters"
				},
				"stringPattern": {
					"pattern": "^[a-zA-Z0-9]+$",
					"type": "string",
					"description": "Validation rules:\n- Must match pattern: ^[a-zA-Z0-9]+$"
				},
				"intMin": {
					"minimum": 1,
					"type": "integer",
					"description": "Validation rules:\n- Must be >= 1",
					"format": "int32"
				},
				"intMax": {
					"maximum": 100,
					"type": "integer",
					"description": "Validation rules:\n- Must be <= 100",
					"format": "int32"
				},
				"intRange": {
					"maximum": 100,
					"minimum": 1,
					"type": "integer",
					"description": "Validation rules:\n- Must be >= 1 and <= 100",
					"format": "int32"
				},
				"doubleMin": {
					"minimum": 0.1,
					"type": "number",
					"description": "Validation rules:\n- Must be >= 0.1",
					"format": "double"
				},
				"doubleMax": {
					"maximum": 99.9,
					"type": "number",
					"description": "Validation rules:\n- Must be <= 99.9",
					"format": "double"
				},
				"doubleRange": {
					"maximum": 99.9,
					"minimum": 0.1,
					"type": "number",
					"description": "Validation rules:\n- Must be >= 0.1 and <= 99.9",
					"format": "double"
				},
				"listStringMinCount": {
					"type": "array",
					"items": {
						"type": "string",
						"description": "Validation rules:\n- Must contain at least 1 item."
					},
					"description": "Validation rules:\n- Must contain at least 1 item."
				},
				"listStringMaxCount": {
					"type": "array",
					"items": {
						"type": "string",
						"description": "Validation rules:\n- Must contain at most 10 items."
					},
					"description": "Validation rules:\n- Must contain at most 10 items."
				},
				"listStringRangeCount": {
					"type": "array",
					"items": {
						"type": "string",
						"description": "Validation rules:\n- Must contain between 1 and 10 items."
					},
					"description": "Validation rules:\n- Must contain between 1 and 10 items."
				},
				"listIntMinCount": {
					"type": "array",
					"items": {
						"type": "integer",
						"description": "Validation rules:\n- Must contain at least 1 item.",
						"format": "int32"
					},
					"description": "Validation rules:\n- Must contain at least 1 item."
				},
				"listIntMaxCount": {
					"type": "array",
					"items": {
						"type": "integer",
						"description": "Validation rules:\n- Must contain at most 10 items.",
						"format": "int32"
					},
					"description": "Validation rules:\n- Must contain at most 10 items."
				},
				"listIntRangeCount": {
					"type": "array",
					"items": {
						"type": "integer",
						"description": "Validation rules:\n- Must contain between 1 and 10 items.",
						"format": "int32"
					},
					"description": "Validation rules:\n- Must contain between 1 and 10 items."
				}
			}
		},
		"ExampleApi.Endpoints.Validation.PostFluentValidation.RequestModel": {
			"required": [
				"nestedObject",
				"enumStringValidator",
				"enumIntValidator",
				"enumTest",
				"allBuiltInStringValidators",
				"allBuiltInNumberValidators"
			],
			"type": "object",
			"properties": {
				"stringMin": {
					"minLength": 3,
					"type": "string",
					"description": "Validation rules:\n- Must be 3 characters or more"
				},
				"stringMax": {
					"maxLength": 50,
					"type": "string",
					"description": "Validation rules:\n- Must be 50 characters or fewer"
				},
				"stringRange": {
					"maxLength": 50,
					"minLength": 3,
					"type": "string",
					"description": "Validation rules:\n- Must be at least 3 characters and less than 50 characters"
				},
				"stringPattern": {
					"pattern": "^[a-zA-Z0-9]+$",
					"type": "string",
					"description": "Validation rules:\n- Must match pattern: ^[a-zA-Z0-9]+$"
				},
				"intMin": {
					"minimum": 1,
					"type": "integer",
					"description": "Validation rules:\n- Must be >= 1",
					"format": "int32"
				},
				"intMax": {
					"maximum": 100,
					"type": "integer",
					"description": "Validation rules:\n- Must be <= 100",
					"format": "int32"
				},
				"intRange": {
					"maximum": 100,
					"minimum": 1,
					"type": "integer",
					"description": "Validation rules:\n- Must be >= 1 and <= 100",
					"format": "int32"
				},
				"doubleMin": {
					"minimum": 0.1,
					"type": "number",
					"description": "Validation rules:\n- Must be >= 0.1",
					"format": "double"
				},
				"doubleMax": {
					"maximum": 99.9,
					"type": "number",
					"description": "Validation rules:\n- Must be <= 99.9",
					"format": "double"
				},
				"doubleRange": {
					"maximum": 99.9,
					"minimum": 0.1,
					"type": "number",
					"description": "Validation rules:\n- Must be >= 0.1 and <= 99.9",
					"format": "double"
				},
				"listStringMinCount": {
					"type": "array",
					"items": {
						"type": "string",
						"description": "Validation rules:\n- Must contain at least 1 item."
					},
					"description": "Validation rules:\n- Must contain at least 1 item."
				},
				"listStringMaxCount": {
					"type": "array",
					"items": {
						"type": "string",
						"description": "Validation rules:\n- Must contain at most 10 items."
					},
					"description": "Validation rules:\n- Must contain at most 10 items."
				},
				"listStringRangeCount": {
					"type": "array",
					"items": {
						"type": "string",
						"description": "Validation rules:\n- Must contain between 1 and 10 items."
					},
					"description": "Validation rules:\n- Must contain between 1 and 10 items."
				},
				"listIntMinCount": {
					"type": "array",
					"items": {
						"type": "integer",
						"description": "Validation rules:\n- Must contain at least 1 item.",
						"format": "int32"
					},
					"description": "Validation rules:\n- Must contain at least 1 item."
				},
				"listIntMaxCount": {
					"type": "array",
					"items": {
						"type": "integer",
						"description": "Validation rules:\n- Must contain at most 10 items.",
						"format": "int32"
					},
					"description": "Validation rules:\n- Must contain at most 10 items."
				},
				"listIntRangeCount": {
					"type": "array",
					"items": {
						"type": "integer",
						"description": "Validation rules:\n- Must contain between 1 and 10 items.",
						"format": "int32"
					},
					"description": "Validation rules:\n- Must contain between 1 and 10 items."
				},
				"nestedObject": {
					"allOf": [
						{
							"$ref": "#/components/schemas/ExampleApi.Endpoints.Validation.PostFluentValidation.NestedObjectModel"
						}
					],
					"description": "Validation rules:\n- Required"
				},
				"listNestedObject": {
					"oneOf": [
						{
							"type": "array",
							"items": {
								"$ref": "#/components/schemas/ExampleApi.Endpoints.Validation.PostFluentValidation.NestedObjectModel"
							}
						},
						{
							"nullable": true
						}
					]
				},
				"allBuiltInStringValidators": {
					"oneOf": [
						{
							"maxLength": 250,
							"minLength": 2,
							"pattern": "^[a-zA-Z0-9]+$",
							"type": "string",
							"description": "Validation rules:\n- Is required\n- Must not be equal to 'TestNotEqual'.\n- Must be equal to 'TestEqual'.\n- Must be at least 2 characters and less than 250 characters\n- Must be 250 characters or fewer\n- Must be 2 characters or more\n- The specified condition was not met for 'AllBuiltInStringValidators'.\n- Must match pattern: ^[a-zA-Z0-9]+$\n- Must be a valid email address\n- Must be a valid credit card number\n- Must be empty.",
							"format": "email"
						},
						{
							"nullable": true
						}
					]
				},
				"allBuiltInNumberValidators": {
					"oneOf": [
						{
							"maximum": 100,
							"exclusiveMaximum": true,
							"minimum": 0,
							"exclusiveMinimum": true,
							"type": "number",
							"description": "Validation rules:\n- Is required\n- Must not be equal to '10'.\n- Must be equal to '10'.\n- Must be < 100\n- Must be less than 'MaxNumberTest'.\n- Must be <= 100\n- Must be less than or equal to 'MaxNumberTest'.\n- Must be > 0\n- Must be greater than 'MinNumberTest'.\n- Must be >= 1\n- Must be greater than or equal to 'MinNumberTest'.\n- Must be empty.\n- Must be >= 1 and <= 10\n- Must not be more than 4 digits in total, with allowance for 2 decimals. "
						},
						{
							"nullable": true
						}
					]
				},
				"maxNumberTest": {
					"type": "integer",
					"format": "int32"
				},
				"minNumberTest": {
					"type": "integer",
					"format": "int32"
				},
				"enumStringValidator": {
					"type": "string",
					"description": "Enum: Success, Failure",
					"enum": [
						"Success",
						"Failure"
					]
				},
				"enumIntValidator": {
					"type": "integer",
					"description": "Enum: Success, Failure",
					"format": "int32",
					"enum": [
						0,
						1
					],
					"x-enum-varnames": [
						"Success",
						"Failure"
					]
				},
				"enumTest": {
					"$ref": "#/components/schemas/ExampleApi.Endpoints.Validation.PostFluentValidation.StatusEnum"
				}
			}
		},
		"ExampleApi.Endpoints.Validation.PostFluentValidation.StatusEnum": {
			"type": "integer",
			"description": "Enum: Success, Failure",
			"enum": [
				0,
				1
			],
			"x-enum-varnames": [
				"Success",
				"Failure"
			]
		},
		"ExampleApi.Endpoints.Validation.PostFluentValidationFromFrom.NestedObjectModel": {
			"type": "object",
			"properties": {
				"stringMin": {
					"minLength": 3,
					"type": "string",
					"description": "Validation rules:\n- Must be 3 characters or more"
				},
				"stringMax": {
					"maxLength": 50,
					"type": "string",
					"description": "Validation rules:\n- Must be 50 characters or fewer"
				},
				"stringRange": {
					"maxLength": 50,
					"minLength": 3,
					"type": "string",
					"description": "Validation rules:\n- Must be at least 3 characters and less than 50 characters"
				},
				"stringPattern": {
					"pattern": "^[a-zA-Z0-9]+$",
					"type": "string",
					"description": "Validation rules:\n- Must match pattern: ^[a-zA-Z0-9]+$"
				},
				"intMin": {
					"minimum": 1,
					"type": "integer",
					"description": "Validation rules:\n- Must be >= 1",
					"format": "int32"
				},
				"intMax": {
					"maximum": 100,
					"type": "integer",
					"description": "Validation rules:\n- Must be <= 100",
					"format": "int32"
				},
				"intRange": {
					"maximum": 100,
					"minimum": 1,
					"type": "integer",
					"description": "Validation rules:\n- Must be >= 1 and <= 100",
					"format": "int32"
				},
				"doubleMin": {
					"minimum": 0.1,
					"type": "number",
					"description": "Validation rules:\n- Must be >= 0.1",
					"format": "double"
				},
				"doubleMax": {
					"maximum": 99.9,
					"type": "number",
					"description": "Validation rules:\n- Must be <= 99.9",
					"format": "double"
				},
				"doubleRange": {
					"maximum": 99.9,
					"minimum": 0.1,
					"type": "number",
					"description": "Validation rules:\n- Must be >= 0.1 and <= 99.9",
					"format": "double"
				},
				"listStringMinCount": {
					"type": "array",
					"items": {
						"type": "string",
						"description": "Validation rules:\n- Must contain at least 1 item."
					},
					"description": "Validation rules:\n- Must contain at least 1 item."
				},
				"listStringMaxCount": {
					"type": "array",
					"items": {
						"type": "string",
						"description": "Validation rules:\n- Must contain at most 10 items."
					},
					"description": "Validation rules:\n- Must contain at most 10 items."
				},
				"listStringRangeCount": {
					"type": "array",
					"items": {
						"type": "string",
						"description": "Validation rules:\n- Must contain between 1 and 10 items."
					},
					"description": "Validation rules:\n- Must contain between 1 and 10 items."
				},
				"listIntMinCount": {
					"type": "array",
					"items": {
						"type": "integer",
						"description": "Validation rules:\n- Must contain at least 1 item.",
						"format": "int32"
					},
					"description": "Validation rules:\n- Must contain at least 1 item."
				},
				"listIntMaxCount": {
					"type": "array",
					"items": {
						"type": "integer",
						"description": "Validation rules:\n- Must contain at most 10 items.",
						"format": "int32"
					},
					"description": "Validation rules:\n- Must contain at most 10 items."
				},
				"listIntRangeCount": {
					"type": "array",
					"items": {
						"type": "integer",
						"description": "Validation rules:\n- Must contain between 1 and 10 items.",
						"format": "int32"
					},
					"description": "Validation rules:\n- Must contain between 1 and 10 items."
				}
			}
		},
		"ExampleApi.Endpoints.Validation.PostFluentValidationFromFrom.RequestModel": {
			"required": [
				"nestedObject",
				"enumStringValidator",
				"enumIntValidator",
				"enumTest",
				"allBuiltInStringValidators",
				"allBuiltInNumberValidators"
			],
			"type": "object",
			"properties": {
				"stringMin": {
					"minLength": 3,
					"type": "string",
					"description": "Validation rules:\n- Must be 3 characters or more"
				},
				"stringMax": {
					"maxLength": 50,
					"type": "string",
					"description": "Validation rules:\n- Must be 50 characters or fewer"
				},
				"stringRange": {
					"maxLength": 50,
					"minLength": 3,
					"type": "string",
					"description": "Validation rules:\n- Must be at least 3 characters and less than 50 characters"
				},
				"stringPattern": {
					"pattern": "^[a-zA-Z0-9]+$",
					"type": "string",
					"description": "Validation rules:\n- Must match pattern: ^[a-zA-Z0-9]+$"
				},
				"intMin": {
					"minimum": 1,
					"type": "integer",
					"description": "Validation rules:\n- Must be >= 1",
					"format": "int32"
				},
				"intMax": {
					"maximum": 100,
					"type": "integer",
					"description": "Validation rules:\n- Must be <= 100",
					"format": "int32"
				},
				"intRange": {
					"maximum": 100,
					"minimum": 1,
					"type": "integer",
					"description": "Validation rules:\n- Must be >= 1 and <= 100",
					"format": "int32"
				},
				"doubleMin": {
					"minimum": 0.1,
					"type": "number",
					"description": "Validation rules:\n- Must be >= 0.1",
					"format": "double"
				},
				"doubleMax": {
					"maximum": 99.9,
					"type": "number",
					"description": "Validation rules:\n- Must be <= 99.9",
					"format": "double"
				},
				"doubleRange": {
					"maximum": 99.9,
					"minimum": 0.1,
					"type": "number",
					"description": "Validation rules:\n- Must be >= 0.1 and <= 99.9",
					"format": "double"
				},
				"listStringMinCount": {
					"type": "array",
					"items": {
						"type": "string",
						"description": "Validation rules:\n- Must contain at least 1 item."
					},
					"description": "Validation rules:\n- Must contain at least 1 item."
				},
				"listStringMaxCount": {
					"type": "array",
					"items": {
						"type": "string",
						"description": "Validation rules:\n- Must contain at most 10 items."
					},
					"description": "Validation rules:\n- Must contain at most 10 items."
				},
				"listStringRangeCount": {
					"type": "array",
					"items": {
						"type": "string",
						"description": "Validation rules:\n- Must contain between 1 and 10 items."
					},
					"description": "Validation rules:\n- Must contain between 1 and 10 items."
				},
				"listIntMinCount": {
					"type": "array",
					"items": {
						"type": "integer",
						"description": "Validation rules:\n- Must contain at least 1 item.",
						"format": "int32"
					},
					"description": "Validation rules:\n- Must contain at least 1 item."
				},
				"listIntMaxCount": {
					"type": "array",
					"items": {
						"type": "integer",
						"description": "Validation rules:\n- Must contain at most 10 items.",
						"format": "int32"
					},
					"description": "Validation rules:\n- Must contain at most 10 items."
				},
				"listIntRangeCount": {
					"type": "array",
					"items": {
						"type": "integer",
						"description": "Validation rules:\n- Must contain between 1 and 10 items.",
						"format": "int32"
					},
					"description": "Validation rules:\n- Must contain between 1 and 10 items."
				},
				"nestedObject": {
					"allOf": [
						{
							"$ref": "#/components/schemas/ExampleApi.Endpoints.Validation.PostFluentValidationFromFrom.NestedObjectModel"
						}
					],
					"description": "Validation rules:\n- Required"
				},
				"listNestedObject": {
					"oneOf": [
						{
							"type": "array",
							"items": {
								"$ref": "#/components/schemas/ExampleApi.Endpoints.Validation.PostFluentValidationFromFrom.NestedObjectModel"
							}
						},
						{
							"nullable": true
						}
					]
				},
				"allBuiltInStringValidators": {
					"oneOf": [
						{
							"maxLength": 250,
							"minLength": 2,
							"pattern": "^[a-zA-Z0-9]+$",
							"type": "string",
							"description": "Validation rules:\n- Is required\n- Must not be equal to 'TestNotEqual'.\n- Must be equal to 'TestEqual'.\n- Must be at least 2 characters and less than 250 characters\n- Must be 250 characters or fewer\n- Must be 2 characters or more\n- The specified condition was not met for 'AllBuiltInStringValidators'.\n- Must match pattern: ^[a-zA-Z0-9]+$\n- Must be a valid email address\n- Must be a valid credit card number\n- Must be empty.",
							"format": "email"
						},
						{
							"nullable": true
						}
					]
				},
				"allBuiltInNumberValidators": {
					"oneOf": [
						{
							"maximum": 100,
							"exclusiveMaximum": true,
							"minimum": 0,
							"exclusiveMinimum": true,
							"type": "number",
							"description": "Validation rules:\n- Is required\n- Must not be equal to '10'.\n- Must be equal to '10'.\n- Must be < 100\n- Must be less than 'MaxNumberTest'.\n- Must be <= 100\n- Must be less than or equal to 'MaxNumberTest'.\n- Must be > 0\n- Must be greater than 'MinNumberTest'.\n- Must be >= 1\n- Must be greater than or equal to 'MinNumberTest'.\n- Must be empty.\n- Must be >= 1 and <= 10\n- Must not be more than 4 digits in total, with allowance for 2 decimals. "
						},
						{
							"nullable": true
						}
					]
				},
				"maxNumberTest": {
					"type": "integer",
					"format": "int32"
				},
				"minNumberTest": {
					"type": "integer",
					"format": "int32"
				},
				"enumStringValidator": {
					"type": "string",
					"description": "Enum: Success, Failure",
					"enum": [
						"Success",
						"Failure"
					]
				},
				"enumIntValidator": {
					"type": "integer",
					"description": "Enum: Success, Failure",
					"format": "int32",
					"enum": [
						0,
						1
					],
					"x-enum-varnames": [
						"Success",
						"Failure"
					]
				},
				"enumTest": {
					"$ref": "#/components/schemas/ExampleApi.Endpoints.Validation.PostFluentValidationFromFrom.StatusEnum"
				}
			}
		},
		"ExampleApi.Endpoints.Validation.PostFluentValidationFromFrom.StatusEnum": {
			"type": "integer",
			"description": "Enum: Success, Failure",
			"enum": [
				0,
				1
			],
			"x-enum-varnames": [
				"Success",
				"Failure"
			]
		},
		"ExampleApi.Endpoints.Validation.PostWithValidation.NestedObjectModel": {
			"type": "object",
			"properties": {
				"stringMin": {
					"minLength": 5,
					"type": "string",
					"description": "Nested string minimum length\n\nValidation rules:\n- Must be 5 characters or more"
				},
				"stringMax": {
					"maxLength": 100,
					"type": "string",
					"description": "Validation rules:\n- Must be 100 characters or fewer"
				},
				"stringRange": {
					"type": "string"
				},
				"stringPattern": {
					"pattern": "^[A-Z]+$",
					"type": "string",
					"description": "Validation rules:\n- Must match pattern: ^[A-Z]+$"
				},
				"intMin": {
					"minimum": 10,
					"type": "integer",
					"description": "Validation rules:\n- Must be >= 10",
					"format": "int32"
				},
				"intMax": {
					"maximum": 500,
					"exclusiveMaximum": true,
					"type": "integer",
					"description": "Validation rules:\n- Must be < 500",
					"format": "int32"
				},
				"intRange": {
					"type": "integer",
					"format": "int32"
				},
				"doubleMin": {
					"type": "number",
					"format": "double"
				},
				"doubleMax": {
					"maximum": 1000,
					"type": "number",
					"description": "Validation rules:\n- Must be <= 1000",
					"format": "double"
				},
				"doubleRange": {
					"type": "number",
					"format": "double"
				},
				"listStringMinCount": {
					"type": "array",
					"items": {
						"type": "string"
					}
				},
				"listStringMaxCount": {
					"type": "array",
					"items": {
						"type": "string"
					}
				},
				"listStringRangeCount": {
					"type": "array",
					"items": {
						"type": "string"
					}
				},
				"listIntMinCount": {
					"type": "array",
					"items": {
						"type": "integer",
						"format": "int32"
					}
				},
				"listIntMaxCount": {
					"type": "array",
					"items": {
						"type": "integer",
						"format": "int32"
					}
				},
				"listIntRangeCount": {
					"type": "array",
					"items": {
						"type": "integer",
						"format": "int32"
					}
				}
			}
		},
		"ExampleApi.Endpoints.Validation.PostWithValidation.RequestModel": {
			"required": [
				"allRules",
				"nestedObject"
			],
			"type": "object",
			"properties": {
				"stringMin": {
					"minLength": 3,
					"type": "string",
					"description": "Validation rules:\n- Must be 3 characters or more"
				},
				"stringMax": {
					"maxLength": 50,
					"type": "string",
					"description": "Validation rules:\n- Must be 50 characters or fewer"
				},
				"stringRange": {
					"maxLength": 50,
					"minLength": 3,
					"type": "string",
					"description": "Validation rules:\n- Must be 3 characters or more\n- Must be 50 characters or fewer"
				},
				"stringPattern": {
					"pattern": "^[a-zA-Z0-9]+$",
					"type": "string",
					"description": "Validation rules:\n- Must match pattern: ^[a-zA-Z0-9]+$"
				},
				"intMin": {
					"type": "integer",
					"description": "Validation rules:\n- Must be >= 1",
					"format": "int32"
				},
				"intMax": {
					"type": "integer",
					"description": "Validation rules:\n- Must be <= 100",
					"format": "int32"
				},
				"intRange": {
					"type": "integer",
					"description": "Validation rules:\n- Must be between 1 and 100",
					"format": "int32"
				},
				"doubleMin": {
					"type": "number",
					"description": "Validation rules:\n- Must be >= 0.1",
					"format": "double"
				},
				"doubleMax": {
					"type": "number",
					"description": "Validation rules:\n- Must be <= 99.9",
					"format": "double"
				},
				"doubleRange": {
					"type": "number",
					"description": "Validation rules:\n- Must be between 0.1 and 99.9",
					"format": "double"
				},
				"listStringMinCount": {
					"type": "array",
					"items": {
						"type": "string",
						"description": "Validation rules:\n- Must contain at least 1 item."
					},
					"description": "Validation rules:\n- Must contain at least 1 item."
				},
				"listStringMaxCount": {
					"type": "array",
					"items": {
						"type": "string",
						"description": "Validation rules:\n- Must contain at most 10 items."
					},
					"description": "Validation rules:\n- Must contain at most 10 items."
				},
				"listStringRangeCount": {
					"type": "array",
					"items": {
						"type": "string",
						"description": "Validation rules:\n- Must contain between 1 and 10 items."
					},
					"description": "Validation rules:\n- Must contain between 1 and 10 items."
				},
				"listIntMinCount": {
					"type": "array",
					"items": {
						"type": "integer",
						"description": "Validation rules:\n- Must contain at least 1 item.",
						"format": "int32"
					},
					"description": "Validation rules:\n- Must contain at least 1 item."
				},
				"listIntMaxCount": {
					"type": "array",
					"items": {
						"type": "integer",
						"description": "Validation rules:\n- Must contain at most 10 items.",
						"format": "int32"
					},
					"description": "Validation rules:\n- Must contain at most 10 items."
				},
				"listIntRangeCount": {
					"type": "array",
					"items": {
						"type": "integer",
						"description": "Validation rules:\n- Must contain between 1 and 10 items.",
						"format": "int32"
					},
					"description": "Validation rules:\n- Must contain between 1 and 10 items."
				},
				"allRules": {
					"maximum": 100,
					"exclusiveMaximum": true,
					"minimum": 10,
					"exclusiveMinimum": true,
					"maxLength": 100,
					"minLength": 10,
					"pattern": "^[a-zA-Z0-9]+$",
					"type": "string",
					"description": "Custom description\n\nValidation rules:\n- Is required\n- Must be 10 characters or more\n- Must be 100 characters or fewer\n- Must be at least 10 characters and less than 100 characters\n- Must match pattern: ^[a-zA-Z0-9]+$\n- Must be a valid email address\n- Must be a valid URL\n- Custom rule\n- Must be > 10\n- Must be >= 11\n- Must be < 100\n- Must be <= 100\n- Must be >= 10 and <= 100",
					"format": "uri"
				},
				"nestedObject": {
					"allOf": [
						{
							"$ref": "#/components/schemas/ExampleApi.Endpoints.Validation.PostWithValidation.NestedObjectModel"
						}
					],
					"description": "Validation rules:\n- Required"
				},
				"listNestedObject": {
					"oneOf": [
						{
							"type": "array",
							"items": {
								"$ref": "#/components/schemas/ExampleApi.Endpoints.Validation.PostWithValidation.NestedObjectModel"
							}
						},
						{
							"nullable": true
						}
					]
				}
			}
		},
		"ExampleApi.Endpoints.Validation.PostWithValidationAlterRemove.RequestModel": {
			"type": "object",
			"properties": {
				"alter": {
					"minLength": 10,
					"pattern": "^[a-zA-Z0-9]+$",
					"type": "string",
					"description": "Validation rules:\n- Must be alphanumeric\n- Must be 10 characters or more"
				},
				"remove1": {
					"minLength": 10,
					"pattern": "^[a-zA-Z0-9]+$",
					"type": "string",
					"description": "Validation rules:\n- Must be 10 characters or more\n- Must match pattern: ^[a-zA-Z0-9]+$"
				},
				"removeAll": {
					"type": "string"
				}
			}
		},
		""";
}
