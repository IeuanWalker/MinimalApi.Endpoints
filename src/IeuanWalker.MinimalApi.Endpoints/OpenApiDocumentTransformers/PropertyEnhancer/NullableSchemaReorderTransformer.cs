using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.PropertyEnhancer.Core;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.PropertyEnhancer;

/// <summary>
/// Reorders oneOf/anyOf/allOf schema arrays to ensure nullable markers appear last.
/// </summary>
sealed class NullableSchemaReorderTransformer : IOpenApiDocumentTransformer
{
	public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		HashSet<IOpenApiSchema> visitedSchemas = new(ReferenceEqualityComparer.Instance);
		HashSet<object> visitedContainers = new(ReferenceEqualityComparer.Instance);

		ProcessComponents(document, visitedSchemas, visitedContainers, cancellationToken);
		ProcessPathItems(document.Paths?.Values, document, visitedSchemas, visitedContainers, cancellationToken);
		ProcessPathItems(document.Webhooks?.Values, document, visitedSchemas, visitedContainers, cancellationToken);

		return Task.CompletedTask;
	}

	static void ProcessComponents(OpenApiDocument document, HashSet<IOpenApiSchema> visitedSchemas, HashSet<object> visitedContainers, CancellationToken cancellationToken)
	{
		if (document.Components is null)
		{
			return;
		}

		if (document.Components.Schemas is not null)
		{
			foreach (IOpenApiSchema schema in document.Components.Schemas.Values)
			{
				ProcessSchema(schema, document, visitedSchemas, cancellationToken);
			}
		}

		if (document.Components.Parameters is not null)
		{
			foreach (IOpenApiParameter parameter in document.Components.Parameters.Values)
			{
				ProcessParameter(parameter, document, visitedSchemas, cancellationToken);
			}
		}

		if (document.Components.RequestBodies is not null)
		{
			foreach (IOpenApiRequestBody requestBody in document.Components.RequestBodies.Values)
			{
				ProcessRequestBody(requestBody, document, visitedSchemas, cancellationToken);
			}
		}

		if (document.Components.Responses is not null)
		{
			foreach (IOpenApiResponse response in document.Components.Responses.Values)
			{
				ProcessResponse(response, document, visitedSchemas, cancellationToken);
			}
		}

		if (document.Components.Headers is not null)
		{
			foreach (IOpenApiHeader header in document.Components.Headers.Values)
			{
				ProcessHeader(header, document, visitedSchemas, cancellationToken);
			}
		}

		ProcessPathItems(document.Components.PathItems?.Values, document, visitedSchemas, visitedContainers, cancellationToken);
		foreach (IOpenApiCallback callback in document.Components.Callbacks?.Values ?? [])
		{
			ProcessCallback(callback, document, visitedSchemas, visitedContainers, cancellationToken);
		}
	}

	static void ProcessPathItems(IEnumerable<IOpenApiPathItem>? pathItems, OpenApiDocument document, HashSet<IOpenApiSchema> visitedSchemas, HashSet<object> visitedContainers, CancellationToken cancellationToken)
	{
		foreach (IOpenApiPathItem pathItem in pathItems ?? [])
		{
			ProcessPathItem(pathItem, document, visitedSchemas, visitedContainers, cancellationToken);
		}
	}

	static void ProcessPathItem(IOpenApiPathItem pathItem, OpenApiDocument document, HashSet<IOpenApiSchema> visitedSchemas, HashSet<object> visitedContainers, CancellationToken cancellationToken)
	{
		IOpenApiPathItem resolved = OpenApiSchemaHelper.ResolveReference(pathItem, document.Components?.PathItems);
		if (!visitedContainers.Add(resolved))
		{
			return;
		}

		cancellationToken.ThrowIfCancellationRequested();
		ProcessParameterCollection(resolved.Parameters, document, visitedSchemas, cancellationToken);
		foreach (OpenApiOperation operation in resolved.Operations?.Values.AsEnumerable() ?? [])
		{
			ProcessOperation(operation, document, visitedSchemas, visitedContainers, cancellationToken);
		}
	}

	static void ProcessOperation(OpenApiOperation operation, OpenApiDocument document, HashSet<IOpenApiSchema> visitedSchemas, HashSet<object> visitedContainers, CancellationToken cancellationToken)
	{
		ProcessParameterCollection(operation.Parameters, document, visitedSchemas, cancellationToken);
		ProcessRequestBody(operation.RequestBody, document, visitedSchemas, cancellationToken);
		foreach (IOpenApiResponse response in operation.Responses?.Values.AsEnumerable() ?? [])
		{
			ProcessResponse(response, document, visitedSchemas, cancellationToken);
		}
		foreach (IOpenApiCallback callback in operation.Callbacks?.Values ?? [])
		{
			ProcessCallback(callback, document, visitedSchemas, visitedContainers, cancellationToken);
		}
	}

	static void ProcessCallback(IOpenApiCallback callback, OpenApiDocument document, HashSet<IOpenApiSchema> visitedSchemas, HashSet<object> visitedContainers, CancellationToken cancellationToken)
	{
		IOpenApiCallback resolved = OpenApiSchemaHelper.ResolveReference(callback, document.Components?.Callbacks);
		if (!visitedContainers.Add(resolved))
		{
			return;
		}

		ProcessPathItems(resolved.PathItems?.Values, document, visitedSchemas, visitedContainers, cancellationToken);
	}

	static void ProcessParameterCollection(IEnumerable<IOpenApiParameter>? parameters, OpenApiDocument document, HashSet<IOpenApiSchema> visitedSchemas, CancellationToken cancellationToken)
	{
		if (parameters is null)
		{
			return;
		}

		foreach (IOpenApiParameter parameter in parameters)
		{
			ProcessParameter(parameter, document, visitedSchemas, cancellationToken);
		}
	}

	static void ProcessParameter(IOpenApiParameter parameter, OpenApiDocument document, HashSet<IOpenApiSchema> visitedSchemas, CancellationToken cancellationToken)
	{
		IOpenApiParameter resolved = OpenApiSchemaHelper.ResolveReference(parameter, document.Components?.Parameters);

		if (resolved is not OpenApiParameter openApiParameter)
		{
			return;
		}

		if (openApiParameter.Schema is not null)
		{
			ProcessSchema(openApiParameter.Schema, document, visitedSchemas, cancellationToken);
		}
		ProcessContent(openApiParameter.Content, document, visitedSchemas, cancellationToken);
	}

	static void ProcessHeader(IOpenApiHeader header, OpenApiDocument document, HashSet<IOpenApiSchema> visitedSchemas, CancellationToken cancellationToken)
	{
		IOpenApiHeader resolved = OpenApiSchemaHelper.ResolveReference(header, document.Components?.Headers);

		if (resolved is not OpenApiHeader openApiHeader)
		{
			return;
		}

		if (openApiHeader.Schema is not null)
		{
			ProcessSchema(openApiHeader.Schema, document, visitedSchemas, cancellationToken);
		}
		ProcessContent(openApiHeader.Content, document, visitedSchemas, cancellationToken);
	}

	static void ProcessRequestBody(IOpenApiRequestBody? requestBody, OpenApiDocument document, HashSet<IOpenApiSchema> visitedSchemas, CancellationToken cancellationToken)
	{
		if (requestBody is null)
		{
			return;
		}

		IOpenApiRequestBody resolved = OpenApiSchemaHelper.ResolveReference(requestBody, document.Components?.RequestBodies);

		if (resolved is not OpenApiRequestBody openApiRequestBody)
		{
			return;
		}

		ProcessContent(openApiRequestBody.Content, document, visitedSchemas, cancellationToken);
	}

	static void ProcessResponse(IOpenApiResponse response, OpenApiDocument document, HashSet<IOpenApiSchema> visitedSchemas, CancellationToken cancellationToken)
	{
		IOpenApiResponse resolved = OpenApiSchemaHelper.ResolveReference(response, document.Components?.Responses);

		if (resolved is not OpenApiResponse openApiResponse)
		{
			return;
		}

		ProcessContent(openApiResponse.Content, document, visitedSchemas, cancellationToken);

		if (openApiResponse.Headers is null)
		{
			return;
		}

		foreach (IOpenApiHeader header in openApiResponse.Headers.Values)
		{
			ProcessHeader(header, document, visitedSchemas, cancellationToken);
		}
	}

	static void ProcessContent(IDictionary<string, OpenApiMediaType>? content, OpenApiDocument document, HashSet<IOpenApiSchema> visitedSchemas, CancellationToken cancellationToken)
	{
		if (content is null)
		{
			return;
		}

		foreach (OpenApiMediaType mediaType in content.Values)
		{
			if (mediaType.Schema is not null)
			{
				ProcessSchema(mediaType.Schema, document, visitedSchemas, cancellationToken);
			}

			if (mediaType.Encoding is null)
			{
				continue;
			}

			foreach (OpenApiEncoding encoding in mediaType.Encoding.Values)
			{
				if (encoding.Headers is null)
				{
					continue;
				}

				foreach (IOpenApiHeader header in encoding.Headers.Values)
				{
					ProcessHeader(header, document, visitedSchemas, cancellationToken);
				}
			}
		}
	}

	static void ProcessSchema(IOpenApiSchema schema, OpenApiDocument document, HashSet<IOpenApiSchema> visitedSchemas, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		if (schema is OpenApiSchemaReference schemaReference)
		{
			string? referenceId = schemaReference.Reference?.Id;
			if (referenceId is not null && document.Components?.Schemas?.TryGetValue(referenceId, out IOpenApiSchema? referencedSchema) == true)
			{
				ProcessSchema(referencedSchema, document, visitedSchemas, cancellationToken);
			}
			return;
		}

		if (!OpenApiSchemaHelper.TryAsOpenApiSchema(schema, out OpenApiSchema? openApiSchema) || openApiSchema is null)
		{
			return;
		}

		if (!visitedSchemas.Add(openApiSchema))
		{
			return;
		}

		ReorderNullableVariants(openApiSchema.AllOf);
		ReorderNullableVariants(openApiSchema.OneOf);
		ReorderNullableVariants(openApiSchema.AnyOf);

		ProcessSchemas(openApiSchema.Properties?.Values, document, visitedSchemas, cancellationToken);
		ProcessSchemas(openApiSchema.Definitions?.Values, document, visitedSchemas, cancellationToken);
		ProcessSchemas(openApiSchema.PatternProperties?.Values, document, visitedSchemas, cancellationToken);
		ProcessSchemas(openApiSchema.DependentSchemas?.Values, document, visitedSchemas, cancellationToken);

		if (openApiSchema.Items is not null)
		{
			ProcessSchema(openApiSchema.Items, document, visitedSchemas, cancellationToken);
		}

		ProcessSchemas(openApiSchema.AllOf, document, visitedSchemas, cancellationToken);
		ProcessSchemas(openApiSchema.OneOf, document, visitedSchemas, cancellationToken);
		ProcessSchemas(openApiSchema.AnyOf, document, visitedSchemas, cancellationToken);

		if (openApiSchema.AdditionalProperties is not null)
		{
			ProcessSchema(openApiSchema.AdditionalProperties, document, visitedSchemas, cancellationToken);
		}

		if (openApiSchema.Not is not null)
		{
			ProcessSchema(openApiSchema.Not, document, visitedSchemas, cancellationToken);
		}
		if (openApiSchema.Contains is not null)
		{
			ProcessSchema(openApiSchema.Contains, document, visitedSchemas, cancellationToken);
		}
		if (openApiSchema.PropertyNames is not null)
		{
			ProcessSchema(openApiSchema.PropertyNames, document, visitedSchemas, cancellationToken);
		}
		if (openApiSchema.UnevaluatedPropertiesSchema is not null)
		{
			ProcessSchema(openApiSchema.UnevaluatedPropertiesSchema, document, visitedSchemas, cancellationToken);
		}
		if (openApiSchema.ContentSchema is not null)
		{
			ProcessSchema(openApiSchema.ContentSchema, document, visitedSchemas, cancellationToken);
		}
		if (openApiSchema.If is not null)
		{
			ProcessSchema(openApiSchema.If, document, visitedSchemas, cancellationToken);
		}
		if (openApiSchema.Then is not null)
		{
			ProcessSchema(openApiSchema.Then, document, visitedSchemas, cancellationToken);
		}
		if (openApiSchema.Else is not null)
		{
			ProcessSchema(openApiSchema.Else, document, visitedSchemas, cancellationToken);
		}
	}

	static void ProcessSchemas(IEnumerable<IOpenApiSchema>? schemas, OpenApiDocument document, HashSet<IOpenApiSchema> visitedSchemas, CancellationToken cancellationToken)
	{
		foreach (IOpenApiSchema schema in schemas ?? [])
		{
			ProcessSchema(schema, document, visitedSchemas, cancellationToken);
		}
	}

	static void ReorderNullableVariants(IList<IOpenApiSchema>? schemas)
	{
		if (schemas is null || schemas.Count < 2)
		{
			return;
		}

		List<IOpenApiSchema> nullableSchemas = [];
		List<IOpenApiSchema> nonNullableSchemas = [];

		foreach (IOpenApiSchema schema in schemas)
		{
			if (IsNullableMarker(schema))
			{
				nullableSchemas.Add(schema);
			}
			else
			{
				nonNullableSchemas.Add(schema);
			}
		}

		if (nullableSchemas.Count == 0)
		{
			return;
		}

		List<IOpenApiSchema> reordered = [];
		reordered.AddRange(nonNullableSchemas);
		reordered.AddRange(nullableSchemas);

		if (!reordered.SequenceEqual(schemas, ReferenceEqualityComparer.Instance))
		{
			schemas.Clear();
			foreach (IOpenApiSchema schema in reordered)
			{
				schemas.Add(schema);
			}
		}
	}

	static bool IsNullableMarker(IOpenApiSchema schema)
	{
		return OpenApiSchemaHelper.TryAsOpenApiSchema(schema, out OpenApiSchema? openApiSchema) &&
			openApiSchema?.Type == JsonSchemaType.Null;
	}
}
