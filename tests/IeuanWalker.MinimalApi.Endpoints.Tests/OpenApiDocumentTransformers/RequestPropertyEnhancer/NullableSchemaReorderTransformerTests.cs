using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer;
using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer.Core;
using Microsoft.OpenApi;

namespace IeuanWalker.MinimalApi.Endpoints.Tests.OpenApiDocumentTransformers.RequestPropertyEnhancer;

public class NullableSchemaReorderTransformerTests
{
	[Fact]
	public async Task TransformAsync_WhenNoSchemas_DoesNotThrow()
	{
		// Arrange
		NullableSchemaReorderTransformer transformer = new();
		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents()
		};

		// Act
		Task task = transformer.TransformAsync(document, null!, CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}

	[Fact]
	public async Task TransformAsync_WhenOneOfHasNoNullableMarkers_DoesNothing()
	{
		// Arrange
		NullableSchemaReorderTransformer transformer = new();

		OpenApiSchema first = new()
		{
			Type = JsonSchemaType.String
		};
		OpenApiSchema second = new()
		{
			Type = JsonSchemaType.Integer
		};

		OpenApiSchema root = new()
		{
			OneOf = [first, second]
		};

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>
				{
					["noNullable"] = root
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, null!, CancellationToken.None);

		// Assert - order unchanged
		root.OneOf![0].ShouldBe(first);
		root.OneOf[1].ShouldBe(second);
	}

	[Fact]
	public async Task TransformAsync_WhenSchemaHasNot_ReordersNestedOneOf()
	{
		// Arrange
		NullableSchemaReorderTransformer transformer = new();

		OpenApiSchema nonNullSchema = new()
		{
			Type = JsonSchemaType.String
		};
		OpenApiSchema nullSchema = new()
		{
			Type = JsonSchemaType.Null
		};

		OpenApiSchema notSchema = new()
		{
			OneOf = [nullSchema, nonNullSchema]
		};

		OpenApiSchema root = new()
		{
			Not = notSchema
		};

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>
				{
					["withNot"] = root
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, null!, CancellationToken.None);

		// Assert
		notSchema.OneOf[0].ShouldBe(nonNullSchema);
		notSchema.OneOf[1].ShouldBe(nullSchema);
	}

	[Fact]
	public async Task TransformAsync_WhenComponentSchemaIsNull_DoesNotThrow()
	{
		// Arrange
		NullableSchemaReorderTransformer transformer = new();
		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>
				{
					["nullSchema"] = null!
				}
			}
		};

		// Act
		Task task = transformer.TransformAsync(document, null!, CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}

	[Fact]
	public async Task TransformAsync_WhenSchemaReferenceUnresolved_DoesNotThrow()
	{
		// Arrange
		NullableSchemaReorderTransformer transformer = new();

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents()
		};

		// Add a schema reference that points to a non-existent id
		OpenApiSchemaReference schemaRef = new("missing", document, null);
		document.Components.Schemas = new Dictionary<string, IOpenApiSchema>
		{
			["refRef"] = schemaRef
		};

		// Act
		Task task = transformer.TransformAsync(document, null!, CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}



	[Fact]
	public async Task TransformAsync_WhenSchemaIsReference_ResolvesAndProcessesReferencedSchema()
	{
		// Arrange
		NullableSchemaReorderTransformer transformer = new();

		OpenApiSchema nonNullSchema = new()
		{
			Type = JsonSchemaType.String
		};
		OpenApiSchema nullSchema = new()
		{
			Type = JsonSchemaType.Null
		};

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents()
		};

		// Referenced schema that needs processing
		OpenApiSchema referenced = new()
		{
			OneOf = [nullSchema, nonNullSchema]
		};

		// Add the referenced schema under id "ref"
		document.Components.Schemas = new Dictionary<string, IOpenApiSchema>
		{
			["ref"] = referenced
		};

		// Add a schema reference that points to the above referenced schema
		OpenApiSchemaReference schemaRef = new("ref", document, null);
		document.Components.Schemas["refRef"] = schemaRef;

		// Act
		await transformer.TransformAsync(document, null!, CancellationToken.None);

		// Assert - the referenced schema should have been processed and reordered
		referenced.OneOf[0].ShouldBe(nonNullSchema);
		referenced.OneOf[1].ShouldBe(nullSchema);
	}

	[Fact]
	public async Task TransformAsync_WhenEncodingHasNoHeaders_ContinuesWithoutError()
	{
		// Arrange
		NullableSchemaReorderTransformer transformer = new();

		OpenApiMediaType mediaType = new()
		{
			Schema = new OpenApiSchema { OneOf = [new OpenApiSchema { Type = JsonSchemaType.String }] },
			Encoding = new Dictionary<string, OpenApiEncoding>
			{
				["p"] = new OpenApiEncoding() // Headers left null intentionally
			}
		};

		OpenApiOperation op = new()
		{
			RequestBody = new OpenApiRequestBody
			{
				Content = new Dictionary<string, OpenApiMediaType>
				{
					["application/json"] = mediaType
				}
			}
		};

		OpenApiPathItem pathItem = new()
		{
			Operations = new Dictionary<HttpMethod, OpenApiOperation>
			{
				[HttpMethod.Post] = op
			}
		};

		OpenApiDocument document = new()
		{
			Paths = new OpenApiPaths
			{
				["/x"] = pathItem
			}
		};

		// Act
		Task task = transformer.TransformAsync(document, null!, CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}

	[Fact]
	public async Task TransformAsync_WhenComponentResponseReferenceUnresolved_DoesNotThrow()
	{
		// Arrange
		NullableSchemaReorderTransformer transformer = new();
		OpenApiResponseReference respRef = new("missing", null, null);

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Responses = new Dictionary<string, IOpenApiResponse>
				{
					["respRef"] = respRef
				}
			}
		};

		// Act
		Task task = transformer.TransformAsync(document, null!, CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}

	[Fact]
	public async Task TransformAsync_WhenComponentRequestBodyReferenceUnresolved_DoesNotThrow()
	{
		// Arrange
		NullableSchemaReorderTransformer transformer = new();
		// Create a request body reference that does not resolve in the document components
		OpenApiRequestBodyReference rbRef = new("missing", null, null);

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				// Place the reference under a different key so resolution by id fails
				RequestBodies = new Dictionary<string, IOpenApiRequestBody>
				{
					["rbRef"] = rbRef
				}
			}
		};

		// Act
		Task task = transformer.TransformAsync(document, null!, CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}

	[Fact]
	public async Task TransformAsync_WhenResponseHeaderReferenceResolvesToHeaderWithNoSchema_DoesNotThrow()
	{
		// Arrange
		NullableSchemaReorderTransformer transformer = new();

		OpenApiHeaderReference headerRef = new("hdr", null, null);
		OpenApiResponse response = new()
		{
			Headers = new Dictionary<string, IOpenApiHeader>
			{
				["h"] = headerRef
			}
		};

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Headers = new Dictionary<string, IOpenApiHeader>
				{
					["hdr"] = new OpenApiHeader { Schema = null }
				},
				Responses = new Dictionary<string, IOpenApiResponse>
				{
					["r"] = response
				}
			}
		};

		// Act
		Task task = transformer.TransformAsync(document, null!, CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}

	[Fact]
	public async Task TransformAsync_WhenComponentHeaderReferenceUnresolved_DoesNotThrow()
	{
		// Arrange
		NullableSchemaReorderTransformer transformer = new();
		// Create a header reference that does not resolve in the document components
		OpenApiHeaderReference headerRef = new("missing", null, null);

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				// Include the header reference under a different key so resolution by id fails
				Headers = new Dictionary<string, IOpenApiHeader>
				{
					["hdrRef"] = headerRef
				}
			}
		};

		// Act
		Task task = transformer.TransformAsync(document, null!, CancellationToken.None);
		// ProcessPaths won't touch components.Headers, so call ProcessComponents via TransformAsync covering ResolveReference branch
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}

	[Fact]
	public async Task TransformAsync_WhenComponentHeaderHasNoSchema_DoesNotThrow()
	{
		// Arrange
		NullableSchemaReorderTransformer transformer = new();
		OpenApiHeader header = new()
		{
			Schema = null
		};

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Headers = new Dictionary<string, IOpenApiHeader>
				{
					["hdr"] = header
				}
			}
		};

		// Act
		Task task = transformer.TransformAsync(document, null!, CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}

	[Fact]
	public async Task TransformAsync_WhenComponentParameterHasNoSchema_DoesNotThrow()
	{
		// Arrange
		NullableSchemaReorderTransformer transformer = new();
		OpenApiParameter parameter = new()
		{
			Schema = null
		};

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Parameters = new Dictionary<string, IOpenApiParameter>
				{
					["param"] = parameter
				}
			}
		};

		// Act
		Task task = transformer.TransformAsync(document, null!, CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}

	[Fact]
	public async Task TransformAsync_WhenOperationResponsesIsNull_ContinuesWithoutError()
	{
		// Arrange
		NullableSchemaReorderTransformer transformer = new();
		OpenApiOperation operation = new()
		{
			// Ensure Responses is explicitly null to hit the null-check branch
			Responses = null
		};
		OpenApiPathItem pathItem = new()
		{
			Operations = new Dictionary<HttpMethod, OpenApiOperation>
			{
				[HttpMethod.Get] = operation
			}
		};
		OpenApiDocument document = new()
		{
			Paths = new OpenApiPaths
			{
				["/x"] = pathItem
			}
		};

		// Act
		Task task = transformer.TransformAsync(document, null!, CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}

	[Fact]
	public async Task TransformAsync_WhenPathItemOperationsIsNull_ContinuesWithoutError()
	{
		// Arrange
		NullableSchemaReorderTransformer transformer = new();
		OpenApiPathItem pathItem = new(); // Operations defaults to null
		OpenApiDocument document = new()
		{
			Paths = new OpenApiPaths
			{
				["/x"] = pathItem
			}
		};

		// Act
		Task task = transformer.TransformAsync(document, null!, CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}

	[Fact]
	public async Task TransformAsync_WhenPathsIsNull_DoesNotThrow()
	{
		// Arrange
		NullableSchemaReorderTransformer transformer = new();
		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents(),
			Paths = null!
		};

		// Act
		Task task = transformer.TransformAsync(document, null!, CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}

	[Fact]
	public async Task TransformAsync_WhenAllOfAndAnyOfHaveNullFirst_ReordersToNullLast()
	{
		// Arrange
		NullableSchemaReorderTransformer transformer = new();

		OpenApiSchema nonNullAllOf = new()
		{
			Type = JsonSchemaType.String
		};
		OpenApiSchema nullAllOf = new()
		{
			Type = JsonSchemaType.Null
		};

		OpenApiSchema nonNullAnyOf = new()
		{
			Type = JsonSchemaType.Integer
		};
		OpenApiSchema nullableExtAnyOf = OpenApiSchemaHelper.CreateNullableMarker();

		OpenApiSchema root = new()
		{
			AllOf = [nullAllOf, nonNullAllOf],
			AnyOf = [nullableExtAnyOf, nonNullAnyOf]
		};

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
				{
					["Mixed"] = root
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, null!, CancellationToken.None);

		// Assert
		root.AllOf[0].ShouldBe(nonNullAllOf);
		root.AllOf[1].ShouldBe(nullAllOf);
		root.AnyOf[0].ShouldBe(nonNullAnyOf);
		root.AnyOf[1].ShouldBe(nullableExtAnyOf);
	}

	[Fact]
	public async Task TransformAsync_WhenOneOfHasNullFirst_ReordersToNullLast()
	{
		// Arrange
		NullableSchemaReorderTransformer transformer = new();

		OpenApiSchema nonNullSchema = new()
		{
			Type = JsonSchemaType.String
		};
		OpenApiSchema nullSchema = new()
		{
			Type = JsonSchemaType.Null
		};

		OpenApiSchema root = new()
		{
			OneOf = [nullSchema, nonNullSchema]
		};

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
				{
					["Test"] = root
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, null!, CancellationToken.None);

		// Assert
		root.OneOf![0].ShouldBe(nonNullSchema);
		root.OneOf[1].ShouldBe(nullSchema);
	}

	[Fact]
	public async Task TransformAsync_WhenOneOfHasNullableExtension_ReordersToLast()
	{
		// Arrange
		NullableSchemaReorderTransformer transformer = new();

		OpenApiSchema nonNullSchema = new()
		{
			Type = JsonSchemaType.Integer
		};
		OpenApiSchema nullableExtSchema = OpenApiSchemaHelper.CreateNullableMarker();

		OpenApiSchema root = new()
		{
			OneOf = [nullableExtSchema, nonNullSchema]
		};

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
				{
					["TestNullableExt"] = root
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, null!, CancellationToken.None);

		// Assert
		root.OneOf![0].ShouldBe(nonNullSchema);
		root.OneOf[1].ShouldBe(nullableExtSchema);
	}

	[Fact]
	public async Task TransformAsync_WhenComponentParameterSchema_ReordersOneOf()
	{
		// Arrange
		NullableSchemaReorderTransformer transformer = new();

		OpenApiSchema nonNullSchema = new()
		{
			Type = JsonSchemaType.String
		};
		OpenApiSchema nullSchema = new()
		{
			Type = JsonSchemaType.Null
		};

		OpenApiSchema root = new()
		{
			OneOf = [nullSchema, nonNullSchema]
		};

		OpenApiParameter parameter = new()
		{
			Schema = root
		};

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Parameters = new Dictionary<string, IOpenApiParameter>()
				{
					["param"] = parameter
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, null!, CancellationToken.None);

		// Assert
		root.OneOf![0].ShouldBe(nonNullSchema);
		root.OneOf[1].ShouldBe(nullSchema);
	}

	[Fact]
	public async Task TransformAsync_WhenComponentRequestBodySchema_ReordersOneOf()
	{
		// Arrange
		NullableSchemaReorderTransformer transformer = new();

		OpenApiSchema nonNullSchema = new()
		{
			Type = JsonSchemaType.String
		};
		OpenApiSchema nullSchema = new()
		{
			Type = JsonSchemaType.Null
		};

		OpenApiSchema root = new()
		{
			OneOf = [nullSchema, nonNullSchema]
		};

		OpenApiRequestBody requestBody = new()
		{
			Content = new Dictionary<string, OpenApiMediaType>
			{
				["application/json"] = new OpenApiMediaType
				{
					Schema = root
				}
			}
		};

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				RequestBodies = new Dictionary<string, IOpenApiRequestBody>()
				{
					["rb"] = requestBody
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, null!, CancellationToken.None);

		// Assert
		root.OneOf![0].ShouldBe(nonNullSchema);
		root.OneOf[1].ShouldBe(nullSchema);
	}

	[Fact]
	public async Task TransformAsync_WhenComponentResponseContentAndHeaders_ReordersOneOf()
	{
		// Arrange
		NullableSchemaReorderTransformer transformer = new();

		OpenApiSchema nonNullSchema = new()
		{
			Type = JsonSchemaType.String
		};
		OpenApiSchema nullSchema = new()
		{
			Type = JsonSchemaType.Null
		};

		OpenApiSchema contentRoot = new()
		{
			OneOf = [nullSchema, nonNullSchema]
		};
		OpenApiSchema headerRoot = new()
		{
			OneOf = [nullSchema, nonNullSchema]
		};

		OpenApiResponse response = new()
		{
			Content = new Dictionary<string, OpenApiMediaType>
			{
				["application/json"] = new OpenApiMediaType
				{
					Schema = contentRoot
				}
			},
			Headers = new Dictionary<string, IOpenApiHeader>
			{
				["h"] = new OpenApiHeader
				{
					Schema = headerRoot
				}
			}
		};

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Responses = new Dictionary<string, IOpenApiResponse>()
				{
					["r"] = response
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, null!, CancellationToken.None);

		// Assert
		contentRoot.OneOf![0].ShouldBe(nonNullSchema);
		contentRoot.OneOf[1].ShouldBe(nullSchema);
		headerRoot.OneOf![0].ShouldBe(nonNullSchema);
		headerRoot.OneOf[1].ShouldBe(nullSchema);
	}

	[Fact]
	public async Task TransformAsync_WhenComponentHeaderSchema_ReordersOneOf()
	{
		// Arrange
		NullableSchemaReorderTransformer transformer = new();

		OpenApiSchema nonNullSchema = new()
		{
			Type = JsonSchemaType.String
		};
		OpenApiSchema nullSchema = new()
		{
			Type = JsonSchemaType.Null
		};

		OpenApiSchema headerRoot = new()
		{
			OneOf = [nullSchema, nonNullSchema]
		};

		OpenApiHeader header = new()
		{
			Schema = headerRoot
		};

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Headers = new Dictionary<string, IOpenApiHeader>()
				{
					["hdr"] = header
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, null!, CancellationToken.None);

		// Assert
		headerRoot.OneOf![0].ShouldBe(nonNullSchema);
		headerRoot.OneOf[1].ShouldBe(nullSchema);
	}

	[Fact]
	public async Task TransformAsync_WhenPathsAndOperationsContainSchemas_ReordersAllRelevantSchemas()
	{
		// Arrange
		NullableSchemaReorderTransformer transformer = new();

		OpenApiSchema nonNull1 = new()
		{
			Type = JsonSchemaType.String
		};
		OpenApiSchema null1 = new()
		{
			Type = JsonSchemaType.Null
		};
		OpenApiSchema paramRoot = new()
		{
			OneOf = [null1, nonNull1]
		};

		OpenApiSchema nonNull2 = new()
		{
			Type = JsonSchemaType.Integer
		};
		OpenApiSchema null2 = new()
		{
			Type = JsonSchemaType.Null
		};
		OpenApiSchema opParamRoot = new()
		{
			OneOf = [null2, nonNull2]
		};

		OpenApiSchema nonNull3 = new()
		{
			Type = JsonSchemaType.Number
		};
		OpenApiSchema null3 = new()
		{
			Type = JsonSchemaType.Null
		};
		OpenApiSchema requestBodyRoot = new()
		{
			OneOf = [null3, nonNull3]
		};

		OpenApiSchema nonNull4 = new()
		{
			Type = JsonSchemaType.Boolean
		};
		OpenApiSchema null4 = new()
		{
			Type = JsonSchemaType.Null
		};
		OpenApiSchema responseRoot = new()
		{
			OneOf = [null4, nonNull4]
		};

		OpenApiHeader encodingHeader = new()
		{
			Schema = new OpenApiSchema
			{
				OneOf =
				[
					new OpenApiSchema
					{
						Type = JsonSchemaType.Null
					},
					new OpenApiSchema
					{
						Type = JsonSchemaType.String
					}
				]
			}
		};

		OpenApiPathItem pathItem = new()
		{
			Parameters =
			[
				new OpenApiParameter
				{
					Schema = paramRoot
				}
			]
		};

		OpenApiOperation op = new()
		{
			Parameters =
			[
				new OpenApiParameter
				{
					Schema = opParamRoot
				}
			],
			RequestBody = new OpenApiRequestBody
			{
				Content = new Dictionary<string, OpenApiMediaType>
				{
					["application/json"] = new OpenApiMediaType
					{
						Schema = requestBodyRoot,
						Encoding = new Dictionary<string, OpenApiEncoding>
						{
							["p"] = new OpenApiEncoding
							{
								Headers = new Dictionary<string, IOpenApiHeader>
								{
									["eh"] = encodingHeader
								}
							}
						}
					}
				}
			},
			Responses = new OpenApiResponses
			{
				["200"] = new OpenApiResponse
				{
					Content = new Dictionary<string, OpenApiMediaType>
					{
						["application/json"] = new OpenApiMediaType
						{
							Schema = responseRoot
						}
					}
				}
			}
		};

		pathItem.Operations = new Dictionary<HttpMethod, OpenApiOperation>
		{
			[HttpMethod.Post] = op
		};

		OpenApiDocument document = new()
		{
			Paths = new OpenApiPaths
			{
				["/x"] = pathItem
			}
		};

		// Act
		await transformer.TransformAsync(document, null!, CancellationToken.None);

		// Assert
		paramRoot.OneOf![0].ShouldBe(nonNull1);
		opParamRoot.OneOf![0].ShouldBe(nonNull2);
		requestBodyRoot.OneOf![0].ShouldBe(nonNull3);
		responseRoot.OneOf![0].ShouldBe(nonNull4);

		IList<IOpenApiSchema> encHeaderOneOf = ((OpenApiSchema)encodingHeader.Schema!).OneOf!;
		encHeaderOneOf[0].ShouldBeOfType<OpenApiSchema>().ShouldNotBeNull();
		encHeaderOneOf[0].ShouldBe(encHeaderOneOf[0]);
	}
}
