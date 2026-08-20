using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.PropertyEnhancer.Core;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.PropertyEnhancer;

/// <summary>
/// Converts inline value-or-null compositions to nullable schema types. Microsoft.OpenApi serializes these as
/// <c>type</c> plus <c>nullable: true</c> for OpenAPI 3.0 and as a type union for OpenAPI 3.1.
/// Nullable references remain composed because OpenAPI 3.0 does not allow nullable semantics on a Reference Object.
/// Pure-null composition branches are represented as <c>enum: [null]</c> so OpenAPI 3.0 output does not contain a
/// redundant, ineffective <c>nullable: true</c> keyword without a sibling <c>type</c>.
/// </summary>
sealed class NullableSchemaNormalizationTransformer : IOpenApiDocumentTransformer
{
	public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		HashSet<IOpenApiSchema> visitedSchemas = new(ReferenceEqualityComparer.Instance);
		HashSet<object> visitedContainers = new(ReferenceEqualityComparer.Instance);

		ProcessComponents(document, visitedSchemas, visitedContainers, cancellationToken);
		ProcessPathItems(document.Paths?.Values, document.Components, visitedSchemas, visitedContainers, cancellationToken);
		ProcessPathItems(document.Webhooks?.Values, document.Components, visitedSchemas, visitedContainers, cancellationToken);

		return Task.CompletedTask;
	}

	static void ProcessComponents(OpenApiDocument document, HashSet<IOpenApiSchema> visitedSchemas, HashSet<object> visitedContainers, CancellationToken cancellationToken)
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
		ProcessPathItems(components.PathItems?.Values, components, visitedSchemas, visitedContainers, cancellationToken);
		foreach (IOpenApiCallback callback in components.Callbacks?.Values ?? [])
		{
			ProcessCallback(callback, components, visitedSchemas, visitedContainers, cancellationToken);
		}
	}

	static void ProcessPathItems(IEnumerable<IOpenApiPathItem>? pathItems, OpenApiComponents? components, HashSet<IOpenApiSchema> visitedSchemas, HashSet<object> visitedContainers, CancellationToken cancellationToken)
	{
		foreach (IOpenApiPathItem pathItem in pathItems ?? [])
		{
			ProcessPathItem(pathItem, components, visitedSchemas, visitedContainers, cancellationToken);
		}
	}

	static void ProcessPathItem(IOpenApiPathItem pathItem, OpenApiComponents? components, HashSet<IOpenApiSchema> visitedSchemas, HashSet<object> visitedContainers, CancellationToken cancellationToken)
	{
		IOpenApiPathItem resolved = OpenApiSchemaHelper.ResolveReference(pathItem, components?.PathItems);
		if (!visitedContainers.Add(resolved))
		{
			return;
		}

		cancellationToken.ThrowIfCancellationRequested();
		foreach (IOpenApiParameter parameter in resolved.Parameters ?? [])
		{
			ProcessParameter(parameter, components, visitedSchemas, cancellationToken);
		}
		foreach (OpenApiOperation operation in resolved.Operations?.Values.AsEnumerable() ?? [])
		{
			ProcessOperation(operation, components, visitedSchemas, visitedContainers, cancellationToken);
		}
	}

	static void ProcessOperation(OpenApiOperation operation, OpenApiComponents? components, HashSet<IOpenApiSchema> visitedSchemas, HashSet<object> visitedContainers, CancellationToken cancellationToken)
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
		foreach (IOpenApiCallback callback in operation.Callbacks?.Values ?? [])
		{
			ProcessCallback(callback, components, visitedSchemas, visitedContainers, cancellationToken);
		}
	}

	static void ProcessCallback(IOpenApiCallback callback, OpenApiComponents? components, HashSet<IOpenApiSchema> visitedSchemas, HashSet<object> visitedContainers, CancellationToken cancellationToken)
	{
		IOpenApiCallback resolved = OpenApiSchemaHelper.ResolveReference(callback, components?.Callbacks);
		if (!visitedContainers.Add(resolved))
		{
			return;
		}

		ProcessPathItems(resolved.PathItems?.Values, components, visitedSchemas, visitedContainers, cancellationToken);
	}

	static void ProcessParameter(IOpenApiParameter parameter, OpenApiComponents? components, HashSet<IOpenApiSchema> visitedSchemas, CancellationToken cancellationToken)
	{
		IOpenApiParameter resolved = OpenApiSchemaHelper.ResolveReference(parameter, components?.Parameters);
		if (resolved is not OpenApiParameter openApiParameter)
		{
			return;
		}

		if (openApiParameter.Schema is not null)
		{
			openApiParameter.Schema = ProcessSchema(openApiParameter.Schema, visitedSchemas, cancellationToken);
		}
		ProcessContent(openApiParameter.Content, components, visitedSchemas, cancellationToken);
	}

	static void ProcessHeader(IOpenApiHeader header, OpenApiComponents? components, HashSet<IOpenApiSchema> visitedSchemas, CancellationToken cancellationToken)
	{
		IOpenApiHeader resolved = OpenApiSchemaHelper.ResolveReference(header, components?.Headers);
		if (resolved is not OpenApiHeader openApiHeader)
		{
			return;
		}

		if (openApiHeader.Schema is not null)
		{
			openApiHeader.Schema = ProcessSchema(openApiHeader.Schema, visitedSchemas, cancellationToken);
		}
		ProcessContent(openApiHeader.Content, components, visitedSchemas, cancellationToken);
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
		openApiSchema = NormalizeNullOnlySchema(openApiSchema);
		if (!visitedSchemas.Add(openApiSchema))
		{
			return openApiSchema;
		}

		ProcessSchemaDictionary(openApiSchema.Properties, visitedSchemas, cancellationToken);
		ProcessSchemaDictionary(openApiSchema.Definitions, visitedSchemas, cancellationToken);
		ProcessSchemaDictionary(openApiSchema.PatternProperties, visitedSchemas, cancellationToken);
		ProcessSchemaDictionary(openApiSchema.DependentSchemas, visitedSchemas, cancellationToken);
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
		if (openApiSchema.Contains is not null)
		{
			openApiSchema.Contains = ProcessSchema(openApiSchema.Contains, visitedSchemas, cancellationToken);
		}
		if (openApiSchema.PropertyNames is not null)
		{
			openApiSchema.PropertyNames = ProcessSchema(openApiSchema.PropertyNames, visitedSchemas, cancellationToken);
		}
		if (openApiSchema.UnevaluatedPropertiesSchema is not null)
		{
			openApiSchema.UnevaluatedPropertiesSchema = ProcessSchema(openApiSchema.UnevaluatedPropertiesSchema, visitedSchemas, cancellationToken);
		}
		if (openApiSchema.ContentSchema is not null)
		{
			openApiSchema.ContentSchema = ProcessSchema(openApiSchema.ContentSchema, visitedSchemas, cancellationToken);
		}
		if (openApiSchema.If is not null)
		{
			openApiSchema.If = ProcessSchema(openApiSchema.If, visitedSchemas, cancellationToken);
		}
		if (openApiSchema.Then is not null)
		{
			openApiSchema.Then = ProcessSchema(openApiSchema.Then, visitedSchemas, cancellationToken);
		}
		if (openApiSchema.Else is not null)
		{
			openApiSchema.Else = ProcessSchema(openApiSchema.Else, visitedSchemas, cancellationToken);
		}

		return openApiSchema;
	}

	static void ProcessSchemaDictionary(IDictionary<string, IOpenApiSchema>? schemas, HashSet<IOpenApiSchema> visitedSchemas, CancellationToken cancellationToken)
	{
		if (schemas is null)
		{
			return;
		}

		foreach (string key in schemas.Keys.ToArray())
		{
			schemas[key] = ProcessSchema(schemas[key], visitedSchemas, cancellationToken);
		}
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
		bool usesOneOf = wrapper.OneOf is { Count: 2 };
		IList<IOpenApiSchema>? composition = usesOneOf
			? wrapper.OneOf
			: wrapper.AnyOf is { Count: 2 }
				? wrapper.AnyOf
				: null;

		if (composition is null || HasSemanticSiblingKeywords(wrapper, usesOneOf))
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
			valueSchema.Type.Value.HasFlag(JsonSchemaType.Null) ||
			HasAssertionsThatApplyToNull(valueSchema))
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

	static OpenApiSchema NormalizeNullOnlySchema(OpenApiSchema schema)
	{
		if (!IsNullSchema(schema))
		{
			return schema;
		}

		OpenApiSchema normalized = (OpenApiSchema)schema.CreateShallowCopy();
		normalized.Type = null;
		normalized.Enum = [null!];
		return normalized;
	}

	static bool HasAssertionsThatApplyToNull(OpenApiSchema schema)
	{
		return schema.Const is not null ||
			schema.AllOf is { Count: > 0 } ||
			schema.OneOf is { Count: > 0 } ||
			schema.AnyOf is { Count: > 0 } ||
			schema.Not is not null ||
			schema.If is not null ||
			schema.Then is not null ||
			schema.Else is not null ||
			schema.DynamicRef is not null ||
			schema.UnrecognizedKeywords is { Count: > 0 };
	}

	static bool HasSemanticSiblingKeywords(OpenApiSchema schema, bool usesOneOf)
	{
		return schema.Type.HasValue ||
			schema.Format is not null ||
			schema.Maximum is not null ||
			schema.Minimum is not null ||
			schema.ExclusiveMaximum is not null ||
			schema.ExclusiveMinimum is not null ||
			schema.MultipleOf.HasValue ||
			schema.MaxLength.HasValue ||
			schema.MinLength.HasValue ||
			schema.Pattern is not null ||
			schema.MaxItems.HasValue ||
			schema.MinItems.HasValue ||
			schema.UniqueItems.HasValue ||
			schema.MaxProperties.HasValue ||
			schema.MinProperties.HasValue ||
			schema.Required is { Count: > 0 } ||
			schema.Enum is { Count: > 0 } ||
			schema.Properties is { Count: > 0 } ||
			schema.Items is not null ||
			schema.AdditionalProperties is not null ||
			!schema.AdditionalPropertiesAllowed ||
			schema.AllOf is { Count: > 0 } ||
			(usesOneOf ? schema.AnyOf is { Count: > 0 } : schema.OneOf is { Count: > 0 }) ||
			schema.Not is not null ||
			schema.Discriminator is not null ||
			schema.ReadOnly ||
			schema.WriteOnly ||
			schema.Deprecated ||
			schema.Xml is not null ||
			schema.ExternalDocs is not null ||
			schema.Example is not null ||
			schema.Examples is { Count: > 0 } ||
			schema.Default is not null ||
			schema.Extensions is { Count: > 0 } ||
			schema.Id is not null ||
			schema.Schema is not null ||
			schema.Anchor is not null ||
			schema.DynamicAnchor is not null ||
			schema.DynamicRef is not null ||
			schema.Vocabulary is { Count: > 0 } ||
			schema.Comment is not null ||
			schema.Definitions is { Count: > 0 } ||
			schema.Const is not null ||
			schema.PatternProperties is { Count: > 0 } ||
			schema.PropertyNames is not null ||
			schema.UnevaluatedPropertiesSchema is not null ||
			!schema.UnevaluatedProperties ||
			schema.DependentRequired is { Count: > 0 } ||
			schema.DependentSchemas is { Count: > 0 } ||
			schema.Contains is not null ||
			schema.MaxContains.HasValue ||
			schema.MinContains.HasValue ||
			schema.If is not null ||
			schema.Then is not null ||
			schema.Else is not null ||
			schema.ContentEncoding is not null ||
			schema.ContentMediaType is not null ||
			schema.ContentSchema is not null ||
			schema.UnrecognizedKeywords is { Count: > 0 } ||
			schema.Metadata is { Count: > 0 };
	}

	static bool IsNullSchema(OpenApiSchema schema)
	{
		return schema.Type == JsonSchemaType.Null &&
			!HasAssertionsThatApplyToNull(schema) &&
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
