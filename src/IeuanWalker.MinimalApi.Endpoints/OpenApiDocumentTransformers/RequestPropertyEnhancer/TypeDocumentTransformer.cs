using System.Reflection;
using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Routing;
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
		// Step 1: Fix all schema property types
		FixSchemaPropertyTypes(document);

		// Step 2: Fix nullable array structures (arrays with nullable items should be nullable arrays instead)
		FixNullableArrays(document);

		// Step 3: Fix double-wrapped arrays (defensive cleanup)
		FixDoubleWrappedArrays(document);

		// Step 4: Build endpoint-to-request-type mapping
		Dictionary<string, Type> endpointToRequestType = BuildEndpointToRequestTypeMapping(context, document);

		// Step 5: Fix parameter types (query/path parameters)
		FixParameterTypes(document, endpointToRequestType);

		// Step 6: Ensure unwrapped enum schemas exist (before reordering)
		EnsureUnwrappedEnumSchemasExist(document);

		return Task.CompletedTask;
	}

	static void FixSchemaPropertyTypes(OpenApiDocument document)
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

			if (schema.Properties?.Count > 0)
			{
				foreach ((string propertyName, IOpenApiSchema propertySchema) in schema.Properties)
				{
					PropertyInfo? propertyInfo = schemaType?.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
					schema.Properties[propertyName] = FixSchemaType(propertySchema, document, propertyInfo?.PropertyType);
				}
			}

			if (schema.Items is not null)
			{
				schema.Items = FixSchemaType(schema.Items, document, null);
			}

			FixInlineSchemaType(schema, schemaEntry.Key);
		}
	}

	static void FixNullableArrays(OpenApiDocument document)
	{
		if (document.Components?.Schemas is null)
		{
			return;
		}

		foreach (KeyValuePair<string, IOpenApiSchema> schemaEntry in document.Components.Schemas)
		{
			if (!OpenApiSchemaHelper.TryAsOpenApiSchema(schemaEntry.Value, out OpenApiSchema? schema) ||
				schema is null ||
				schema.Properties is null ||
				schema.Properties.Count == 0)
			{
				continue;
			}

			foreach ((string propertyName, IOpenApiSchema propertySchema) in schema.Properties)
			{
				schema.Properties[propertyName] = FixNullableArraySchema(propertySchema);
			}
		}
	}

	static void FixDoubleWrappedArrays(OpenApiDocument document)
	{
		if (document.Components?.Schemas is null)
		{
			return;
		}

		foreach (KeyValuePair<string, IOpenApiSchema> schemaEntry in document.Components.Schemas)
		{
			if (!OpenApiSchemaHelper.TryAsOpenApiSchema(schemaEntry.Value, out OpenApiSchema? schema) ||
				schema is null ||
				schema.Properties is null ||
				schema.Properties.Count == 0)
			{
				continue;
			}

			foreach ((string propertyName, IOpenApiSchema propertySchema) in schema.Properties)
			{
				schema.Properties[propertyName] = UnwrapDoubleArrays(propertySchema);
			}
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
			if (schemaName.Contains(SchemaConstants.IFormFile))
			{
				schema.Type = JsonSchemaType.String;
				schema.Format = SchemaConstants.FormatBinary;
			}
			else if (schemaName.Contains(SchemaConstants.IFormFileCollection))
			{
				schema.Type = JsonSchemaType.Array;
				schema.Items = new OpenApiSchema
				{
					Type = JsonSchemaType.String,
					Format = SchemaConstants.FormatBinary
				};
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

		OpenApiSchema inlineSchema = new();

		if (refId.EndsWith(SchemaConstants.ArraySuffix))
		{
			inlineSchema.Type = JsonSchemaType.Array;
			string elementRefId = refId[..^2];
			OpenApiSchemaReference elementRef = new(elementRefId, null, null);
			inlineSchema.Items = InlinePrimitiveTypeReferenceForParameter(elementRef);
		}
		else if (refId.Contains(SchemaConstants.SystemString))
		{
			inlineSchema.Type = JsonSchemaType.String;
		}
		else if (refId.Contains(SchemaConstants.SystemInt32))
		{
			inlineSchema.Type = JsonSchemaType.Integer;
			inlineSchema.Format = SchemaConstants.FormatInt32;
		}
		else if (refId.Contains(SchemaConstants.SystemInt64))
		{
			inlineSchema.Type = JsonSchemaType.Integer;
			inlineSchema.Format = SchemaConstants.FormatInt64;
		}
		else if (refId.Contains(SchemaConstants.SystemInt16) || refId.Contains(SchemaConstants.SystemByte))
		{
			inlineSchema.Type = JsonSchemaType.Integer;
			inlineSchema.Format = SchemaConstants.FormatInt32;
		}
		else if (refId.Contains(SchemaConstants.SystemDecimal))
		{
			inlineSchema.Type = JsonSchemaType.Number;
		}
		else if (refId.Contains(SchemaConstants.SystemDouble))
		{
			inlineSchema.Type = JsonSchemaType.Number;
			inlineSchema.Format = SchemaConstants.FormatDouble;
		}
		else if (refId.Contains(SchemaConstants.SystemSingle))
		{
			inlineSchema.Type = JsonSchemaType.Number;
			inlineSchema.Format = SchemaConstants.FormatFloat;
		}
		else if (refId.Contains(SchemaConstants.SystemBoolean))
		{
			inlineSchema.Type = JsonSchemaType.Boolean;
		}
		else if (refId.Contains(SchemaConstants.SystemDateTime) || refId.Contains(SchemaConstants.SystemDateTimeOffset))
		{
			inlineSchema.Type = JsonSchemaType.String;
			inlineSchema.Format = SchemaConstants.FormatDateTime;
		}
		else if (refId.Contains(SchemaConstants.SystemDateOnly))
		{
			inlineSchema.Type = JsonSchemaType.String;
			inlineSchema.Format = SchemaConstants.FormatDate;
		}
		else if (refId.Contains(SchemaConstants.SystemTimeOnly))
		{
			inlineSchema.Type = JsonSchemaType.String;
			inlineSchema.Format = SchemaConstants.FormatTime;
		}
		else if (refId.Contains(SchemaConstants.SystemGuid))
		{
			inlineSchema.Type = JsonSchemaType.String;
			inlineSchema.Format = SchemaConstants.FormatUuid;
		}
		else
		{
			return schemaRef;
		}

		return inlineSchema;
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

	static Dictionary<string, Type> BuildEndpointToRequestTypeMapping(OpenApiDocumentTransformerContext context, OpenApiDocument document)
	{
		Dictionary<string, Type> mapping = new(StringComparer.OrdinalIgnoreCase);

		if (context.ApplicationServices.GetService(typeof(EndpointDataSource)) is not EndpointDataSource endpointDataSource)
		{
			return mapping;
		}

		HashSet<Type> requestTypes = [];
		if (document.Components?.Schemas is not null)
		{
			requestTypes.UnionWith(
				document.Components.Schemas.Keys
					.Select(SchemaTypeResolver.GetSchemaType)
					.OfType<Type>());
		}

		foreach (Microsoft.AspNetCore.Http.Endpoint endpoint in endpointDataSource.Endpoints)
		{
			if (endpoint is not RouteEndpoint routeEndpoint)
			{
				continue;
			}

			string? routePattern = routeEndpoint.RoutePattern.RawText;
			if (string.IsNullOrEmpty(routePattern))
			{
				continue;
			}

			MethodInfo? handlerMethod = routeEndpoint.Metadata.OfType<MethodInfo>().FirstOrDefault();
			if (handlerMethod is null)
			{
				continue;
			}

			ParameterInfo[] parameters = handlerMethod.GetParameters();

			Type? matchingParamType = parameters
				.Select(param => param.ParameterType)
				.FirstOrDefault(paramType => requestTypes.Contains(paramType));

			if (matchingParamType is not null)
			{
				mapping[routePattern] = matchingParamType;
			}
		}

		return mapping;
	}
}
