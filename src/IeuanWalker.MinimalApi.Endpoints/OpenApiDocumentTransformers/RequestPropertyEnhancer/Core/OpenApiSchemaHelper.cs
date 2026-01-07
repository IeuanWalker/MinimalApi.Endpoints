using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi;

namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer.Core;

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
		OpenApiSchema schema = new();

		if (refId.Contains(SchemaConstants.SystemString) && !refId.Contains(SchemaConstants.ArraySuffix))
		{
			schema.Type = JsonSchemaType.String;
		}
		else if (refId.Contains(SchemaConstants.SystemInt32))
		{
			schema.Type = JsonSchemaType.Integer;
			schema.Format = SchemaConstants.FormatInt32;
		}
		else if (refId.Contains(SchemaConstants.SystemInt64))
		{
			schema.Type = JsonSchemaType.Integer;
			schema.Format = SchemaConstants.FormatInt64;
		}
		else if (refId.Contains(SchemaConstants.SystemInt16) || refId.Contains(SchemaConstants.SystemByte))
		{
			schema.Type = JsonSchemaType.Integer;
			schema.Format = SchemaConstants.FormatInt32;
		}
		else if (refId.Contains(SchemaConstants.SystemDecimal))
		{
			schema.Type = JsonSchemaType.Number;
		}
		else if (refId.Contains(SchemaConstants.SystemDouble))
		{
			schema.Type = JsonSchemaType.Number;
			schema.Format = SchemaConstants.FormatDouble;
		}
		else if (refId.Contains(SchemaConstants.SystemSingle))
		{
			schema.Type = JsonSchemaType.Number;
			schema.Format = SchemaConstants.FormatFloat;
		}
		else if (refId.Contains(SchemaConstants.SystemBoolean))
		{
			schema.Type = JsonSchemaType.Boolean;
		}
		else if (refId.Contains(SchemaConstants.SystemDateTime) || refId.Contains(SchemaConstants.SystemDateTimeOffset))
		{
			schema.Type = JsonSchemaType.String;
			schema.Format = SchemaConstants.FormatDateTime;
		}
		else if (refId.Contains(SchemaConstants.SystemDateOnly))
		{
			schema.Type = JsonSchemaType.String;
			schema.Format = SchemaConstants.FormatDate;
		}
		else if (refId.Contains(SchemaConstants.SystemTimeOnly))
		{
			schema.Type = JsonSchemaType.String;
			schema.Format = SchemaConstants.FormatTime;
		}
		else if (refId.Contains(SchemaConstants.SystemGuid))
		{
			schema.Type = JsonSchemaType.String;
			schema.Format = SchemaConstants.FormatUuid;
		}
		else if (refId.Contains(SchemaConstants.IFormFile) || refId.Equals("IFormFile", StringComparison.Ordinal))
		{
			schema.Type = JsonSchemaType.String;
			schema.Format = SchemaConstants.FormatBinary;
		}
		else if (refId.Contains(SchemaConstants.IFormFileCollection) || refId.Equals("IFormFileCollection", StringComparison.Ordinal))
		{
			schema.Type = JsonSchemaType.Array;
			schema.Items = new OpenApiSchema
			{
				Type = JsonSchemaType.String,
				Format = SchemaConstants.FormatBinary
			};
		}
		else if (refId.EndsWith(SchemaConstants.ArraySuffix))
		{
			schema.Type = JsonSchemaType.Array;
			string elementRefId = refId[..^2];
			OpenApiSchemaReference elementRef = new(elementRefId, document, null);
			schema.Items = InlinePrimitiveTypeReference(elementRef, document);
		}
		else if (SchemaConstants.IsCollectionType(refId))
		{
			schema.Type = JsonSchemaType.Array;
			int startIdx = refId.IndexOf("[[");
			int endIdx = refId.IndexOf(',', startIdx);
			if (startIdx >= 0 && endIdx > startIdx)
			{
				string elementType = refId.Substring(startIdx + 2, endIdx - startIdx - 2);
				OpenApiSchemaReference elementRef = new(elementType, document, null);
				schema.Items = InlinePrimitiveTypeReference(elementRef, document);
			}
		}
		else
		{
			return null;
		}

		return schema;
	}
}
