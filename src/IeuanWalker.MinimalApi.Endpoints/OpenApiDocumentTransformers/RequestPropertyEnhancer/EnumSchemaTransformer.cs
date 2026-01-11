using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer.Core;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer;

/// <summary>
/// Transforms OpenAPI document by enriching enum schemas with value information and member names.
/// </summary>
sealed class EnumSchemaTransformer : IOpenApiDocumentTransformer
{
	public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
	{
		if (document.Components?.Schemas is null)
		{
			return Task.CompletedTask;
		}

		foreach (KeyValuePair<string, IOpenApiSchema> schemaEntry in document.Components.Schemas)
		{
			if (schemaEntry.Value is not OpenApiSchema schema)
			{
				continue;
			}

			Type? enumType = SchemaTypeResolver.GetEnumType(schemaEntry.Key);
			if (enumType is null)
			{
				continue;
			}

			// Use shared enum enrichment method from OpenApiSchemaHelper
			OpenApiSchemaHelper.EnrichSchemaWithEnumValues(schema, enumType, forStringSchema: false);
		}

		return Task.CompletedTask;
	}
}
