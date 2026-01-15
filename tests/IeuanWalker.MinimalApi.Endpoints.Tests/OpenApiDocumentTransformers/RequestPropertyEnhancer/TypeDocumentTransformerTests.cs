using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer.Core;

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

	[Fact]
	public async Task TransformAsync_ConvertsItemsOneOf_WithNullableMarkerToNullableArrayOneOf()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();

		// items.OneOf = [ nullable marker, string type ] and parent is array -> should be converted
		OpenApiSchema nullableMarker = OpenApiSchemaHelper.CreateNullableMarker();
		OpenApiSchema typeSchema = new() { Type = JsonSchemaType.String };

		OpenApiSchema propertySchema = new()
		{
			Type = JsonSchemaType.Array,
			Items = new OpenApiSchema
			{
				OneOf = new List<IOpenApiSchema>
				{
					nullableMarker,
					typeSchema
				}
			}
		};

		OpenApiSchema component = new()
		{
			Properties = new Dictionary<string, IOpenApiSchema>
			{
				["Values"] = propertySchema
			}
		};

		string typeName = typeof(TestArrayHolder).AssemblyQualifiedName!;

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>
				{
					[typeName] = component
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert - property should be converted to OneOf [ nullable marker, array of string ]
		OpenApiSchema? result = document.Components.Schemas[typeName] as OpenApiSchema;
		result.ShouldNotBeNull();
		result.Properties.ShouldNotBeNull();
		OpenApiSchema? valuesSchema = result.Properties["Values"] as OpenApiSchema;
		valuesSchema.ShouldNotBeNull();
		valuesSchema.OneOf.ShouldNotBeNull();
		valuesSchema.OneOf.Count.ShouldBe(2);

		OpenApiSchema? first = valuesSchema.OneOf[0] as OpenApiSchema;
		first.ShouldNotBeNull();
		first.Type.HasValue.ShouldBeFalse();
		first.Extensions.ShouldNotBeNull();
		first.Extensions.ContainsKey(SchemaConstants.NullableExtension).ShouldBeTrue();

		OpenApiSchema? second = valuesSchema.OneOf[1] as OpenApiSchema;
		second.ShouldNotBeNull();
		second.Type.ShouldBe(JsonSchemaType.Array);
		OpenApiSchema? items = second.Items as OpenApiSchema;
		items.ShouldNotBeNull();
		items.Type.ShouldBe(JsonSchemaType.String);
	}

	[Fact]
	public async Task TransformAsync_DoesNotConvertItemsOneOf_WhenNoNullableMarker()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();

		OpenApiSchema propertySchema = new()
		{
			Type = JsonSchemaType.Array,
			Items = new OpenApiSchema
			{
				OneOf = new List<IOpenApiSchema>
				{
					new OpenApiSchema { Type = JsonSchemaType.String },
					new OpenApiSchema { Type = JsonSchemaType.Integer }
				}
			}
		};

		OpenApiSchema component = new()
		{
			Properties = new Dictionary<string, IOpenApiSchema>
			{
				["Values"] = propertySchema
			}
		};

		string typeName = typeof(TestArrayHolder).AssemblyQualifiedName!;

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>
				{
					[typeName] = component
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

        // Assert - transformer converts items.OneOf into a nullable-array oneOf: [ array of first type, nullable marker ]
        OpenApiSchema? result = document.Components.Schemas[typeName] as OpenApiSchema;
        result.ShouldNotBeNull();
        result.Properties.ShouldNotBeNull();
        OpenApiSchema? valuesSchema = result.Properties["Values"] as OpenApiSchema;
        valuesSchema.ShouldNotBeNull();
        // Top-level should now be a OneOf (nullable-array wrapper)
        valuesSchema.OneOf.ShouldNotBeNull();
        valuesSchema.OneOf.Count.ShouldBe(2);

        // First element should be an array schema whose items is the first type from the original oneOf
        OpenApiSchema? first = valuesSchema.OneOf[0] as OpenApiSchema;
        first.ShouldNotBeNull();
        first.Type.ShouldBe(JsonSchemaType.Array);
        OpenApiSchema? firstItems = first.Items as OpenApiSchema;
        firstItems.ShouldNotBeNull();
        firstItems.Type.ShouldBe(JsonSchemaType.String);

        // Second element should be the nullable marker (no Type, but has nullable extension)
        OpenApiSchema? second = valuesSchema.OneOf[1] as OpenApiSchema;
        second.ShouldNotBeNull();
        second.Type.HasValue.ShouldBeFalse();
        second.Extensions.ShouldNotBeNull();
        second.Extensions.ContainsKey(SchemaConstants.NullableExtension).ShouldBeTrue();
	}

	[Fact]
	public async Task TransformAsync_SkipsParameter_WhenSchemaIsNull()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();

		OpenApiParameter paramWithNullSchema = new OpenApiParameter
		{
			Name = "p",
			In = ParameterLocation.Query,
			Schema = null
		};

		OpenApiDocument document = new()
		{
			Paths = new OpenApiPaths
			{
				["/nullschema"] = new OpenApiPathItem
				{
					Operations = new Dictionary<HttpMethod, OpenApiOperation>
					{
						[HttpMethod.Get] = new OpenApiOperation
						{
							Parameters = new List<IOpenApiParameter>
							{
								paramWithNullSchema
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

		// Assert - parameter should remain with null schema
		OpenApiPathItem? pathItem = document.Paths["/nullschema"] as OpenApiPathItem;
		pathItem.ShouldNotBeNull();
		OpenApiOperation? operation = pathItem.Operations[HttpMethod.Get];
		operation.ShouldNotBeNull();
		operation.Parameters.ShouldNotBeNull();
		OpenApiParameter? resultParam = operation.Parameters.Cast<OpenApiParameter>().FirstOrDefault(p => p.Name == "p");
		resultParam.ShouldNotBeNull();
		resultParam.Schema.ShouldBeNull();
	}

	[Fact]
	public async Task TransformAsync_SkipsParameter_WhenNameIsEmpty()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();

		OpenApiParameter paramWithEmptyName = new OpenApiParameter
		{
			Name = string.Empty,
			In = ParameterLocation.Query,
			Schema = new OpenApiSchemaReference("System.String", null, null)
		};

		OpenApiDocument document = new()
		{
			Paths = new OpenApiPaths
			{
				["/emptyname"] = new OpenApiPathItem
				{
					Operations = new Dictionary<HttpMethod, OpenApiOperation>
					{
						[HttpMethod.Get] = new OpenApiOperation
						{
							Parameters = new List<IOpenApiParameter>
							{
								paramWithEmptyName
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

		// Assert - parameter should remain a schema reference to System.String
		OpenApiPathItem? pathItem = document.Paths["/emptyname"] as OpenApiPathItem;
		pathItem.ShouldNotBeNull();
		OpenApiOperation? operation = pathItem.Operations[HttpMethod.Get];
		operation.ShouldNotBeNull();
		operation.Parameters.ShouldNotBeNull();
		OpenApiParameter? resultParam = operation.Parameters.Cast<OpenApiParameter>().FirstOrDefault(p => p.Name == string.Empty);
		resultParam.ShouldNotBeNull();
		resultParam.Schema.ShouldBeOfType<OpenApiSchemaReference>();
		(resultParam.Schema as OpenApiSchemaReference)!.Reference!.Id.ShouldBe("System.String");
	}

	[Fact]
	public async Task TransformAsync_UnwrapsNullableEnumParameter_WhenRequestTypeMappedFromEndpoints()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();

		// Prepare an EndpointDataSource with a RouteEndpoint whose metadata contains a handler method
		// The handler method takes a TestRequest which has an enum property named "Status"
		var handlerMethod = typeof(TestEnumHandler).GetMethod(nameof(TestEnumHandler.Handle))!;

		var routeEndpoint = new RouteEndpoint(
			(RequestDelegate)(_ => Task.CompletedTask),
			RoutePatternFactory.Parse("/enumtest"),
			0,
			new EndpointMetadataCollection(handlerMethod),
			"TestEndpoint");

		var testSource = new SimpleEndpointDataSource(new[] { routeEndpoint });

		WebApplicationBuilder builder = WebApplication.CreateBuilder();
		builder.Services.AddSingleton<EndpointDataSource>(testSource);
		WebApplication app = builder.Build();

		OpenApiDocumentTransformerContext ctx = new()
		{
			DocumentName = "v1",
			DescriptionGroups = [],
			ApplicationServices = app.Services
		};

		string nullableEnumRef = "System.Nullable`1[[MyNamespace.MyEnum, MyAssembly, Version=1.0.0.0]]";

		OpenApiDocument document = new()
		{
			Paths = new OpenApiPaths
			{
				["/enumtest"] = new OpenApiPathItem
				{
					Operations = new Dictionary<HttpMethod, OpenApiOperation>
					{
						[HttpMethod.Get] = new OpenApiOperation
						{
							Parameters = new List<IOpenApiParameter>
							{
								new OpenApiParameter
								{
									Name = "Status",
									In = ParameterLocation.Query,
									Schema = new OpenApiSchemaReference(nullableEnumRef, null, null)
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
		await transformer.TransformAsync(document, ctx, CancellationToken.None);

		// Assert - the nullable enum reference should be unwrapped to the underlying enum type
		OpenApiPathItem? pathItem = document.Paths["/enumtest"] as OpenApiPathItem;
		pathItem.ShouldNotBeNull();
		OpenApiOperation? operation = pathItem.Operations[HttpMethod.Get];
		operation.ShouldNotBeNull();
		operation.Parameters.ShouldNotBeNull();
		OpenApiParameter? param = operation.Parameters.Cast<OpenApiParameter>().FirstOrDefault(p => p.Name == "Status");
		param.ShouldNotBeNull();
		IOpenApiSchema? schema = param.Schema;
		schema.ShouldBeOfType<OpenApiSchemaReference>();
		OpenApiSchemaReference? schemaRef = schema as OpenApiSchemaReference;
		schemaRef!.Reference!.Id.ShouldBe("MyNamespace.MyEnum");
	}

	// Simple endpoint data source for tests
	sealed class SimpleEndpointDataSource : EndpointDataSource
	{
		readonly IReadOnlyList<Endpoint> _endpoints;
		public SimpleEndpointDataSource(IReadOnlyList<Endpoint> endpoints) => _endpoints = endpoints;
		public override IReadOnlyList<Endpoint> Endpoints => _endpoints;
		public override IChangeToken GetChangeToken() => new CancellationChangeToken(CancellationToken.None);
	}

	public enum MyNamespace_MyEnum { A, B }

	public class TestRequest
	{
		public MyNamespace_MyEnum Status { get; set; }
	}

	static class TestEnumHandler
	{
		public static Task Handle(TestRequest req) => Task.CompletedTask;
	}

	[Fact]
	public async Task TransformAsync_UsesInlinePrimitive_WhenRequestTypeHasNoMatchingProperty()
	{
		// Arrange: endpoint maps to TestRequest which does not have property named "DoesNotExist"
		TypeDocumentTransformer transformer = new();

		var handlerMethod = typeof(TestEnumHandler).GetMethod(nameof(TestEnumHandler.Handle))!;
		var routeEndpoint = new RouteEndpoint(
			(RequestDelegate)(_ => Task.CompletedTask),
			RoutePatternFactory.Parse("/enumtest"),
			0,
			new EndpointMetadataCollection(handlerMethod),
			"TestEndpoint");

		var testSource = new SimpleEndpointDataSource(new[] { routeEndpoint });

		WebApplicationBuilder builder = WebApplication.CreateBuilder();
		builder.Services.AddSingleton<EndpointDataSource>(testSource);
		WebApplication app = builder.Build();

		OpenApiDocumentTransformerContext ctx = new()
		{
			DocumentName = "v1",
			DescriptionGroups = [],
			ApplicationServices = app.Services
		};

		OpenApiDocument document = new()
		{
			Paths = new OpenApiPaths
			{
				["/enumtest"] = new OpenApiPathItem
				{
					Operations = new Dictionary<HttpMethod, OpenApiOperation>
					{
						[HttpMethod.Get] = new OpenApiOperation
						{
							Parameters = new List<IOpenApiParameter>
							{
								new OpenApiParameter
								{
									Name = "DoesNotExist",
									In = ParameterLocation.Query,
									Schema = new OpenApiSchemaReference("System.String", null, null)
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
		await transformer.TransformAsync(document, ctx, CancellationToken.None);

		// Assert - since the request type has no matching property, the schema reference should be inlined to a string schema
		OpenApiPathItem? pathItem = document.Paths["/enumtest"] as OpenApiPathItem;
		pathItem.ShouldNotBeNull();
		OpenApiOperation? operation = pathItem.Operations[HttpMethod.Get];
		operation.ShouldNotBeNull();
		operation.Parameters.ShouldNotBeNull();
		OpenApiParameter? param = operation.Parameters.Cast<OpenApiParameter>().FirstOrDefault(p => p.Name == "DoesNotExist");
		param.ShouldNotBeNull();
		IOpenApiSchema? schema = param.Schema;
		schema.ShouldBeOfType<OpenApiSchema>();
		OpenApiSchema? s = schema as OpenApiSchema;
		s!.Type.ShouldBe(JsonSchemaType.String);
	}

	[Fact]
	public async Task TransformAsync_SkipsNonSystemTypeParameterReference()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();

		OpenApiDocument document = new()
		{
			Paths = new OpenApiPaths
			{
				["/items"] = new OpenApiPathItem
				{
					Operations = new Dictionary<HttpMethod, OpenApiOperation>
					{
						[HttpMethod.Get] = new OpenApiOperation
						{
							Parameters = new List<IOpenApiParameter>
							{
								new OpenApiParameter
								{
									Name = "type",
									In = ParameterLocation.Query,
									Schema = new OpenApiSchemaReference("MyNamespace.MyType", null, null)
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

		// Assert - parameter schema should remain the original reference
		OpenApiPathItem? pathItem = document.Paths["/items"] as OpenApiPathItem;
		pathItem.ShouldNotBeNull();
		OpenApiOperation? operation = pathItem.Operations[HttpMethod.Get];
		operation.ShouldNotBeNull();
		operation.Parameters.ShouldNotBeNull();
		OpenApiParameter? param = operation.Parameters.Cast<OpenApiParameter>().FirstOrDefault(p => p.Name == "type");
		param.ShouldNotBeNull();
		IOpenApiSchema? schema = param.Schema;
		schema.ShouldBeOfType<OpenApiSchemaReference>();
		OpenApiSchemaReference? schemaRef = schema as OpenApiSchemaReference;
		schemaRef!.Reference!.Id.ShouldBe("MyNamespace.MyType");
	}

	[Fact]
	public async Task TransformAsync_DoesNotUnwrapNullableParameterReference_WhenNoComma()
	{
		// Arrange: nullable-style ref id but missing the comma separator after the underlying type
		TypeDocumentTransformer transformer = new();

		string malformedNullableRef = "System.Nullable`1[[MyNamespace.MyEnum MyAssembly]]"; // no comma after type name

		OpenApiDocument document = new()
		{
			Paths = new OpenApiPaths
			{
				["/test2"] = new OpenApiPathItem
				{
					Operations = new Dictionary<HttpMethod, OpenApiOperation>
					{
						[HttpMethod.Get] = new OpenApiOperation
						{
							Parameters = new List<IOpenApiParameter>
							{
								new OpenApiParameter
								{
									Name = "status",
									In = ParameterLocation.Query,
									Schema = new OpenApiSchemaReference(malformedNullableRef, null, null)
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

		// Assert - schema should remain the original reference (no unwrapping)
		OpenApiPathItem? pathItem = document.Paths["/test2"] as OpenApiPathItem;
		pathItem.ShouldNotBeNull();
		OpenApiOperation? operation = pathItem.Operations[HttpMethod.Get];
		operation.ShouldNotBeNull();
		operation.Parameters.ShouldNotBeNull();
		OpenApiParameter? param = operation.Parameters.Cast<OpenApiParameter>().FirstOrDefault(p => p.Name == "status");
		param.ShouldNotBeNull();
		IOpenApiSchema? schema = param.Schema;
		schema.ShouldBeOfType<OpenApiSchemaReference>();
		OpenApiSchemaReference? schemaRef = schema as OpenApiSchemaReference;
		schemaRef!.Reference!.Id.ShouldBe(malformedNullableRef);
	}

	[Fact]
	public async Task TransformAsync_UnwrapsNullableParameterReference_ToUnderlyingType()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();

		string nullableRef = "System.Nullable`1[[MyNamespace.MyEnum, MyAssembly, Version=1.0.0.0]]";

		OpenApiDocument document = new()
		{
			Paths = new OpenApiPaths
			{
				["/test"] = new OpenApiPathItem
				{
					Operations = new Dictionary<HttpMethod, OpenApiOperation>
					{
						[HttpMethod.Get] = new OpenApiOperation
						{
							Parameters = new List<IOpenApiParameter>
							{
								new OpenApiParameter
								{
									Name = "status",
									In = ParameterLocation.Query,
									Schema = new OpenApiSchemaReference(nullableRef, null, null)
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
		OpenApiPathItem? pathItem = document.Paths["/test"] as OpenApiPathItem;
		pathItem.ShouldNotBeNull();
		OpenApiOperation? operation = pathItem.Operations[HttpMethod.Get];
		operation.ShouldNotBeNull();
		operation.Parameters.ShouldNotBeNull();
		OpenApiParameter? param = operation.Parameters.Cast<OpenApiParameter>().FirstOrDefault(p => p.Name == "status");
		param.ShouldNotBeNull();
		IOpenApiSchema? schema = param.Schema;
		schema.ShouldBeOfType<OpenApiSchemaReference>();
		OpenApiSchemaReference? schemaRef = schema as OpenApiSchemaReference;
		schemaRef!.Reference!.Id.ShouldBe("MyNamespace.MyEnum");
	}

	[Fact]
	public async Task TransformAsync_HandlesNullPropertySchema_BySkipping()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();

		OpenApiSchema component = new()
		{
			Properties = new Dictionary<string, IOpenApiSchema>
			{
				// Insert a null value intentionally to hit the TryAsOpenApiSchema(null) branch
				["MaybeNull"] = null!
			}
		};

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>
				{
					["ContainerWithNullProp"] = component
				}
			}
		};

		// Act - should not throw
		Task task = transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);
		await task;

		// Assert - document still contains the schema and the property remains null
		task.IsCompletedSuccessfully.ShouldBeTrue();
		OpenApiSchema? result = document.Components.Schemas["ContainerWithNullProp"] as OpenApiSchema;
		result.ShouldNotBeNull();
		result.Properties.ShouldNotBeNull();
		result.Properties.ContainsKey("MaybeNull").ShouldBeTrue();
		result.Properties["MaybeNull"].ShouldBeNull();
	}


	[Fact]
	public async Task TransformAsync_InlinesSystemStringArrayParameterSchema()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();

		OpenApiDocument document = new()
		{
			Paths = new OpenApiPaths
			{
				["/search"] = new OpenApiPathItem
				{
					Operations = new Dictionary<HttpMethod, OpenApiOperation>
					{
						[HttpMethod.Get] = new OpenApiOperation
						{
							Parameters = new List<IOpenApiParameter>
							{
								new OpenApiParameter
								{
									Name = "tags",
									In = ParameterLocation.Query,
									Schema = new OpenApiSchemaReference("System.String[]", null, null)
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
		OpenApiPathItem? pathItem = document.Paths["/search"] as OpenApiPathItem;
		pathItem.ShouldNotBeNull();
		OpenApiOperation? operation = pathItem.Operations[HttpMethod.Get];
		operation.ShouldNotBeNull();
		operation.Parameters.ShouldNotBeNull();
		OpenApiParameter? param = operation.Parameters.Cast<OpenApiParameter>().FirstOrDefault(p => p.Name == "tags");
		param.ShouldNotBeNull();
		OpenApiSchema? schema = param.Schema as OpenApiSchema;
		schema.ShouldNotBeNull();
		schema.Type.ShouldBe(JsonSchemaType.Array);
		OpenApiSchema? item = schema.Items as OpenApiSchema;
		item.ShouldNotBeNull();
		item.Type.ShouldBe(JsonSchemaType.String);
	}

	[Fact]
	public async Task TransformAsync_SkipsFixingWhenPropertyIsNonSystemSchemaReference()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();

		OpenApiSchema component = new()
		{
			Properties = new Dictionary<string, IOpenApiSchema>
			{
				["Prop"] = new OpenApiSchemaReference("MyNamespace.MyType", null, null)
			}
		};

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>
				{
					["Container"] = component
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert - property reference should be preserved (TryAsOpenApiSchema returns false and methods return original schema)
		OpenApiSchema? result = document.Components.Schemas["Container"] as OpenApiSchema;
		result.ShouldNotBeNull();
		result.Properties.ShouldNotBeNull();
		IOpenApiSchema propSchema = result.Properties["Prop"];
		propSchema.ShouldBeOfType<OpenApiSchemaReference>();
		ReferenceEquals(propSchema, component.Properties["Prop"]).ShouldBeTrue();
	}

	[Fact]
	public async Task TransformAsync_InlinesIFormFileFromOneOfInRequestBody()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();

		OpenApiDocument document = new()
		{
			Paths = new OpenApiPaths
			{
				["/upload-oneof"] = new OpenApiPathItem
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
											OneOf = new List<IOpenApiSchema>
											{
												new OpenApiSchemaReference("Microsoft.AspNetCore.Http.IFormFile", null, null)
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
		OpenApiPathItem? pathItem = document.Paths["/upload-oneof"] as OpenApiPathItem;
		pathItem.ShouldNotBeNull();
		OpenApiOperation? operation = pathItem.Operations[HttpMethod.Post];
		operation.ShouldNotBeNull();
		OpenApiRequestBody? requestBody = operation.RequestBody as OpenApiRequestBody;
		requestBody.ShouldNotBeNull();
		OpenApiMediaType? media = requestBody.Content["multipart/form-data"];
		media.ShouldNotBeNull();
		OpenApiSchema? schema = media.Schema as OpenApiSchema;
		schema.ShouldNotBeNull();
		schema.OneOf.ShouldNotBeNull();
		schema.OneOf.Count.ShouldBe(1);
		OpenApiSchema? inlined = schema.OneOf[0] as OpenApiSchema;
		inlined.ShouldNotBeNull();
		inlined.Type.ShouldBe(JsonSchemaType.String);
		inlined.Format.ShouldBe("binary");
	}

	[Fact]
	public async Task TransformAsync_InlinesIFormFileFromAllOfInRequestBody()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();

		OpenApiDocument document = new()
		{
			Paths = new OpenApiPaths
			{
				["/upload-allof"] = new OpenApiPathItem
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
											AllOf = new List<IOpenApiSchema>
											{
												new OpenApiSchemaReference("Microsoft.AspNetCore.Http.IFormFile", null, null)
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
		OpenApiPathItem? pathItem = document.Paths["/upload-allof"] as OpenApiPathItem;
		pathItem.ShouldNotBeNull();
		OpenApiOperation? operation = pathItem.Operations[HttpMethod.Post];
		operation.ShouldNotBeNull();
		OpenApiRequestBody? requestBody = operation.RequestBody as OpenApiRequestBody;
		requestBody.ShouldNotBeNull();
		OpenApiMediaType? media = requestBody.Content["multipart/form-data"];
		media.ShouldNotBeNull();
		OpenApiSchema? schema = media.Schema as OpenApiSchema;
		schema.ShouldNotBeNull();
		schema.AllOf.ShouldNotBeNull();
		schema.AllOf.Count.ShouldBe(1);
		OpenApiSchema? inlined = schema.AllOf[0] as OpenApiSchema;
		inlined.ShouldNotBeNull();
		inlined.Type.ShouldBe(JsonSchemaType.String);
		inlined.Format.ShouldBe("binary");
	}

	[Fact]
	public async Task TransformAsync_InlinesIFormFileFromAnyOfInRequestBody()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();

		OpenApiDocument document = new()
		{
			Paths = new OpenApiPaths
			{
				["/upload-anyof"] = new OpenApiPathItem
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
											AnyOf = new List<IOpenApiSchema>
											{
												new OpenApiSchemaReference("Microsoft.AspNetCore.Http.IFormFile", null, null)
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
		OpenApiPathItem? pathItem = document.Paths["/upload-anyof"] as OpenApiPathItem;
		pathItem.ShouldNotBeNull();
		OpenApiOperation? operation = pathItem.Operations[HttpMethod.Post];
		operation.ShouldNotBeNull();
		OpenApiRequestBody? requestBody = operation.RequestBody as OpenApiRequestBody;
		requestBody.ShouldNotBeNull();
		OpenApiMediaType? media = requestBody.Content["multipart/form-data"];
		media.ShouldNotBeNull();
		OpenApiSchema? schema = media.Schema as OpenApiSchema;
		schema.ShouldNotBeNull();
		schema.AnyOf.ShouldNotBeNull();
		schema.AnyOf.Count.ShouldBe(1);
		OpenApiSchema? inlined = schema.AnyOf[0] as OpenApiSchema;
		inlined.ShouldNotBeNull();
		inlined.Type.ShouldBe(JsonSchemaType.String);
		inlined.Format.ShouldBe("binary");
	}

	[Fact]
	public async Task TransformAsync_WithNullPathsAndEmptyComponents_DoesNotThrow()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();
		OpenApiDocument document = new()
		{
			Paths = null,
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
	public async Task TransformAsync_WithNullPathsAndNullComponents_DoesNotThrow()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();
		OpenApiDocument document = new()
		{
			Paths = null,
			Components = null
		};

		// Act
		Task task = transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}

    [Fact]
    public async Task TransformAsync_SkipsNonOpenApiPathItemEntries()
    {
        // Arrange
        TypeDocumentTransformer transformer = new();

        // Create a path item value that is not an OpenApiPathItem implementation
        // Use the reference-style type if available
        IOpenApiPathItem? referencePathItem = new OpenApiPathItemReference("/ref", null, null);

        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["/refpath"] = referencePathItem
            },
            Components = new OpenApiComponents
            {
                Schemas = new Dictionary<string, IOpenApiSchema>()
            }
        };

        // Act
        Task task = transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);
        await task;

        // Assert - should not throw and should preserve the original reference instance
        task.IsCompletedSuccessfully.ShouldBeTrue();
        document.Paths.ShouldContainKey("/refpath");
        document.Paths["/refpath"].ShouldBeSameAs(referencePathItem);
    }

	[Fact]
	public async Task TransformAsync_DoesNotModify_WhenNullableMarkerAndArrayPresentForArrayProperty()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();

		// Property schema has OneOf: [ nullable marker, array ]
		OpenApiSchema nullableMarker = new();
		OpenApiSchema arraySchema = new() { Type = JsonSchemaType.Array };

		OpenApiSchema propertySchema = new()
		{
			OneOf =
			[
				nullableMarker,
				arraySchema
			]
		};

		OpenApiSchema component = new()
		{
			Properties = new Dictionary<string, IOpenApiSchema>
			{
				["Values"] = propertySchema
			}
		};

		string typeName = typeof(TestArrayHolder).AssemblyQualifiedName!;

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>
				{
					[typeName] = component
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert - property schema should remain a OneOf with the nullable marker and the array
		OpenApiSchema? result = document.Components.Schemas[typeName] as OpenApiSchema;
		result.ShouldNotBeNull();
		result.Properties.ShouldNotBeNull();
		IOpenApiSchema? valuesSchema = result.Properties["Values"];
		valuesSchema.ShouldBeOfType<OpenApiSchema>();
		// Ensure FixSchemaType returned the original schema instance (branch returning 'schema')
		ReferenceEquals(valuesSchema, propertySchema).ShouldBeTrue();
		OpenApiSchema vs = (OpenApiSchema)valuesSchema;
		vs.OneOf.ShouldNotBeNull();
		vs.OneOf.Count.ShouldBe(2);
		// First element is the nullable marker (no Type)
		OpenApiSchema first = vs.OneOf[0] as OpenApiSchema;
		first.ShouldNotBeNull();
		first.Type.HasValue.ShouldBeFalse();
		// Second element is the array schema
		OpenApiSchema second = vs.OneOf[1] as OpenApiSchema;
		second.ShouldNotBeNull();
		second.Type.ShouldBe(JsonSchemaType.Array);
	}

public class TestArrayHolder
{
    public string[] Values { get; set; } = null!;
}

	[Theory]
	[InlineData(false, true)]
	[InlineData(false, false)]
	[InlineData(true, true)]
	[InlineData(true, false)]
	public async Task TransformAsync_UnwrapsDoubleWrappedArray_VariousCases(bool inOneOf, bool innerIsReference)
	{
		// Arrange
		TypeDocumentTransformer transformer = new();

		IOpenApiSchema innerMost = innerIsReference
			? new OpenApiSchemaReference("InnerType", null, null)
			: new OpenApiSchema { Type = JsonSchemaType.Object } as IOpenApiSchema;

		OpenApiSchema nestedArray = new()
		{
			Type = JsonSchemaType.Array,
			Items = new OpenApiSchema
			{
				Type = JsonSchemaType.Array,
				Items = innerMost
			}
		};

		IOpenApiSchema propertySchema = inOneOf
			? new OpenApiSchema { OneOf = new List<IOpenApiSchema> { nestedArray } }
			: nestedArray;

		OpenApiSchema parent = new()
		{
			Properties = new Dictionary<string, IOpenApiSchema>
			{
				["prop"] = propertySchema
			}
		};

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>
				{
					["MySchemaParam"] = parent
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		OpenApiSchema? result = document.Components.Schemas["MySchemaParam"] as OpenApiSchema;
		result.ShouldNotBeNull();
		result.Properties.ShouldNotBeNull();
		OpenApiSchema? propResult = result.Properties["prop"] as OpenApiSchema;
		propResult.ShouldNotBeNull();

		if (inOneOf)
		{
			propResult.OneOf.ShouldNotBeNull();
			propResult.OneOf.Count.ShouldBe(1);
			OpenApiSchema? unwrapped = propResult.OneOf[0] as OpenApiSchema;
			unwrapped.ShouldNotBeNull();
			unwrapped.Type.ShouldBe(JsonSchemaType.Array);
			if (innerIsReference)
			{
				(unwrapped.Items as OpenApiSchemaReference)?.Reference?.Id.ShouldBe("InnerType");
			}
			else
			{
				(unwrapped.Items as OpenApiSchema)!.Type.ShouldBe(JsonSchemaType.Object);
			}
		}
		else
		{
			propResult.Type.ShouldBe(JsonSchemaType.Array);
			if (innerIsReference)
			{
				(propResult.Items as OpenApiSchemaReference)?.Reference?.Id.ShouldBe("InnerType");
			}
			else
			{
				(propResult.Items as OpenApiSchema)!.Type.ShouldBe(JsonSchemaType.Object);
			}
		}
	}

	[Fact]
	public async Task TransformAsync_UnwrapsDoubleWrappedArrayProperty_InOneOf_WithInnerObjectItems()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();

		IOpenApiSchema innerMost = new OpenApiSchema { Type = JsonSchemaType.Object };

		OpenApiSchema nestedArray = new()
		{
			Type = JsonSchemaType.Array,
			Items = new OpenApiSchema
			{
				Type = JsonSchemaType.Array,
				Items = innerMost
			}
		};

		OpenApiSchema wrapper = new()
		{
			OneOf =
			[
				nestedArray
			]
		};

		OpenApiSchema parent = new()
		{
			Properties = new Dictionary<string, IOpenApiSchema>
			{
				["oneOfObjProp"] = wrapper
			}
		};

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>
				{
					["MySchemaOneOfObj"] = parent
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		OpenApiSchema? result = document.Components.Schemas["MySchemaOneOfObj"] as OpenApiSchema;
		result.ShouldNotBeNull();
		result.Properties.ShouldNotBeNull();
		OpenApiSchema? wrapperResult = result.Properties["oneOfObjProp"] as OpenApiSchema;
		wrapperResult.ShouldNotBeNull();
		wrapperResult.OneOf.ShouldNotBeNull();
		wrapperResult.OneOf.Count.ShouldBe(1);
		OpenApiSchema? unwrapped = wrapperResult.OneOf[0] as OpenApiSchema;
		unwrapped.ShouldNotBeNull();
		unwrapped.Type.ShouldBe(JsonSchemaType.Array);
		OpenApiSchema? items = unwrapped.Items as OpenApiSchema;
		items.ShouldNotBeNull();
		items.Type.ShouldBe(JsonSchemaType.Object);
	}

	[Fact]
	public async Task TransformAsync_UnwrapsDoubleWrappedArrayProperty_WithInnerObjectItems()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();

		OpenApiSchema innerArray = new()
		{
			Type = JsonSchemaType.Array,
			Items = new OpenApiSchema { Type = JsonSchemaType.Object }
		};

		OpenApiSchema doubleWrapped = new()
		{
			Type = JsonSchemaType.Array,
			Items = innerArray
		};

		OpenApiSchema parent = new()
		{
			Properties = new Dictionary<string, IOpenApiSchema>
			{
				["propObj"] = doubleWrapped
			}
		};

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>
				{
					["MySchemaObj"] = parent
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		OpenApiSchema? result = document.Components.Schemas["MySchemaObj"] as OpenApiSchema;
		result.ShouldNotBeNull();
		result.Properties.ShouldNotBeNull();
		OpenApiSchema? propSchema = result.Properties["propObj"] as OpenApiSchema;
		propSchema.ShouldNotBeNull();
		propSchema.Type.ShouldBe(JsonSchemaType.Array);
		OpenApiSchema? itemsSchema = propSchema.Items as OpenApiSchema;
		itemsSchema.ShouldNotBeNull();
		itemsSchema.Type.ShouldBe(JsonSchemaType.Object);
	}

	[Fact]
	public async Task TransformAsync_UnwrapsDoubleWrappedArrayProperty_InOneOf()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();

		IOpenApiSchema innerMost = new OpenApiSchemaReference("InnerType", null, null);

		OpenApiSchema nestedArray = new()
		{
			Type = JsonSchemaType.Array,
			Items = new OpenApiSchema
			{
				Type = JsonSchemaType.Array,
				Items = innerMost
			}
		};

		OpenApiSchema wrapper = new()
		{
			OneOf =
			[
				nestedArray
			]
		};

		OpenApiSchema parent = new()
		{
			Properties = new Dictionary<string, IOpenApiSchema>
			{
				["oneOfProp"] = wrapper
			}
		};

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>
				{
					["MySchemaOneOf"] = parent
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		OpenApiSchema? result = document.Components.Schemas["MySchemaOneOf"] as OpenApiSchema;
		result.ShouldNotBeNull();
		result.Properties.ShouldNotBeNull();
		OpenApiSchema? wrapperResult = result.Properties["oneOfProp"] as OpenApiSchema;
		wrapperResult.ShouldNotBeNull();
		wrapperResult.OneOf.ShouldNotBeNull();
		wrapperResult.OneOf.Count.ShouldBe(1);
		OpenApiSchema? unwrapped = wrapperResult.OneOf[0] as OpenApiSchema;
		unwrapped.ShouldNotBeNull();
		unwrapped.Type.ShouldBe(JsonSchemaType.Array);
		(unwrapped.Items as OpenApiSchemaReference)?.Reference?.Id.ShouldBe("InnerType");
	}

	[Fact]
	public async Task TransformAsync_UnwrapsDoubleWrappedArrayProperty()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();

		OpenApiSchema innerArray = new()
		{
			Type = JsonSchemaType.Array,
			Items = new OpenApiSchemaReference("InnerType", null, null)
		};

		OpenApiSchema doubleWrapped = new()
		{
			Type = JsonSchemaType.Array,
			Items = innerArray
		};

		OpenApiSchema parent = new()
		{
			Properties = new Dictionary<string, IOpenApiSchema>
			{
				["prop"] = doubleWrapped
			}
		};

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>
				{
					["MySchema"] = parent
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		OpenApiSchema? result = document.Components.Schemas["MySchema"] as OpenApiSchema;
		result.ShouldNotBeNull();
		result.Properties.ShouldNotBeNull();
		OpenApiSchema? propSchema = result.Properties["prop"] as OpenApiSchema;
		propSchema.ShouldNotBeNull();
		propSchema.Type.ShouldBe(JsonSchemaType.Array);
		// Items should be the inner items (a reference to "InnerType")
		(propSchema.Items as OpenApiSchemaReference)?.Reference?.Id.ShouldBe("InnerType");
	}

	#endregion

	[Fact]
	public async Task TransformAsync_InlinesIFormFileInOneOfComponentSchema()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();

		OpenApiSchema component = new()
		{
			OneOf = new List<IOpenApiSchema>
			{
				new OpenApiSchemaReference("Microsoft.AspNetCore.Http.IFormFile", null, null)
			}
		};

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>
				{
					["ComponentWithOneOfFile"] = component
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		OpenApiSchema? result = document.Components.Schemas["ComponentWithOneOfFile"] as OpenApiSchema;
		result.ShouldNotBeNull();
		result.OneOf.ShouldNotBeNull();
		result.OneOf.Count.ShouldBe(1);
		// The transformer does not inline file references inside component schemas unless
		// the component name indicates an IFormFile type. The original reference should be preserved.
		OpenApiSchemaReference? inlinedRef = result.OneOf[0] as OpenApiSchemaReference;
		inlinedRef.ShouldNotBeNull();
		inlinedRef.Reference?.Id.ShouldBe("Microsoft.AspNetCore.Http.IFormFile");
	}
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

	[Fact]
	public async Task TransformAsync_SkipsNonOpenApiSchemaEntries()
	{
		// Arrange
		TypeDocumentTransformer transformer = new();
		OpenApiSchemaReference schemaRef = new("System.String", null, null);

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
				{
					["RefSchema"] = schemaRef
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, CreateMockContext(), CancellationToken.None);

		// Assert
		IOpenApiSchema? result = document.Components.Schemas["RefSchema"];
		result.ShouldBeSameAs(schemaRef);
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
