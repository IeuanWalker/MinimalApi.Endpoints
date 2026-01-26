using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi;

namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.PropertyEnhancer.Core;

/// <summary>
/// Helper methods for working with OpenAPI schemas, including type inlining and schema manipulation.
/// </summary>
static class OpenApiSchemaHelper
{
	/// <summary>
	/// Attempts to cast an IOpenApiSchema to OpenApiSchema.
	/// </summary>
	/// <param name="schema">The schema interface to cast.</param>
	/// <param name="openApiSchema">The resulting OpenApiSchema if successful.</param>
	/// <returns>True if the cast was successful, false otherwise.</returns>
	public static bool TryAsOpenApiSchema(IOpenApiSchema? schema, out OpenApiSchema? openApiSchema)
	{
		openApiSchema = schema as OpenApiSchema;
		return openApiSchema is not null;
	}

	/// <summary>
	/// Recursively transforms all schema references using the provided transformer function.
	/// Handles properties, items, additionalProperties, oneOf, allOf, and anyOf.
	/// </summary>
	/// <param name="schema">The schema to transform.</param>
	/// <param name="transformer">Function that transforms each schema reference.</param>
	public static void TransformSchemaReferences(OpenApiSchema schema, Func<IOpenApiSchema, IOpenApiSchema> transformer)
	{
		if (schema.Properties is { Count: > 0 })
		{
			foreach ((string propertyName, IOpenApiSchema propertySchema) in schema.Properties)
			{
				schema.Properties[propertyName] = transformer(propertySchema);
			}
		}

		if (schema.AdditionalProperties is not null)
		{
			schema.AdditionalProperties = transformer(schema.AdditionalProperties);
		}

		if (schema.Items is not null)
		{
			schema.Items = transformer(schema.Items);
		}

		if (schema.OneOf is { Count: > 0 })
		{
			for (int i = 0; i < schema.OneOf.Count; i++)
			{
				schema.OneOf[i] = transformer(schema.OneOf[i]);
			}
		}

		if (schema.AllOf is { Count: > 0 })
		{
			for (int i = 0; i < schema.AllOf.Count; i++)
			{
				schema.AllOf[i] = transformer(schema.AllOf[i]);
			}
		}

		if (schema.AnyOf is { Count: > 0 })
		{
			for (int i = 0; i < schema.AnyOf.Count; i++)
			{
				schema.AnyOf[i] = transformer(schema.AnyOf[i]);
			}
		}
	}

	/// <summary>
	/// Converts primitive type references to inline schemas, with document-level resolution.
	/// </summary>
	/// <param name="schemaRef">The schema reference to inline.</param>
	/// <param name="document">The OpenAPI document for resolving nested references.</param>
	/// <returns>An inlined schema for primitives, or the original reference for non-primitives.</returns>
	public static IOpenApiSchema InlinePrimitiveTypeReference(OpenApiSchemaReference schemaRef, OpenApiDocument document)
	{
		string? refId = schemaRef.Reference?.Id;
		if (string.IsNullOrEmpty(refId))
		{
			return schemaRef;
		}

		if (refId.StartsWith(SchemaConstants.NullableTypePrefix + "[["))
		{
			int startIdx = refId.IndexOf("[[");
			int endIdx = refId.IndexOf(',', startIdx);
			if (startIdx >= 0 && endIdx > startIdx)
			{
				string underlyingType = refId.Substring(startIdx + 2, endIdx - startIdx - 2);
				return new OpenApiSchemaReference(underlyingType, document, null);
			}
		}

		if (!SchemaConstants.IsSystemType(refId) &&
			!refId.StartsWith(SchemaConstants.AspNetCoreHttpPrefix) &&
			!refId.Equals("IFormFile", StringComparison.Ordinal) &&
			!refId.Equals("IFormFileCollection", StringComparison.Ordinal))
		{
			return schemaRef;
		}

		OpenApiSchema? inlineSchema = CreateInlineSchemaFromRefIdWithCollections(refId, document);
		return inlineSchema is not null ? inlineSchema : (IOpenApiSchema)schemaRef;
	}

	/// <summary>
	/// Creates an OpenAPI schema from a .NET type.
	/// </summary>
	/// <param name="type">The .NET type to convert.</param>
	/// <returns>An OpenAPI schema representing the type.</returns>
	public static OpenApiSchema CreateSchemaFromType(Type type)
	{
		Type actualType = Nullable.GetUnderlyingType(type) ?? type;
		bool isNullable = Nullable.GetUnderlyingType(type) is not null;

		OpenApiSchema schema = new();

		if (actualType.IsArray)
		{
			schema.Type = JsonSchemaType.Array;
			Type elementType = actualType.GetElementType()!;
			schema.Items = CreateSchemaFromType(elementType);
		}
		else if (IsGenericCollection(actualType))
		{
			schema.Type = JsonSchemaType.Array;
			Type elementType = actualType.GetGenericArguments()[0];
			schema.Items = CreateSchemaFromType(elementType);
		}
		else
		{
			SetPrimitiveTypeInfo(schema, actualType);
		}

		if (isNullable && actualType != typeof(string) && schema.Type.HasValue)
		{
			return WrapAsNullable(schema);
		}

		return schema;
	}

	/// <summary>
	/// Unwraps a nullable enum reference.
	/// </summary>
	public static OpenApiSchemaReference UnwrapNullableEnumReference(OpenApiSchemaReference schemaRef)
	{
		string? refId = schemaRef.Reference?.Id;
		if (string.IsNullOrEmpty(refId))
		{
			return schemaRef;
		}

		if (refId.StartsWith(SchemaConstants.NullableTypePrefix + "[["))
		{
			int startIndex = (SchemaConstants.NullableTypePrefix + "[[").Length;
			int endIndex = refId.IndexOf(',', startIndex);
			if (endIndex > startIndex)
			{
				string underlyingTypeFullName = refId[startIndex..endIndex];
				return new OpenApiSchemaReference(underlyingTypeFullName, null, null);
			}
		}

		return schemaRef;
	}

	/// <summary>
	/// Creates a nullable wrapper around a schema using oneOf pattern.
	/// </summary>
	public static OpenApiSchema WrapAsNullable(OpenApiSchema schema)
	{
		return new OpenApiSchema
		{
			OneOf =
			[
				schema,
				new OpenApiSchema
				{
					Extensions = new Dictionary<string, IOpenApiExtension>
					{
						[SchemaConstants.NullableExtension] = new JsonNodeExtension(JsonValue.Create(true)!)
					}
				}
			]
		};
	}

	/// <summary>
	/// Creates a nullable marker schema for use in oneOf patterns.
	/// </summary>
	public static OpenApiSchema CreateNullableMarker()
	{
		return new OpenApiSchema
		{
			Extensions = new Dictionary<string, IOpenApiExtension>
			{
				[SchemaConstants.NullableExtension] = new JsonNodeExtension(JsonValue.Create(true)!)
			}
		};
	}

	/// <summary>
	/// Checks if a schema is a reference to an enum type.
	/// </summary>
	public static bool IsEnumSchemaReference(IOpenApiSchema propertySchema, OpenApiDocument document)
	{
		if (TryAsOpenApiSchema(propertySchema, out OpenApiSchema? schema) && schema!.OneOf is { Count: > 0 })
		{
			return schema.OneOf.Any(oneOfSchema => IsEnumSchemaReference(oneOfSchema, document));
		}

		if (propertySchema is not OpenApiSchemaReference schemaRef)
		{
			return false;
		}

		string? refId = schemaRef.Reference?.Id;
		if (string.IsNullOrEmpty(refId))
		{
			return false;
		}

		if (SchemaConstants.IsSystemType(refId) && !SchemaConstants.IsNullableType(refId))
		{
			return false;
		}

		if (document.Components?.Schemas?.TryGetValue(refId, out IOpenApiSchema? referencedSchema) != true)
		{
			return false;
		}

		if (TryAsOpenApiSchema(referencedSchema, out OpenApiSchema? enumSchema))
		{
			if (enumSchema!.Extensions?.ContainsKey(SchemaConstants.EnumExtension) == true)
			{
				return true;
			}

			if (enumSchema.Enum is { Count: > 0 })
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Checks if a schema is an inline enum.
	/// </summary>
	public static bool IsInlineEnumSchema(IOpenApiSchema schema)
	{
		if (!TryAsOpenApiSchema(schema, out OpenApiSchema? openApiSchema))
		{
			return false;
		}

		if (openApiSchema!.Enum is { Count: > 0 })
		{
			return true;
		}

		if (openApiSchema.OneOf is { Count: > 0 })
		{
			return openApiSchema.OneOf.Any(IsInlineEnumSchema);
		}

		return false;
	}

	/// <summary>
	/// Ensures a schema has a type set.
	/// </summary>
	public static void EnsureSchemaHasType(OpenApiSchema schema)
	{
		if (schema.Type.HasValue && schema.Type != JsonSchemaType.Null)
		{
			return;
		}

		if (schema.Items is not null)
		{
			schema.Type = JsonSchemaType.Array;
		}
		else if (schema.Properties is { Count: > 0 })
		{
			schema.Type = JsonSchemaType.Object;
		}
	}

	/// <summary>
	/// Creates an inline OpenAPI schema for primitive types based on a reference ID string.
	/// This is the canonical method for primitive type schema creation.
	/// </summary>
	/// <param name="refId">The reference ID (e.g., "System.String", "System.Int32").</param>
	/// <returns>An OpenAPI schema for the primitive type, or null if not a recognized primitive.</returns>
	public static OpenApiSchema? CreatePrimitiveSchemaFromRefId(string refId) => refId switch
	{
		_ when refId.Contains(SchemaConstants.SystemString) && !refId.Contains(SchemaConstants.ArraySuffix) => new OpenApiSchema { Type = JsonSchemaType.String },
		_ when refId.Contains(SchemaConstants.SystemInt32) => new OpenApiSchema { Type = JsonSchemaType.Integer, Format = SchemaConstants.FormatInt32 },
		_ when refId.Contains(SchemaConstants.SystemInt64) => new OpenApiSchema { Type = JsonSchemaType.Integer, Format = SchemaConstants.FormatInt64 },
		_ when refId.Contains(SchemaConstants.SystemInt16) || refId.Contains(SchemaConstants.SystemByte) => new OpenApiSchema { Type = JsonSchemaType.Integer, Format = SchemaConstants.FormatInt32 },
		_ when refId.Contains(SchemaConstants.SystemDecimal) => new OpenApiSchema { Type = JsonSchemaType.Number },
		_ when refId.Contains(SchemaConstants.SystemDouble) => new OpenApiSchema { Type = JsonSchemaType.Number, Format = SchemaConstants.FormatDouble },
		_ when refId.Contains(SchemaConstants.SystemSingle) => new OpenApiSchema { Type = JsonSchemaType.Number, Format = SchemaConstants.FormatFloat },
		_ when refId.Contains(SchemaConstants.SystemBoolean) => new OpenApiSchema { Type = JsonSchemaType.Boolean },
		_ when refId.Contains(SchemaConstants.SystemDateTime) || refId.Contains(SchemaConstants.SystemDateTimeOffset) => new OpenApiSchema { Type = JsonSchemaType.String, Format = SchemaConstants.FormatDateTime },
		_ when refId.Contains(SchemaConstants.SystemDateOnly) => new OpenApiSchema { Type = JsonSchemaType.String, Format = SchemaConstants.FormatDate },
		_ when refId.Contains(SchemaConstants.SystemTimeOnly) => new OpenApiSchema { Type = JsonSchemaType.String, Format = SchemaConstants.FormatTime },
		_ when refId.Contains(SchemaConstants.SystemGuid) => new OpenApiSchema { Type = JsonSchemaType.String, Format = SchemaConstants.FormatUuid },
		_ when refId.Contains(SchemaConstants.SystemUri) => new OpenApiSchema { Type = JsonSchemaType.String, Format = SchemaConstants.FormatUri },
		_ when refId.Contains(SchemaConstants.IFormFileCollection) || refId.Equals("IFormFileCollection", StringComparison.Ordinal) => new OpenApiSchema
		{
			Type = JsonSchemaType.Array,
			Items = new OpenApiSchema { Type = JsonSchemaType.String, Format = SchemaConstants.FormatBinary }
		},
		_ when refId.Contains(SchemaConstants.IFormFile) || refId.Equals("IFormFile", StringComparison.Ordinal) => new OpenApiSchema { Type = JsonSchemaType.String, Format = SchemaConstants.FormatBinary },
		_ => null
	};

	/// <summary>
	/// Determines the JSON schema type and format from a reference ID.
	/// This is useful when you need just the type information without creating a full schema.
	/// </summary>
	/// <param name="refId">The reference ID (e.g., "System.String", "System.Int32").</param>
	/// <returns>A tuple containing the JSON schema type and format, or (null, null) if not recognized.</returns>
	public static (JsonSchemaType? type, string? format) GetTypeAndFormatFromRefId(string refId)
	{
		if (refId.EndsWith(SchemaConstants.ArraySuffix) || SchemaConstants.IsCollectionType(refId))
		{
			return (JsonSchemaType.Array, null);
		}

		OpenApiSchema? schema = CreatePrimitiveSchemaFromRefId(refId);
		return schema is not null ? (schema.Type, schema.Format) : (null, null);
	}

	/// <summary>
	/// Enriches an OpenAPI schema with enum values, names, and descriptions.
	/// This is the canonical method for enum enrichment used by both EnumSchemaTransformer and ValidationDocumentTransformer.
	/// </summary>
	/// <param name="schema">The schema to enrich.</param>
	/// <param name="enumType">The .NET enum type.</param>
	/// <param name="forStringSchema">If true, enum values are serialized as strings; otherwise as integers.</param>
	public static void EnrichSchemaWithEnumValues(OpenApiSchema schema, Type enumType, bool forStringSchema = false)
	{
		Array enumValues = Enum.GetValues(enumType);
		string[] enumNames = Enum.GetNames(enumType);

		JsonArray valueArray = [];
		JsonArray varNamesArray = [];
		JsonObject? descObj = null;

		for (int i = 0; i < enumValues.Length; i++)
		{
			object enumValue = enumValues.GetValue(i)!;
			string enumName = enumNames[i];

			if (forStringSchema)
			{
				valueArray.Add(JsonValue.Create(enumName)!);
			}
			else
			{
				long numericValue = Convert.ToInt64(enumValue);
				valueArray.Add(JsonValue.Create(numericValue)!);
			}
			varNamesArray.Add(JsonValue.Create(enumName)!);

			FieldInfo? field = enumType.GetField(enumName);
			DescriptionAttribute? descriptionAttr = field?.GetCustomAttributes(typeof(DescriptionAttribute), false)
				.OfType<DescriptionAttribute>()
				.FirstOrDefault();

			if (descriptionAttr is not null && !string.IsNullOrWhiteSpace(descriptionAttr.Description))
			{
				descObj ??= [];
				descObj[enumName] = descriptionAttr.Description;
			}
		}

		schema.Extensions ??= new Dictionary<string, IOpenApiExtension>();
		schema.Extensions[SchemaConstants.EnumExtension] = new JsonNodeExtension(valueArray);

		// Only add varnames for non-string schemas (where values are integers)
		if (!forStringSchema)
		{
			schema.Extensions[SchemaConstants.EnumVarNamesExtension] = new JsonNodeExtension(varNamesArray);
		}

		if (descObj is not null)
		{
			schema.Extensions[SchemaConstants.EnumDescriptionsExtension] = new JsonNodeExtension(descObj);
		}

		// Set description if not already set or if it contains validation error patterns
		bool shouldSetDescription = string.IsNullOrWhiteSpace(schema.Description) ||
			schema.Description.Contains("has a range of values") ||
			schema.Description.StartsWith("Validation rules:");

		if (shouldSetDescription)
		{
			schema.Description = $"Enum: {string.Join(", ", enumNames)}";
		}
		else if (!schema.Description?.Contains("Enum:") ?? false)
		{
			schema.Description = $"Enum: {string.Join(", ", enumNames)}\n\n{schema.Description}";
		}
	}

	/// <summary>
	/// Resolves a reference to a component if it exists, otherwise returns the original object.
	/// </summary>
	/// <typeparam name="T">The type of the referenceable object.</typeparam>
	/// <param name="referenceable">The object that may be a reference.</param>
	/// <param name="componentSection">The component section to resolve references from.</param>
	/// <returns>The resolved object if it's a reference and exists in the component section, otherwise the original.</returns>
	internal static T ResolveReference<T>(T referenceable, IDictionary<string, T>? componentSection) where T : class
	{
		if (componentSection is null)
		{
			return referenceable;
		}



		string? referenceId = referenceable switch
		{
			OpenApiParameterReference { Reference.Id: { Length: > 0 } id } => id,
			OpenApiRequestBodyReference { Reference.Id: { Length: > 0 } id } => id,
			OpenApiResponseReference { Reference.Id: { Length: > 0 } id } => id,
			OpenApiHeaderReference { Reference.Id: { Length: > 0 } id } => id,
			_ => null
		};

		if (referenceId is not null && componentSection.TryGetValue(referenceId, out T? referenced))
		{
			return referenced;
		}

		return referenceable;
	}

	static bool IsGenericCollection(Type type)
	{
		if (!type.IsGenericType)
		{
			return false;
		}

		Type genericDef = type.GetGenericTypeDefinition();
		return genericDef == typeof(List<>) ||
			   genericDef == typeof(IEnumerable<>) ||
			   genericDef == typeof(ICollection<>) ||
			   genericDef == typeof(IReadOnlyList<>) ||
			   genericDef == typeof(IReadOnlyCollection<>);
	}

	static void SetPrimitiveTypeInfo(OpenApiSchema schema, Type actualType)
	{
		if (actualType == typeof(string))
		{
			schema.Type = JsonSchemaType.String;
		}
		else if (actualType == typeof(int))
		{
			schema.Type = JsonSchemaType.Integer;
			schema.Format = SchemaConstants.FormatInt32;
		}
		else if (actualType == typeof(long))
		{
			schema.Type = JsonSchemaType.Integer;
			schema.Format = SchemaConstants.FormatInt64;
		}
		else if (actualType == typeof(short) || actualType == typeof(byte))
		{
			schema.Type = JsonSchemaType.Integer;
			schema.Format = SchemaConstants.FormatInt32;
		}
		else if (actualType == typeof(decimal))
		{
			schema.Type = JsonSchemaType.Number;
		}
		else if (actualType == typeof(double))
		{
			schema.Type = JsonSchemaType.Number;
			schema.Format = SchemaConstants.FormatDouble;
		}
		else if (actualType == typeof(float))
		{
			schema.Type = JsonSchemaType.Number;
			schema.Format = SchemaConstants.FormatFloat;
		}
		else if (actualType == typeof(bool))
		{
			schema.Type = JsonSchemaType.Boolean;
		}
		else if (actualType == typeof(DateTime) || actualType == typeof(DateTimeOffset))
		{
			schema.Type = JsonSchemaType.String;
			schema.Format = SchemaConstants.FormatDateTime;
		}
		else if (actualType == typeof(DateOnly))
		{
			schema.Type = JsonSchemaType.String;
			schema.Format = SchemaConstants.FormatDate;
		}
		else if (actualType == typeof(TimeOnly))
		{
			schema.Type = JsonSchemaType.String;
			schema.Format = SchemaConstants.FormatTime;
		}
		else if (actualType == typeof(Guid))
		{
			schema.Type = JsonSchemaType.String;
			schema.Format = SchemaConstants.FormatUuid;
		}
		else if (actualType == typeof(Uri))
		{
			schema.Type = JsonSchemaType.String;
			schema.Format = SchemaConstants.FormatUri;
		}
		else if (actualType == typeof(IFormFile))
		{
			schema.Type = JsonSchemaType.String;
			schema.Format = SchemaConstants.FormatBinary;
		}
		else if (actualType == typeof(IFormFileCollection))
		{
			schema.Type = JsonSchemaType.Array;
			schema.Items = new OpenApiSchema
			{
				Type = JsonSchemaType.String,
				Format = SchemaConstants.FormatBinary
			};
		}
	}

	static OpenApiSchema? CreateInlineSchemaFromRefIdWithCollections(string refId, OpenApiDocument document)
	{
		// First try the primitive schema helper
		OpenApiSchema? primitiveSchema = CreatePrimitiveSchemaFromRefId(refId);
		if (primitiveSchema is not null)
		{
			return primitiveSchema;
		}

		// Handle array types
		if (refId.EndsWith(SchemaConstants.ArraySuffix))
		{
			string elementRefId = refId[..^2];
			OpenApiSchemaReference elementRef = new(elementRefId, document, null);
			return new OpenApiSchema
			{
				Type = JsonSchemaType.Array,
				Items = InlinePrimitiveTypeReference(elementRef, document)
			};
		}

		// Handle collection types (List<T>, IEnumerable<T>, etc.)
		if (SchemaConstants.IsCollectionType(refId))
		{
			int startIdx = refId.IndexOf("[[");
			int endIdx = refId.IndexOf(',', startIdx);
			if (startIdx >= 0 && endIdx > startIdx)
			{
				string elementType = refId.Substring(startIdx + 2, endIdx - startIdx - 2);
				OpenApiSchemaReference elementRef = new(elementType, document, null);
				return new OpenApiSchema
				{
					Type = JsonSchemaType.Array,
					Items = InlinePrimitiveTypeReference(elementRef, document)
				};
			}
		}

		return null;
	}
}
