using System.Text.Json.Nodes;
using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.PropertyEnhancer;
using Microsoft.OpenApi;

namespace IeuanWalker.MinimalApi.Endpoints.Tests.OpenApiDocumentTransformers.PropertyEnhancer;

public class NullableSchemaNormalizationTransformerTests
{
	[Fact]
	public async Task TransformAsync_NormalizesInlineValueAndNullComposition()
	{
		OpenApiDocument document = CreateDocument(new OpenApiSchema
		{
			OneOf =
			[
				new OpenApiSchema { Type = JsonSchemaType.Boolean },
				new OpenApiSchema { Type = JsonSchemaType.Null }
			]
		});

		await new NullableSchemaNormalizationTransformer().TransformAsync(document, null!, CancellationToken.None);

		OpenApiSchema result = document.Components!.Schemas!["test"].ShouldBeOfType<OpenApiSchema>();
		result.Type.ShouldBe(JsonSchemaType.Boolean | JsonSchemaType.Null);
		result.OneOf.ShouldBeNull();
	}

	[Fact]
	public async Task TransformAsync_PreservesValueSchemaWithConstAssertion()
	{
		OpenApiSchema valueSchema = new()
		{
			Type = JsonSchemaType.String,
			Const = "fixed"
		};
		OpenApiSchema wrapper = new()
		{
			OneOf = [valueSchema, new OpenApiSchema { Type = JsonSchemaType.Null }]
		};
		OpenApiDocument document = CreateDocument(wrapper);

		await new NullableSchemaNormalizationTransformer().TransformAsync(document, null!, CancellationToken.None);

		OpenApiSchema result = document.Components!.Schemas!["test"].ShouldBeOfType<OpenApiSchema>();
		result.ShouldBeSameAs(wrapper);
		result.OneOf.ShouldNotBeNull();
		result.OneOf[0].ShouldBeSameAs(valueSchema);
	}

	[Fact]
	public async Task TransformAsync_PreservesNullSchemaWithConstAssertion()
	{
		OpenApiSchema nullSchema = new()
		{
			Type = JsonSchemaType.Null,
			Const = "not-null"
		};
		OpenApiSchema wrapper = new()
		{
			OneOf = [new OpenApiSchema { Type = JsonSchemaType.String }, nullSchema]
		};
		OpenApiDocument document = CreateDocument(wrapper);

		await new NullableSchemaNormalizationTransformer().TransformAsync(document, null!, CancellationToken.None);

		OpenApiSchema result = document.Components!.Schemas!["test"].ShouldBeOfType<OpenApiSchema>();
		result.ShouldBeSameAs(wrapper);
		result.OneOf.ShouldNotBeNull();
		result.OneOf[1].ShouldBeSameAs(nullSchema);
	}

	[Fact]
	public async Task TransformAsync_PreservesValueSchemaWithNestedComposition()
	{
		OpenApiSchema valueSchema = new()
		{
			Type = JsonSchemaType.String,
			AllOf = [new OpenApiSchema { Type = JsonSchemaType.String, MinLength = 1 }]
		};
		OpenApiSchema wrapper = new()
		{
			OneOf = [valueSchema, new OpenApiSchema { Type = JsonSchemaType.Null }]
		};
		OpenApiDocument document = CreateDocument(wrapper);

		await new NullableSchemaNormalizationTransformer().TransformAsync(document, null!, CancellationToken.None);

		OpenApiSchema result = document.Components!.Schemas!["test"].ShouldBeOfType<OpenApiSchema>();
		result.ShouldBeSameAs(wrapper);
		result.OneOf.ShouldNotBeNull();
		result.OneOf[0].ShouldBeSameAs(valueSchema);
	}

	[Fact]
	public async Task TransformAsync_NormalizesNestedInlineSchemaAndPreservesMetadata()
	{
		OpenApiSchema wrapper = new()
		{
			Description = "Nullable count",
			OneOf =
			[
				new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32", Minimum = "1" },
				new OpenApiSchema { Type = JsonSchemaType.Null }
			]
		};
		OpenApiDocument document = CreateDocument(new OpenApiSchema
		{
			Type = JsonSchemaType.Object,
			Properties = new Dictionary<string, IOpenApiSchema> { ["count"] = wrapper }
		});

		await new NullableSchemaNormalizationTransformer().TransformAsync(document, null!, CancellationToken.None);

		OpenApiSchema root = document.Components!.Schemas!["test"].ShouldBeOfType<OpenApiSchema>();
		OpenApiSchema result = root.Properties!["count"].ShouldBeOfType<OpenApiSchema>();
		result.Type.ShouldBe(JsonSchemaType.Integer | JsonSchemaType.Null);
		result.Format.ShouldBe("int32");
		result.Minimum.ShouldBe("1");
		result.Description.ShouldBe("Nullable count");
		result.OneOf.ShouldBeNull();
	}

	[Fact]
	public async Task TransformAsync_PreservesWrapperWithSemanticSiblingKeywords()
	{
		OpenApiSchema wrapper = new()
		{
			MinLength = 3,
			Extensions = new Dictionary<string, IOpenApiExtension>
			{
				["x-wrapper"] = new JsonNodeExtension(JsonValue.Create(true)!)
			},
			OneOf =
			[
				new OpenApiSchema { Type = JsonSchemaType.String },
				new OpenApiSchema { Type = JsonSchemaType.Null }
			]
		};
		OpenApiDocument document = CreateDocument(wrapper);

		await new NullableSchemaNormalizationTransformer().TransformAsync(document, null!, CancellationToken.None);

		OpenApiSchema result = document.Components!.Schemas!["test"].ShouldBeOfType<OpenApiSchema>();
		result.ShouldBeSameAs(wrapper);
		result.MinLength.ShouldBe(3);
		result.Extensions.ShouldNotBeNull();
		result.Extensions.ShouldContainKey("x-wrapper");
		result.OneOf.ShouldNotBeNull();
		result.OneOf.Count.ShouldBe(2);
	}

	[Fact]
	public async Task TransformAsync_PreservesNullableReferenceComposition()
	{
		OpenApiSchemaReference reference = new("Referenced", null, null);
		OpenApiSchema wrapper = new()
		{
			OneOf =
			[
				reference,
				new OpenApiSchema { Type = JsonSchemaType.Null }
			]
		};
		OpenApiDocument document = CreateDocument(wrapper);

		await new NullableSchemaNormalizationTransformer().TransformAsync(document, null!, CancellationToken.None);

		OpenApiSchema result = document.Components!.Schemas!["test"].ShouldBeOfType<OpenApiSchema>();
		result.ShouldBeSameAs(wrapper);
		result.OneOf.ShouldNotBeNull();
		result.OneOf.Count.ShouldBe(2);
		result.OneOf[0].ShouldBeSameAs(reference);
	}

	[Fact]
	public async Task TransformAsync_SerializesInlineNullableEnumUsingOpenApi30NullableKeyword()
	{
		OpenApiSchema valueSchema = new()
		{
			Type = JsonSchemaType.Integer,
			Enum = [JsonValue.Create(1), JsonValue.Create(2), JsonValue.Create(3)]
		};
		OpenApiDocument document = CreateDocument(new OpenApiSchema
		{
			OneOf = [valueSchema, new OpenApiSchema { Type = JsonSchemaType.Null }]
		});

		await new NullableSchemaNormalizationTransformer().TransformAsync(document, null!, CancellationToken.None);

		OpenApiSchema result = document.Components!.Schemas!["test"].ShouldBeOfType<OpenApiSchema>();
		result.Enum.ShouldNotBeNull();
		result.Enum.Count.ShouldBe(4);
		result.Enum[^1].ShouldBeNull();
		string json = await result.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_0, TestContext.Current.CancellationToken);
		json.ShouldContain("\"type\": \"integer\"");
		json.ShouldContain("\"nullable\": true");
		json.ShouldContain("null");
	}

	[Fact]
	public async Task TransformAsync_NormalizesContentSchemasForParametersAndHeaders()
	{
		OpenApiParameter parameter = new()
		{
			Content = new Dictionary<string, OpenApiMediaType>
			{
				["application/json"] = new() { Schema = CreateNullableSchema(JsonSchemaType.Integer) }
			}
		};
		OpenApiHeader header = new()
		{
			Content = new Dictionary<string, OpenApiMediaType>
			{
				["application/json"] = new() { Schema = CreateNullableSchema(JsonSchemaType.String) }
			}
		};
		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Parameters = new Dictionary<string, IOpenApiParameter> { ["parameter"] = parameter },
				Headers = new Dictionary<string, IOpenApiHeader> { ["header"] = header }
			}
		};

		await new NullableSchemaNormalizationTransformer().TransformAsync(document, null!, CancellationToken.None);

		OpenApiSchema parameterSchema = parameter.Content!["application/json"].Schema.ShouldBeOfType<OpenApiSchema>();
		parameterSchema.Type.ShouldBe(JsonSchemaType.Integer | JsonSchemaType.Null);
		parameterSchema.OneOf.ShouldBeNull();
		OpenApiSchema headerSchema = header.Content!["application/json"].Schema.ShouldBeOfType<OpenApiSchema>();
		headerSchema.Type.ShouldBe(JsonSchemaType.String | JsonSchemaType.Null);
		headerSchema.OneOf.ShouldBeNull();
	}

	static OpenApiSchema CreateNullableSchema(JsonSchemaType type)
	{
		return new OpenApiSchema
		{
			OneOf =
			[
				new OpenApiSchema { Type = type },
				new OpenApiSchema { Type = JsonSchemaType.Null }
			]
		};
	}

	static OpenApiDocument CreateDocument(IOpenApiSchema schema)
	{
		return new OpenApiDocument
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema> { ["test"] = schema }
			}
		};
	}
}
