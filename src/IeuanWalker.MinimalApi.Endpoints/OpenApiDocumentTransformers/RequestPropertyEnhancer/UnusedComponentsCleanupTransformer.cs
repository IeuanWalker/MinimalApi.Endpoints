using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer.Core;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer;

/// <summary>
/// OpenAPI document transformer that removes unused component schemas from the OpenAPI document.
/// This transformer should run after all other transformers to clean up schemas that are no longer
/// referenced due to aggressive inlining and unwrapping of nullable objects.
/// </summary>
sealed class UnusedComponentsCleanupTransformer : IOpenApiDocumentTransformer
{
	public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
	{
		IDictionary<string, IOpenApiSchema>? schemas = document.Components?.Schemas;
		if (schemas is null || schemas.Count == 0)
		{
			return Task.CompletedTask;
		}

		int totalSchemas = schemas.Count;

		cancellationToken.ThrowIfCancellationRequested();

		HashSet<string> usedSchemaIds = [];

		if (document.Paths is not null)
		{
			foreach (KeyValuePair<string, IOpenApiPathItem> pathItem in document.Paths)
			{
				if (pathItem.Value is not OpenApiPathItem pathItemValue)
				{
					continue;
				}

				cancellationToken.ThrowIfCancellationRequested();

				CollectParameterSchemas(pathItemValue.Parameters, usedSchemaIds, document);

				foreach (KeyValuePair<HttpMethod, OpenApiOperation> operation in pathItemValue.Operations ?? [])
				{
					cancellationToken.ThrowIfCancellationRequested();

					CollectParameterSchemas(operation.Value.Parameters, usedSchemaIds, document);
					CollectRequestBodySchemas(operation.Value.RequestBody, usedSchemaIds, document);

					if (operation.Value.Responses is not null)
					{
						foreach (IOpenApiResponse response in operation.Value.Responses.Values)
						{
							CollectResponseSchemas(response, usedSchemaIds, document);
						}
					}
				}
			}
		}

		if (usedSchemaIds.Count >= totalSchemas)
		{
			return Task.CompletedTask;
		}

		List<string> schemasToRemove = [.. schemas.Keys.Where(schemaId => !usedSchemaIds.Contains(schemaId))];

		foreach (string schemaId in schemasToRemove)
		{
			schemas.Remove(schemaId);
		}

		return Task.CompletedTask;
	}

	static void CollectResponseSchemas(IOpenApiResponse response, HashSet<string> usedSchemaIds, OpenApiDocument document)
	{
		IOpenApiResponse resolvedResponse = OpenApiSchemaHelper.ResolveReference(response, document.Components?.Responses);

		if (resolvedResponse is not OpenApiResponse openApiResponse)
		{
			return;
		}

		CollectContentSchemas(openApiResponse.Content, usedSchemaIds, document);

		if (openApiResponse.Headers is null)
		{
			return;
		}

		foreach (IOpenApiHeader header in openApiResponse.Headers.Values)
		{
			CollectHeaderSchemas(header, usedSchemaIds, document);
		}
	}

	static void CollectRequestBodySchemas(IOpenApiRequestBody? requestBody, HashSet<string> usedSchemaIds, OpenApiDocument document)
	{
		if (requestBody is null)
		{
			return;
		}

		IOpenApiRequestBody resolvedBody = OpenApiSchemaHelper.ResolveReference(requestBody, document.Components?.RequestBodies);

		if (resolvedBody is not OpenApiRequestBody openApiRequestBody)
		{
			return;
		}

		CollectContentSchemas(openApiRequestBody.Content, usedSchemaIds, document);
	}

	static void CollectContentSchemas(IDictionary<string, OpenApiMediaType>? content, HashSet<string> usedSchemaIds, OpenApiDocument document)
	{
		if (content is null)
		{
			return;
		}

		foreach (KeyValuePair<string, OpenApiMediaType> contentItem in content)
		{
			if (contentItem.Value.Schema is not null)
			{
				CollectSchemaReferences(contentItem.Value.Schema, usedSchemaIds, document);
			}

			if (contentItem.Value.Encoding is null)
			{
				continue;
			}

			foreach (OpenApiEncoding encoding in contentItem.Value.Encoding.Values)
			{
				if (encoding.Headers is null)
				{
					continue;
				}

				foreach (IOpenApiHeader header in encoding.Headers.Values)
				{
					CollectHeaderSchemas(header, usedSchemaIds, document);
				}
			}
		}
	}

	static void CollectHeaderSchemas(IOpenApiHeader? header, HashSet<string> usedSchemaIds, OpenApiDocument document)
	{
		if (header is null)
		{
			return;
		}

		IOpenApiHeader resolvedHeader = OpenApiSchemaHelper.ResolveReference(header, document.Components?.Headers);

		if (resolvedHeader is not OpenApiHeader openApiHeader || openApiHeader.Schema is null)
		{
			return;
		}

		CollectSchemaReferences(openApiHeader.Schema, usedSchemaIds, document);
	}

	static void CollectParameterSchemas(IEnumerable<IOpenApiParameter>? parameters, HashSet<string> usedSchemaIds, OpenApiDocument document)
	{
		if (parameters is null)
		{
			return;
		}

		foreach (IOpenApiParameter parameter in parameters)
		{
			IOpenApiParameter resolvedParameter = OpenApiSchemaHelper.ResolveReference(parameter, document.Components?.Parameters);

			if (resolvedParameter is OpenApiParameter openApiParameter && openApiParameter.Schema is not null)
			{
				CollectSchemaReferences(openApiParameter.Schema, usedSchemaIds, document);
			}
		}
	}

	/// <summary>
	/// Recursively collects all schema references from a schema and its nested elements.
	/// </summary>
	static void CollectSchemaReferences(IOpenApiSchema schema, HashSet<string> usedSchemaIds, OpenApiDocument document)
	{
		if (schema is OpenApiSchemaReference schemaRef)
		{
			string? refId = schemaRef.Reference?.Id;
			if (!string.IsNullOrEmpty(refId)
				&& usedSchemaIds.Add(refId)
				&& document.Components?.Schemas?.TryGetValue(refId, out IOpenApiSchema? referencedSchema) == true)
			{
				CollectSchemaReferences(referencedSchema, usedSchemaIds, document);
			}
			return;
		}

		if (!OpenApiSchemaHelper.TryAsOpenApiSchema(schema, out OpenApiSchema? openApiSchema) || openApiSchema is null)
		{
			return;
		}

		if (openApiSchema.Properties is not null)
		{
			foreach (IOpenApiSchema propertySchema in openApiSchema.Properties.Values)
			{
				CollectSchemaReferences(propertySchema, usedSchemaIds, document);
			}
		}

		if (openApiSchema.Items is not null)
		{
			CollectSchemaReferences(openApiSchema.Items, usedSchemaIds, document);
		}

		if (openApiSchema.AllOf is not null)
		{
			foreach (IOpenApiSchema allOfSchema in openApiSchema.AllOf)
			{
				CollectSchemaReferences(allOfSchema, usedSchemaIds, document);
			}
		}

		if (openApiSchema.OneOf is not null)
		{
			foreach (IOpenApiSchema oneOfSchema in openApiSchema.OneOf)
			{
				CollectSchemaReferences(oneOfSchema, usedSchemaIds, document);
			}
		}

		if (openApiSchema.AnyOf is not null)
		{
			foreach (IOpenApiSchema anyOfSchema in openApiSchema.AnyOf)
			{
				CollectSchemaReferences(anyOfSchema, usedSchemaIds, document);
			}
		}

		if (openApiSchema.AdditionalProperties is not null)
		{
			CollectSchemaReferences(openApiSchema.AdditionalProperties, usedSchemaIds, document);
		}

		if (openApiSchema.Not is not null)
		{
			CollectSchemaReferences(openApiSchema.Not, usedSchemaIds, document);
		}
	}
}
