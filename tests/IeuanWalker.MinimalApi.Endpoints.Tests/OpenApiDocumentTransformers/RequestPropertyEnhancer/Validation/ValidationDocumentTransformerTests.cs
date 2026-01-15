using System.Runtime.CompilerServices;
using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace IeuanWalker.MinimalApi.Endpoints.Tests.OpenApiDocumentTransformers.RequestPropertyEnhancer.Validation;

public class ValidationDocumentTransformerTests
{
	#region Test Helpers

	static OpenApiDocumentTransformerContext CreateMockContext()
	{
		WebApplicationBuilder builder = WebApplication.CreateBuilder();
		WebApplication app = builder.Build();

		Type contextType = typeof(OpenApiDocumentTransformerContext);
		object context = RuntimeHelpers.GetUninitializedObject(contextType);

		System.Reflection.PropertyInfo? applicationServicesProp = contextType.GetProperty("ApplicationServices");
		applicationServicesProp?.SetValue(context, app.Services);

		return (OpenApiDocumentTransformerContext)context;
	}

	static OpenApiDocument CreateEmptyDocument()
	{
		return new OpenApiDocument();
	}

	static OpenApiDocument CreateDocumentWithComponents()
	{
		return new OpenApiDocument
		{
			Components = new OpenApiComponents()
		};
	}

	static OpenApiDocument CreateDocumentWithSchemas(Dictionary<string, IOpenApiSchema>? schemas = null)
	{
		return new OpenApiDocument
		{
			Components = new OpenApiComponents
			{
				Schemas = schemas ?? []
			}
		};
	}

	static OpenApiDocument CreateDocumentWithPaths(OpenApiPaths? paths = null)
	{
		return new OpenApiDocument
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
			},
			Paths = paths ?? []
		};
	}

	#endregion

	#region Constructor and Property Tests

	[Fact]
	public void Constructor_SetsDefaultPropertyValues()
	{
		// Act
		ValidationDocumentTransformer transformer = new();

		// Assert
		transformer.AutoDocumentFluentValidation.ShouldBeTrue();
		transformer.AutoDocumentDataAnnotationValidation.ShouldBeTrue();
		transformer.AppendRulesToPropertyDescription.ShouldBeTrue();
	}

	[Fact]
	public void AutoDocumentFluentValidation_CanBeSetToFalse()
	{
		// Arrange
		ValidationDocumentTransformer transformer = new()
		{
			// Act
			AutoDocumentFluentValidation = false
		};

		// Assert
		transformer.AutoDocumentFluentValidation.ShouldBeFalse();
	}

	[Fact]
	public void AutoDocumentDataAnnotationValidation_CanBeSetToFalse()
	{
		// Arrange
		ValidationDocumentTransformer transformer = new()
		{
			// Act
			AutoDocumentDataAnnotationValidation = false
		};

		// Assert
		transformer.AutoDocumentDataAnnotationValidation.ShouldBeFalse();
	}

	[Fact]
	public void AppendRulesToPropertyDescription_CanBeSetToFalse()
	{
		// Arrange
		ValidationDocumentTransformer transformer = new()
		{
			// Act
			AppendRulesToPropertyDescription = false
		};

		// Assert
		transformer.AppendRulesToPropertyDescription.ShouldBeFalse();
	}

	[Fact]
	public void Transformer_ImplementsIOpenApiDocumentTransformer()
	{
		// Act
		ValidationDocumentTransformer transformer = new();

		// Assert
		transformer.ShouldBeAssignableTo<IOpenApiDocumentTransformer>();
	}

	#endregion

	#region TransformAsync - Basic Scenarios

	[Fact]
	public async Task TransformAsync_WhenDocumentIsEmpty_CompletesSuccessfully()
	{
		// Arrange
		ValidationDocumentTransformer transformer = new();
		OpenApiDocument document = CreateEmptyDocument();

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
		ValidationDocumentTransformer transformer = new();
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
		ValidationDocumentTransformer transformer = new();
		OpenApiDocument document = CreateDocumentWithComponents();

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
		ValidationDocumentTransformer transformer = new();
		OpenApiDocument document = CreateDocumentWithSchemas([]);

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
		ValidationDocumentTransformer transformer = new();
		OpenApiDocument document = CreateDocumentWithSchemas(
			new Dictionary<string, IOpenApiSchema>
			{
				["TestSchema"] = new OpenApiSchema { Type = JsonSchemaType.Object }
			});

		// Act
		Task task = transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}

	[Fact]
	public async Task TransformAsync_WhenEmptyPaths_DoesNotThrow()
	{
		// Arrange
		ValidationDocumentTransformer transformer = new();
		OpenApiDocument document = CreateDocumentWithPaths([]);

		// Act
		Task task = transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}

	#endregion

	#region TransformAsync - Configuration Options

	[Fact]
	public async Task TransformAsync_WithAllOptionsDisabled_CompletesSuccessfully()
	{
		// Arrange
		ValidationDocumentTransformer transformer = new()
		{
			AutoDocumentFluentValidation = false,
			AutoDocumentDataAnnotationValidation = false,
			AppendRulesToPropertyDescription = false
		};

		OpenApiDocument document = CreateDocumentWithSchemas(
			new Dictionary<string, IOpenApiSchema>
			{
				["TestSchema"] = new OpenApiSchema
				{
					Type = JsonSchemaType.Object,
					Properties = new Dictionary<string, IOpenApiSchema>
					{
						["name"] = new OpenApiSchema { Type = JsonSchemaType.String }
					}
				}
			});

		// Act
		Task task = transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}

	[Fact]
	public async Task TransformAsync_WithOnlyFluentValidationEnabled_CompletesSuccessfully()
	{
		// Arrange
		ValidationDocumentTransformer transformer = new()
		{
			AutoDocumentFluentValidation = true,
			AutoDocumentDataAnnotationValidation = false,
			AppendRulesToPropertyDescription = true
		};

		OpenApiDocument document = CreateDocumentWithPaths(
			new OpenApiPaths
			{
				["/test"] = new OpenApiPathItem
				{
					Operations = new Dictionary<HttpMethod, OpenApiOperation>
					{
						[HttpMethod.Get] = new OpenApiOperation()
					}
				}
			});

		// Act
		Task task = transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}

	[Fact]
	public async Task TransformAsync_WithOnlyDataAnnotationsEnabled_CompletesSuccessfully()
	{
		// Arrange
		ValidationDocumentTransformer transformer = new()
		{
			AutoDocumentFluentValidation = false,
			AutoDocumentDataAnnotationValidation = true,
			AppendRulesToPropertyDescription = true
		};

		OpenApiDocument document = CreateDocumentWithPaths(
			new OpenApiPaths
			{
				["/test"] = new OpenApiPathItem
				{
					Operations = new Dictionary<HttpMethod, OpenApiOperation>
					{
						[HttpMethod.Post] = new OpenApiOperation()
					}
				}
			});

		// Act
		Task task = transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}

	#endregion

	#region TransformAsync - Document Structure Handling

	[Fact]
	public async Task TransformAsync_WithSchemaWithoutProperties_DoesNotThrow()
	{
		// Arrange
		ValidationDocumentTransformer transformer = new();
		OpenApiDocument document = CreateDocumentWithSchemas(new Dictionary<string, IOpenApiSchema>
		{
			["System.String"] = new OpenApiSchema { Type = JsonSchemaType.String }
		});

		// Act
		Task task = transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}

	[Fact]
	public async Task TransformAsync_WithComplexSchema_DoesNotThrow()
	{
		// Arrange
		ValidationDocumentTransformer transformer = new();

		OpenApiDocument document = CreateDocumentWithSchemas(new Dictionary<string, IOpenApiSchema>
		{
			["ParentSchema"] = new OpenApiSchema
			{
				Type = JsonSchemaType.Object,
				Properties = new Dictionary<string, IOpenApiSchema>
				{
					["name"] = new OpenApiSchema { Type = JsonSchemaType.String },
					["child"] = new OpenApiSchema
					{
						Type = JsonSchemaType.Object,
						Properties = new Dictionary<string, IOpenApiSchema>
						{
							["value"] = new OpenApiSchema { Type = JsonSchemaType.Integer }
						}
					}
				}
			}
		});

		// Act
		Task task = transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}

	[Fact]
	public async Task TransformAsync_WithArraySchema_DoesNotThrow()
	{
		// Arrange
		ValidationDocumentTransformer transformer = new();

		OpenApiDocument document = CreateDocumentWithSchemas(new Dictionary<string, IOpenApiSchema>
		{
			["ArraySchema"] = new OpenApiSchema
			{
				Type = JsonSchemaType.Array,
				Items = new OpenApiSchema
				{
					Type = JsonSchemaType.String
				}
			}
		});

		// Act
		Task task = transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}

	[Fact]
	public async Task TransformAsync_WithPathParameters_DoesNotThrow()
	{
		// Arrange
		ValidationDocumentTransformer transformer = new();

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>
				{
					["TestSchema"] = new OpenApiSchema
					{
						Type = JsonSchemaType.Object
					}
				}
			},
			Paths = new OpenApiPaths
			{
				["/test/{id}"] = new OpenApiPathItem
				{
					Operations = new Dictionary<HttpMethod, OpenApiOperation>
					{
						[HttpMethod.Get] = new OpenApiOperation
						{
							Parameters =
							[
								new OpenApiParameter
								{
									Name = "id",
									In = ParameterLocation.Path,
									Schema = new OpenApiSchema
									{
										Type = JsonSchemaType.Integer
									}
								}
							]
						}
					}
				}
			}
		};

		// Act
		Task task = transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}

	[Fact]
	public async Task TransformAsync_WithQueryParameters_DoesNotThrow()
	{
		// Arrange
		ValidationDocumentTransformer transformer = new();

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
			},
			Paths = new OpenApiPaths
			{
				["/search"] = new OpenApiPathItem
				{
					Operations = new Dictionary<HttpMethod, OpenApiOperation>
					{
						[HttpMethod.Get] = new OpenApiOperation
						{
							Parameters =
							[
								new OpenApiParameter
								{
									Name = "query",
									In = ParameterLocation.Query,
									Schema = new OpenApiSchema
									{
										Type = JsonSchemaType.String
									}
								},
								new OpenApiParameter
								{
									Name = "limit",
									In = ParameterLocation.Query,
									Schema = new OpenApiSchema
									{
										Type = JsonSchemaType.Integer
									}
								}
							]
						}
					}
				}
			}
		};

		// Act
		Task task = transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}

	[Fact]
	public async Task TransformAsync_WithRequestBody_DoesNotThrow()
	{
		// Arrange
		ValidationDocumentTransformer transformer = new();

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>
				{
					["RequestBody"] = new OpenApiSchema
					{
						Type = JsonSchemaType.Object,
						Properties = new Dictionary<string, IOpenApiSchema>
						{
							["name"] = new OpenApiSchema
							{
								Type = JsonSchemaType.String
							},
							["email"] = new OpenApiSchema
							{
								Type = JsonSchemaType.String
							}
						}
					}
				}
			},
			Paths = new OpenApiPaths
			{
				["/users"] = new OpenApiPathItem
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
										Schema = new OpenApiSchema
										{
											Type = JsonSchemaType.Object
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
		Task task = transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}

	[Fact]
	public async Task TransformAsync_WithMultipleOperations_DoesNotThrow()
	{
		// Arrange
		ValidationDocumentTransformer transformer = new();

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
			},
			Paths = new OpenApiPaths
			{
				["/items"] = new OpenApiPathItem
				{
					Operations = new Dictionary<HttpMethod, OpenApiOperation>
					{
						[HttpMethod.Get] = new OpenApiOperation(),
						[HttpMethod.Post] = new OpenApiOperation(),
						[HttpMethod.Put] = new OpenApiOperation(),
						[HttpMethod.Delete] = new OpenApiOperation()
					}
				}
			}
		};

		// Act
		Task task = transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}

	[Fact]
	public async Task TransformAsync_WithMultiplePaths_DoesNotThrow()
	{
		// Arrange
		ValidationDocumentTransformer transformer = new();

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
			},
			Paths = new OpenApiPaths
			{
				["/users"] = new OpenApiPathItem
				{
					Operations = new Dictionary<HttpMethod, OpenApiOperation>
					{
						[HttpMethod.Get] = new OpenApiOperation()
					}
				},
				["/products"] = new OpenApiPathItem
				{
					Operations = new Dictionary<HttpMethod, OpenApiOperation>
					{
						[HttpMethod.Get] = new OpenApiOperation()
					}
				},
				["/orders"] = new OpenApiPathItem
				{
					Operations = new Dictionary<HttpMethod, OpenApiOperation>
					{
						[HttpMethod.Get] = new OpenApiOperation()
					}
				}
			}
		};

		// Act
		Task task = transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}

	#endregion

	#region TransformAsync - Edge Cases

	[Fact]
	public async Task TransformAsync_WithNonOpenApiPathItem_DoesNotThrow()
	{
		// Arrange
		ValidationDocumentTransformer transformer = new();

		// Document with paths but the path item might not be OpenApiPathItem
		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
			},
			Paths = []
		};

		// Act
		Task task = transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}

	[Fact]
	public async Task TransformAsync_WithParameterWithoutName_DoesNotThrow()
	{
		// Arrange
		ValidationDocumentTransformer transformer = new();

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
			},
			Paths = new OpenApiPaths
			{
				["/test"] = new OpenApiPathItem
				{
					Operations = new Dictionary<HttpMethod, OpenApiOperation>
					{
						[HttpMethod.Get] = new OpenApiOperation
						{
							Parameters =
							[
								new OpenApiParameter
								{
									// Name is null/empty
									In = ParameterLocation.Query,
									Schema = new OpenApiSchema
									{
										Type = JsonSchemaType.String
									}
								}
							]
						}
					}
				}
			}
		};

		// Act
		Task task = transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}

	[Fact]
	public async Task TransformAsync_WithParameterWithoutSchema_DoesNotThrow()
	{
		// Arrange
		ValidationDocumentTransformer transformer = new();

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
			},
			Paths = new OpenApiPaths
			{
				["/test"] = new OpenApiPathItem
				{
					Operations = new Dictionary<HttpMethod, OpenApiOperation>
					{
						[HttpMethod.Get] = new OpenApiOperation
						{
							Parameters =
							[
								new OpenApiParameter
								{
									Name = "test",
									In = ParameterLocation.Query
									// Schema is null
								}
							]
						}
					}
				}
			}
		};

		// Act
		Task task = transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}

	[Fact]
	public async Task TransformAsync_WithOperationWithoutParameters_DoesNotThrow()
	{
		// Arrange
		ValidationDocumentTransformer transformer = new();

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
			},
			Paths = new OpenApiPaths
			{
				["/test"] = new OpenApiPathItem
				{
					Operations = new Dictionary<HttpMethod, OpenApiOperation>
					{
						[HttpMethod.Get] = new OpenApiOperation
						{
							// Parameters is null
						}
					}
				}
			}
		};

		// Act
		Task task = transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}

	[Fact]
	public async Task TransformAsync_WithPathItemWithoutOperations_DoesNotThrow()
	{
		// Arrange
		ValidationDocumentTransformer transformer = new();

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
			},
			Paths = new OpenApiPaths
			{
				["/test"] = new OpenApiPathItem
				{
					// Operations is null
				}
			}
		};

		// Act
		Task task = transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}

	#endregion

	#region TransformAsync - Return Value

	[Fact]
	public async Task TransformAsync_ReturnsCompletedTask()
	{
		// Arrange
		ValidationDocumentTransformer transformer = new();
		OpenApiDocument document = CreateEmptyDocument();

		// Act
		Task result = transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);
		await result;

		// Assert
		result.IsCompleted.ShouldBeTrue();
		result.IsFaulted.ShouldBeFalse();
		result.IsCanceled.ShouldBeFalse();
	}

	#endregion
}
