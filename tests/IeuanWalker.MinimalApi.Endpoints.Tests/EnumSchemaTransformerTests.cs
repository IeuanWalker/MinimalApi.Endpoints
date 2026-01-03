using System.ComponentModel;
using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Shouldly;
using Xunit;

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
		var transformer = new EnumSchemaTransformer();
		var document = CreateTestDocumentWithEnum<SimpleEnum>();

		// Act
		await transformer.TransformAsync(document, CreateContext(), CancellationToken.None);

		// Assert
		OpenApiSchema? enumSchema = GetSchema(document, typeof(SimpleEnum));
		enumSchema.ShouldNotBeNull();
		enumSchema.Type.ShouldBe(JsonSchemaType.Integer);
		enumSchema.Description.ShouldContain("First");
		enumSchema.Description.ShouldContain("Second");
		enumSchema.Description.ShouldContain("Third");
		
		// Check enum extension
		enumSchema.Extensions.ShouldContainKey("enum");
		enumSchema.Extensions.ShouldContainKey("x-enum-varnames");
	}

	[Fact]
	public async Task TransformAsync_NumericEnum_PreservesNumericValues()
	{
		// Arrange
		var transformer = new EnumSchemaTransformer();
		var document = CreateTestDocumentWithEnum<NumericEnum>();

		// Act
		await transformer.TransformAsync(document, CreateContext(), CancellationToken.None);

		// Assert
		OpenApiSchema? enumSchema = GetSchema(document, typeof(NumericEnum));
		enumSchema.ShouldNotBeNull();
		enumSchema.Type.ShouldBe(JsonSchemaType.Integer);
		enumSchema.Description.ShouldContain("Zero");
		enumSchema.Description.ShouldContain("Ten");
		enumSchema.Description.ShouldContain("Twenty");
		enumSchema.Description.ShouldContain("Thirty");
		enumSchema.Extensions.ShouldContainKey("enum");
		enumSchema.Extensions.ShouldContainKey("x-enum-varnames");
	}

	[Fact]
	public async Task TransformAsync_EnumWithDescriptions_AddsDescriptionExtension()
	{
		// Arrange
		var transformer = new EnumSchemaTransformer();
		var document = CreateTestDocumentWithEnum<EnumWithDescriptions>();

		// Act
		await transformer.TransformAsync(document, CreateContext(), CancellationToken.None);

		// Assert
		OpenApiSchema? enumSchema = GetSchema(document, typeof(EnumWithDescriptions));
		enumSchema.ShouldNotBeNull();
		enumSchema.Extensions.ShouldContainKey("x-enum-descriptions");
	}

	[Fact]
	public async Task TransformAsync_ByteEnum_UsesIntegerType()
	{
		// Arrange
		var transformer = new EnumSchemaTransformer();
		var document = CreateTestDocumentWithEnum<ByteEnum>();

		// Act
		await transformer.TransformAsync(document, CreateContext(), CancellationToken.None);

		// Assert
		OpenApiSchema? enumSchema = GetSchema(document, typeof(ByteEnum));
		enumSchema.ShouldNotBeNull();
		enumSchema.Type.ShouldBe(JsonSchemaType.Integer);
		enumSchema.Description.ShouldContain("Min");
		enumSchema.Description.ShouldContain("Mid");
		enumSchema.Description.ShouldContain("Max");
	}

	[Fact]
	public async Task TransformAsync_NonEnumType_DoesNotModifySchema()
	{
		// Arrange
		var transformer = new EnumSchemaTransformer();
		var document = CreateTestDocument<string>();

		string originalDescription = document.Components?.Schemas.Values.First().Description ?? "";

		// Act
		await transformer.TransformAsync(document, CreateContext(), CancellationToken.None);

		// Assert
		OpenApiSchema? schema = document.Components?.Schemas.Values.First() as OpenApiSchema;
		schema.ShouldNotBeNull();
		
		// Should not have enum extensions
		(schema.Extensions?.ContainsKey("enum") ?? false).ShouldBe(false);
		(schema.Extensions?.ContainsKey("x-enum-varnames") ?? false).ShouldBe(false);
	}

	[Fact]
	public async Task TransformAsync_NullDocument_DoesNotThrow()
	{
		// Arrange
		var transformer = new EnumSchemaTransformer();
		var document = new OpenApiDocument();

		// Act & Assert
		await Should.NotThrowAsync(async () =>
			await transformer.TransformAsync(document, CreateContext(), CancellationToken.None)
		);
	}

	[Fact]
	public async Task TransformAsync_EnumWithoutDescription_DoesNotAddDescriptionExtension()
	{
		// Arrange
		var transformer = new EnumSchemaTransformer();
		var document = CreateTestDocumentWithEnum<SimpleEnum>();

		// Act
		await transformer.TransformAsync(document, CreateContext(), CancellationToken.None);

		// Assert
		OpenApiSchema? enumSchema = GetSchema(document, typeof(SimpleEnum));
		enumSchema.ShouldNotBeNull();
		enumSchema.Extensions.ShouldContainKey("enum");
		enumSchema.Extensions.ShouldContainKey("x-enum-varnames");
		
		// Should not have descriptions extension since SimpleEnum has no Description attributes
		enumSchema.Extensions.ContainsKey("x-enum-descriptions").ShouldBe(false);
	}

	static OpenApiDocument CreateTestDocumentWithEnum<TEnum>() where TEnum : struct, Enum
	{
		var document = new OpenApiDocument
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
		var document = new OpenApiDocument
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
		if (!document.Components.Schemas.ContainsKey(schemaKey))
		{
			return null;
		}

		return document.Components.Schemas[schemaKey] as OpenApiSchema;
	}

	[Fact]
	public async Task TransformAsync_WithNullableEnum_EnrichesSchema()
	{
		// Arrange
		var transformer = new EnumSchemaTransformer();
		var document = CreateTestDocumentWithNullableEnum<SimpleEnum>();
		var context = CreateContext();

		// Act
		await transformer.TransformAsync(document, context, CancellationToken.None);

		// Assert
		var nullableEnumSchema = GetSchema(document, typeof(SimpleEnum?));
		nullableEnumSchema.ShouldNotBeNull();
		
		// Should have enum values
		nullableEnumSchema.Extensions.ShouldContainKey("enum");
		
		// Should have varnames
		nullableEnumSchema.Extensions.ShouldContainKey("x-enum-varnames");
		
		// Should have description
		nullableEnumSchema.Description.ShouldContain("First");
		nullableEnumSchema.Description.ShouldContain("Second");
		nullableEnumSchema.Description.ShouldContain("Third");
	}

	static OpenApiDocument CreateTestDocumentWithNullableEnum<TEnum>() where TEnum : struct, Enum
	{
		var document = new OpenApiDocument
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
