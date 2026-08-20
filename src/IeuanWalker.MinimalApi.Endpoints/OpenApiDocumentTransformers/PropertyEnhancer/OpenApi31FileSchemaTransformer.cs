using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.PropertyEnhancer.Core;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.PropertyEnhancer;

/// <summary>
/// Applies the OpenAPI 3.1 representation for raw file content and removes URL-encoded alternatives from request
/// bodies that contain files. OpenAPI 3.0 continues to use <c>type: string, format: binary</c>.
/// </summary>
sealed class OpenApi31FileSchemaTransformer(bool enabled = true) : IOpenApiDocumentTransformer
{
	const string multipartFormData = "multipart/form-data";
	const string formUrlEncoded = "application/x-www-form-urlencoded";
	const string octetStream = "application/octet-stream";

	public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
	{
		if (!enabled)
		{
			return Task.CompletedTask;
		}

		cancellationToken.ThrowIfCancellationRequested();
		RemoveUrlEncodedFileRequestBodies(document, cancellationToken);

		HashSet<IOpenApiSchema> visited = new(ReferenceEqualityComparer.Instance);
		ProcessComponents(document.Components, visited, cancellationToken);
		ProcessPathItems(document.Paths?.Values, document.Components, visited, cancellationToken);
		ProcessPathItems(document.Webhooks?.Values, document.Components, visited, cancellationToken);

		return Task.CompletedTask;
	}

	static void RemoveUrlEncodedFileRequestBodies(OpenApiDocument document, CancellationToken cancellationToken)
	{
		ProcessRequestBodies(document.Paths?.Values, document.Components, cancellationToken);
		ProcessRequestBodies(document.Webhooks?.Values, document.Components, cancellationToken);

		foreach (IOpenApiRequestBody requestBody in document.Components?.RequestBodies?.Values ?? [])
		{
			RemoveUrlEncodedContent(requestBody, document.Components, cancellationToken);
		}
	}

	static void ProcessRequestBodies(IEnumerable<IOpenApiPathItem>? pathItems, OpenApiComponents? components, CancellationToken cancellationToken)
	{
		foreach (IOpenApiPathItem pathItem in pathItems ?? [])
		{
			IOpenApiPathItem resolvedPathItem = OpenApiSchemaHelper.ResolveReference(pathItem, components?.PathItems);
			foreach (OpenApiOperation operation in resolvedPathItem.Operations?.Values.AsEnumerable() ?? [])
			{
				if (operation.RequestBody is not null)
				{
					RemoveUrlEncodedContent(operation.RequestBody, components, cancellationToken);
				}
			}
		}
	}

	static void RemoveUrlEncodedContent(IOpenApiRequestBody requestBody, OpenApiComponents? components, CancellationToken cancellationToken)
	{
		IOpenApiRequestBody resolvedRequestBody = OpenApiSchemaHelper.ResolveReference(requestBody, components?.RequestBodies);
		if (resolvedRequestBody is not OpenApiRequestBody openApiRequestBody ||
			openApiRequestBody.Content is null ||
			!openApiRequestBody.Content.ContainsKey(multipartFormData) ||
			!openApiRequestBody.Content.TryGetValue(formUrlEncoded, out OpenApiMediaType? urlEncoded) ||
			urlEncoded.Schema is null)
		{
			return;
		}

		if (ContainsBinarySchema(urlEncoded.Schema, components?.Schemas, [], cancellationToken))
		{
			openApiRequestBody.Content.Remove(formUrlEncoded);
		}
	}

	static bool ContainsBinarySchema(IOpenApiSchema schema, IDictionary<string, IOpenApiSchema>? components, HashSet<IOpenApiSchema> visited, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		if (!visited.Add(schema))
		{
			return false;
		}

		if (schema is OpenApiSchemaReference { Reference.Id: { Length: > 0 } id })
		{
			return components?.TryGetValue(id, out IOpenApiSchema? referenced) == true &&
				ContainsBinarySchema(referenced, components, visited, cancellationToken);
		}

		if (!OpenApiSchemaHelper.TryAsOpenApiSchema(schema, out OpenApiSchema? openApiSchema) || openApiSchema is null)
		{
			return false;
		}

		return string.Equals(openApiSchema.Format, SchemaConstants.FormatBinary, StringComparison.Ordinal) ||
			ContainsBinarySchema(openApiSchema.Properties?.Values, components, visited, cancellationToken) ||
			ContainsBinarySchemaIfPresent(openApiSchema.Items, components, visited, cancellationToken) ||
			ContainsBinarySchema(openApiSchema.AllOf, components, visited, cancellationToken) ||
			ContainsBinarySchema(openApiSchema.OneOf, components, visited, cancellationToken) ||
			ContainsBinarySchema(openApiSchema.AnyOf, components, visited, cancellationToken) ||
			ContainsBinarySchemaIfPresent(openApiSchema.AdditionalProperties, components, visited, cancellationToken);
	}

	static bool ContainsBinarySchema(IEnumerable<IOpenApiSchema>? schemas, IDictionary<string, IOpenApiSchema>? components, HashSet<IOpenApiSchema> visited, CancellationToken cancellationToken) =>
		schemas?.Any(schema => ContainsBinarySchema(schema, components, visited, cancellationToken)) == true;

	static bool ContainsBinarySchemaIfPresent(IOpenApiSchema? schema, IDictionary<string, IOpenApiSchema>? components, HashSet<IOpenApiSchema> visited, CancellationToken cancellationToken) =>
		schema is not null && ContainsBinarySchema(schema, components, visited, cancellationToken);

	static void ProcessComponents(OpenApiComponents? components, HashSet<IOpenApiSchema> visited, CancellationToken cancellationToken)
	{
		foreach (IOpenApiSchema schema in components?.Schemas?.Values ?? [])
		{
			ProcessSchema(schema, visited, cancellationToken);
		}
		foreach (IOpenApiParameter parameter in components?.Parameters?.Values ?? [])
		{
			ProcessParameter(parameter, components, visited, cancellationToken);
		}
		foreach (IOpenApiRequestBody requestBody in components?.RequestBodies?.Values ?? [])
		{
			ProcessRequestBody(requestBody, components, visited, cancellationToken);
		}
		foreach (IOpenApiResponse response in components?.Responses?.Values ?? [])
		{
			ProcessResponse(response, components, visited, cancellationToken);
		}
		foreach (IOpenApiHeader header in components?.Headers?.Values ?? [])
		{
			ProcessHeader(header, components, visited, cancellationToken);
		}
	}

	static void ProcessPathItems(IEnumerable<IOpenApiPathItem>? pathItems, OpenApiComponents? components, HashSet<IOpenApiSchema> visited, CancellationToken cancellationToken)
	{
		foreach (IOpenApiPathItem pathItem in pathItems ?? [])
		{
			IOpenApiPathItem resolvedPathItem = OpenApiSchemaHelper.ResolveReference(pathItem, components?.PathItems);
			foreach (IOpenApiParameter parameter in resolvedPathItem.Parameters ?? [])
			{
				ProcessParameter(parameter, components, visited, cancellationToken);
			}
			foreach (OpenApiOperation operation in resolvedPathItem.Operations?.Values.AsEnumerable() ?? [])
			{
				foreach (IOpenApiParameter parameter in operation.Parameters ?? [])
				{
					ProcessParameter(parameter, components, visited, cancellationToken);
				}
				if (operation.RequestBody is not null)
				{
					ProcessRequestBody(operation.RequestBody, components, visited, cancellationToken);
				}
				foreach (IOpenApiResponse response in operation.Responses?.Values.AsEnumerable() ?? [])
				{
					ProcessResponse(response, components, visited, cancellationToken);
				}
			}
		}
	}

	static void ProcessParameter(IOpenApiParameter parameter, OpenApiComponents? components, HashSet<IOpenApiSchema> visited, CancellationToken cancellationToken)
	{
		IOpenApiParameter resolved = OpenApiSchemaHelper.ResolveReference(parameter, components?.Parameters);
		if (resolved is OpenApiParameter openApiParameter)
		{
			ProcessSchema(openApiParameter.Schema, visited, cancellationToken);
			ProcessContent(openApiParameter.Content, visited, cancellationToken);
		}
	}

	static void ProcessRequestBody(IOpenApiRequestBody requestBody, OpenApiComponents? components, HashSet<IOpenApiSchema> visited, CancellationToken cancellationToken)
	{
		IOpenApiRequestBody resolved = OpenApiSchemaHelper.ResolveReference(requestBody, components?.RequestBodies);
		if (resolved is OpenApiRequestBody openApiRequestBody)
		{
			ProcessContent(openApiRequestBody.Content, visited, cancellationToken);
		}
	}

	static void ProcessResponse(IOpenApiResponse response, OpenApiComponents? components, HashSet<IOpenApiSchema> visited, CancellationToken cancellationToken)
	{
		IOpenApiResponse resolved = OpenApiSchemaHelper.ResolveReference(response, components?.Responses);
		if (resolved is OpenApiResponse openApiResponse)
		{
			ProcessContent(openApiResponse.Content, visited, cancellationToken);
			foreach (IOpenApiHeader header in openApiResponse.Headers?.Values ?? [])
			{
				ProcessHeader(header, components, visited, cancellationToken);
			}
		}
	}

	static void ProcessHeader(IOpenApiHeader header, OpenApiComponents? components, HashSet<IOpenApiSchema> visited, CancellationToken cancellationToken)
	{
		IOpenApiHeader resolved = OpenApiSchemaHelper.ResolveReference(header, components?.Headers);
		if (resolved is OpenApiHeader openApiHeader)
		{
			ProcessSchema(openApiHeader.Schema, visited, cancellationToken);
			ProcessContent(openApiHeader.Content, visited, cancellationToken);
		}
	}

	static void ProcessContent(IDictionary<string, OpenApiMediaType>? content, HashSet<IOpenApiSchema> visited, CancellationToken cancellationToken)
	{
		foreach (OpenApiMediaType mediaType in content?.Values ?? [])
		{
			ProcessSchema(mediaType.Schema, visited, cancellationToken);
		}
	}

	static void ProcessSchema(IOpenApiSchema? schema, HashSet<IOpenApiSchema> visited, CancellationToken cancellationToken)
	{
		if (schema is null || schema is OpenApiSchemaReference || !visited.Add(schema))
		{
			return;
		}

		cancellationToken.ThrowIfCancellationRequested();
		if (!OpenApiSchemaHelper.TryAsOpenApiSchema(schema, out OpenApiSchema? openApiSchema) || openApiSchema is null)
		{
			return;
		}

		if (string.Equals(openApiSchema.Format, SchemaConstants.FormatBinary, StringComparison.Ordinal))
		{
			bool allowsNull = openApiSchema.Type?.HasFlag(JsonSchemaType.Null) == true;
			openApiSchema.Type = null;
			openApiSchema.Format = null;

			if (allowsNull)
			{
				openApiSchema.OneOf =
				[
					CreateRawFileSchema(excludeNull: true),
					new OpenApiSchema { Type = JsonSchemaType.Null }
				];
			}
			else
			{
				openApiSchema.ContentMediaType ??= octetStream;
			}
		}

		ProcessSchemas(openApiSchema.Properties?.Values, visited, cancellationToken);
		ProcessSchema(openApiSchema.Items, visited, cancellationToken);
		ProcessSchemas(openApiSchema.AllOf, visited, cancellationToken);
		ProcessSchemas(openApiSchema.OneOf, visited, cancellationToken);
		ProcessSchemas(openApiSchema.AnyOf, visited, cancellationToken);
		ProcessSchema(openApiSchema.AdditionalProperties, visited, cancellationToken);
		ProcessSchema(openApiSchema.Not, visited, cancellationToken);
		ProcessSchema(openApiSchema.Contains, visited, cancellationToken);
		ProcessSchema(openApiSchema.PropertyNames, visited, cancellationToken);
		ProcessSchema(openApiSchema.ContentSchema, visited, cancellationToken);
	}

	static OpenApiSchema CreateRawFileSchema(bool excludeNull) => new()
	{
		ContentMediaType = octetStream,
		Not = excludeNull ? new OpenApiSchema { Type = JsonSchemaType.Null } : null
	};

	static void ProcessSchemas(IEnumerable<IOpenApiSchema>? schemas, HashSet<IOpenApiSchema> visited, CancellationToken cancellationToken)
	{
		foreach (IOpenApiSchema schema in schemas ?? [])
		{
			ProcessSchema(schema, visited, cancellationToken);
		}
	}
}
