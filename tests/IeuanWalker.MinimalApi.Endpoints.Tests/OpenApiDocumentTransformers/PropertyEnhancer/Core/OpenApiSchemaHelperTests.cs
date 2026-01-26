using System.Text.Json.Nodes;
using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.PropertyEnhancer.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi;

namespace IeuanWalker.MinimalApi.Endpoints.Tests.OpenApiDocumentTransformers.PropertyEnhancer.Core;

public class OpenApiSchemaHelperTests
{
	#region TryAsOpenApiSchema Tests

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

	#endregion

	#region CreatePrimitiveSchemaFromRefId Tests

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

	[Theory]
	[InlineData(SchemaConstants.IFormFileCollection)]
	[InlineData("IFormFileCollection")]
	public void CreatePrimitiveSchemaFromRefId_IFormFileCollection_ReturnsArrayOfBinary(string refId)
	{
		// Arrange + Act
		OpenApiSchema? schema = OpenApiSchemaHelper.CreatePrimitiveSchemaFromRefId(refId);

		// Assert
		schema.ShouldNotBeNull();
		schema.Type.ShouldBe(JsonSchemaType.Array);
		schema.Items.ShouldNotBeNull();
		schema.Items.Type.ShouldBe(JsonSchemaType.String);
		schema.Items.Format.ShouldBe(SchemaConstants.FormatBinary);
	}

	#endregion

	#region GetTypeAndFormatFromRefId Tests

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

	[Theory]
	[InlineData(SchemaConstants.SystemInt32, JsonSchemaType.Integer, SchemaConstants.FormatInt32)]
	[InlineData(SchemaConstants.SystemString, JsonSchemaType.String, null)]
	[InlineData(SchemaConstants.SystemGuid, JsonSchemaType.String, SchemaConstants.FormatUuid)]
	public void GetTypeAndFormatFromRefId_PrimitiveTypes_ReturnsTypeAndFormat(string refId, JsonSchemaType expectedType, string? expectedFormat)
	{
		// Act
		(JsonSchemaType? type, string? format) = OpenApiSchemaHelper.GetTypeAndFormatFromRefId(refId);

		// Assert
		type.ShouldBe(expectedType);
		format.ShouldBe(expectedFormat);
	}

	[Fact]
	public void GetTypeAndFormatFromRefId_UnrecognizedType_ReturnsNulls()
	{
		// Arrange
		string customTypeRefId = "MyNamespace.CustomType";

		// Act
		(JsonSchemaType? type, string? format) = OpenApiSchemaHelper.GetTypeAndFormatFromRefId(customTypeRefId);

		// Assert
		type.ShouldBeNull();
		format.ShouldBeNull();
	}

	#endregion

	#region CreateSchemaFromType Tests

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

	[Theory]
	[InlineData(typeof(IEnumerable<int>), JsonSchemaType.Integer)]
	[InlineData(typeof(ICollection<string>), JsonSchemaType.String)]
	[InlineData(typeof(IReadOnlyList<bool>), JsonSchemaType.Boolean)]
	[InlineData(typeof(IReadOnlyCollection<double>), JsonSchemaType.Number)]
	public void CreateSchemaFromType_VariousCollectionTypes_CreatesArraySchema(Type collectionType, JsonSchemaType expectedItemType)
	{
		// Act
		OpenApiSchema schema = OpenApiSchemaHelper.CreateSchemaFromType(collectionType);

		// Assert
		schema.Type.ShouldBe(JsonSchemaType.Array);
		schema.Items.ShouldNotBeNull();
		schema.Items.Type.ShouldBe(expectedItemType);
	}

	[Theory]
	[InlineData(typeof(short), JsonSchemaType.Integer, SchemaConstants.FormatInt32)]
	[InlineData(typeof(byte), JsonSchemaType.Integer, SchemaConstants.FormatInt32)]
	[InlineData(typeof(decimal), JsonSchemaType.Number, null)]
	[InlineData(typeof(float), JsonSchemaType.Number, SchemaConstants.FormatFloat)]
	[InlineData(typeof(double), JsonSchemaType.Number, SchemaConstants.FormatDouble)]
	[InlineData(typeof(DateTime), JsonSchemaType.String, SchemaConstants.FormatDateTime)]
	[InlineData(typeof(DateTimeOffset), JsonSchemaType.String, SchemaConstants.FormatDateTime)]
	[InlineData(typeof(DateOnly), JsonSchemaType.String, SchemaConstants.FormatDate)]
	[InlineData(typeof(TimeOnly), JsonSchemaType.String, SchemaConstants.FormatTime)]
	[InlineData(typeof(Guid), JsonSchemaType.String, SchemaConstants.FormatUuid)]
	[InlineData(typeof(Uri), JsonSchemaType.String, SchemaConstants.FormatUri)]
	[InlineData(typeof(IFormFile), JsonSchemaType.String, SchemaConstants.FormatBinary)]
	public void CreateSchemaFromType_VariousPrimitiveTypes_CreatesCorrectSchema(Type primitiveType, JsonSchemaType expectedType, string? expectedFormat)
	{
		// Act
		OpenApiSchema schema = OpenApiSchemaHelper.CreateSchemaFromType(primitiveType);

		// Assert
		schema.Type.ShouldBe(expectedType);
		schema.Format.ShouldBe(expectedFormat);
	}

	[Fact]
	public void CreateSchemaFromType_IFormFileCollection_CreatesArrayOfBinary()
	{
		// Act
		OpenApiSchema schema = OpenApiSchemaHelper.CreateSchemaFromType(typeof(IFormFileCollection));

		// Assert
		schema.Type.ShouldBe(JsonSchemaType.Array);
		schema.Items.ShouldNotBeNull();
		schema.Items.Type.ShouldBe(JsonSchemaType.String);
		schema.Items.Format.ShouldBe(SchemaConstants.FormatBinary);
	}

	#endregion

	#region UnwrapNullableEnumReference Tests

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

	#endregion

	#region InlinePrimitiveTypeReference Tests

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
	public void InlinePrimitiveTypeReference_MalformedCollectionRefId_ReturnsOriginalReference()
	{
		// Arrange
		OpenApiDocument doc = new();
		// Malformed collection refId - has [[ but missing comma separator
		// Using a custom type so it doesn't match any primitive patterns
		string malformedRefId = "System.Collections.Generic.List`1[[MyNamespace.CustomType";
		OpenApiSchemaReference schemaRef = new(malformedRefId, doc, null);

		// Act
		IOpenApiSchema result = OpenApiSchemaHelper.InlinePrimitiveTypeReference(schemaRef, doc);

		// Assert
		// Should return the original reference when parsing fails
		result.ShouldBeSameAs(schemaRef);
	}

	#endregion

	#region IsEnumSchemaReference Tests

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

	[Theory]
	[InlineData(true, true, false)]  // Has components, has schemas, but schema not found
	[InlineData(true, false, false)] // Has components, no schemas
	[InlineData(false, false, false)] // No components
	public void IsEnumSchemaReference_WithMissingSchema_ReturnsFalse(bool hasComponents, bool hasSchemas, bool addSchema)
	{
		// Arrange
		OpenApiDocument doc = new();

		if (hasComponents)
		{
			doc.Components = new OpenApiComponents();

			if (hasSchemas)
			{
				doc.Components.Schemas = new Dictionary<string, IOpenApiSchema>();

				if (addSchema)
				{
					doc.Components.Schemas["DifferentEnum"] = new OpenApiSchema();
				}
			}
		}

		OpenApiSchemaReference schemaRef = new("MyEnum", doc, null);

		// Act
		bool result = OpenApiSchemaHelper.IsEnumSchemaReference(schemaRef, doc);

		// Assert
		result.ShouldBeFalse();
	}

	// Note: creating IOpenApiAny (OpenApiString) types is not available in test context reliably,
	// so IsInlineEnumSchema behavior is exercised indirectly via EnrichSchemaWithEnumValues and
	// IsEnumSchemaReference tests which cover the relevant code paths.

	#endregion

	#region EnsureSchemaHasType Tests

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

	#endregion

	#region EnrichSchemaWithEnumValues Tests

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

	[Fact]
	public void EnrichSchemaWithEnumValues_WithExistingDescription_PrependsEnumInfo()
	{
		// Arrange
		OpenApiSchema schema = new()
		{
			Description = "This is a custom status field for the entity."
		};

		// Act
		OpenApiSchemaHelper.EnrichSchemaWithEnumValues(schema, typeof(TestEnum), forStringSchema: false);

		// Assert
		schema.Description.ShouldNotBeNull();
		schema.Description.ShouldStartWith("Enum: A, B");
		schema.Description.ShouldContain("\n\n");
		schema.Description.ShouldContain("This is a custom status field for the entity.");
		schema.Description.ShouldBe("Enum: A, B\n\nThis is a custom status field for the entity.");
	}

	#endregion

	#region TransformSchemaReferences Tests

	[Fact]
	public void TransformSchemaReferences_TransformsProperties()
	{
		// Arrange
		OpenApiSchema propertySchema1 = new() { Type = JsonSchemaType.String };
		OpenApiSchema propertySchema2 = new() { Type = JsonSchemaType.Integer };

		OpenApiSchema parentSchema = new()
		{
			Properties = new Dictionary<string, IOpenApiSchema>
			{
				["name"] = propertySchema1,
				["age"] = propertySchema2
			}
		};

		OpenApiSchema replacementSchema = new() { Type = JsonSchemaType.Number };
		int transformCallCount = 0;

		// Act
		OpenApiSchemaHelper.TransformSchemaReferences(parentSchema, schema =>
		{
			transformCallCount++;
			return replacementSchema;
		});

		// Assert
		transformCallCount.ShouldBe(2);
		parentSchema.Properties["name"].ShouldBeSameAs(replacementSchema);
		parentSchema.Properties["age"].ShouldBeSameAs(replacementSchema);
	}

	[Fact]
	public void TransformSchemaReferences_TransformsItems()
	{
		// Arrange
		OpenApiSchema itemsSchema = new() { Type = JsonSchemaType.String };

		OpenApiSchema parentSchema = new()
		{
			Type = JsonSchemaType.Array,
			Items = itemsSchema
		};

		OpenApiSchema replacementSchema = new() { Type = JsonSchemaType.Number };
		int transformCallCount = 0;

		// Act
		OpenApiSchemaHelper.TransformSchemaReferences(parentSchema, schema =>
		{
			transformCallCount++;
			return replacementSchema;
		});

		// Assert
		transformCallCount.ShouldBe(1);
		parentSchema.Items.ShouldBeSameAs(replacementSchema);
	}

	[Fact]
	public void TransformSchemaReferences_TransformsAdditionalProperties()
	{
		// Arrange
		OpenApiSchema additionalPropsSchema = new() { Type = JsonSchemaType.String };

		OpenApiSchema parentSchema = new()
		{
			Type = JsonSchemaType.Object,
			AdditionalProperties = additionalPropsSchema
		};

		OpenApiSchema replacementSchema = new() { Type = JsonSchemaType.Number };
		int transformCallCount = 0;

		// Act
		OpenApiSchemaHelper.TransformSchemaReferences(parentSchema, schema =>
		{
			transformCallCount++;
			return replacementSchema;
		});

		// Assert
		transformCallCount.ShouldBe(1);
		parentSchema.AdditionalProperties.ShouldBeSameAs(replacementSchema);
	}

	[Fact]
	public void TransformSchemaReferences_TransformsOneOf()
	{
		// Arrange
		OpenApiSchema originalSchema1 = new() { Type = JsonSchemaType.String };
		OpenApiSchema originalSchema2 = new() { Type = JsonSchemaType.Integer };

		OpenApiSchema parentSchema = new()
		{
			OneOf = [originalSchema1, originalSchema2]
		};

		OpenApiSchema replacementSchema = new() { Type = JsonSchemaType.Number };
		int transformCallCount = 0;

		// Act
		OpenApiSchemaHelper.TransformSchemaReferences(parentSchema, schema =>
		{
			transformCallCount++;
			return replacementSchema;
		});

		// Assert
		transformCallCount.ShouldBe(2);
		parentSchema.OneOf[0].ShouldBeSameAs(replacementSchema);
		parentSchema.OneOf[1].ShouldBeSameAs(replacementSchema);
	}

	[Fact]
	public void TransformSchemaReferences_TransformsAllOf()
	{
		// Arrange
		OpenApiSchema originalSchema1 = new() { Type = JsonSchemaType.String };
		OpenApiSchema originalSchema2 = new() { Type = JsonSchemaType.Integer };

		OpenApiSchema parentSchema = new()
		{
			AllOf = [originalSchema1, originalSchema2]
		};

		OpenApiSchema replacementSchema = new() { Type = JsonSchemaType.Number };
		int transformCallCount = 0;

		// Act
		OpenApiSchemaHelper.TransformSchemaReferences(parentSchema, schema =>
		{
			transformCallCount++;
			return replacementSchema;
		});

		// Assert
		transformCallCount.ShouldBe(2);
		parentSchema.AllOf[0].ShouldBeSameAs(replacementSchema);
		parentSchema.AllOf[1].ShouldBeSameAs(replacementSchema);
	}

	[Fact]
	public void TransformSchemaReferences_TransformsAnyOf()
	{
		// Arrange
		OpenApiSchema originalSchema1 = new() { Type = JsonSchemaType.String };
		OpenApiSchema originalSchema2 = new() { Type = JsonSchemaType.Integer };
		OpenApiSchema originalSchema3 = new() { Type = JsonSchemaType.Boolean };

		OpenApiSchema parentSchema = new()
		{
			AnyOf = [originalSchema1, originalSchema2, originalSchema3]
		};

		OpenApiSchema replacementSchema = new() { Type = JsonSchemaType.Number };
		int transformCallCount = 0;

		// Act
		OpenApiSchemaHelper.TransformSchemaReferences(parentSchema, schema =>
		{
			transformCallCount++;
			return replacementSchema;
		});

		// Assert
		transformCallCount.ShouldBe(3);
		parentSchema.AnyOf.Count.ShouldBe(3);
		parentSchema.AnyOf[0].ShouldBeSameAs(replacementSchema);
		parentSchema.AnyOf[1].ShouldBeSameAs(replacementSchema);
		parentSchema.AnyOf[2].ShouldBeSameAs(replacementSchema);
	}

	#endregion

	#region Nullable Schema Tests

	[Fact]
	public void CreateNullableMarker_CreatesSchemaWithNullableExtension()
	{
		// Act
		OpenApiSchema marker = OpenApiSchemaHelper.CreateNullableMarker();

		// Assert
		marker.ShouldNotBeNull();
		marker.Extensions.ShouldNotBeNull();
		marker.Extensions.ContainsKey(SchemaConstants.NullableExtension).ShouldBeTrue();

		JsonNodeExtension? extension = marker.Extensions[SchemaConstants.NullableExtension] as JsonNodeExtension;
		extension.ShouldNotBeNull();
		extension!.Node.ShouldNotBeNull();
	}

	[Fact]
	public void WrapAsNullable_CreatesOneOfWithNullableMarker()
	{
		// Arrange
		OpenApiSchema originalSchema = new() { Type = JsonSchemaType.Integer };

		// Act
		OpenApiSchema wrappedSchema = OpenApiSchemaHelper.WrapAsNullable(originalSchema);

		// Assert
		wrappedSchema.OneOf.ShouldNotBeNull();
		wrappedSchema.OneOf.Count.ShouldBe(2);
		wrappedSchema.OneOf[0].ShouldBeSameAs(originalSchema);

		OpenApiSchema? nullMarker = wrappedSchema.OneOf[1] as OpenApiSchema;
		nullMarker.ShouldNotBeNull();
		nullMarker!.Extensions.ShouldNotBeNull();
		nullMarker.Extensions.ContainsKey(SchemaConstants.NullableExtension).ShouldBeTrue();
	}

	#endregion

	#region ResolveReference Tests

	[Fact]
	public void ResolveReference_WithNullComponentSection_ReturnsOriginal()
	{
		// Arrange
		IOpenApiParameter param = new OpenApiParameter
		{
			Name = "testParam",
			In = ParameterLocation.Query
		};

		// Act
		IOpenApiParameter result = OpenApiSchemaHelper.ResolveReference(param, null);

		// Assert
		result.ShouldBeSameAs(param);
	}

	[Fact]
	public void ResolveReference_WithNonReferenceType_ReturnsOriginal()
	{
		// Arrange
		IOpenApiParameter param = new OpenApiParameter
		{
			Name = "testParam",
			In = ParameterLocation.Query
		};

		Dictionary<string, IOpenApiParameter> components = new()
		{
			["otherParam"] = new OpenApiParameter()
		};

		// Act
		IOpenApiParameter result = OpenApiSchemaHelper.ResolveReference(param, components);

		// Assert
		result.ShouldBeSameAs(param);
	}

	[Fact]
	public void ResolveReference_ParameterNotFoundInComponents_ReturnsOriginal()
	{
		// Arrange
		OpenApiDocument doc = new();
		IOpenApiParameter paramRef = new OpenApiParameterReference("missingParam", doc, null);

		Dictionary<string, IOpenApiParameter> components = new()
		{
			["otherParam"] = new OpenApiParameter()
		};

		// Act
		IOpenApiParameter result = OpenApiSchemaHelper.ResolveReference(paramRef, components);

		// Assert
		result.ShouldBeSameAs(paramRef);
	}

	[Fact]
	public void ResolveReference_ParameterFoundInComponents_ReturnsResolved()
	{
		// Arrange
		OpenApiDocument doc = new();
		IOpenApiParameter resolvedParam = new OpenApiParameter
		{
			Name = "resolvedParam",
			In = ParameterLocation.Header,
			Description = "Resolved parameter"
		};

		Dictionary<string, IOpenApiParameter> components = new()
		{
			["testParam"] = resolvedParam
		};

		IOpenApiParameter paramRef = new OpenApiParameterReference("testParam", doc, null);

		// Act
		IOpenApiParameter result = OpenApiSchemaHelper.ResolveReference(paramRef, components);

		// Assert
		result.ShouldBeSameAs(resolvedParam);
	}

	[Fact]
	public void ResolveReference_RequestBodyFoundInComponents_ReturnsResolved()
	{
		// Arrange
		OpenApiDocument doc = new();
		IOpenApiRequestBody resolvedBody = new OpenApiRequestBody
		{
			Description = "Resolved request body",
			Required = true
		};

		Dictionary<string, IOpenApiRequestBody> components = new()
		{
			["testBody"] = resolvedBody
		};

		IOpenApiRequestBody bodyRef = new OpenApiRequestBodyReference("testBody", doc, null);

		// Act
		IOpenApiRequestBody result = OpenApiSchemaHelper.ResolveReference(bodyRef, components);

		// Assert
		result.ShouldBeSameAs(resolvedBody);
	}

	[Fact]
	public void ResolveReference_RequestBodyNotFoundInComponents_ReturnsOriginal()
	{
		// Arrange
		OpenApiDocument doc = new();
		Dictionary<string, IOpenApiRequestBody> components = new()
		{
			["otherBody"] = new OpenApiRequestBody()
		};

		IOpenApiRequestBody bodyRef = new OpenApiRequestBodyReference("missingBody", doc, null);

		// Act
		IOpenApiRequestBody result = OpenApiSchemaHelper.ResolveReference(bodyRef, components);

		// Assert
		result.ShouldBeSameAs(bodyRef);
	}

	[Fact]
	public void ResolveReference_ResponseFoundInComponents_ReturnsResolved()
	{
		// Arrange
		OpenApiDocument doc = new();
		IOpenApiResponse resolvedResponse = new OpenApiResponse
		{
			Description = "Success response"
		};

		Dictionary<string, IOpenApiResponse> components = new()
		{
			["200"] = resolvedResponse
		};

		IOpenApiResponse responseRef = new OpenApiResponseReference("200", doc, null);

		// Act
		IOpenApiResponse result = OpenApiSchemaHelper.ResolveReference(responseRef, components);

		// Assert
		result.ShouldBeSameAs(resolvedResponse);
	}

	[Fact]
	public void ResolveReference_ResponseNotFoundInComponents_ReturnsOriginal()
	{
		// Arrange
		OpenApiDocument doc = new();
		Dictionary<string, IOpenApiResponse> components = new()
		{
			["200"] = new OpenApiResponse()
		};

		IOpenApiResponse responseRef = new OpenApiResponseReference("404", doc, null);

		// Act
		IOpenApiResponse result = OpenApiSchemaHelper.ResolveReference(responseRef, components);

		// Assert
		result.ShouldBeSameAs(responseRef);
	}

	[Fact]
	public void ResolveReference_HeaderFoundInComponents_ReturnsResolved()
	{
		// Arrange
		OpenApiDocument doc = new();
		IOpenApiHeader resolvedHeader = new OpenApiHeader
		{
			Description = "Custom header",
			Required = true
		};

		Dictionary<string, IOpenApiHeader> components = new()
		{
			["X-Custom-Header"] = resolvedHeader
		};

		IOpenApiHeader headerRef = new OpenApiHeaderReference("X-Custom-Header", doc, null);

		// Act
		IOpenApiHeader result = OpenApiSchemaHelper.ResolveReference(headerRef, components);

		// Assert
		result.ShouldBeSameAs(resolvedHeader);
	}

	[Fact]
	public void ResolveReference_HeaderNotFoundInComponents_ReturnsOriginal()
	{
		// Arrange
		OpenApiDocument doc = new();
		Dictionary<string, IOpenApiHeader> components = new()
		{
			["X-Custom-Header"] = new OpenApiHeader()
		};

		IOpenApiHeader headerRef = new OpenApiHeaderReference("X-Missing-Header", doc, null);

		// Act
		IOpenApiHeader result = OpenApiSchemaHelper.ResolveReference(headerRef, components);

		// Assert
		result.ShouldBeSameAs(headerRef);
	}

	#endregion
}

