using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers;

/// <summary>
/// OpenAPI document transformer that removes unused component schemas from the OpenAPI document.
/// This transformer should run after all other transformers to clean up schemas that are no longer
/// referenced due to aggressive inlining and unwrapping of nullable objects.
/// </summary>
sealed class UnusedComponentsCleanupTransformer : IOpenApiDocumentTransformer
{
	public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
	{
		if (document.Components?.Schemas is null || document.Components.Schemas.Count == 0)
		{
			return Task.CompletedTask;
		}

		// Step 1: Find all referenced schema IDs
		HashSet<string> usedSchemaIds = [];

		// Step 2: Scan all paths and operations for schema references
		if (document.Paths is not null)
		{
			foreach (KeyValuePair<string, IOpenApiPathItem> pathItem in document.Paths)
			{
				if (pathItem.Value is not OpenApiPathItem pathItemValue)
				{
					continue;
				}

				// Check parameters at path level
				if (pathItemValue.Parameters is not null)
				{
					foreach (IOpenApiParameter parameter in pathItemValue.Parameters)
					{
						if (parameter is OpenApiParameter openApiParameter && openApiParameter.Schema is not null)
						{
							CollectSchemaReferences(openApiParameter.Schema, usedSchemaIds, document);
						}
					}
				}

				// Check each operation
				foreach (KeyValuePair<HttpMethod, OpenApiOperation> operation in pathItemValue.Operations ?? [])
				{
					// Check operation parameters
					if (operation.Value.Parameters is not null)
					{
						foreach (IOpenApiParameter parameter in operation.Value.Parameters)
						{
							if (parameter is OpenApiParameter openApiParameter && openApiParameter.Schema is not null)
							{
								CollectSchemaReferences(openApiParameter.Schema, usedSchemaIds, document);
							}
						}
					}

					// Check request body
					if (operation.Value.RequestBody is OpenApiRequestBody requestBody)
					{
						if (requestBody.Content is not null)
						{
							foreach (KeyValuePair<string, OpenApiMediaType> content in requestBody.Content)
							{
								if (content.Value.Schema is not null)
								{
									CollectSchemaReferences(content.Value.Schema, usedSchemaIds, document);
								}
							}
						}
					}

					// Check responses
					if (operation.Value.Responses is not null)
					{
						foreach (KeyValuePair<string, IOpenApiResponse> response in operation.Value.Responses)
						{
							if (response.Value is OpenApiResponse responseValue && responseValue.Content is not null)
							{
								foreach (KeyValuePair<string, OpenApiMediaType> content in responseValue.Content)
								{
									if (content.Value.Schema is not null)
									{
										CollectSchemaReferences(content.Value.Schema, usedSchemaIds, document);
									}
								}
							}
						}
					}
				}
			}
		}

		// Step 3: Remove unused schemas
		List<string> schemasToRemove = [.. document.Components.Schemas.Keys.Where(schemaId => !usedSchemaIds.Contains(schemaId))];

		foreach (string schemaId in schemasToRemove)
		{
			document.Components.Schemas.Remove(schemaId);
		}

		return Task.CompletedTask;
	}

	/// <summary>
	/// Recursively collects all schema references from a schema and its nested elements.
	/// </summary>
	static void CollectSchemaReferences(IOpenApiSchema schema, HashSet<string> usedSchemaIds, OpenApiDocument document)
	{
		// Handle schema references
		if (schema is OpenApiSchemaReference schemaRef)
		{
			string? refId = schemaRef.Reference?.Id;
			if (!string.IsNullOrEmpty(refId))
			{
				// Add this reference if we haven't seen it before
				if (usedSchemaIds.Add(refId))
				{
					// Recursively process the referenced schema to find nested references
					if (document.Components?.Schemas?.TryGetValue(refId, out IOpenApiSchema? referencedSchema) == true)
					{
						CollectSchemaReferences(referencedSchema, usedSchemaIds, document);
					}
				}
			}
			return;
		}

		// Handle inline schemas
		if (schema is not OpenApiSchema openApiSchema)
		{
			return;
		}

		// Check properties for nested references
		if (openApiSchema.Properties is not null)
		{
			foreach (IOpenApiSchema propertySchema in openApiSchema.Properties.Values)
			{
				CollectSchemaReferences(propertySchema, usedSchemaIds, document);
			}
		}

		// Check array items
		if (openApiSchema.Items is not null)
		{
			CollectSchemaReferences(openApiSchema.Items, usedSchemaIds, document);
		}

		// Check allOf schemas
		if (openApiSchema.AllOf is not null)
		{
			foreach (IOpenApiSchema allOfSchema in openApiSchema.AllOf)
			{
				CollectSchemaReferences(allOfSchema, usedSchemaIds, document);
			}
		}

		// Check oneOf schemas
		if (openApiSchema.OneOf is not null)
		{
			foreach (IOpenApiSchema oneOfSchema in openApiSchema.OneOf)
			{
				CollectSchemaReferences(oneOfSchema, usedSchemaIds, document);
			}
		}

		// Check anyOf schemas
		if (openApiSchema.AnyOf is not null)
		{
			foreach (IOpenApiSchema anyOfSchema in openApiSchema.AnyOf)
			{
				CollectSchemaReferences(anyOfSchema, usedSchemaIds, document);
			}
		}

		// Check additionalProperties
		if (openApiSchema.AdditionalProperties is not null)
		{
			CollectSchemaReferences(openApiSchema.AdditionalProperties, usedSchemaIds, document);
		}
	}
}
