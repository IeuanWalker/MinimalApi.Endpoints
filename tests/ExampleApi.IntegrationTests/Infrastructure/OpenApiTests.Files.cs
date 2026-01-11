namespace ExampleApi.IntegrationTests.Infrastructure;

public partial class OpenApiTests : IClassFixture<ExampleApiWebApplicationFactory>
{
	[Fact]
	public async Task OpenApiJson_ValidFileStructure()
	{
		// Act
		HttpResponseMessage response = await _client.GetAsync("/openapi/v1.json");
		string actualContent = await response.Content.ReadAsStringAsync();
		string normalizedActual = RemoveNewLinesAndWhitespace(NormalizeOpenApiJson(actualContent, GetOptions()));

		// Assert
		normalizedActual.ShouldContain(RemoveNewLinesAndWhitespace(expectedFileEndpoints));
		normalizedActual.ShouldContain(RemoveNewLinesAndWhitespace(expectedFileComponenets));
	}

	static readonly string expectedFileEndpoints = """
		"/api/v1/FileHandling/ListOfFiles": {
		  "post": {
		    "tags": [
		      "FileHandling"
		    ],
		    "operationId": "post_FileHandlingListOfFiles_4",
		    "requestBody": {
		      "content": {
		        "multipart/form-data": {
		          "schema": {
		            "type": "object",
		            "properties": {
		              "request": {
		                "type": "array",
		                "items": {
		                  "type": "string",
		                  "format": "binary"
		                }
		              }
		            }
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
		              "type": "array",
		              "items": {
		                "$ref": "#/components/schemas/ExampleApi.Endpoints.FileHandling.PostListOfFiles.ResponseModel"
		              }
		            }
		          }
		        }
		      }
		    }
		  }
		},
		"/api/v1/FileHandling/Multipart": {
		  "post": {
		    "tags": [
		      "FileHandling"
		    ],
		    "operationId": "post_FileHandlingMultipart_5",
		    "requestBody": {
		      "content": {
		        "multipart/form-data": {
		          "schema": {
		            "$ref": "#/components/schemas/ExampleApi.Endpoints.FileHandling.PostMultipart.RequestModel"
		          }
		        },
		        "application/x-www-form-urlencoded": {
		          "schema": {
		            "$ref": "#/components/schemas/ExampleApi.Endpoints.FileHandling.PostMultipart.RequestModel"
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
		              "$ref": "#/components/schemas/ExampleApi.Endpoints.FileHandling.PostMultipart.ResponseModel"
		            }
		          }
		        }
		      }
		    }
		  }
		},
		"/api/v1/FileHandling/SingleFile": {
		  "post": {
		    "tags": [
		      "FileHandling"
		    ],
		    "operationId": "post_FileHandlingSingleFile_6",
		    "requestBody": {
		      "content": {
		        "multipart/form-data": {
		          "schema": {
		            "type": "object",
		            "properties": {
		              "request": {
		                "type": "string",
		                "format": "binary"
		              }
		            }
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
		              "$ref": "#/components/schemas/ExampleApi.Endpoints.FileHandling.PostSingleFile.ResponseModel"
		            }
		          }
		        }
		      }
		    }
		  }
		},
		""";

	static readonly string expectedFileComponenets = """
		"ExampleApi.Endpoints.FileHandling.PostListOfFiles.ResponseModel": {
		  "required": [
		    "fileName",
		    "propertyName"
		  ],
		  "type": "object",
		  "properties": {
		    "fileName": {
		      "type": "string"
		    },
		    "propertyName": {
		      "type": "string"
		    },
		    "size": {
		      "type": "integer",
		      "format": "int32"
		    }
		  }
		},
		"ExampleApi.Endpoints.FileHandling.PostMultipart.FileInfo": {
		  "required": [
		    "fileName",
		    "propertyName"
		  ],
		  "type": "object",
		  "properties": {
		    "fileName": {
		      "type": "string"
		    },
		    "propertyName": {
		      "type": "string"
		    },
		    "size": {
		      "type": "integer",
		      "format": "int32"
		    }
		  }
		},
		"ExampleApi.Endpoints.FileHandling.PostMultipart.RequestModel": {
		  "required": [
		    "someData",
		    "readOnlyList1",
		    "readOnlyList2",
		    "fileCollectionList"
		  ],
		  "type": "object",
		  "properties": {
		    "someData": {
		      "type": "string"
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
		    "fileCollectionList": {
		      "type": "array",
		      "items": {
		        "type": "string",
		        "format": "binary"
		      }
		    }
		  }
		},
		"ExampleApi.Endpoints.FileHandling.PostMultipart.ResponseModel": {
		  "required": [
		    "someData",
		    "singleFile",
		    "totalFileCount",
		    "readOnlyList1",
		    "readOnlyList2",
		    "fileCollectionList"
		  ],
		  "type": "object",
		  "properties": {
		    "someData": {
		      "type": "string"
		    },
		    "singleFile": {
		      "$ref": "#/components/schemas/ExampleApi.Endpoints.FileHandling.PostMultipart.FileInfo"
		    },
		    "totalFileCount": {
		      "type": "integer",
		      "format": "int32"
		    },
		    "readOnlyList1": {
		      "type": "array",
		      "items": {
		        "$ref": "#/components/schemas/ExampleApi.Endpoints.FileHandling.PostMultipart.FileInfo"
		      }
		    },
		    "readOnlyList2": {
		      "type": "array",
		      "items": {
		        "$ref": "#/components/schemas/ExampleApi.Endpoints.FileHandling.PostMultipart.FileInfo"
		      }
		    },
		    "fileCollectionList": {
		      "type": "array",
		      "items": {
		        "$ref": "#/components/schemas/ExampleApi.Endpoints.FileHandling.PostMultipart.FileInfo"
		      }
		    }
		  }
		},
		"ExampleApi.Endpoints.FileHandling.PostSingleFile.ResponseModel": {
		  "required": [
		    "fileName",
		    "propertyName"
		  ],
		  "type": "object",
		  "properties": {
		    "fileName": {
		      "type": "string"
		    },
		    "propertyName": {
		      "type": "string"
		    },
		    "size": {
		      "type": "integer",
		      "format": "int32"
		    }
		  }
		},
		""";
}
