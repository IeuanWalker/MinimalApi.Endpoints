using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.PropertyEnhancer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace IeuanWalker.MinimalApi.Endpoints.Tests.OpenApiDocumentTransformers.PropertyEnhancer;

public class UnusedComponentsCleanupTransformerTests
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

	static OpenApiDocument CreateDocumentWithPaths(Dictionary<string, IOpenApiSchema>? schemas = null, OpenApiPaths? paths = null)
	{
		return new OpenApiDocument
		{
			Components = new OpenApiComponents
			{
				Schemas = schemas ?? []
			},
			Paths = paths ?? []
		};
	}

	#endregion

	#region Basic Scenarios

	[Fact]
	public async Task TransformAsync_WhenNullComponents_DoesNotThrow()
	{
		// Arrange
		UnusedComponentsCleanupTransformer transformer = new();
		OpenApiDocument document = new();

		// Act
		Task task = transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}

	[Fact]
	public async Task TransformAsync_WhenNullSchemas_DoesNotThrow()
	{
		// Arrange
		UnusedComponentsCleanupTransformer transformer = new();
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
	public async Task TransformAsync_WhenEmptySchemas_DoesNotThrow()
	{
		// Arrange
		UnusedComponentsCleanupTransformer transformer = new();
		OpenApiDocument document = CreateDocumentWithPaths([]);

		// Act
		Task task = transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
		document.Components!.Schemas.ShouldBeEmpty();
	}

	[Fact]
	public async Task TransformAsync_WhenNullPaths_DoesNotThrow()
	{
		// Arrange
		UnusedComponentsCleanupTransformer transformer = new();
		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>
				{
					["UnusedSchema"] = new OpenApiSchema()
				}
			}
		};

		// Act
		Task task = transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
		document.Components.Schemas.ShouldNotContainKey("UnusedSchema");
	}

	#endregion

	#region Unused Schema Removal

	[Fact]
	public async Task TransformAsync_RemovesUnreferencedSchemas()
	{
		// Arrange
		UnusedComponentsCleanupTransformer transformer = new();
		OpenApiDocument document = CreateDocumentWithPaths(schemas: new Dictionary<string, IOpenApiSchema>
		{
			["UnusedSchema1"] = new OpenApiSchema { Type = JsonSchemaType.String },
			["UnusedSchema2"] = new OpenApiSchema { Type = JsonSchemaType.Integer }
		},
		paths: []);

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		document.Components!.Schemas.ShouldBeEmpty();
	}

	[Fact]
	public async Task TransformAsync_KeepsSchemaReferencedInRequestBody()
	{
		// Arrange
		UnusedComponentsCleanupTransformer transformer = new();

		OpenApiDocument document = CreateDocumentWithPaths(schemas: new Dictionary<string, IOpenApiSchema>
		{
			["UsedSchema"] = new OpenApiSchema { Type = JsonSchemaType.Object },
			["UnusedSchema"] = new OpenApiSchema { Type = JsonSchemaType.String }
		});

		OpenApiSchemaReference schemaRef = new("UsedSchema", document, null);

		document.Paths = new OpenApiPaths
		{
			["/test"] = new OpenApiPathItem
			{
				Operations = new Dictionary<HttpMethod, OpenApiOperation>
				{
					[HttpMethod.Post] = new OpenApiOperation
					{
						RequestBody = new OpenApiRequestBody
						{
							Content = new Dictionary<string, OpenApiMediaType>
							{
								["application/json"] = new OpenApiMediaType
								{
									Schema = schemaRef
								}
							}
						}
					}
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		document.Components.ShouldNotBeNull();
		document.Components.Schemas.ShouldNotBeNull();
		document.Components.Schemas.ShouldContainKey("UsedSchema");
		document.Components.Schemas.ShouldNotContainKey("UnusedSchema");
	}

	[Fact]
	public async Task TransformAsync_KeepsSchemaReferencedInResponse()
	{
		// Arrange
		UnusedComponentsCleanupTransformer transformer = new();

		OpenApiDocument document = CreateDocumentWithPaths(schemas: new Dictionary<string, IOpenApiSchema>
		{
			["ResponseSchema"] = new OpenApiSchema { Type = JsonSchemaType.Object },
			["UnusedSchema"] = new OpenApiSchema { Type = JsonSchemaType.String }
		});

		OpenApiSchemaReference schemaRef = new("ResponseSchema", document, null);

		document.Paths = new OpenApiPaths
		{
			["/test"] = new OpenApiPathItem
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
									["application/json"] = new OpenApiMediaType
									{
										Schema = schemaRef
									}
								}
							}
						}
					}
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		document.Components.ShouldNotBeNull();
		document.Components.Schemas.ShouldNotBeNull();
		document.Components.Schemas.ShouldContainKey("ResponseSchema");
		document.Components.Schemas.ShouldNotContainKey("UnusedSchema");
	}

	[Fact]
	public async Task TransformAsync_KeepsSchemaReferencedInParameter()
	{
		// Arrange
		UnusedComponentsCleanupTransformer transformer = new();

		OpenApiDocument document = CreateDocumentWithPaths(schemas: new Dictionary<string, IOpenApiSchema>
		{
			["ParameterSchema"] = new OpenApiSchema { Type = JsonSchemaType.String },
			["UnusedSchema"] = new OpenApiSchema { Type = JsonSchemaType.Object }
		});

		OpenApiSchemaReference schemaRef = new("ParameterSchema", document, null);

		document.Paths = new OpenApiPaths
		{
			["/test/{id}"] = new OpenApiPathItem
			{
				Operations = new Dictionary<HttpMethod, OpenApiOperation>
				{
					[HttpMethod.Get] = new OpenApiOperation
					{
						Parameters = [
							new OpenApiParameter
							{
								Name = "id",
								In = ParameterLocation.Path,
								Schema = schemaRef
							}
						]
					}
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		document.Components.ShouldNotBeNull();
		document.Components.Schemas.ShouldNotBeNull();
		document.Components.Schemas.ShouldContainKey("ParameterSchema");
		document.Components.Schemas.ShouldNotContainKey("UnusedSchema");
	}

	[Fact]
	public async Task TransformAsync_KeepsSchemaReferencedInPathItemParameter()
	{
		// Arrange
		UnusedComponentsCleanupTransformer transformer = new();

		OpenApiDocument document = CreateDocumentWithPaths(schemas: new Dictionary<string, IOpenApiSchema>
		{
			["PathParamSchema"] = new OpenApiSchema { Type = JsonSchemaType.String },
			["UnusedSchema"] = new OpenApiSchema { Type = JsonSchemaType.Object }
		});

		OpenApiSchemaReference schemaRef = new("PathParamSchema", document, null);

		document.Paths = new OpenApiPaths
		{
			["/test/{id}"] = new OpenApiPathItem
			{
				Parameters = [
					new OpenApiParameter
					{
						Name = "id",
						In = ParameterLocation.Path,
						Schema = schemaRef
					}
				],
				Operations = new Dictionary<HttpMethod, OpenApiOperation>
				{
					[HttpMethod.Get] = new OpenApiOperation()
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		document.Components.ShouldNotBeNull();
		document.Components.Schemas.ShouldNotBeNull();
		document.Components.Schemas.ShouldContainKey("PathParamSchema");
		document.Components.Schemas.ShouldNotContainKey("UnusedSchema");
	}

	#endregion

	#region Nested Schema References

	[Fact]
	public async Task TransformAsync_KeepsNestedReferencedSchemas()
	{
		// Arrange
		UnusedComponentsCleanupTransformer transformer = new();

		OpenApiDocument document = CreateDocumentWithPaths(schemas: new Dictionary<string, IOpenApiSchema>
		{
			["ParentSchema"] = new OpenApiSchema { Type = JsonSchemaType.Object },
			["ChildSchema"] = new OpenApiSchema { Type = JsonSchemaType.String },
			["UnusedSchema"] = new OpenApiSchema { Type = JsonSchemaType.Integer }
		});

		OpenApiSchemaReference childRef = new("ChildSchema", document, null);
		((OpenApiSchema)document.Components!.Schemas!["ParentSchema"]).Properties = new Dictionary<string, IOpenApiSchema>
		{
			["child"] = childRef
		};

		OpenApiSchemaReference parentRef = new("ParentSchema", document, null);

		document.Paths = new OpenApiPaths
		{
			["/test"] = new OpenApiPathItem
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
									["application/json"] = new OpenApiMediaType { Schema = parentRef }
								}
							}
						}
					}
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		document.Components.Schemas.ShouldContainKey("ParentSchema");
		document.Components.Schemas.ShouldContainKey("ChildSchema");
		document.Components.Schemas.ShouldNotContainKey("UnusedSchema");
	}

	[Fact]
	public async Task TransformAsync_KeepsSchemasReferencedInArrayItems()
	{
		// Arrange
		UnusedComponentsCleanupTransformer transformer = new();

		OpenApiDocument document = CreateDocumentWithPaths(schemas: new Dictionary<string, IOpenApiSchema>
		{
			["ArraySchema"] = new OpenApiSchema { Type = JsonSchemaType.Array },
			["ItemSchema"] = new OpenApiSchema { Type = JsonSchemaType.Object },
			["UnusedSchema"] = new OpenApiSchema { Type = JsonSchemaType.String }
		});

		OpenApiSchemaReference itemRef = new("ItemSchema", document, null);
		((OpenApiSchema)document.Components!.Schemas!["ArraySchema"]).Items = itemRef;

		OpenApiSchemaReference arrayRef = new("ArraySchema", document, null);

		document.Paths = new OpenApiPaths
		{
			["/items"] = new OpenApiPathItem
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
									["application/json"] = new OpenApiMediaType { Schema = arrayRef }
								}
							}
						}
					}
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		document.Components.Schemas.ShouldContainKey("ArraySchema");
		document.Components.Schemas.ShouldContainKey("ItemSchema");
		document.Components.Schemas.ShouldNotContainKey("UnusedSchema");
	}

	[Fact]
	public async Task TransformAsync_KeepsSchemasReferencedInAllOf()
	{
		// Arrange
		UnusedComponentsCleanupTransformer transformer = new();

		OpenApiDocument document = CreateDocumentWithPaths(schemas: new Dictionary<string, IOpenApiSchema>
		{
			["CompositeSchema"] = new OpenApiSchema { Type = JsonSchemaType.Object },
			["BaseSchema"] = new OpenApiSchema { Type = JsonSchemaType.Object },
			["UnusedSchema"] = new OpenApiSchema { Type = JsonSchemaType.String }
		});

		OpenApiSchemaReference baseRef = new("BaseSchema", document, null);
		((OpenApiSchema)document.Components!.Schemas!["CompositeSchema"]).AllOf = [baseRef];

		OpenApiSchemaReference compositeRef = new("CompositeSchema", document, null);

		document.Paths = new OpenApiPaths
		{
			["/composite"] = new OpenApiPathItem
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
									["application/json"] = new OpenApiMediaType { Schema = compositeRef }
								}
							}
						}
					}
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		document.Components.Schemas.ShouldContainKey("CompositeSchema");
		document.Components.Schemas.ShouldContainKey("BaseSchema");
		document.Components.Schemas.ShouldNotContainKey("UnusedSchema");
	}

	[Fact]
	public async Task TransformAsync_KeepsSchemasReferencedInOneOf()
	{
		// Arrange
		UnusedComponentsCleanupTransformer transformer = new();

		OpenApiDocument document = CreateDocumentWithPaths(schemas: new Dictionary<string, IOpenApiSchema>
		{
			["UnionSchema"] = new OpenApiSchema(),
			["OptionA"] = new OpenApiSchema { Type = JsonSchemaType.String },
			["OptionB"] = new OpenApiSchema { Type = JsonSchemaType.Integer },
			["UnusedSchema"] = new OpenApiSchema { Type = JsonSchemaType.Boolean }
		});

		OpenApiSchemaReference optionARef = new("OptionA", document, null);
		OpenApiSchemaReference optionBRef = new("OptionB", document, null);
		((OpenApiSchema)document.Components!.Schemas!["UnionSchema"]).OneOf = [optionARef, optionBRef];

		OpenApiSchemaReference unionRef = new("UnionSchema", document, null);

		document.Paths = new OpenApiPaths
		{
			["/union"] = new OpenApiPathItem
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
									["application/json"] = new OpenApiMediaType { Schema = unionRef }
								}
							}
						}
					}
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		document.Components.Schemas.ShouldContainKey("UnionSchema");
		document.Components.Schemas.ShouldContainKey("OptionA");
		document.Components.Schemas.ShouldContainKey("OptionB");
		document.Components.Schemas.ShouldNotContainKey("UnusedSchema");
	}

	[Fact]
	public async Task TransformAsync_KeepsSchemasReferencedInAnyOf()
	{
		// Arrange
		UnusedComponentsCleanupTransformer transformer = new();

		OpenApiDocument document = CreateDocumentWithPaths(schemas: new Dictionary<string, IOpenApiSchema>
		{
			["AnyOfSchema"] = new OpenApiSchema(),
			["TypeA"] = new OpenApiSchema { Type = JsonSchemaType.String },
			["TypeB"] = new OpenApiSchema { Type = JsonSchemaType.Number },
			["UnusedSchema"] = new OpenApiSchema { Type = JsonSchemaType.Array }
		});

		OpenApiSchemaReference typeARef = new("TypeA", document, null);
		OpenApiSchemaReference typeBRef = new("TypeB", document, null);
		((OpenApiSchema)document.Components!.Schemas!["AnyOfSchema"]).AnyOf = [typeARef, typeBRef];

		OpenApiSchemaReference anyOfRef = new("AnyOfSchema", document, null);

		document.Paths = new OpenApiPaths
		{
			["/anyof"] = new OpenApiPathItem
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
									["application/json"] = new OpenApiMediaType { Schema = anyOfRef }
								}
							}
						}
					}
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		document.Components.Schemas.ShouldContainKey("AnyOfSchema");
		document.Components.Schemas.ShouldContainKey("TypeA");
		document.Components.Schemas.ShouldContainKey("TypeB");
		document.Components.Schemas.ShouldNotContainKey("UnusedSchema");
	}

	[Fact]
	public async Task TransformAsync_KeepsSchemasReferencedInAdditionalProperties()
	{
		// Arrange
		UnusedComponentsCleanupTransformer transformer = new();

		OpenApiDocument document = CreateDocumentWithPaths(schemas: new Dictionary<string, IOpenApiSchema>
		{
			["DictionarySchema"] = new OpenApiSchema { Type = JsonSchemaType.Object },
			["ValueSchema"] = new OpenApiSchema { Type = JsonSchemaType.String },
			["UnusedSchema"] = new OpenApiSchema { Type = JsonSchemaType.Integer }
		});

		OpenApiSchemaReference valueRef = new("ValueSchema", document, null);
		((OpenApiSchema)document.Components!.Schemas!["DictionarySchema"]).AdditionalProperties = valueRef;

		OpenApiSchemaReference dictRef = new("DictionarySchema", document, null);

		document.Paths = new OpenApiPaths
		{
			["/dictionary"] = new OpenApiPathItem
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
									["application/json"] = new OpenApiMediaType { Schema = dictRef }
								}
							}
						}
					}
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		document.Components.Schemas.ShouldContainKey("DictionarySchema");
		document.Components.Schemas.ShouldContainKey("ValueSchema");
		document.Components.Schemas.ShouldNotContainKey("UnusedSchema");
	}

	[Fact]
	public async Task TransformAsync_KeepsSchemasReferencedInNot()
	{
		// Arrange
		UnusedComponentsCleanupTransformer transformer = new();

		OpenApiDocument document = CreateDocumentWithPaths(schemas: new Dictionary<string, IOpenApiSchema>
		{
			["NotSchema"] = new OpenApiSchema { Type = JsonSchemaType.Object },
			["ExcludedSchema"] = new OpenApiSchema { Type = JsonSchemaType.String },
			["UnusedSchema"] = new OpenApiSchema { Type = JsonSchemaType.Integer }
		});

		OpenApiSchemaReference excludedRef = new("ExcludedSchema", document, null);
		((OpenApiSchema)document.Components!.Schemas!["NotSchema"]).Not = excludedRef;

		OpenApiSchemaReference notRef = new("NotSchema", document, null);

		document.Paths = new OpenApiPaths
		{
			["/not"] = new OpenApiPathItem
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
									["application/json"] = new OpenApiMediaType { Schema = notRef }
								}
							}
						}
					}
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		document.Components.Schemas.ShouldContainKey("NotSchema");
		document.Components.Schemas.ShouldContainKey("ExcludedSchema");
		document.Components.Schemas.ShouldNotContainKey("UnusedSchema");
	}

	#endregion

	#region Header Schema References

	[Fact]
	public async Task TransformAsync_KeepsSchemasReferencedInResponseHeaders()
	{
		// Arrange
		UnusedComponentsCleanupTransformer transformer = new();

		OpenApiDocument document = CreateDocumentWithPaths(schemas: new Dictionary<string, IOpenApiSchema>
		{
			["HeaderSchema"] = new OpenApiSchema { Type = JsonSchemaType.String },
			["UnusedSchema"] = new OpenApiSchema { Type = JsonSchemaType.Object }
		});

		OpenApiSchemaReference headerSchemaRef = new("HeaderSchema", document, null);

		document.Paths = new OpenApiPaths
		{
			["/test"] = new OpenApiPathItem
			{
				Operations = new Dictionary<HttpMethod, OpenApiOperation>
				{
					[HttpMethod.Get] = new OpenApiOperation
					{
						Responses = new OpenApiResponses
						{
							["200"] = new OpenApiResponse
							{
								Headers = new Dictionary<string, IOpenApiHeader>
								{
									["X-Custom-Header"] = new OpenApiHeader
									{
										Schema = headerSchemaRef
									}
								}
							}
						}
					}
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		document.Components.ShouldNotBeNull();
		document.Components.Schemas.ShouldNotBeNull();
		document.Components.Schemas.ShouldContainKey("HeaderSchema");
		document.Components.Schemas.ShouldNotContainKey("UnusedSchema");
	}

	[Fact]
	public async Task TransformAsync_KeepsSchemasReferencedInEncodingHeaders()
	{
		// Arrange
		UnusedComponentsCleanupTransformer transformer = new();

		OpenApiDocument document = CreateDocumentWithPaths(schemas: new Dictionary<string, IOpenApiSchema>
		{
			["EncodingHeaderSchema"] = new OpenApiSchema { Type = JsonSchemaType.String },
			["UnusedSchema"] = new OpenApiSchema { Type = JsonSchemaType.Object }
		});

		OpenApiSchemaReference headerSchemaRef = new("EncodingHeaderSchema", document, null);

		document.Paths = new OpenApiPaths
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
									Encoding = new Dictionary<string, OpenApiEncoding>
									{
										["file"] = new OpenApiEncoding
										{
											Headers = new Dictionary<string, IOpenApiHeader>
											{
												["Content-Disposition"] = new OpenApiHeader
												{
													Schema = headerSchemaRef
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		document.Components.ShouldNotBeNull();
		document.Components.Schemas.ShouldNotBeNull();
		document.Components.Schemas.ShouldContainKey("EncodingHeaderSchema");
		document.Components.Schemas.ShouldNotContainKey("UnusedSchema");
	}

	#endregion

	#region All Schemas Used

	[Fact]
	public async Task TransformAsync_WhenAllSchemasAreUsed_DoesNotRemoveAny()
	{
		// Arrange
		UnusedComponentsCleanupTransformer transformer = new();

		OpenApiDocument document = CreateDocumentWithPaths(schemas: new Dictionary<string, IOpenApiSchema>
		{
			["RequestSchema"] = new OpenApiSchema { Type = JsonSchemaType.Object },
			["ResponseSchema"] = new OpenApiSchema { Type = JsonSchemaType.Object }
		});

		OpenApiSchemaReference requestRef = new("RequestSchema", document, null);
		OpenApiSchemaReference responseRef = new("ResponseSchema", document, null);

		document.Paths = new OpenApiPaths
		{
			["/test"] = new OpenApiPathItem
			{
				Operations = new Dictionary<HttpMethod, OpenApiOperation>
				{
					[HttpMethod.Post] = new OpenApiOperation
					{
						RequestBody = new OpenApiRequestBody
						{
							Content = new Dictionary<string, OpenApiMediaType>
							{
								["application/json"] = new OpenApiMediaType { Schema = requestRef }
							}
						},
						Responses = new OpenApiResponses
						{
							["200"] = new OpenApiResponse
							{
								Content = new Dictionary<string, OpenApiMediaType>
								{
									["application/json"] = new OpenApiMediaType { Schema = responseRef }
								}
							}
						}
					}
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		document.Components.ShouldNotBeNull();
		document.Components.Schemas.ShouldNotBeNull();
		document.Components.Schemas.Count.ShouldBe(2);
		document.Components.Schemas.ShouldContainKey("RequestSchema");
		document.Components.Schemas.ShouldContainKey("ResponseSchema");
	}

	#endregion

	#region Multiple Paths and Operations

	[Fact]
	public async Task TransformAsync_WithMultiplePathsAndOperations_CorrectlyIdentifiesUsedSchemas()
	{
		// Arrange
		UnusedComponentsCleanupTransformer transformer = new();

		OpenApiDocument document = CreateDocumentWithPaths(schemas: new Dictionary<string, IOpenApiSchema>
		{
			["UserSchema"] = new OpenApiSchema { Type = JsonSchemaType.Object },
			["ProductSchema"] = new OpenApiSchema { Type = JsonSchemaType.Object },
			["OrderSchema"] = new OpenApiSchema { Type = JsonSchemaType.Object },
			["UnusedSchema"] = new OpenApiSchema { Type = JsonSchemaType.String }
		});

		OpenApiSchemaReference userRef = new("UserSchema", document, null);
		OpenApiSchemaReference productRef = new("ProductSchema", document, null);
		OpenApiSchemaReference orderRef = new("OrderSchema", document, null);

		document.Paths = new OpenApiPaths
		{
			["/users"] = new OpenApiPathItem
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
									["application/json"] = new OpenApiMediaType { Schema = userRef }
								}
							}
						}
					}
				}
			},
			["/products"] = new OpenApiPathItem
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
									["application/json"] = new OpenApiMediaType { Schema = productRef }
								}
							}
						}
					},
					[HttpMethod.Post] = new OpenApiOperation
					{
						RequestBody = new OpenApiRequestBody
						{
							Content = new Dictionary<string, OpenApiMediaType>
							{
								["application/json"] = new OpenApiMediaType { Schema = productRef }
							}
						}
					}
				}
			},
			["/orders"] = new OpenApiPathItem
			{
				Operations = new Dictionary<HttpMethod, OpenApiOperation>
				{
					[HttpMethod.Post] = new OpenApiOperation
					{
						RequestBody = new OpenApiRequestBody
						{
							Content = new Dictionary<string, OpenApiMediaType>
							{
								["application/json"] = new OpenApiMediaType { Schema = orderRef }
							}
						}
					}
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		document.Components.ShouldNotBeNull();
		document.Components.Schemas.ShouldNotBeNull();
		document.Components.Schemas.Count.ShouldBe(3);
		document.Components.Schemas.ShouldContainKey("UserSchema");
		document.Components.Schemas.ShouldContainKey("ProductSchema");
		document.Components.Schemas.ShouldContainKey("OrderSchema");
		document.Components.Schemas.ShouldNotContainKey("UnusedSchema");
	}

	#endregion

	#region Cancellation Token Support

	[Fact]
	public async Task TransformAsync_WhenCancellationRequested_ThrowsOperationCanceledException()
	{
		// Arrange
		UnusedComponentsCleanupTransformer transformer = new();
		OpenApiDocument document = CreateDocumentWithPaths(
			schemas: new Dictionary<string, IOpenApiSchema>
			{
				["Schema1"] = new OpenApiSchema { Type = JsonSchemaType.Object }
			},
			paths: new OpenApiPaths
			{
				["/test"] = new OpenApiPathItem
				{
					Operations = new Dictionary<HttpMethod, OpenApiOperation>
					{
						[HttpMethod.Get] = new OpenApiOperation()
					}
				}
			});

		using CancellationTokenSource cts = new();
		cts.Cancel();

		// Act & Assert
		await Should.ThrowAsync<OperationCanceledException>(
			async () => await transformer.TransformAsync(document, CreateMockContext(), cts.Token));
	}

	#endregion

	#region Direct Schema Usage (Not via Reference)

	[Fact]
	public async Task TransformAsync_WhenInlineSchemaUsed_DoesNotAffectComponentSchemas()
	{
		// Arrange
		UnusedComponentsCleanupTransformer transformer = new();

		OpenApiDocument document = CreateDocumentWithPaths(schemas: new Dictionary<string, IOpenApiSchema>
		{
			["UnusedSchema"] = new OpenApiSchema { Type = JsonSchemaType.Object }
		});

		// Using inline schema instead of reference
		document.Paths = new OpenApiPaths
		{
			["/test"] = new OpenApiPathItem
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
									["application/json"] = new OpenApiMediaType
									{
										Schema = new OpenApiSchema { Type = JsonSchemaType.String }
									}
								}
							}
						}
					}
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert - Schema should be removed since it's not referenced
		document.Components.ShouldNotBeNull();
		document.Components.Schemas.ShouldNotBeNull();
		document.Components.Schemas.ShouldNotContainKey("UnusedSchema");
	}

	#endregion

	#region Circular References

	[Fact]
	public async Task TransformAsync_WithCircularReferences_DoesNotInfiniteLoop()
	{
		// Arrange
		UnusedComponentsCleanupTransformer transformer = new();

		OpenApiDocument document = CreateDocumentWithPaths(schemas: new Dictionary<string, IOpenApiSchema>
		{
			["NodeSchema"] = new OpenApiSchema { Type = JsonSchemaType.Object },
			["UnusedSchema"] = new OpenApiSchema { Type = JsonSchemaType.String }
		});

		// Create a self-referencing schema (node with children of same type)
		OpenApiSchemaReference selfRef = new("NodeSchema", document, null);
		((OpenApiSchema)document.Components!.Schemas!["NodeSchema"]).Properties = new Dictionary<string, IOpenApiSchema>
		{
			["children"] = new OpenApiSchema
			{
				Type = JsonSchemaType.Array,
				Items = selfRef
			}
		};

		OpenApiSchemaReference nodeRef = new("NodeSchema", document, null);

		document.Paths = new OpenApiPaths
		{
			["/nodes"] = new OpenApiPathItem
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
									["application/json"] = new OpenApiMediaType { Schema = nodeRef }
								}
							}
						}
					}
				}
			}
		};

		// Act - Should complete without infinite loop
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		document.Components.Schemas.ShouldContainKey("NodeSchema");
		document.Components.Schemas.ShouldNotContainKey("UnusedSchema");
	}

	#endregion
}
