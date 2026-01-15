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
