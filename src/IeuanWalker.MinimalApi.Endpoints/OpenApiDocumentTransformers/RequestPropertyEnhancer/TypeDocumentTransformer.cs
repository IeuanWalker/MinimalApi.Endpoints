using System.Reflection;
using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer;

/// <summary>
/// OpenAPI document transformer that ensures all property types and formats are correctly documented.
/// This transformer runs before other transformers to establish a baseline of correct type information.
/// It handles:
/// - Schema properties (primitives, arrays, nested objects)
/// - Query and path parameters
/// - IFormFile and IFormFileCollection types
/// - Nullable types
/// </summary>
sealed class TypeDocumentTransformer : IOpenApiDocumentTransformer
{
	public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
	{
		// Step 1: Fix all schema property types, nullable arrays, and double-wrapped arrays in one pass
		FixComponentSchemas(document);

		// Step 2: Build endpoint-to-request-type mapping using shared helper
		Dictionary<string, Type> endpointToRequestType = EndpointRequestTypeMapper.BuildEndpointToRequestTypeMapping(context);

		// Step 3: Fix parameter types (query/path parameters)
		FixParameterTypes(document, endpointToRequestType);

		// Step 4: Inline System.String[] references within component schemas
		InlineSystemStringArrayInComponents(document);

		// Step 5: Inline primitive type references (System.String, System.String[], etc.) and IFormFile types
		InlinePrimitiveAndFileTypeReferences(document);

		// Step 6: Inline collection and dictionary references, then remove unused component schemas
		InlineCollectionAndDictionaryReferences(document);
		RemoveInlinedComponentSchemas(document);

		// Step 7: Ensure unwrapped enum schemas exist (before reordering)
		EnsureUnwrappedEnumSchemasExist(document);

		return Task.CompletedTask;
	}

	/// <summary>
	/// Fixes component schema structures in a single pass: property types, nullable arrays, and double-wrapped arrays.
	/// </summary>
	static void FixComponentSchemas(OpenApiDocument document)
	{
		if (document.Components?.Schemas is null)
		{
			return;
		}

		foreach (KeyValuePair<string, IOpenApiSchema> schemaEntry in document.Components.Schemas)
		{
			if (!OpenApiSchemaHelper.TryAsOpenApiSchema(schemaEntry.Value, out OpenApiSchema? schema) || schema is null)
			{
				continue;
			}

			Type? schemaType = SchemaTypeResolver.GetSchemaType(schemaEntry.Key);

			// Fix property types, nullable arrays, and double-wrapped arrays
			if (schema.Properties?.Count > 0)
			{
				foreach ((string propertyName, IOpenApiSchema propertySchema) in schema.Properties)
				{
					PropertyInfo? propertyInfo = schemaType?.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
					IOpenApiSchema fixedSchema = FixSchemaType(propertySchema, document, propertyInfo?.PropertyType);
					fixedSchema = FixNullableArraySchema(fixedSchema);
					fixedSchema = UnwrapDoubleArrays(fixedSchema);
					schema.Properties[propertyName] = fixedSchema;
				}
			}

			// Fix array items
			if (schema.Items is not null)
			{
				schema.Items = FixSchemaType(schema.Items, document, null);
			}

			// Fix inline schema type (IFormFile etc.)
			FixInlineSchemaType(schema, schemaEntry.Key);
		}
	}

	static IOpenApiSchema UnwrapDoubleArrays(IOpenApiSchema schema)
	{
		if (!OpenApiSchemaHelper.TryAsOpenApiSchema(schema, out OpenApiSchema? openApiSchema) || openApiSchema is null)
		{
			return schema;
		}

		if (openApiSchema.Type == JsonSchemaType.Array &&
			openApiSchema.Items is OpenApiSchema itemsSchema &&
			itemsSchema.Type == JsonSchemaType.Array &&
			itemsSchema.Items is not null &&
			(itemsSchema.Items is OpenApiSchemaReference ||
			(itemsSchema.Items is OpenApiSchema innerSchema && innerSchema.Type == JsonSchemaType.Object)))
		{
			return new OpenApiSchema
			{
				Type = JsonSchemaType.Array,
				Items = itemsSchema.Items
			};
		}

		if (openApiSchema.OneOf is not null && openApiSchema.OneOf.Count > 0)
		{
			for (int i = 0; i < openApiSchema.OneOf.Count; i++)
			{
				if (openApiSchema.OneOf[i] is OpenApiSchema oneOfSchema &&
					oneOfSchema.Type == JsonSchemaType.Array &&
					oneOfSchema.Items is OpenApiSchema itemsSchema2 &&
					itemsSchema2.Type == JsonSchemaType.Array &&
					itemsSchema2.Items is not null &&
					(itemsSchema2.Items is OpenApiSchemaReference ||
					(itemsSchema2.Items is OpenApiSchema innerSchema2 && innerSchema2.Type == JsonSchemaType.Object)))
				{
					openApiSchema.OneOf[i] = new OpenApiSchema
					{
						Type = JsonSchemaType.Array,
						Items = itemsSchema2.Items
					};
				}
			}
		}

		return schema;
	}

	static IOpenApiSchema FixNullableArraySchema(IOpenApiSchema schema)
	{
		if (!OpenApiSchemaHelper.TryAsOpenApiSchema(schema, out OpenApiSchema? openApiSchema) || openApiSchema is null)
		{
			return schema;
		}

		if (openApiSchema.Type == JsonSchemaType.Array &&
			openApiSchema.Items is OpenApiSchema itemsSchema &&
			itemsSchema.OneOf is not null &&
			itemsSchema.OneOf.Count == 2)
		{
			IOpenApiSchema? typeSchema = itemsSchema.OneOf
				.OfType<OpenApiSchema>()
				.FirstOrDefault(os => os.Type.HasValue && os.Type != JsonSchemaType.Null);

			if (typeSchema is not null)
			{
				OpenApiSchema correctedArraySchema = new()
				{
					Type = JsonSchemaType.Array,
					Items = typeSchema
				};

				return new OpenApiSchema
				{
					OneOf =
					[
						correctedArraySchema,
						OpenApiSchemaHelper.CreateNullableMarker()
					]
				};
			}
		}

		return schema;
	}

	static IOpenApiSchema FixSchemaType(IOpenApiSchema schema, OpenApiDocument document, Type? actualPropertyType = null)
	{
		if (actualPropertyType is not null && OpenApiSchemaHelper.TryAsOpenApiSchema(schema, out OpenApiSchema? openApiSchema) && openApiSchema is not null)
		{
			Type actualType = Nullable.GetUnderlyingType(actualPropertyType) ?? actualPropertyType;
			bool isNullable = Nullable.GetUnderlyingType(actualPropertyType) is not null;

			bool isArrayOrCollection = actualType.IsArray ||
				actualType == typeof(IFormFileCollection) ||
				(actualType.IsGenericType && (
					actualType.GetGenericTypeDefinition() == typeof(List<>) ||
					actualType.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
					actualType.GetGenericTypeDefinition() == typeof(ICollection<>) ||
					actualType.GetGenericTypeDefinition() == typeof(IReadOnlyList<>) ||
					actualType.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>)));

			if (isArrayOrCollection)
			{
				if (openApiSchema.OneOf is not null && openApiSchema.OneOf.Count == 2)
				{
					bool hasNullableMarker = openApiSchema.OneOf.Any(s =>
						s is OpenApiSchema os && !os.Type.HasValue);
					bool hasArray = openApiSchema.OneOf.Any(s =>
						s is OpenApiSchema os && os.Type == JsonSchemaType.Array);
					bool hasCollectionRef = openApiSchema.OneOf.Any(s =>
						s is OpenApiSchemaReference sr && sr.Reference?.Id is not null &&
						SchemaConstants.IsCollectionType(sr.Reference.Id));

					if (hasNullableMarker && (hasArray || hasCollectionRef))
					{
						return schema;
					}
				}

				if (openApiSchema.Type != JsonSchemaType.Array && openApiSchema.Items is null)
				{
					if (openApiSchema.Properties is not null && openApiSchema.Properties.Count > 0)
					{
						return openApiSchema;
					}

					IOpenApiSchema itemsSchema = openApiSchema;

					if (isNullable && openApiSchema.OneOf is not null && openApiSchema.OneOf.Count > 0)
					{
						IOpenApiSchema? nonNullableSchema = openApiSchema.OneOf
							.FirstOrDefault(s => s is OpenApiSchema os && os.Extensions is not null && !os.Extensions.ContainsKey(SchemaConstants.NullableExtension));
						if (nonNullableSchema is not null)
						{
							itemsSchema = nonNullableSchema;
						}
					}

					OpenApiSchema arraySchema = new()
					{
						Type = JsonSchemaType.Array,
						Items = itemsSchema
					};

					if (isNullable)
					{
						return OpenApiSchemaHelper.WrapAsNullable(arraySchema);
					}

					return arraySchema;
				}
				else if (openApiSchema.Type == JsonSchemaType.Array)
				{
					IOpenApiSchema? items = openApiSchema.Items;

					if (items is OpenApiSchema itemsSchema && itemsSchema.OneOf is not null && itemsSchema.OneOf.Count == 2)
					{
						IOpenApiSchema? nullableMarker = itemsSchema.OneOf
							.OfType<OpenApiSchema>()
							.FirstOrDefault(os => !os.Type.HasValue || os.Type == JsonSchemaType.Null);

						IOpenApiSchema? typeSchema = itemsSchema.OneOf
							.OfType<OpenApiSchema>()
							.FirstOrDefault(os => os.Type.HasValue && os.Type != JsonSchemaType.Null);

						if (nullableMarker is not null && typeSchema is not null)
						{
							OpenApiSchema correctedArraySchema = new()
							{
								Type = JsonSchemaType.Array,
								Items = typeSchema
							};

							return new OpenApiSchema
							{
								OneOf =
								[
									OpenApiSchemaHelper.CreateNullableMarker(),
									correctedArraySchema
								]
							};
						}
					}
				}
			}
		}

		if (schema is OpenApiSchemaReference schemaRef)
		{
			return OpenApiSchemaHelper.InlinePrimitiveTypeReference(schemaRef, document);
		}

		if (OpenApiSchemaHelper.TryAsOpenApiSchema(schema, out OpenApiSchema? openApiSchemaToFix) && openApiSchemaToFix is not null)
		{
			if (openApiSchemaToFix.OneOf is not null && openApiSchemaToFix.OneOf.Count > 0)
			{
				for (int i = 0; i < openApiSchemaToFix.OneOf.Count; i++)
				{
					openApiSchemaToFix.OneOf[i] = FixSchemaType(openApiSchemaToFix.OneOf[i], document, null);
				}
			}

			if (openApiSchemaToFix.Items is not null)
			{
				openApiSchemaToFix.Items = FixSchemaType(openApiSchemaToFix.Items, document, null);
			}

			if (openApiSchemaToFix.Properties is not null && openApiSchemaToFix.Properties.Count > 0)
			{
				foreach ((string propertyName, IOpenApiSchema propertySchema) in openApiSchemaToFix.Properties)
				{
					openApiSchemaToFix.Properties[propertyName] = FixSchemaType(propertySchema, document, null);
				}
			}

			return openApiSchemaToFix;
		}

		return schema;
	}

	static void FixInlineSchemaType(OpenApiSchema schema, string schemaName)
	{
		if (!schema.Type.HasValue || schema.Type == JsonSchemaType.Null)
		{
			// Check IFormFileCollection FIRST since it contains "IFormFile" in its name
			if (schemaName.Contains(SchemaConstants.IFormFileCollection))
			{
				schema.Type = JsonSchemaType.Array;
				schema.Items = new OpenApiSchema
				{
					Type = JsonSchemaType.String,
					Format = SchemaConstants.FormatBinary
				};
			}
			else if (schemaName.Contains(SchemaConstants.IFormFile))
			{
				schema.Type = JsonSchemaType.String;
				schema.Format = SchemaConstants.FormatBinary;
			}
		}
	}

	static void FixParameterTypes(OpenApiDocument document, Dictionary<string, Type> endpointToRequestType)
	{
		if (document.Paths is null)
		{
			return;
		}

		Dictionary<string, Type?> pathMatchCache = new(StringComparer.OrdinalIgnoreCase);

		foreach (KeyValuePair<string, IOpenApiPathItem> pathItem in document.Paths)
		{
			if (pathItem.Value is not OpenApiPathItem pathItemValue)
			{
				continue;
			}

			string pathPattern = pathItem.Key;

			if (!pathMatchCache.TryGetValue(pathPattern, out Type? pathRequestType))
			{
				pathRequestType = endpointToRequestType
					.Where(mapping => OpenApiPathMatcher.PathsMatch(pathPattern, mapping.Key))
					.Select(mapping => mapping.Value)
					.FirstOrDefault();

				pathMatchCache[pathPattern] = pathRequestType;
			}

			foreach (KeyValuePair<HttpMethod, OpenApiOperation> operation in pathItemValue.Operations ?? [])
			{
				if (operation.Value.Parameters is null || operation.Value.Parameters.Count == 0)
				{
					continue;
				}

				foreach (OpenApiParameter parameter in operation.Value.Parameters.Cast<OpenApiParameter>())
				{
					if (parameter.Schema is null || string.IsNullOrEmpty(parameter.Name))
					{
						continue;
					}

					parameter.Schema = FixParameterSchemaType(parameter.Schema, parameter.Name, pathRequestType);
				}
			}
		}
	}

	static IOpenApiSchema FixParameterSchemaType(IOpenApiSchema schema, string parameterName, Type? requestType)
	{
		if (requestType is not null)
		{
			PropertyInfo? property = requestType.GetProperty(parameterName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
			if (property is not null)
			{
				Type actualType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
				if (!actualType.IsEnum)
				{
					return OpenApiSchemaHelper.CreateSchemaFromType(property.PropertyType);
				}
				else if (schema is OpenApiSchemaReference enumSchemaRef)
				{
					return OpenApiSchemaHelper.UnwrapNullableEnumReference(enumSchemaRef);
				}
			}
		}

		if (schema is OpenApiSchemaReference schemaRef)
		{
			return InlinePrimitiveTypeReferenceForParameter(schemaRef);
		}

		if (OpenApiSchemaHelper.TryAsOpenApiSchema(schema, out OpenApiSchema? openApiSchema) && openApiSchema is not null)
		{
			OpenApiSchemaHelper.EnsureSchemaHasType(openApiSchema);
		}

		return schema;
	}

	static IOpenApiSchema InlinePrimitiveTypeReferenceForParameter(OpenApiSchemaReference schemaRef)
	{
		string? refId = schemaRef.Reference?.Id;
		if (string.IsNullOrEmpty(refId))
		{
			return schemaRef;
		}

		// Unwrap nullable type references to underlying type
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

		if (!SchemaConstants.IsSystemType(refId))
		{
			return schemaRef;
		}

		// Handle array types
		if (refId.EndsWith(SchemaConstants.ArraySuffix) && refId.Length > SchemaConstants.ArraySuffix.Length)
		{
			string elementRefId = refId[..^SchemaConstants.ArraySuffix.Length];
			OpenApiSchemaReference elementRef = new(elementRefId, null, null);
			return new OpenApiSchema
			{
				Type = JsonSchemaType.Array,
				Items = InlinePrimitiveTypeReferenceForParameter(elementRef)
			};
		}

		// Create inline schema for primitive types using the shared helper
		OpenApiSchema? primitiveSchema = OpenApiSchemaHelper.CreatePrimitiveSchemaFromRefId(refId);
		return primitiveSchema is not null ? primitiveSchema : schemaRef;
	}

	static void EnsureUnwrappedEnumSchemasExist(OpenApiDocument document)
	{
		if (document.Components?.Schemas is null)
		{
			return;
		}

		List<(string nullableSchemaName, string unwrappedSchemaName, OpenApiSchema nullableSchema)> schemasToCreate = [];

		foreach (KeyValuePair<string, IOpenApiSchema> schemaEntry in document.Components.Schemas
			.Where(kvp => kvp.Key.StartsWith(SchemaConstants.NullableTypePrefix + "[[") && kvp.Value is OpenApiSchema))
		{
			OpenApiSchema nullableSchema = (OpenApiSchema)schemaEntry.Value;

			int startIndex = (SchemaConstants.NullableTypePrefix + "[[").Length;
			int endIndex = schemaEntry.Key.IndexOf(',', startIndex);
			if (endIndex > startIndex)
			{
				string underlyingTypeName = schemaEntry.Key[startIndex..endIndex];

				if (!document.Components.Schemas.ContainsKey(underlyingTypeName))
				{
					schemasToCreate.Add((schemaEntry.Key, underlyingTypeName, nullableSchema));
				}
			}
		}

		foreach ((string _, string unwrappedSchemaName, OpenApiSchema nullableSchema) in schemasToCreate)
		{
			OpenApiSchema unwrappedSchema = new()
			{
				Type = nullableSchema.Type,
				Format = nullableSchema.Format,
				Description = nullableSchema.Description,
				Enum = nullableSchema.Enum is not null ? [.. nullableSchema.Enum] : null,
				Extensions = nullableSchema.Extensions is not null ? new Dictionary<string, IOpenApiExtension>(nullableSchema.Extensions) : null
			};

			document.Components.Schemas[unwrappedSchemaName] = unwrappedSchema;
		}
	}

	static void InlineSystemStringArrayInComponents(OpenApiDocument document)
	{
		if (document.Components?.Schemas is null)
		{
			return;
		}

		// Inline System.String[] references within component schemas
		foreach (KeyValuePair<string, IOpenApiSchema> schemaEntry in document.Components.Schemas)
		{
			if (!OpenApiSchemaHelper.TryAsOpenApiSchema(schemaEntry.Value, out OpenApiSchema? schema) || schema is null)
			{
				continue;
			}

			// Process properties
			if (schema.Properties is not null && schema.Properties.Count > 0)
			{
				foreach ((string propertyName, IOpenApiSchema propertySchema) in schema.Properties)
				{
					schema.Properties[propertyName] = InlineSystemStringArrayReference(propertySchema);
				}
			}

			// Process additionalProperties (this is where IDictionary uses System.String[])
			if (schema.AdditionalProperties is not null)
			{
				schema.AdditionalProperties = InlineSystemStringArrayReference(schema.AdditionalProperties);
			}

			// Process items
			if (schema.Items is not null)
			{
				schema.Items = InlineSystemStringArrayReference(schema.Items);
			}

			// Process oneOf
			if (schema.OneOf is not null && schema.OneOf.Count > 0)
			{
				for (int i = 0; i < schema.OneOf.Count; i++)
				{
					schema.OneOf[i] = InlineSystemStringArrayReference(schema.OneOf[i]);
				}
			}

			// Process allOf
			if (schema.AllOf is not null && schema.AllOf.Count > 0)
			{
				for (int i = 0; i < schema.AllOf.Count; i++)
				{
					schema.AllOf[i] = InlineSystemStringArrayReference(schema.AllOf[i]);
				}
			}

			// Process anyOf
			if (schema.AnyOf is not null && schema.AnyOf.Count > 0)
			{
				for (int i = 0; i < schema.AnyOf.Count; i++)
				{
					schema.AnyOf[i] = InlineSystemStringArrayReference(schema.AnyOf[i]);
				}
			}
		}
	}

	static IOpenApiSchema InlineSystemStringArrayReference(IOpenApiSchema schema)
	{
		if (schema is OpenApiSchemaReference schemaRef)
		{
			string? refId = schemaRef.Reference?.Id;
			if (!string.IsNullOrEmpty(refId) && refId.Equals("System.String[]", StringComparison.Ordinal))
			{
				return new OpenApiSchema
				{
					Type = JsonSchemaType.Array,
					Items = new OpenApiSchema { Type = JsonSchemaType.String }
				};
			}
		}

		// Recursively process nested schemas
		if (OpenApiSchemaHelper.TryAsOpenApiSchema(schema, out OpenApiSchema? openApiSchema) && openApiSchema is not null)
		{
			OpenApiSchemaHelper.TransformSchemaReferences(openApiSchema, InlineSystemStringArrayReference);
		}

		return schema;
	}

	static IOpenApiSchema InlineFileTypeReferences(IOpenApiSchema schema, OpenApiDocument document)
	{
		if (schema is OpenApiSchemaReference schemaRef)
		{
			string? refId = schemaRef.Reference?.Id;
			if (!string.IsNullOrEmpty(refId))
			{
				// Check IFormFileCollection FIRST since it contains "IFormFile" in its name
				if (refId.Contains(SchemaConstants.IFormFileCollection) || refId.Equals("IFormFileCollection", StringComparison.Ordinal))
				{
					return new OpenApiSchema
					{
						Type = JsonSchemaType.Array,
						Items = new OpenApiSchema
						{
							Type = JsonSchemaType.String,
							Format = SchemaConstants.FormatBinary
						}
					};
				}
				else if (refId.Contains(SchemaConstants.IFormFile) || refId.Equals("IFormFile", StringComparison.Ordinal))
				{
					return new OpenApiSchema
					{
						Type = JsonSchemaType.String,
						Format = SchemaConstants.FormatBinary
					};
				}
			}

			return schemaRef;
		}

		if (!OpenApiSchemaHelper.TryAsOpenApiSchema(schema, out OpenApiSchema? openApiSchema) || openApiSchema is null)
		{
			return schema;
		}

		if (openApiSchema.Properties is not null && openApiSchema.Properties.Count > 0)
		{
			foreach ((string propertyName, IOpenApiSchema propertySchema) in openApiSchema.Properties)
			{
				openApiSchema.Properties[propertyName] = InlineFileTypeReferences(propertySchema, document);
			}
		}

		if (openApiSchema.Items is not null)
		{
			openApiSchema.Items = InlineFileTypeReferences(openApiSchema.Items, document);
		}

		if (openApiSchema.OneOf is not null && openApiSchema.OneOf.Count > 0)
		{
			for (int i = 0; i < openApiSchema.OneOf.Count; i++)
			{
				openApiSchema.OneOf[i] = InlineFileTypeReferences(openApiSchema.OneOf[i], document);
			}
		}

		if (openApiSchema.AllOf is not null && openApiSchema.AllOf.Count > 0)
		{
			for (int i = 0; i < openApiSchema.AllOf.Count; i++)
			{
				openApiSchema.AllOf[i] = InlineFileTypeReferences(openApiSchema.AllOf[i], document);
			}
		}

		if (openApiSchema.AnyOf is not null && openApiSchema.AnyOf.Count > 0)
		{
			for (int i = 0; i < openApiSchema.AnyOf.Count; i++)
			{
				openApiSchema.AnyOf[i] = InlineFileTypeReferences(openApiSchema.AnyOf[i], document);
			}
		}

		return openApiSchema;
	}

	static void InlinePrimitiveAndFileTypeReferences(OpenApiDocument document)
	{
		if (document.Paths is null)
		{
			return;
		}

		// Process all request bodies and response bodies
		foreach (KeyValuePair<string, IOpenApiPathItem> pathItem in document.Paths)
		{
			if (pathItem.Value is not OpenApiPathItem pathItemValue)
			{
				continue;
			}

			foreach (KeyValuePair<HttpMethod, OpenApiOperation> operation in pathItemValue.Operations ?? [])
			{
				// Process request body - inline IFormFile types
				if (operation.Value.RequestBody is OpenApiRequestBody openApiRequestBody && openApiRequestBody.Content is not null)
				{
					foreach (KeyValuePair<string, OpenApiMediaType> contentItem in openApiRequestBody.Content.Where(x => x.Value.Schema is not null))
					{
						contentItem.Value.Schema = InlineFileTypeReferences(contentItem.Value.Schema!, document);
					}
				}

				// Process response bodies - inline simple System.String but not arrays
				if (operation.Value.Responses is not null)
				{
					foreach (KeyValuePair<string, IOpenApiResponse> response in operation.Value.Responses)
					{
						if (response.Value is OpenApiResponse openApiResponse && openApiResponse.Content is not null)
						{
							foreach (KeyValuePair<string, OpenApiMediaType> contentItem in openApiResponse.Content.Where(x => x.Value.Schema is not null))
							{
								contentItem.Value.Schema = InlineSimpleSystemTypes(contentItem.Value.Schema!);
							}
						}
					}
				}
			}
		}
	}

	static IOpenApiSchema InlineSimpleSystemTypes(IOpenApiSchema schema)
	{
		// Only inline simple System.String, not arrays or complex types
		if (schema is OpenApiSchemaReference schemaRef && (schemaRef.Reference?.Id?.Equals("System.String", StringComparison.Ordinal) ?? false))
		{
			return new OpenApiSchema
			{
				Type = JsonSchemaType.String
			};
		}

		return schema;
	}

	static void InlineCollectionAndDictionaryReferences(OpenApiDocument document)
	{
		// Inline references in all paths (collections and dictionaries in single pass)
		if (document.Paths is not null)
		{
			foreach (KeyValuePair<string, IOpenApiPathItem> pathItem in document.Paths)
			{
				if (pathItem.Value is not OpenApiPathItem pathItemValue)
				{
					continue;
				}

				foreach (KeyValuePair<HttpMethod, OpenApiOperation> operation in pathItemValue.Operations ?? [])
				{
					// Inline in request body
					if (operation.Value.RequestBody is OpenApiRequestBody requestBody && requestBody.Content is not null)
					{
						foreach (KeyValuePair<string, OpenApiMediaType> contentItem in requestBody.Content.Where(x => x.Value.Schema is not null))
						{
							contentItem.Value.Schema = InlineCollectionOrDictionarySchema(contentItem.Value.Schema!, document);
						}
					}

					// Inline in responses
					if (operation.Value.Responses is not null)
					{
						foreach (KeyValuePair<string, IOpenApiResponse> response in operation.Value.Responses)
						{
							if (response.Value is OpenApiResponse openApiResponse && openApiResponse.Content is not null)
							{
								foreach (KeyValuePair<string, OpenApiMediaType> contentItem in openApiResponse.Content.Where(x => x.Value.Schema is not null))
								{
									contentItem.Value.Schema = InlineCollectionOrDictionarySchema(contentItem.Value.Schema!, document);
								}
							}
						}
					}
				}
			}
		}

		// Also inline references in component schemas
		if (document.Components?.Schemas is not null)
		{
			foreach (KeyValuePair<string, IOpenApiSchema> schemaEntry in document.Components.Schemas)
			{
				if (OpenApiSchemaHelper.TryAsOpenApiSchema(schemaEntry.Value, out OpenApiSchema? schema) && schema is not null)
				{
					InlineCollectionsAndDictionariesInSchema(schema, document);
				}
			}
		}
	}

	static IOpenApiSchema InlineCollectionOrDictionarySchema(IOpenApiSchema schema, OpenApiDocument document)
	{
		// Check if this is a reference to a collection or dictionary schema
		if (schema is OpenApiSchemaReference schemaRef)
		{
			string? refId = schemaRef.Reference?.Id;
			if (!string.IsNullOrEmpty(refId))
			{
				// Check for collection reference
				if (IsCollectionSchemaReference(refId) &&
					document.Components?.Schemas?.TryGetValue(refId, out IOpenApiSchema? collectionSchema) == true &&
					OpenApiSchemaHelper.TryAsOpenApiSchema(collectionSchema, out OpenApiSchema? openApiCollectionSchema) &&
					openApiCollectionSchema?.Items is not null)
				{
					return new OpenApiSchema
					{
						Type = JsonSchemaType.Array,
						Items = openApiCollectionSchema.Items
					};
				}

				// Check for dictionary reference
				if (IsDictionarySchemaReference(refId) &&
					document.Components?.Schemas?.TryGetValue(refId, out IOpenApiSchema? dictionarySchema) == true &&
					OpenApiSchemaHelper.TryAsOpenApiSchema(dictionarySchema, out OpenApiSchema? openApiDictionarySchema) &&
					openApiDictionarySchema?.AdditionalProperties is not null)
				{
					return new OpenApiSchema
					{
						Type = JsonSchemaType.Object,
						AdditionalProperties = openApiDictionarySchema.AdditionalProperties
					};
				}
			}
		}

		// Recursively process oneOf schemas
		if (OpenApiSchemaHelper.TryAsOpenApiSchema(schema, out OpenApiSchema? openApiSchema) &&
			openApiSchema is not null &&
			openApiSchema.OneOf is { Count: > 0 })
		{
			for (int i = 0; i < openApiSchema.OneOf.Count; i++)
			{
				openApiSchema.OneOf[i] = InlineCollectionOrDictionarySchema(openApiSchema.OneOf[i], document);
			}
		}

		return schema;
	}

	static void InlineCollectionsAndDictionariesInSchema(OpenApiSchema schema, OpenApiDocument document)
	{
		// Process properties
		if (schema.Properties is { Count: > 0 })
		{
			foreach ((string propertyName, IOpenApiSchema propertySchema) in schema.Properties)
			{
				schema.Properties[propertyName] = InlineCollectionOrDictionarySchema(propertySchema, document);
			}
		}

		// Process additionalProperties
		if (schema.AdditionalProperties is not null)
		{
			schema.AdditionalProperties = InlineCollectionOrDictionarySchema(schema.AdditionalProperties, document);
		}

		// Process items
		if (schema.Items is not null)
		{
			schema.Items = InlineCollectionOrDictionarySchema(schema.Items, document);
		}

		// Process oneOf
		if (schema.OneOf is { Count: > 0 })
		{
			for (int i = 0; i < schema.OneOf.Count; i++)
			{
				schema.OneOf[i] = InlineCollectionOrDictionarySchema(schema.OneOf[i], document);
			}
		}

		// Process allOf
		if (schema.AllOf is { Count: > 0 })
		{
			for (int i = 0; i < schema.AllOf.Count; i++)
			{
				schema.AllOf[i] = InlineCollectionOrDictionarySchema(schema.AllOf[i], document);
			}
		}

		// Process anyOf
		if (schema.AnyOf is { Count: > 0 })
		{
			for (int i = 0; i < schema.AnyOf.Count; i++)
			{
				schema.AnyOf[i] = InlineCollectionOrDictionarySchema(schema.AnyOf[i], document);
			}
		}
	}

	static bool IsCollectionSchemaReference(string refId) =>
		refId.EndsWith(SchemaConstants.ArraySuffix, StringComparison.Ordinal) || SchemaConstants.IsCollectionType(refId);

	static bool IsDictionarySchemaReference(string refId) =>
		SchemaConstants.IsDictionaryType(refId);

	static void RemoveInlinedComponentSchemas(OpenApiDocument document)
	{
		if (document.Components?.Schemas is null)
		{
			return;
		}

		List<string> schemasToRemove = [];

		foreach (KeyValuePair<string, IOpenApiSchema> schemaEntry in document.Components.Schemas.Where(x => IsCollectionSchemaReference(x.Key) || IsDictionarySchemaReference(x.Key)))
		{
			schemasToRemove.Add(schemaEntry.Key);
		}

		foreach (string schemaName in schemasToRemove)
		{
			document.Components.Schemas.Remove(schemaName);
		}
	}
}
