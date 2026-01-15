using System.Text.Json.Nodes;
using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer.Core;
using Microsoft.OpenApi;

namespace IeuanWalker.MinimalApi.Endpoints.Tests.OpenApiDocumentTransformers.RequestPropertyEnhancer.Core;

public class OpenApiSchemaHelperTests
{
	[Fact]
	public void TryAsOpenApiSchema_ReturnsTrueForOpenApiSchema()
	{
		// Arrange
		OpenApiSchema schema = new();

		// Act
		bool result = OpenApiSchemaHelper.TryAsOpenApiSchema(schema, out OpenApiSchema? openApiSchema);

		// Assert
		result.ShouldBeTrue();
		openApiSchema.ShouldBeSameAs(schema);
	}

	[Theory]
	[InlineData(SchemaConstants.SystemString, JsonSchemaType.String, null)]
	[InlineData(SchemaConstants.SystemInt32, JsonSchemaType.Integer, SchemaConstants.FormatInt32)]
	public void CreatePrimitiveSchemaFromRefId_ReturnsExpected(string refId, JsonSchemaType expectedType, string? expectedFormat)
	{
		// Arrange + Act
		OpenApiSchema? schema = OpenApiSchemaHelper.CreatePrimitiveSchemaFromRefId(refId);

		// Assert
		schema.ShouldNotBeNull();
		schema.Type.ShouldNotBeNull();
		schema.Type.ShouldBe(expectedType);
		schema.Format.ShouldBe(expectedFormat);
	}

	[Fact]
	public void GetTypeAndFormatFromRefId_ArrayAndCollection_ReturnsArray()
	{
		// Arrange + Act
		(JsonSchemaType? t, _) = OpenApiSchemaHelper.GetTypeAndFormatFromRefId("MyNamespace.Foo[]");

		// Assert
		t.ShouldBe(JsonSchemaType.Array);

		// Act
		(t, _) = OpenApiSchemaHelper.GetTypeAndFormatFromRefId(SchemaConstants.ListGenericType + "[System.String]");

		// Assert
		t.ShouldBe(JsonSchemaType.Array);
	}

	[Fact]
	public void CreateSchemaFromType_ArrayAndGenericAndNullable_Works()
	{
		// Arrange + Act
		OpenApiSchema arraySchema = OpenApiSchemaHelper.CreateSchemaFromType(typeof(int[]));
		OpenApiSchema listSchema = OpenApiSchemaHelper.CreateSchemaFromType(typeof(List<string>));
		OpenApiSchema nullableSchema = OpenApiSchemaHelper.CreateSchemaFromType(typeof(int?));

		// Assert
		arraySchema.Type.ShouldBe(JsonSchemaType.Array);
		arraySchema.Items.ShouldNotBeNull();
		arraySchema.Items.Type.ShouldNotBeNull();
		arraySchema.Items.Type.ShouldBe(JsonSchemaType.Integer);

		listSchema.Type.ShouldBe(JsonSchemaType.Array);
		listSchema.Items.ShouldNotBeNull();
		listSchema.Items.Type.ShouldNotBeNull();
		listSchema.Items.Type.ShouldBe(JsonSchemaType.String);

		nullableSchema.OneOf.ShouldNotBeNull();
		nullableSchema.OneOf.Count.ShouldBe(2);

		nullableSchema.OneOf[1].ShouldNotBeNull();
		OpenApiSchema? marker = nullableSchema.OneOf[1] as OpenApiSchema;
		marker.ShouldNotBeNull();
		marker.Extensions.ShouldNotBeNull();
		marker.Extensions.ContainsKey(SchemaConstants.NullableExtension).ShouldBeTrue();
	}

	[Fact]
	public void UnwrapNullableEnumReference_ExtractsUnderlyingType()
	{
		// Arrange
		OpenApiDocument doc = new();
		string id = SchemaConstants.NullableTypePrefix + "[[MyNamespace.MyEnum, MyAssembly]]";
		OpenApiSchemaReference schemaRef = new(id, doc, null);

		// Act
		OpenApiSchemaReference unwrapped = OpenApiSchemaHelper.UnwrapNullableEnumReference(schemaRef);

		// Assert
		unwrapped.Reference.ShouldNotBeNull();
		unwrapped.Reference.Id.ShouldBe("MyNamespace.MyEnum");
	}

	[Fact]
	public void InlinePrimitiveTypeReference_InlinesSystemTypes()
	{
		// Arrange
		OpenApiDocument doc = new();
		OpenApiSchemaReference schemaRef = new(SchemaConstants.SystemString, doc, null);

		// Act
		IOpenApiSchema result = OpenApiSchemaHelper.InlinePrimitiveTypeReference(schemaRef, doc);
		OpenApiSchemaReference otherRef = new("MyNamespace.Foo", doc, null);
		IOpenApiSchema otherResult = OpenApiSchemaHelper.InlinePrimitiveTypeReference(otherRef, doc);

		// Assert
		result.ShouldBeOfType<OpenApiSchema>();
		OpenApiSchema s = (OpenApiSchema)result;
		s.Type.ShouldBe(JsonSchemaType.String);
		otherResult.ShouldBeSameAs(otherRef);
	}

	[Fact]
	public void IsEnumSchemaReference_FindsReferencedEnum()
	{
		// Arrange
		OpenApiDocument doc = new()
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
			}
		};
		OpenApiSchema enumSchema = new()
		{
			Extensions = new Dictionary<string, IOpenApiExtension>
			{
				[SchemaConstants.EnumExtension] = new JsonNodeExtension(new JsonArray
				{
					JsonValue.Create("A")!
				})
			}
		};
		doc.Components.Schemas!["MyEnum"] = enumSchema;

		OpenApiSchemaReference schemaRef = new("MyEnum", doc, null);

		// Act
		bool result = OpenApiSchemaHelper.IsEnumSchemaReference(schemaRef, doc);

		// Assert
		result.ShouldBeTrue();
	}

	// Note: creating IOpenApiAny (OpenApiString) types is not available in test context reliably,
	// so IsInlineEnumSchema behavior is exercised indirectly via EnrichSchemaWithEnumValues and
	// IsEnumSchemaReference tests which cover the relevant code paths.

	[Fact]
	public void EnsureSchemaHasType_SetsArrayOrObject()
	{
		OpenApiSchema schema = new()
		{
			Items = new OpenApiSchema()
		};
		OpenApiSchemaHelper.EnsureSchemaHasType(schema);
		schema.Type.ShouldBe(JsonSchemaType.Array);

		OpenApiSchema objSchema = new()
		{
			Properties = new Dictionary<string, IOpenApiSchema>
			{
				["a"] = new OpenApiSchema()
			},
			Type = null
		};
		OpenApiSchemaHelper.EnsureSchemaHasType(objSchema);
		objSchema.Type.ShouldBe(JsonSchemaType.Object);
	}

	enum TestEnum
	{
		[System.ComponentModel.Description("First value")] A,
		B
	}

	[Fact]
	public void EnrichSchemaWithEnumValues_AddsExtensionsAndDescription()
	{
		// Arrange
		OpenApiSchema schema = new();

		// Act
		OpenApiSchemaHelper.EnrichSchemaWithEnumValues(schema, typeof(TestEnum), forStringSchema: false);

		// Assert
		schema.Extensions.ShouldNotBeNull();
		schema.Extensions.ContainsKey(SchemaConstants.EnumExtension).ShouldBeTrue();
		schema.Extensions.ContainsKey(SchemaConstants.EnumVarNamesExtension).ShouldBeTrue();
		schema.Extensions.ContainsKey(SchemaConstants.EnumDescriptionsExtension).ShouldBeTrue();
		schema.Description.ShouldStartWith("Enum:");
	}
}
