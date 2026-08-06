using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.PropertyEnhancer.Core;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.PropertyEnhancer;

/// <summary>
/// Converts inline value-or-null compositions to nullable schema types. Microsoft.OpenApi serializes these as
/// <c>type</c> plus <c>nullable: true</c> for OpenAPI 3.0 and as a type union for OpenAPI 3.1.
/// Nullable references remain composed because OpenAPI 3.0 does not allow nullable semantics on a Reference Object.
/// </summary>
sealed class NullableSchemaNormalizationTransformer : IOpenApiDocumentTransformer
{
	public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		HashSet<IOpenApiSchema> visitedSchemas = new(ReferenceEqualityComparer.Instance);

		ProcessComponents(document, visitedSchemas, cancellationToken);
		ProcessPaths(document, visitedSchemas, cancellationToken);

		return Task.CompletedTask;
	}

	static void ProcessComponents(OpenApiDocument document, HashSet<IOpenApiSchema> visitedSchemas, CancellationToken cancellationToken)
	{
		OpenApiComponents? components = document.Components;
		if (components is null)
		{
			return;
		}

		if (components.Schemas is not null)
		{
			foreach (string schemaId in components.Schemas.Keys.ToArray())
			{
				components.Schemas[schemaId] = ProcessSchema(components.Schemas[schemaId], visitedSchemas, cancellationToken);
			}
		}

		foreach (IOpenApiParameter parameter in components.Parameters?.Values ?? [])
		{
			ProcessParameter(parameter, components, visitedSchemas, cancellationToken);
		}
		foreach (IOpenApiRequestBody requestBody in components.RequestBodies?.Values ?? [])
		{
			ProcessRequestBody(requestBody, components, visitedSchemas, cancellationToken);
		}
		foreach (IOpenApiResponse response in components.Responses?.Values ?? [])
		{
			ProcessResponse(response, components, visitedSchemas, cancellationToken);
		}
		foreach (IOpenApiHeader header in components.Headers?.Values ?? [])
		{
			ProcessHeader(header, components, visitedSchemas, cancellationToken);
		}
	}

	static void ProcessPaths(OpenApiDocument document, HashSet<IOpenApiSchema> visitedSchemas, CancellationToken cancellationToken)
	{
		if (document.Paths is null)
		{
			return;
		}

		OpenApiComponents? components = document.Components;
		foreach (OpenApiPathItem pathItem in document.Paths.Values.OfType<OpenApiPathItem>())
		{
			foreach (IOpenApiParameter parameter in pathItem.Parameters ?? [])
			{
				ProcessParameter(parameter, components, visitedSchemas, cancellationToken);
			}

			foreach (OpenApiOperation operation in pathItem.Operations?.Values.AsEnumerable() ?? [])
			{
				foreach (IOpenApiParameter parameter in operation.Parameters ?? [])
				{
					ProcessParameter(parameter, components, visitedSchemas, cancellationToken);
				}

				if (operation.RequestBody is not null)
				{
					ProcessRequestBody(operation.RequestBody, components, visitedSchemas, cancellationToken);
				}

				foreach (IOpenApiResponse response in operation.Responses?.Values.AsEnumerable() ?? [])
				{
					ProcessResponse(response, components, visitedSchemas, cancellationToken);
				}
			}
		}
	}

	static void ProcessParameter(IOpenApiParameter parameter, OpenApiComponents? components, HashSet<IOpenApiSchema> visitedSchemas, CancellationToken cancellationToken)
	{
		IOpenApiParameter resolved = OpenApiSchemaHelper.ResolveReference(parameter, components?.Parameters);
		if (resolved is OpenApiParameter openApiParameter && openApiParameter.Schema is not null)
		{
			openApiParameter.Schema = ProcessSchema(openApiParameter.Schema, visitedSchemas, cancellationToken);
		}
	}

	static void ProcessHeader(IOpenApiHeader header, OpenApiComponents? components, HashSet<IOpenApiSchema> visitedSchemas, CancellationToken cancellationToken)
	{
		IOpenApiHeader resolved = OpenApiSchemaHelper.ResolveReference(header, components?.Headers);
		if (resolved is OpenApiHeader openApiHeader && openApiHeader.Schema is not null)
		{
			openApiHeader.Schema = ProcessSchema(openApiHeader.Schema, visitedSchemas, cancellationToken);
		}
	}

	static void ProcessRequestBody(IOpenApiRequestBody requestBody, OpenApiComponents? components, HashSet<IOpenApiSchema> visitedSchemas, CancellationToken cancellationToken)
	{
		IOpenApiRequestBody resolved = OpenApiSchemaHelper.ResolveReference(requestBody, components?.RequestBodies);
		if (resolved is OpenApiRequestBody openApiRequestBody)
		{
			ProcessContent(openApiRequestBody.Content, components, visitedSchemas, cancellationToken);
		}
	}

	static void ProcessResponse(IOpenApiResponse response, OpenApiComponents? components, HashSet<IOpenApiSchema> visitedSchemas, CancellationToken cancellationToken)
	{
		IOpenApiResponse resolved = OpenApiSchemaHelper.ResolveReference(response, components?.Responses);
		if (resolved is not OpenApiResponse openApiResponse)
		{
			return;
		}

		ProcessContent(openApiResponse.Content, components, visitedSchemas, cancellationToken);
		foreach (IOpenApiHeader header in openApiResponse.Headers?.Values ?? [])
		{
			ProcessHeader(header, components, visitedSchemas, cancellationToken);
		}
	}

	static void ProcessContent(IDictionary<string, OpenApiMediaType>? content, OpenApiComponents? components, HashSet<IOpenApiSchema> visitedSchemas, CancellationToken cancellationToken)
	{
		foreach (OpenApiMediaType mediaType in content?.Values ?? [])
		{
			if (mediaType.Schema is not null)
			{
				mediaType.Schema = ProcessSchema(mediaType.Schema, visitedSchemas, cancellationToken);
			}

			foreach (OpenApiEncoding encoding in mediaType.Encoding?.Values ?? [])
			{
				foreach (IOpenApiHeader header in encoding.Headers?.Values ?? [])
				{
					ProcessHeader(header, components, visitedSchemas, cancellationToken);
				}
			}
		}
	}

	static IOpenApiSchema ProcessSchema(IOpenApiSchema schema, HashSet<IOpenApiSchema> visitedSchemas, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		if (schema is OpenApiSchemaReference)
		{
			return schema;
		}
		if (!OpenApiSchemaHelper.TryAsOpenApiSchema(schema, out OpenApiSchema? openApiSchema) || openApiSchema is null)
		{
			return schema;
		}

		openApiSchema = NormalizeInlineNullableSchema(openApiSchema);
		if (!visitedSchemas.Add(openApiSchema))
		{
			return openApiSchema;
		}

		if (openApiSchema.Properties is not null)
		{
			foreach (string propertyName in openApiSchema.Properties.Keys.ToArray())
			{
				openApiSchema.Properties[propertyName] = ProcessSchema(openApiSchema.Properties[propertyName], visitedSchemas, cancellationToken);
			}
		}
		if (openApiSchema.Items is not null)
		{
			openApiSchema.Items = ProcessSchema(openApiSchema.Items, visitedSchemas, cancellationToken);
		}
		ProcessSchemaCollection(openApiSchema.AllOf, visitedSchemas, cancellationToken);
		ProcessSchemaCollection(openApiSchema.OneOf, visitedSchemas, cancellationToken);
		ProcessSchemaCollection(openApiSchema.AnyOf, visitedSchemas, cancellationToken);
		if (openApiSchema.AdditionalProperties is not null)
		{
			openApiSchema.AdditionalProperties = ProcessSchema(openApiSchema.AdditionalProperties, visitedSchemas, cancellationToken);
		}
		if (openApiSchema.Not is not null)
		{
			openApiSchema.Not = ProcessSchema(openApiSchema.Not, visitedSchemas, cancellationToken);
		}

		return openApiSchema;
	}

	static void ProcessSchemaCollection(IList<IOpenApiSchema>? schemas, HashSet<IOpenApiSchema> visitedSchemas, CancellationToken cancellationToken)
	{
		if (schemas is null)
		{
			return;
		}

		for (int i = 0; i < schemas.Count; i++)
		{
			schemas[i] = ProcessSchema(schemas[i], visitedSchemas, cancellationToken);
		}
	}

	static OpenApiSchema NormalizeInlineNullableSchema(OpenApiSchema wrapper)
	{
		IList<IOpenApiSchema>? composition = wrapper.OneOf is { Count: 2 }
			? wrapper.OneOf
			: wrapper.AnyOf is { Count: 2 }
				? wrapper.AnyOf
				: null;

		if (composition is null)
		{
			return wrapper;
		}

		OpenApiSchema? nullSchema = composition.OfType<OpenApiSchema>().FirstOrDefault(IsNullSchema);
		if (nullSchema is null)
		{
			return wrapper;
		}

		IOpenApiSchema valueCandidate = composition.First(candidate => !ReferenceEquals(candidate, nullSchema));
		if (valueCandidate is not OpenApiSchema valueSchema ||
			!valueSchema.Type.HasValue ||
			valueSchema.Type.Value.HasFlag(JsonSchemaType.Null))
		{
			return wrapper;
		}

		OpenApiSchema normalized = (OpenApiSchema)valueSchema.CreateShallowCopy();
		normalized.Type = valueSchema.Type.Value | JsonSchemaType.Null;
		if (valueSchema.Enum is { Count: > 0 })
		{
			// In OpenAPI 3.0, nullable widens the type but other constraints still apply.
			// The enum must therefore explicitly contain null as an allowed value.
			normalized.Enum = [.. valueSchema.Enum];
			normalized.Enum.Add(null!);
		}

		if (wrapper.Title is not null)
		{
			normalized.Title = wrapper.Title;
		}
		if (wrapper.Description is not null)
		{
			normalized.Description = wrapper.Description;
		}

		return normalized;
	}

	static bool IsNullSchema(OpenApiSchema schema)
	{
		return schema.Type == JsonSchemaType.Null &&
			schema.OneOf is not { Count: > 0 } &&
			schema.AnyOf is not { Count: > 0 } &&
			schema.AllOf is not { Count: > 0 } &&
			schema.Enum is not { Count: > 0 } &&
			schema.Properties is not { Count: > 0 } &&
			schema.Items is null &&
			schema.AdditionalProperties is null &&
			schema.Not is null;
	}
}
