using System.ComponentModel;
using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace IeuanWalker.MinimalApi.Endpoints.Tests;

// Test enums - defined outside the test class so they have proper type names
public enum SimpleEnum
{
	First,
	Second,
	Third
}

public enum NumericEnum
{
	Zero = 0,
	Ten = 10,
	Twenty = 20,
	Thirty = 30
}

public enum EnumWithDescriptions
{
	[Description("This is the first value")]
	First,
	[Description("This is the second value")]
	Second,
	[Description("This is the third value")]
	Third
}

public enum ByteEnum : byte
{
	Min = 0,
	Mid = 127,
	Max = 255
}

public class EnumTestModel
{
	public SimpleEnum Status { get; set; }
	public NumericEnum Priority { get; set; }
}

public class EnumSchemaTransformerTests
{
	[Fact]
	public async Task TransformAsync_SimpleEnum_AddsEnumValuesAndNames()
	{
		// Arrange
		EnumSchemaTransformer transformer = new();
		OpenApiDocument document = CreateTestDocumentWithEnum<SimpleEnum>();

		// Act
		await transformer.TransformAsync(document, CreateContext(), CancellationToken.None);

		// Assert
		OpenApiSchema? enumSchema = GetSchema(document, typeof(SimpleEnum));
		enumSchema.ShouldNotBeNull();
		enumSchema.Type.ShouldBe(JsonSchemaType.Integer);
		enumSchema.Description!.ShouldContain("First");
		enumSchema.Description!.ShouldContain("Second");
		enumSchema.Description!.ShouldContain("Third");

		// Check enum extension
		enumSchema.Extensions!.ShouldContainKey("enum");
		enumSchema.Extensions!.ShouldContainKey("x-enum-varnames");
	}

	[Fact]
	public async Task TransformAsync_NumericEnum_PreservesNumericValues()
	{
		// Arrange
		EnumSchemaTransformer transformer = new();
		OpenApiDocument document = CreateTestDocumentWithEnum<NumericEnum>();

		// Act
		await transformer.TransformAsync(document, CreateContext(), CancellationToken.None);

		// Assert
		OpenApiSchema? enumSchema = GetSchema(document, typeof(NumericEnum));
		enumSchema.ShouldNotBeNull();
		enumSchema.Type.ShouldBe(JsonSchemaType.Integer);
		enumSchema.Description!.ShouldContain("Zero");
		enumSchema.Description!.ShouldContain("Ten");
		enumSchema.Description!.ShouldContain("Twenty");
		enumSchema.Description!.ShouldContain("Thirty");
		enumSchema.Extensions!.ShouldContainKey("enum");
		enumSchema.Extensions!.ShouldContainKey("x-enum-varnames");
	}

	[Fact]
	public async Task TransformAsync_EnumWithDescriptions_AddsDescriptionExtension()
	{
		// Arrange
		EnumSchemaTransformer transformer = new();
		OpenApiDocument document = CreateTestDocumentWithEnum<EnumWithDescriptions>();

		// Act
		await transformer.TransformAsync(document, CreateContext(), CancellationToken.None);

		// Assert
		OpenApiSchema? enumSchema = GetSchema(document, typeof(EnumWithDescriptions));
		enumSchema.ShouldNotBeNull();
		enumSchema.Extensions!.ShouldContainKey("x-enum-descriptions");
	}

	[Fact]
	public async Task TransformAsync_ByteEnum_UsesIntegerType()
	{
		// Arrange
		EnumSchemaTransformer transformer = new();
		OpenApiDocument document = CreateTestDocumentWithEnum<ByteEnum>();

		// Act
		await transformer.TransformAsync(document, CreateContext(), CancellationToken.None);

		// Assert
		OpenApiSchema? enumSchema = GetSchema(document, typeof(ByteEnum));
		enumSchema.ShouldNotBeNull();
		enumSchema.Type.ShouldBe(JsonSchemaType.Integer);
		enumSchema.Description!.ShouldContain("Min");
		enumSchema.Description!.ShouldContain("Mid");
		enumSchema.Description!.ShouldContain("Max");
	}

	[Fact]
	public async Task TransformAsync_NonEnumType_DoesNotModifySchema()
	{
		// Arrange
		EnumSchemaTransformer transformer = new();
		OpenApiDocument document = CreateTestDocument<string>();

		// Act
		await transformer.TransformAsync(document, CreateContext(), CancellationToken.None);

		// Assert
		OpenApiSchema? schema = document.Components?.Schemas?.Values.First() as OpenApiSchema;
		schema.ShouldNotBeNull();

		// Should not have enum extensions
		(schema.Extensions?.ContainsKey("enum") ?? false).ShouldBe(false);
		(schema.Extensions?.ContainsKey("x-enum-varnames") ?? false).ShouldBe(false);
	}

	[Fact]
	public async Task TransformAsync_NullDocument_DoesNotThrow()
	{
		// Arrange
		EnumSchemaTransformer transformer = new();
		OpenApiDocument document = new();

		// Act & Assert
		await Should.NotThrowAsync(async () =>
			await transformer.TransformAsync(document, CreateContext(), CancellationToken.None)
		);
	}

	[Fact]
	public async Task TransformAsync_EnumWithoutDescription_DoesNotAddDescriptionExtension()
	{
		// Arrange
		EnumSchemaTransformer transformer = new();
		OpenApiDocument document = CreateTestDocumentWithEnum<SimpleEnum>();

		// Act
		await transformer.TransformAsync(document, CreateContext(), CancellationToken.None);

		// Assert
		OpenApiSchema? enumSchema = GetSchema(document, typeof(SimpleEnum));
		enumSchema.ShouldNotBeNull();
		enumSchema.Extensions!.ShouldContainKey("enum");
		enumSchema.Extensions!.ShouldContainKey("x-enum-varnames");

		// Should not have descriptions extension since SimpleEnum has no Description attributes
		enumSchema.Extensions!.ContainsKey("x-enum-descriptions").ShouldBe(false);
	}

	static OpenApiDocument CreateTestDocumentWithEnum<TEnum>() where TEnum : struct, Enum
	{
		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>
				{
					[typeof(TEnum).FullName!] = new OpenApiSchema
					{
						Type = JsonSchemaType.Integer
					}
				}
			}
		};

		return document;
	}

	static OpenApiDocument CreateTestDocument<T>()
	{
		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>
				{
					[typeof(T).FullName!] = new OpenApiSchema
					{
						Type = JsonSchemaType.String
					}
				}
			}
		};

		return document;
	}

	static OpenApiDocumentTransformerContext CreateContext()
	{
		return new OpenApiDocumentTransformerContext
		{
			DocumentName = "v1",
			ApplicationServices = new ServiceCollection().BuildServiceProvider(),
			DescriptionGroups = []
		};
	}

	static OpenApiSchema? GetSchema(OpenApiDocument document, Type type)
	{
		if (document.Components?.Schemas is null)
		{
			return null;
		}

		string schemaKey = type.FullName!;
		if (!document.Components.Schemas.TryGetValue(schemaKey, out IOpenApiSchema? value))
		{
			return null;
		}

		return value as OpenApiSchema;
	}

	[Fact]
	public async Task TransformAsync_WithNullableEnum_EnrichesSchema()
	{
		// Arrange
		EnumSchemaTransformer transformer = new();
		OpenApiDocument document = CreateTestDocumentWithNullableEnum<SimpleEnum>();
		OpenApiDocumentTransformerContext context = CreateContext();

		// Act
		await transformer.TransformAsync(document, context, CancellationToken.None);

		// Assert
		OpenApiSchema? nullableEnumSchema = GetSchema(document, typeof(SimpleEnum?));
		nullableEnumSchema.ShouldNotBeNull();

		// Should have enum values
		nullableEnumSchema.Extensions!.ShouldContainKey("enum");

		// Should have varnames
		nullableEnumSchema.Extensions!.ShouldContainKey("x-enum-varnames");

		// Should have description
		nullableEnumSchema.Description!.ShouldContain("First");
		nullableEnumSchema.Description!.ShouldContain("Second");
		nullableEnumSchema.Description!.ShouldContain("Third");
	}

	static OpenApiDocument CreateTestDocumentWithNullableEnum<TEnum>() where TEnum : struct, Enum
	{
		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>
				{
					[typeof(Nullable<TEnum>).FullName!] = new OpenApiSchema
					{
						Type = JsonSchemaType.Integer
					}
				}
			}
		};

		return document;
	}
}
