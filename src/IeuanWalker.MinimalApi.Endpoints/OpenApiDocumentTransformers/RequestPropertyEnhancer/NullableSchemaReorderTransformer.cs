using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer.Core;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer;

/// <summary>
/// Reorders oneOf/anyOf/allOf schema arrays to ensure nullable markers appear last.
/// </summary>
sealed class NullableSchemaReorderTransformer : IOpenApiDocumentTransformer
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
	}

	static void ProcessPaths(OpenApiDocument document, HashSet<IOpenApiSchema> visitedSchemas, CancellationToken cancellationToken)
	{
		if (document.Paths is null)
		{
			return;
		}

		foreach (OpenApiPathItem pathItem in document.Paths.Values.OfType<OpenApiPathItem>())
		{
			cancellationToken.ThrowIfCancellationRequested();

			ProcessParameterCollection(pathItem.Parameters, document, visitedSchemas, cancellationToken);

			if (pathItem.Operations is null)
			{
				continue;
			}

			foreach (OpenApiOperation operation in pathItem.Operations.Values)
			{
				ProcessParameterCollection(operation.Parameters, document, visitedSchemas, cancellationToken);
				ProcessRequestBody(operation.RequestBody, document, visitedSchemas, cancellationToken);

				if (operation.Responses is null)
				{
					continue;
				}

				foreach (IOpenApiResponse response in operation.Responses.Values)
				{
					ProcessResponse(response, document, visitedSchemas, cancellationToken);
				}
			}
		}
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
		IOpenApiParameter resolved = ResolveReference(parameter, document.Components?.Parameters);

		if (resolved is not OpenApiParameter openApiParameter || openApiParameter.Schema is null)
		{
			return;
		}

		ProcessSchema(openApiParameter.Schema, document, visitedSchemas, cancellationToken);
	}

	static void ProcessHeader(IOpenApiHeader header, OpenApiDocument document, HashSet<IOpenApiSchema> visitedSchemas, CancellationToken cancellationToken)
	{
		IOpenApiHeader resolved = ResolveReference(header, document.Components?.Headers);

		if (resolved is not OpenApiHeader openApiHeader || openApiHeader.Schema is null)
		{
			return;
		}

		ProcessSchema(openApiHeader.Schema, document, visitedSchemas, cancellationToken);
	}

	static void ProcessRequestBody(IOpenApiRequestBody? requestBody, OpenApiDocument document, HashSet<IOpenApiSchema> visitedSchemas, CancellationToken cancellationToken)
	{
		if (requestBody is null)
		{
			return;
		}

		IOpenApiRequestBody resolved = ResolveReference(requestBody, document.Components?.RequestBodies);

		if (resolved is not OpenApiRequestBody openApiRequestBody)
		{
			return;
		}

		ProcessContent(openApiRequestBody.Content, document, visitedSchemas, cancellationToken);
	}

	static void ProcessResponse(IOpenApiResponse response, OpenApiDocument document, HashSet<IOpenApiSchema> visitedSchemas, CancellationToken cancellationToken)
	{
		IOpenApiResponse resolved = ResolveReference(response, document.Components?.Responses);

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

		if (openApiSchema.Properties is not null)
		{
			foreach (IOpenApiSchema propertySchema in openApiSchema.Properties.Values)
			{
				ProcessSchema(propertySchema, document, visitedSchemas, cancellationToken);
			}
		}

		if (openApiSchema.Items is not null)
		{
			ProcessSchema(openApiSchema.Items, document, visitedSchemas, cancellationToken);
		}

		if (openApiSchema.AllOf is not null)
		{
			foreach (IOpenApiSchema child in openApiSchema.AllOf)
			{
				ProcessSchema(child, document, visitedSchemas, cancellationToken);
			}
		}

		if (openApiSchema.OneOf is not null)
		{
			foreach (IOpenApiSchema child in openApiSchema.OneOf)
			{
				ProcessSchema(child, document, visitedSchemas, cancellationToken);
			}
		}

		if (openApiSchema.AnyOf is not null)
		{
			foreach (IOpenApiSchema child in openApiSchema.AnyOf)
			{
				ProcessSchema(child, document, visitedSchemas, cancellationToken);
			}
		}

		if (openApiSchema.AdditionalProperties is not null)
		{
			ProcessSchema(openApiSchema.AdditionalProperties, document, visitedSchemas, cancellationToken);
		}

		if (openApiSchema.Not is not null)
		{
			ProcessSchema(openApiSchema.Not, document, visitedSchemas, cancellationToken);
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
		if (!OpenApiSchemaHelper.TryAsOpenApiSchema(schema, out OpenApiSchema? openApiSchema) || openApiSchema is null)
		{
			return false;
		}

		bool hasNullableExtension = openApiSchema.Extensions?.ContainsKey(SchemaConstants.NullableExtension) == true;
		bool hasTypeInformation = openApiSchema.Type.HasValue;
		bool hasCompositeChildren = (openApiSchema.AllOf?.Count ?? 0) > 0 || (openApiSchema.OneOf?.Count ?? 0) > 0 || (openApiSchema.AnyOf?.Count ?? 0) > 0;
		bool hasSchemaMembers = (openApiSchema.Properties?.Count ?? 0) > 0 || openApiSchema.Items is not null || openApiSchema.AdditionalProperties is not null || openApiSchema.Not is not null;

		return openApiSchema.Type == JsonSchemaType.Null || (hasNullableExtension && !hasTypeInformation && !hasCompositeChildren && !hasSchemaMembers);
	}

	static T ResolveReference<T>(T referenceable, IDictionary<string, T>? componentSection) where T : class
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
}
