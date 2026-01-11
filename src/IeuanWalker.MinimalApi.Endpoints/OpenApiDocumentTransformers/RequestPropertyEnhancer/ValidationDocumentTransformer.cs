using IeuanWalker.MinimalApi.Endpoints.Extensions;
using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer.Core;
using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer.Validation;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using ValidationRule = IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer.Validation.ValidationRule;

namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer;

/// <summary>
/// OpenAPI document transformer that applies validation rules from both WithValidation and FluentValidation to schemas.
/// <para>
/// This transformer runs in the following steps:
/// <list type="number">
///   <item>Auto-discover FluentValidation rules (if enabled)</item>
///   <item>Auto-discover DataAnnotations validation rules (if enabled)</item>
///   <item>Discover manual validation rules from .WithValidationRules&lt;T&gt;()</item>
///   <item>Apply collected rules to OpenAPI schemas (request bodies)</item>
///   <item>Apply collected rules to query/path parameters</item>
/// </list>
/// </para>
/// </summary>
sealed partial class ValidationDocumentTransformer : IOpenApiDocumentTransformer
{
	/// <summary>
	/// Gets or sets whether to auto-document FluentValidation rules. Default is true.
	/// </summary>
	public bool AutoDocumentFluentValidation { get; set; } = true;

	/// <summary>
	/// Gets or sets whether to auto-document DataAnnotation validation rules. Default is true.
	/// </summary>
	public bool AutoDocumentDataAnnotationValidation { get; set; } = true;

	/// <summary>
	/// Gets or sets whether to append validation rules to property descriptions in OpenAPI documentation. Default is true.
	/// </summary>
	public bool AppendRulesToPropertyDescription { get; set; } = true;

	/// <inheritdoc />
	public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
	{
		Dictionary<Type, (List<ValidationRule>, bool appendRulesToPropertyDescription)> allValidationRules = [];

		if (AutoDocumentFluentValidation)
		{
			DiscoverFluentValidationRules(context, allValidationRules);
		}

		if (AutoDocumentDataAnnotationValidation)
		{
			DiscoverDataAnnotationValidationRules(context, allValidationRules);
		}

		DiscoverManualValidationRules(context, allValidationRules);

		foreach (KeyValuePair<Type, (List<ValidationRule> rules, bool appendRulesToPropertyDescription)> kvp in allValidationRules)
		{
			Type requestType = kvp.Key;
			List<ValidationRule> rules = kvp.Value.rules;
			bool typeAppendRulesToPropertyDescription = kvp.Value.appendRulesToPropertyDescription;

			ApplyValidationToSchemas(document, requestType, rules, typeAppendRulesToPropertyDescription, AppendRulesToPropertyDescription);
		}

		Dictionary<string, Type> endpointToRequestType = BuildEndpointToRequestTypeMappingWithValidation(context, allValidationRules);
		ApplyValidationToParameters(document, allValidationRules, endpointToRequestType, AppendRulesToPropertyDescription);

		return Task.CompletedTask;
	}

	static void ApplyValidationToSchemas(OpenApiDocument document, Type requestType, List<ValidationRule> rules, bool typeAppendRulesToPropertyDescription, bool appendRulesToPropertyDescription)
	{
		string schemaName = requestType.FullName ?? requestType.Name;

		if (document.Components?.Schemas is null || !document.Components.Schemas.TryGetValue(schemaName, out IOpenApiSchema? schemaInterface))
		{
			return;
		}

		if (!OpenApiSchemaHelper.TryAsOpenApiSchema(schemaInterface, out OpenApiSchema? schema) || schema?.Properties is null)
		{
			return;
		}

		List<ValidationRule> topLevelRules = [];
		Dictionary<string, List<ValidationRule>> nestedRulesBySchema = [];

		foreach (ValidationRule rule in rules)
		{
			if (rule.PropertyName.Contains('.') || rule.PropertyName.Contains("[*]"))
			{
				(string targetSchemaPath, string nestedPropertyName) = ExtractNestedPath(rule.PropertyName);

				if (!nestedRulesBySchema.TryGetValue(targetSchemaPath, out List<ValidationRule>? value))
				{
					value = [];
					nestedRulesBySchema[targetSchemaPath] = value;
				}

				ValidationRule nestedRule = rule with { PropertyName = nestedPropertyName };
				value.Add(nestedRule);
			}
			else
			{
				topLevelRules.Add(rule);
			}
		}

		ApplyValidationToProperties(schema, topLevelRules, typeAppendRulesToPropertyDescription, appendRulesToPropertyDescription, document);

		foreach (KeyValuePair<string, List<ValidationRule>> nestedRules in nestedRulesBySchema)
		{
			ApplyNestedValidation(document, schema, nestedRules.Key, nestedRules.Value, typeAppendRulesToPropertyDescription, appendRulesToPropertyDescription);
		}
	}

	static (string schemaPath, string propertyName) ExtractNestedPath(string fullPath)
	{
		int lastDot = fullPath.LastIndexOf('.');
		if (lastDot == -1)
		{
			return (string.Empty, fullPath);
		}

		string schemaPath = fullPath[..lastDot];
		string propertyName = fullPath[(lastDot + 1)..];

		return (schemaPath, propertyName);
	}

	static void ApplyNestedValidation(
		OpenApiDocument document,
		OpenApiSchema parentSchema,
		string nestedPath,
		List<ValidationRule> rules,
		bool typeAppendRulesToPropertyDescription,
		bool appendRulesToPropertyDescription)
	{
		string[] pathParts = nestedPath.Split('.');
		IOpenApiSchema? currentSchemaInterface = parentSchema;

		foreach (string part in pathParts)
		{
			if (!OpenApiSchemaHelper.TryAsOpenApiSchema(currentSchemaInterface, out OpenApiSchema? currentSchema) ||
				currentSchema?.Properties is null)
			{
				return;
			}

			bool isArray = part.EndsWith("[*]");
			string propertyName = isArray ? part[..^3] : part;
			string propertyKey = propertyName.ToCamelCase();

			if (!currentSchema.Properties.TryGetValue(propertyKey, out IOpenApiSchema? propertySchema))
			{
				return;
			}

			if (isArray)
			{
				if (propertySchema is OpenApiSchema arraySchema && arraySchema.Items is not null)
				{
					currentSchemaInterface = arraySchema.Items;
				}
				else if (propertySchema is OpenApiSchema oneOfSchema && oneOfSchema.OneOf?.Count > 0)
				{
					IOpenApiSchema? arraySchemaInOneOf = oneOfSchema.OneOf
						.OfType<OpenApiSchema>()
						.FirstOrDefault(s => s.Type == JsonSchemaType.Array && s.Items is not null);

					if (arraySchemaInOneOf is OpenApiSchema foundArraySchema)
					{
						currentSchemaInterface = foundArraySchema.Items;
					}
					else
					{
						return;
					}
				}
				else
				{
					return;
				}
			}
			else
			{
				currentSchemaInterface = propertySchema;
			}
		}

		if (currentSchemaInterface is null)
		{
			return;
		}

		OpenApiSchema? targetSchema = ResolveSchemaReference(currentSchemaInterface, document);

		if (targetSchema is not null)
		{
			ApplyValidationToProperties(targetSchema, rules, typeAppendRulesToPropertyDescription, appendRulesToPropertyDescription, document);
		}
	}

	static OpenApiSchema? ResolveSchemaReference(IOpenApiSchema schemaInterface, OpenApiDocument document)
	{
		if (schemaInterface is OpenApiSchemaReference schemaRef)
		{
			string refId = schemaRef.Reference?.Id ?? string.Empty;
			if (document.Components?.Schemas is not null &&
				document.Components.Schemas.TryGetValue(refId, out IOpenApiSchema? referencedSchemaInterface) &&
				referencedSchemaInterface is OpenApiSchema referencedSchema)
			{
				return referencedSchema;
			}
			return null;
		}

		if (OpenApiSchemaHelper.TryAsOpenApiSchema(schemaInterface, out OpenApiSchema? schema) && schema?.AllOf?.Count > 0)
		{
			foreach (IOpenApiSchema allOfSchema in schema.AllOf)
			{
				OpenApiSchema? resolved = ResolveSchemaReference(allOfSchema, document);
				if (resolved is not null)
				{
					return resolved;
				}
			}
		}

		return schemaInterface as OpenApiSchema;
	}

	static void ApplyValidationToProperties(
		OpenApiSchema schema,
		List<ValidationRule> rules,
		bool typeAppendRulesToPropertyDescription,
		bool appendRulesToPropertyDescription,
		OpenApiDocument document)
	{
		if (schema.Properties is null)
		{
			return;
		}

		IEnumerable<IGrouping<string, ValidationRule>> rulesByProperty = rules.GroupBy(r => r.PropertyName);

		List<string> requiredProperties = [];

		foreach (IGrouping<string, ValidationRule> propertyRules in rulesByProperty)
		{
			string propertyKey = propertyRules.Key.ToCamelCase();

			bool hasRequiredRule = propertyRules.Any(r => r is RequiredRule);
			if (hasRequiredRule)
			{
				requiredProperties.Add(propertyKey);
			}

			if (propertyRules.Any() && schema.Properties.TryGetValue(propertyKey, out IOpenApiSchema? propertySchemaInterface))
			{
				if (OpenApiSchemaHelper.IsEnumSchemaReference(propertySchemaInterface, document) ||
					OpenApiSchemaHelper.IsInlineEnumSchema(propertySchemaInterface))
				{
					continue;
				}

				schema.Properties[propertyKey] = CreateInlineSchemaWithAllValidation(propertySchemaInterface, [.. propertyRules], typeAppendRulesToPropertyDescription, appendRulesToPropertyDescription, document);
			}
		}

		if (requiredProperties.Count > 0)
		{
			HashSet<string> allRequired = schema.Required is not null ? [.. schema.Required] : [];

			foreach (string prop in requiredProperties)
			{
				allRequired.Add(prop);
			}

			schema.Required = allRequired;
		}

		foreach (KeyValuePair<string, IOpenApiSchema> property in schema.Properties.ToList())
		{
			if (rulesByProperty.Any(g => g.Key.ToCamelCase() == property.Key))
			{
				continue;
			}

			if (OpenApiSchemaHelper.IsEnumSchemaReference(property.Value, document) ||
				OpenApiSchemaHelper.IsInlineEnumSchema(property.Value))
			{
				continue;
			}

			schema.Properties[property.Key] = property.Value;
		}
	}

	static Dictionary<string, Type> BuildEndpointToRequestTypeMappingWithValidation(
		OpenApiDocumentTransformerContext context,
		Dictionary<Type, (List<ValidationRule> rules, bool appendRulesToPropertyDescription)> allValidationRules)
	{
		// Use the shared helper with a filter that only includes types with validation rules
		Dictionary<string, Type> mapping = EndpointRequestTypeMapper.BuildEndpointToRequestTypeMapping(
			context,
			filterPredicate: paramType => allValidationRules.ContainsKey(paramType));

		// Log any route pattern collisions (this is validation-specific behavior)
		// Note: The shared helper already prevents duplicate mappings, but we can't detect collisions there
		// For now, we accept this limitation as the collision detection was only informational

		return mapping;
	}

	[LoggerMessage(Level = LogLevel.Warning, Message = "Route pattern '{RoutePattern}' is shared by multiple endpoints with different request types. Using validation rules from '{FirstType}', ignoring '{SecondType}'. Consider using different route patterns or HTTP methods to avoid validation rule conflicts.")]
	static partial void LogRoutePatternCollision(ILogger logger, string routePattern, string firstType, string secondType);

	static void ApplyValidationToParameters(OpenApiDocument document, Dictionary<Type, (List<ValidationRule> rules, bool appendRulesToPropertyDescription)> allValidationRules, Dictionary<string, Type> endpointToRequestType, bool appendRulesToPropertyDescription)
	{
		if (document.Paths is null || allValidationRules.Count == 0)
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
				pathRequestType = EndpointRequestTypeMapper.ResolveRequestTypeForPath(pathPattern, endpointToRequestType);
				pathMatchCache[pathPattern] = pathRequestType;
			}

			if (pathRequestType is null || !allValidationRules.TryGetValue(pathRequestType, out (List<ValidationRule> rules, bool appendRulesToPropertyDescription) validationInfo))
			{
				continue;
			}

			List<ValidationRule> rules = validationInfo.rules;
			if (rules.Count == 0)
			{
				continue;
			}

			bool typeAppendRulesToPropertyDescription = validationInfo.appendRulesToPropertyDescription;

			Dictionary<string, List<ValidationRule>> rulesByProperty = rules
				.GroupBy(r => r.PropertyName, StringComparer.OrdinalIgnoreCase)
				.ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

			foreach (KeyValuePair<HttpMethod, OpenApiOperation> operation in pathItemValue.Operations ?? [])
			{
				if (operation.Value.Parameters is null || operation.Value.Parameters.Count == 0)
				{
					continue;
				}

				foreach (OpenApiParameter parameter in operation.Value.Parameters.Cast<OpenApiParameter>())
				{
					if (string.IsNullOrEmpty(parameter.Name))
					{
						continue;
					}

					if (!rulesByProperty.TryGetValue(parameter.Name, out List<ValidationRule>? propertyRules))
					{
						continue;
					}

					bool hasRequiredRule = propertyRules.Any(r => r is RequiredRule);
					if (hasRequiredRule)
					{
						parameter.Required = true;
					}

					if (propertyRules.Count > 0 && parameter.Schema is not null)
					{
						if (OpenApiSchemaHelper.IsEnumSchemaReference(parameter.Schema, document) ||
							OpenApiSchemaHelper.IsInlineEnumSchema(parameter.Schema))
						{
							continue;
						}

						parameter.Schema = CreateInlineSchemaWithAllValidation(parameter.Schema, [.. propertyRules], typeAppendRulesToPropertyDescription, appendRulesToPropertyDescription, document);
					}
				}
			}
		}
	}
}
