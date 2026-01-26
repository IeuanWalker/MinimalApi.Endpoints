using System.ComponentModel;
using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer;
using Microsoft.OpenApi;

namespace IeuanWalker.MinimalApi.Endpoints.Tests.OpenApiDocumentTransformers.RequestPropertyEnhancer;

public class EnumSchemaTransformerTests
{
	[Fact]
	public async Task TransformAsync_WhenNoSchemas_DoesNotThrow()
	{
		// Arrange
		EnumSchemaTransformer transformer = new();
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
	public async Task TransformAsync_WhenSchemaTypeNotFound_DoesNotModifySchema()
	{
		// Arrange
		EnumSchemaTransformer transformer = new();
		OpenApiSchema original = new()
		{
			Description = "Original description"
		};
		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>() // Ensure not null
				{
					["Non.Existing.Type"] = original
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, null!, CancellationToken.None);

		// Assert
		// Schema should remain unmodified because no enum type was resolved for the key
		original.Description.ShouldBe("Original description");
		original.Extensions.ShouldBeNull();
	}

	[Fact]
	public async Task TransformAsync_WithEnumType_EnrichesSchema()
	{
		// Arrange
		EnumSchemaTransformer transformer = new();

		OpenApiSchema schema = new();
		string schemaKey = typeof(TestLocalEnum).FullName!;

		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>() // Ensure not null
				{
					[schemaKey] = schema
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, null!, CancellationToken.None);

		// Assert
		schema.Extensions.ShouldNotBeNull();
		schema.Extensions.ContainsKey("enum").ShouldBeTrue();
		schema.Extensions.ContainsKey("x-enum-varnames").ShouldBeTrue();
		schema.Extensions.ContainsKey("x-enum-descriptions").ShouldBeTrue();
		schema.Description.ShouldStartWith("Enum:");
	}

	[Fact]
	public async Task TransformAsync_WhenSchemaIsReference_DoesNotModifySchema()
	{
		// Arrange
		EnumSchemaTransformer transformer = new();
		OpenApiSchemaReference reference = new("Some.Type", null, null);
		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
				{
					["Some.Type"] = reference
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, null!, CancellationToken.None);

		// Assert
		document.Components.Schemas["Some.Type"].ShouldBe(reference);
	}
}

public enum TestLocalEnum
{
	[Description("First value")] A = 0,
	[Description("Second value")] B = 1
}
