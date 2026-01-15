using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace IeuanWalker.MinimalApi.Endpoints.Tests.OpenApiDocumentTransformers.RequestPropertyEnhancer;

public class TypeDocumentTransformerTests
{
	#region Test Helpers

	static OpenApiDocumentTransformerContext CreateMockContext()
	{
		WebApplicationBuilder builder = WebApplication.CreateBuilder();
		WebApplication app = builder.Build();

		return new OpenApiDocumentTransformerContext
		{
			DocumentName = "v1",
			DescriptionGroups = [],
			ApplicationServices = app.Services
		};
	}

	#endregion
	#region TransformAsync - Basic Scenarios

	[Fact]
	public async Task TransformAsync_WhenNoSchemas_DoesNotThrow()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();
		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents()
		};

		// Act
		Task task = transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}

	[Fact]
	public async Task TransformAsync_WhenNullComponents_DoesNotThrow()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();
		OpenApiDocument document = new();

		// Act
		Task task = transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}

	[Fact]
	public async Task TransformAsync_WhenNullPaths_DoesNotThrow()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();
		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
			}
		};

		// Act
		Task task = transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}

	[Fact]
	public async Task TransformAsync_WhenEmptySchemas_DoesNotThrow()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();
		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
			}
		};

		// Act
		Task task = transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}

	#endregion

	#region IFormFile and IFormFileCollection Handling

	[Fact]
	public async Task TransformAsync_InlinesIFormFileSchema()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();
		OpenApiSchema schema = new();

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
				{
					["Microsoft.AspNetCore.Http.IFormFile"] = schema
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		OpenApiSchema? resultSchema = document.Components.Schemas["Microsoft.AspNetCore.Http.IFormFile"] as OpenApiSchema;
		resultSchema.ShouldNotBeNull();
		resultSchema.Type.ShouldBe(JsonSchemaType.String);
		resultSchema.Format.ShouldBe("binary");
	}

	[Fact]
	public async Task TransformAsync_InlinesIFormFileCollectionSchema()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();
		OpenApiSchema schema = new();

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
				{
					["Microsoft.AspNetCore.Http.IFormFileCollection"] = schema
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		OpenApiSchema? resultSchema = document.Components.Schemas["Microsoft.AspNetCore.Http.IFormFileCollection"] as OpenApiSchema;
		resultSchema.ShouldNotBeNull();
		resultSchema.Type.ShouldBe(JsonSchemaType.Array);
		resultSchema.Items.ShouldNotBeNull();
		OpenApiSchema? itemSchema = resultSchema.Items as OpenApiSchema;
		itemSchema.ShouldNotBeNull();
		itemSchema.Type.ShouldBe(JsonSchemaType.String);
		itemSchema.Format.ShouldBe("binary");
	}

	[Fact]
	public async Task TransformAsync_DoesNotModifyIFormFileSchemaWithExistingType()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();
		OpenApiSchema schema = new()
		{
			Type = JsonSchemaType.Object,
			Description = "Already typed"
		};

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
				{
					["Microsoft.AspNetCore.Http.IFormFile"] = schema
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		OpenApiSchema? resultSchema = document.Components.Schemas["Microsoft.AspNetCore.Http.IFormFile"] as OpenApiSchema;
		resultSchema.ShouldNotBeNull();
		resultSchema.Type.ShouldBe(JsonSchemaType.Object);
		resultSchema.Description.ShouldBe("Already typed");
	}

	#endregion

	#region System.String[] Inlining

	[Fact]
	public async Task TransformAsync_InlinesSystemStringArrayProperty()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();

		OpenApiSchema component = new()
		{
			Properties = new Dictionary<string, IOpenApiSchema>
			{
				["Names"] = new OpenApiSchemaReference("System.String[]", null, null)
			}
		};

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
				{
					["MySchema"] = component
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		OpenApiSchema? result = document.Components.Schemas["MySchema"] as OpenApiSchema;
		result.ShouldNotBeNull();
		result.Properties.ShouldNotBeNull();
		result.Properties.ShouldContainKey("Names");
		OpenApiSchema? namesSchema = result.Properties["Names"] as OpenApiSchema;
		namesSchema.ShouldNotBeNull();
		namesSchema.Type.ShouldBe(JsonSchemaType.Array);
		namesSchema.Items.ShouldNotBeNull();
		OpenApiSchema? item = namesSchema.Items as OpenApiSchema;
		item.ShouldNotBeNull();
		item.Type.ShouldBe(JsonSchemaType.String);
	}

	[Fact]
	public async Task TransformAsync_InlinesSystemStringArrayInAdditionalProperties()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();

		OpenApiSchema component = new()
		{
			Type = JsonSchemaType.Object,
			AdditionalProperties = new OpenApiSchemaReference("System.String[]", null, null)
		};

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
				{
					["DictionarySchema"] = component
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		OpenApiSchema? result = document.Components.Schemas["DictionarySchema"] as OpenApiSchema;
		result.ShouldNotBeNull();
		OpenApiSchema? additionalProps = result.AdditionalProperties as OpenApiSchema;
		additionalProps.ShouldNotBeNull();
		additionalProps.Type.ShouldBe(JsonSchemaType.Array);
		OpenApiSchema? item = additionalProps.Items as OpenApiSchema;
		item.ShouldNotBeNull();
		item.Type.ShouldBe(JsonSchemaType.String);
	}

	[Fact]
	public async Task TransformAsync_InlinesSystemStringArrayInItems()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();

		OpenApiSchema component = new()
		{
			Type = JsonSchemaType.Array,
			Items = new OpenApiSchemaReference("System.String[]", null, null)
		};

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
				{
					["ArrayOfArraySchema"] = component
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		OpenApiSchema? result = document.Components.Schemas["ArrayOfArraySchema"] as OpenApiSchema;
		result.ShouldNotBeNull();
		OpenApiSchema? itemsSchema = result.Items as OpenApiSchema;
		itemsSchema.ShouldNotBeNull();
		itemsSchema.Type.ShouldBe(JsonSchemaType.Array);
	}

	[Fact]
	public async Task TransformAsync_InlinesSystemStringArrayInOneOf()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();

		OpenApiSchema component = new()
		{
			OneOf =
			[
				new OpenApiSchemaReference("System.String[]", null, null),
				new OpenApiSchema
				{
					Type = JsonSchemaType.Null
				}
			]
		};

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
				{
					["NullableArraySchema"] = component
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		OpenApiSchema? result = document.Components.Schemas["NullableArraySchema"] as OpenApiSchema;
		result.ShouldNotBeNull();
		result.OneOf.ShouldNotBeNull();
		result.OneOf.Count.ShouldBe(2);
		OpenApiSchema? arraySchema = result.OneOf.OfType<OpenApiSchema>().FirstOrDefault(s => s.Type == JsonSchemaType.Array);
		arraySchema.ShouldNotBeNull();
	}

	[Fact]
	public async Task TransformAsync_InlinesSystemStringArrayInAllOf()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();

		OpenApiSchema component = new()
		{
			AllOf =
			[
				new OpenApiSchemaReference("System.String[]", null, null)
			]
		};

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
				{
					["AllOfSchema"] = component
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		OpenApiSchema? result = document.Components.Schemas["AllOfSchema"] as OpenApiSchema;
		result.ShouldNotBeNull();
		result.AllOf.ShouldNotBeNull();
		result.AllOf.Count.ShouldBe(1);
		OpenApiSchema? arraySchema = result.AllOf[0] as OpenApiSchema;
		arraySchema.ShouldNotBeNull();
		arraySchema.Type.ShouldBe(JsonSchemaType.Array);
	}

	[Fact]
	public async Task TransformAsync_InlinesSystemStringArrayInAnyOf()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();

		OpenApiSchema component = new()
		{
			AnyOf =
			[
				new OpenApiSchemaReference("System.String[]", null, null)
			]
		};

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
				{
					["AnyOfSchema"] = component
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		OpenApiSchema? result = document.Components.Schemas["AnyOfSchema"] as OpenApiSchema;
		result.ShouldNotBeNull();
		result.AnyOf.ShouldNotBeNull();
		result.AnyOf.Count.ShouldBe(1);
		OpenApiSchema? arraySchema = result.AnyOf[0] as OpenApiSchema;
		arraySchema.ShouldNotBeNull();
		arraySchema.Type.ShouldBe(JsonSchemaType.Array);
	}

	#endregion

	#region Nullable Enum Schema Handling

	[Fact]
	public async Task TransformAsync_CreatesUnwrappedEnumSchemaFromNullableWrapper()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();
		string nullableEnumKey = "System.Nullable`1[[MyNamespace.MyEnum, MyAssembly, Version=1.0.0.0]]";

		OpenApiSchema nullableEnumSchema = new()
		{
			Type = JsonSchemaType.String,
			Format = null,
			Description = "Nullable enum"
		};

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
				{
					[nullableEnumKey] = nullableEnumSchema
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		document.Components.Schemas.ShouldContainKey("MyNamespace.MyEnum");
		OpenApiSchema? unwrappedSchema = document.Components.Schemas["MyNamespace.MyEnum"] as OpenApiSchema;
		unwrappedSchema.ShouldNotBeNull();
		unwrappedSchema.Type.ShouldBe(JsonSchemaType.String);
		unwrappedSchema.Description.ShouldBe("Nullable enum");
	}

	[Fact]
	public async Task TransformAsync_DoesNotDuplicateExistingUnwrappedEnumSchema()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();
		string nullableEnumKey = "System.Nullable`1[[MyNamespace.MyEnum, MyAssembly]]";

		OpenApiSchema existingSchema = new()
		{
			Type = JsonSchemaType.String,
			Description = "Existing schema"
		};

		OpenApiSchema nullableEnumSchema = new()
		{
			Type = JsonSchemaType.String,
			Description = "Nullable version"
		};

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
				{
					["MyNamespace.MyEnum"] = existingSchema,
					[nullableEnumKey] = nullableEnumSchema
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		OpenApiSchema? schema = document.Components.Schemas["MyNamespace.MyEnum"] as OpenApiSchema;
		schema.ShouldNotBeNull();
		schema.Description.ShouldBe("Existing schema");
	}

	#endregion

	#region Collection and Dictionary Inlining

	[Fact]
	public async Task TransformAsync_RemovesCollectionSchemas()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();

		OpenApiSchema collectionSchema = new()
		{
			Type = JsonSchemaType.Array,
			Items = new OpenApiSchema { Type = JsonSchemaType.String }
		};

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
				{
					["System.Collections.Generic.List`1[[System.String]]"] = collectionSchema
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		document.Components.Schemas.ShouldNotContainKey("System.Collections.Generic.List`1[[System.String]]");
	}

	[Fact]
	public async Task TransformAsync_RemovesDictionarySchemas()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();

		OpenApiSchema dictionarySchema = new()
		{
			Type = JsonSchemaType.Object,
			AdditionalProperties = new OpenApiSchema { Type = JsonSchemaType.String }
		};

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
				{
					["System.Collections.Generic.Dictionary`2[[System.String],[System.String]]"] = dictionarySchema
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		document.Components.Schemas.ShouldNotContainKey("System.Collections.Generic.Dictionary`2[[System.String],[System.String]]");
	}

	#endregion

	#region Request Body Processing

	[Fact]
	public async Task TransformAsync_InlinesIFormFileInRequestBody()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();

		OpenApiDocument document = new()
		{
			Paths = new OpenApiPaths
			{
				["/upload"] = new OpenApiPathItem
				{
					Operations = new Dictionary<HttpMethod, OpenApiOperation>
					{
						[HttpMethod.Post] = new OpenApiOperation
						{
							RequestBody = new OpenApiRequestBody
							{
								Content = new Dictionary<string, OpenApiMediaType>
								{
									["multipart/form-data"] = new OpenApiMediaType
									{
										Schema = new OpenApiSchema
										{
											Properties = new Dictionary<string, IOpenApiSchema>
											{
												["file"] = new OpenApiSchemaReference("Microsoft.AspNetCore.Http.IFormFile", null, null)
											}
										}
									}
								}
							}
						}
					}
				}
			},
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		OpenApiPathItem? pathItem = document.Paths["/upload"] as OpenApiPathItem;
		pathItem.ShouldNotBeNull();
		pathItem.Operations.ShouldNotBeNull();
		OpenApiOperation? operation = pathItem.Operations[HttpMethod.Post];
		operation.ShouldNotBeNull();
		OpenApiRequestBody? requestBody = operation.RequestBody as OpenApiRequestBody;
		requestBody.ShouldNotBeNull();
		requestBody.Content.ShouldNotBeNull();
		OpenApiSchema? schema = requestBody.Content["multipart/form-data"].Schema as OpenApiSchema;
		schema.ShouldNotBeNull();
		schema.Properties.ShouldNotBeNull();
		OpenApiSchema? fileSchema = schema.Properties["file"] as OpenApiSchema;
		fileSchema.ShouldNotBeNull();
		fileSchema.Type.ShouldBe(JsonSchemaType.String);
		fileSchema.Format.ShouldBe("binary");
	}

	[Fact]
	public async Task TransformAsync_InlinesIFormFileCollectionInRequestBody()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();

		OpenApiDocument document = new()
		{
			Paths = new OpenApiPaths
			{
				["/upload-multiple"] = new OpenApiPathItem
				{
					Operations = new Dictionary<HttpMethod, OpenApiOperation>
					{
						[HttpMethod.Post] = new OpenApiOperation
						{
							RequestBody = new OpenApiRequestBody
							{
								Content = new Dictionary<string, OpenApiMediaType>
								{
									["multipart/form-data"] = new OpenApiMediaType
									{
										Schema = new OpenApiSchema
										{
											Properties = new Dictionary<string, IOpenApiSchema>
											{
												["files"] = new OpenApiSchemaReference("Microsoft.AspNetCore.Http.IFormFileCollection", null, null)
											}
										}
									}
								}
							}
						}
					}
				}
			},
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		OpenApiPathItem? pathItem = document.Paths["/upload-multiple"] as OpenApiPathItem;
		pathItem.ShouldNotBeNull();
		pathItem.Operations.ShouldNotBeNull();
		OpenApiOperation? operation = pathItem.Operations[HttpMethod.Post];
		OpenApiRequestBody? requestBody = operation.RequestBody as OpenApiRequestBody;
		requestBody.ShouldNotBeNull();
		requestBody.Content.ShouldNotBeNull();
		OpenApiSchema? schema = requestBody.Content["multipart/form-data"].Schema as OpenApiSchema;
		schema.ShouldNotBeNull();
		schema.Properties.ShouldNotBeNull();
		OpenApiSchema? filesSchema = schema.Properties["files"] as OpenApiSchema;
		filesSchema.ShouldNotBeNull();
		filesSchema.Type.ShouldBe(JsonSchemaType.Array);
		OpenApiSchema? itemSchema = filesSchema.Items as OpenApiSchema;
		itemSchema.ShouldNotBeNull();
		itemSchema.Type.ShouldBe(JsonSchemaType.String);
		itemSchema.Format.ShouldBe("binary");
	}

	#endregion

	#region Response Body Processing

	[Fact]
	public async Task TransformAsync_InlinesSystemStringInResponseBody()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();

		OpenApiDocument document = new()
		{
			Paths = new OpenApiPaths
			{
				["/message"] = new OpenApiPathItem
				{
					Operations = new Dictionary<HttpMethod, OpenApiOperation>
					{
						[HttpMethod.Get] = new OpenApiOperation
						{
							Responses = new OpenApiResponses
							{
								["200"] = new OpenApiResponse
								{
									Content = new Dictionary<string, OpenApiMediaType>
									{
										["text/plain"] = new OpenApiMediaType
										{
											Schema = new OpenApiSchemaReference("System.String", null, null)
										}
									}
								}
							}
						}
					}
				}
			},
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		OpenApiPathItem? pathItem = document.Paths["/message"] as OpenApiPathItem;
		pathItem.ShouldNotBeNull();
		pathItem.Operations.ShouldNotBeNull();
		OpenApiOperation? operation = pathItem.Operations[HttpMethod.Get];
		operation.Responses.ShouldNotBeNull();
		OpenApiResponse? response = operation.Responses["200"] as OpenApiResponse;
		response.ShouldNotBeNull();
		response.Content.ShouldNotBeNull();
		OpenApiSchema? schema = response.Content["text/plain"].Schema as OpenApiSchema;
		schema.ShouldNotBeNull();
		schema.Type.ShouldBe(JsonSchemaType.String);
	}

	#endregion

	#region Schema with Properties Edge Cases

	[Fact]
	public async Task TransformAsync_HandlesSchemaWithNoProperties()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();

		OpenApiSchema schema = new()
		{
			Type = JsonSchemaType.Object
		};

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
				{
					["EmptyObject"] = schema
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		OpenApiSchema? result = document.Components.Schemas["EmptyObject"] as OpenApiSchema;
		result.ShouldNotBeNull();
		result.Type.ShouldBe(JsonSchemaType.Object);
	}

	[Fact]
	public async Task TransformAsync_HandlesSchemaWithEmptyProperties()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();

		OpenApiSchema schema = new()
		{
			Type = JsonSchemaType.Object,
			Properties = new Dictionary<string, IOpenApiSchema>()
		};

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
				{
					["ObjectWithEmptyProps"] = schema
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		OpenApiSchema? result = document.Components.Schemas["ObjectWithEmptyProps"] as OpenApiSchema;
		result.ShouldNotBeNull();
		result.Properties.ShouldNotBeNull();
		result.Properties.Count.ShouldBe(0);
	}

	#endregion

	#region Nested Schema Processing

	[Fact]
	public async Task TransformAsync_ProcessesNestedProperties()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();

		OpenApiSchema nestedSchema = new()
		{
			Properties = new Dictionary<string, IOpenApiSchema>
			{
				["tags"] = new OpenApiSchemaReference("System.String[]", null, null)
			}
		};

		OpenApiSchema parentSchema = new()
		{
			Properties = new Dictionary<string, IOpenApiSchema>
			{
				["nested"] = nestedSchema
			}
		};

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
				{
					["Parent"] = parentSchema
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		OpenApiSchema? parent = document.Components.Schemas["Parent"] as OpenApiSchema;
		parent.ShouldNotBeNull();
		parent.Properties.ShouldNotBeNull();
		OpenApiSchema? nested = parent.Properties["nested"] as OpenApiSchema;
		nested.ShouldNotBeNull();
		nested.Properties.ShouldNotBeNull();
		OpenApiSchema? tagsSchema = nested.Properties["tags"] as OpenApiSchema;
		tagsSchema.ShouldNotBeNull();
		tagsSchema.Type.ShouldBe(JsonSchemaType.Array);
	}

	#endregion

	#region Path Item Edge Cases

	[Fact]
	public async Task TransformAsync_HandlesPathWithNoOperations()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();

		OpenApiDocument document = new()
		{
			Paths = new OpenApiPaths
			{
				["/empty"] = new OpenApiPathItem()
			},
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
			}
		};

		// Act
		Task task = transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}

	[Fact]
	public async Task TransformAsync_HandlesOperationWithNoRequestBody()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();

		OpenApiDocument document = new()
		{
			Paths = new OpenApiPaths
			{
				["/get-only"] = new OpenApiPathItem
				{
					Operations = new Dictionary<HttpMethod, OpenApiOperation>
					{
						[HttpMethod.Get] = new OpenApiOperation()
					}
				}
			},
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
			}
		};

		// Act
		Task task = transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}

	[Fact]
	public async Task TransformAsync_HandlesOperationWithNoResponses()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();

		OpenApiDocument document = new()
		{
			Paths = new OpenApiPaths
			{
				["/no-response"] = new OpenApiPathItem
				{
					Operations = new Dictionary<HttpMethod, OpenApiOperation>
					{
						[HttpMethod.Delete] = new OpenApiOperation()
					}
				}
			},
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
			}
		};

		// Act
		Task task = transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}

	#endregion

	#region Array Schema Processing

	[Fact]
	public async Task TransformAsync_HandlesArraySchemaWithItems()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();

		OpenApiSchema arraySchema = new()
		{
			Type = JsonSchemaType.Array,
			Items = new OpenApiSchemaReference("System.String", null, null)
		};

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
				{
					["StringArray"] = arraySchema
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		OpenApiSchema? result = document.Components.Schemas["StringArray"] as OpenApiSchema;
		result.ShouldNotBeNull();
		result.Type.ShouldBe(JsonSchemaType.Array);
		result.Items.ShouldNotBeNull();
	}

	#endregion
}
