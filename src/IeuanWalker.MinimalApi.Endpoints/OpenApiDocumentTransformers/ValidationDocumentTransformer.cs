using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Nodes;
using IeuanWalker.MinimalApi.Endpoints.Extensions;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Routing;
using Microsoft.OpenApi;

namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers;

/// <summary>
/// OpenAPI document transformer that applies validation rules from both WithValidation and FluentValidation to schemas
/// </summary>
sealed partial class ValidationDocumentTransformer : IOpenApiDocumentTransformer
{
	public bool AutoDocumentFluentValdation { get; set; } = true;
	public bool AutoDocumentDataAnnotationValdation { get; set; } = true;
	public bool AppendRulesToPropertyDescription { get; set; } = true;
	public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
	{
		// Dictionary to track all request types and their validation rules (from both manual and FluentValidation)
		Dictionary<Type, (List<Validation.ValidationRule>, bool appendRulesToPropertyDescription)> allValidationRules = [];

		// Step 1: Discover FluentValidation rules
		if (AutoDocumentFluentValdation)
		{
			DiscoverFluentValidationRules(context, allValidationRules);
		}

		// Step 2: Discover DataAnnotationValidation rules
		if (AutoDocumentDataAnnotationValdation)
		{
			DiscoverDataAnnotationValidationRules(context, allValidationRules);
		}

		// Step 3: Discover manual WithValidation rules (these override FluentValidation per property)
		DiscoverManualValidationRules(context, allValidationRules);

		// Step 4: Apply all collected rules to OpenAPI schemas
		foreach (KeyValuePair<Type, (List<Validation.ValidationRule> rules, bool appendRulesToPropertyDescription)> kvp in allValidationRules)
		{
			Type requestType = kvp.Key;
			List<Validation.ValidationRule> rules = kvp.Value.rules;
			bool typeAppendRulesToPropertyDescription = kvp.Value.appendRulesToPropertyDescription;

			ApplyValidationToSchemas(document, requestType, rules, typeAppendRulesToPropertyDescription, AppendRulesToPropertyDescription);
		}

		// Step 5: Build a mapping from endpoints to their request types
		Dictionary<string, Type> endpointToRequestType = BuildEndpointToRequestTypeMapping(context, allValidationRules);

		// Step 6: Apply validation to query/path parameters (for endpoints using RequestAsParameters)
		foreach (KeyValuePair<Type, (List<Validation.ValidationRule> rules, bool appendRulesToPropertyDescription)> kvp in allValidationRules)
		{
			Type requestType = kvp.Key;
			List<Validation.ValidationRule> rules = kvp.Value.rules;
			bool typeAppendRulesToPropertyDescription = kvp.Value.appendRulesToPropertyDescription;

			ApplyValidationToParameters(document, requestType, rules, endpointToRequestType, typeAppendRulesToPropertyDescription, AppendRulesToPropertyDescription);
		}

		return Task.CompletedTask;
	}

	static void ApplyValidationToSchemas(OpenApiDocument document, Type requestType, List<Validation.ValidationRule> rules, bool typeAppendRulesToPropertyDescription, bool appendRulesToPropertyDescription)
	{
		// Get the schema name for the request type
		string schemaName = requestType.FullName ?? requestType.Name;

		// Find the schema in components
		if (document.Components?.Schemas is null || !document.Components.Schemas.TryGetValue(schemaName, out IOpenApiSchema? schemaInterface))
		{
			return;
		}

		if (schemaInterface is not OpenApiSchema schema || schema.Properties is null)
		{
			return;
		}

		// Group rules by property name
		IEnumerable<IGrouping<string, Validation.ValidationRule>> rulesByProperty = rules.GroupBy(r => r.PropertyName);

		// Track required properties
		List<string> requiredProperties = [];

		// Apply validation rules to properties
		foreach (IGrouping<string, Validation.ValidationRule> propertyRules in rulesByProperty)
		{
			string propertyKey = propertyRules.Key.ToCamelCase();

			// Check if any rule for this property is RequiredRule
			bool hasRequiredRule = propertyRules.Any(r => r is Validation.RequiredRule);
			if (hasRequiredRule)
			{
				requiredProperties.Add(propertyKey);
			}

			// Check if there are any rules other than RequiredRule and DescriptionRule
			// If only RequiredRule/DescriptionRule exist, we don't need to modify the schema inline
			// (RequiredRule is handled by adding to required array, DescriptionRule doesn't apply to nested objects)
			bool hasOtherRules = propertyRules.Any(r => r is not Validation.RequiredRule and not Validation.DescriptionRule);

			// Only create inline schema if there are validation rules other than just Required
			// This preserves $ref for nested objects that only have Required validation
			// ALSO preserve $ref for enum types - enum properties should reference the enum schema, not inline it
			if (hasOtherRules)
			{
				if (schema.Properties.TryGetValue(propertyKey, out IOpenApiSchema? propertySchemaInterface))
				{
					// Check if this property is a reference to an enum schema
					// Enum schemas should NOT be inlined - we preserve the $ref to maintain clean schema structure
					if (IsEnumSchemaReference(propertySchemaInterface, document))
					{
						// Don't inline enum properties - preserve the $ref
						// EnumRule validation is redundant since the enum schema itself defines valid values
						continue;
					}

					// Create inline schema with all validation constraints for this property
					schema.Properties[propertyKey] = CreateInlineSchemaWithAllValidation(propertySchemaInterface, [.. propertyRules], typeAppendRulesToPropertyDescription, appendRulesToPropertyDescription);
				}
			}
		}

		// Set required properties on the schema
		if (requiredProperties.Count > 0)
		{
			schema.Required = new HashSet<string>(requiredProperties);
		}
	}

	/// <summary>
	/// Builds a mapping from OpenAPI endpoint paths to their associated request types.
	/// This enables matching validation rules to the correct endpoint when multiple endpoints
	/// use parameters with the same name.
	/// </summary>
	static Dictionary<string, Type> BuildEndpointToRequestTypeMapping(OpenApiDocumentTransformerContext context, Dictionary<Type, (List<Validation.ValidationRule> rules, bool appendRulesToPropertyDescription)> allValidationRules)
	{
		Dictionary<string, Type> mapping = new(StringComparer.OrdinalIgnoreCase);

		// Get all endpoints
		if (context.ApplicationServices.GetService(typeof(EndpointDataSource)) is not EndpointDataSource endpointDataSource)
		{
			return mapping;
		}

		foreach (Microsoft.AspNetCore.Http.Endpoint endpoint in endpointDataSource.Endpoints)
		{
			if (endpoint is not RouteEndpoint routeEndpoint)
			{
				continue;
			}

			// Get the route pattern - this is the full pattern including any group prefixes
			string? routePattern = routeEndpoint.RoutePattern.RawText;
			if (string.IsNullOrEmpty(routePattern))
			{
				continue;
			}

			// Look for the handler method in metadata (usually RuntimeMethodInfo)
			MethodInfo? handlerMethod = routeEndpoint.Metadata.OfType<MethodInfo>().FirstOrDefault();
			if (handlerMethod is null)
			{
				continue;
			}

			// Get the parameters of the handler method
			ParameterInfo[] parameters = handlerMethod.GetParameters();

			// Try to find a parameter that matches one of our request types
			Type? matchingParamType = parameters
				.Select(param => param.ParameterType)
				.FirstOrDefault(paramType => allValidationRules.ContainsKey(paramType));

			if (matchingParamType is not null && !mapping.ContainsKey(routePattern))
			{
				// Only set the mapping if this route pattern hasn't been mapped yet
				// This prevents collisions when multiple endpoints share the same route pattern
				mapping[routePattern] = matchingParamType;
			}
		}

		return mapping;
	}

	static void ApplyValidationToParameters(OpenApiDocument document, Type requestType, List<Validation.ValidationRule> rules, Dictionary<string, Type> endpointToRequestType, bool typeAppendRulesToPropertyDescription, bool appendRulesToPropertyDescription)
	{
		if (document.Paths is null || rules.Count == 0)
		{
			return;
		}

		// Iterate through all paths and operations
		foreach (KeyValuePair<string, IOpenApiPathItem> pathItem in document.Paths)
		{
			if (pathItem.Value is not OpenApiPathItem pathItemValue)
			{
				continue;
			}

			// Check if this path matches the current request type we're processing
			// Use the OpenAPI path as the pattern that will be compared against endpoint route patterns
			string pathPattern = pathItem.Key;

			// Try to find matching endpoint in our mapping
			// We need to check if this path belongs to an endpoint that uses this request type
			Type? pathRequestType = endpointToRequestType
				.Where(mapping => PathsMatch(pathPattern, mapping.Key))
				.Select(mapping => mapping.Value)
				.FirstOrDefault();

			// Skip this path if it doesn't belong to the current request type
			if (pathRequestType is null || pathRequestType != requestType)
			{
				continue;
			}

			foreach (KeyValuePair<HttpMethod, OpenApiOperation> operation in pathItemValue.Operations ?? [])
			{
				if (operation.Value.Parameters is null || operation.Value.Parameters.Count == 0)
				{
					continue;
				}

				// Group rules by property name for easier lookup
				// NOTE: HTTP query/path parameter names are case-insensitive in ASP.NET/OpenAPI.
				// Validation rules are keyed by C# property names (usually PascalCase), while
				// route/query parameters may be camelCase or lowercase. We use OrdinalIgnoreCase
				// to reliably match validation rules to parameters regardless of casing.
				Dictionary<string, List<Validation.ValidationRule>> rulesByProperty = rules
					.GroupBy(r => r.PropertyName, StringComparer.OrdinalIgnoreCase)
					.ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

				// Process each parameter
				foreach (OpenApiParameter parameter in operation.Value.Parameters.Cast<OpenApiParameter>())
				{
					if (string.IsNullOrEmpty(parameter.Name))
					{
						continue;
					}

					// Match parameter name to property rules (case-insensitive)
					if (!rulesByProperty.TryGetValue(parameter.Name, out List<Validation.ValidationRule>? propertyRules))
					{
						continue;
					}

					// Check if any rule for this property is RequiredRule
					bool hasRequiredRule = propertyRules.Any(r => r is Validation.RequiredRule);
					if (hasRequiredRule)
					{
						parameter.Required = true;
					}

					// Check if there are any rules other than RequiredRule and DescriptionRule
					bool hasOtherRules = propertyRules.Any(r => r is not Validation.RequiredRule and not Validation.DescriptionRule);

					// Only modify the parameter schema if there are validation rules other than just Required
					// ALSO preserve $ref for enum types - enum properties should reference the enum schema, not inline it
					if (hasOtherRules && parameter.Schema is not null)
					{
						// Check if this parameter is a reference to an enum schema
						// Enum schemas should NOT be inlined - we preserve the $ref to maintain clean schema structure
						if (IsEnumSchemaReference(parameter.Schema, document))
						{
							// Don't inline enum properties - preserve the $ref
							// EnumRule validation is redundant since the enum schema itself defines valid values
							continue;
						}

						// Create inline schema with all validation constraints for this parameter
						parameter.Schema = CreateInlineSchemaWithAllValidation(parameter.Schema, [.. propertyRules], typeAppendRulesToPropertyDescription, appendRulesToPropertyDescription);
					}
				}
			}
		}
	}

	/// <summary>
	/// Determines if two path patterns match, accounting for different format variations.
	/// Handles OpenAPI path format (/api/v1/endpoint/{param}) vs ASP.NET route patterns (/api/v{version:apiVersion}/endpoint/{param}).
	/// </summary>
	static bool PathsMatch(string openApiPath, string routePattern)
	{
		// Direct match
		if (string.Equals(openApiPath, routePattern, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		// Normalize both paths for comparison
		// Remove leading/trailing slashes and convert to lowercase
		string normalizedOpenApi = openApiPath.Trim('/').ToLowerInvariant();
		string normalizedRoute = routePattern.Trim('/').ToLowerInvariant();

		// Check if they match after normalization
		if (normalizedOpenApi == normalizedRoute)
		{
			return true;
		}

		// Split by '/' and compare segments
		string[] openApiSegments = normalizedOpenApi.Split('/');
		string[] routeSegments = normalizedRoute.Split('/');

		if (openApiSegments.Length != routeSegments.Length)
		{
			return false;
		}

		// Compare each segment
		for (int i = 0; i < openApiSegments.Length; i++)
		{
			string openApiSeg = openApiSegments[i];
			string routeSeg = routeSegments[i];

			// Exact match
			if (openApiSeg == routeSeg)
			{
				continue;
			}

			// Both are parameters (enclosed in {})
			if (openApiSeg.StartsWith('{') && openApiSeg.EndsWith('}') &&
				routeSeg.StartsWith('{') && routeSeg.EndsWith('}'))
			{
				continue;
			}

			// Check for version parameter matching (e.g., "v1" matches "v{version:apiversion}")
			// This handles the case where OpenAPI has "v1" but route pattern has "v{version:apiVersion}"
			// Ensure the route segment is a version placeholder and the OpenAPI segment matches the expected format
			if (routeSeg.StartsWith("v{") &&
			    routeSeg.Contains("version", StringComparison.OrdinalIgnoreCase) &&
			    routeSeg.EndsWith('}') &&
			    openApiSeg.StartsWith('v') &&
			    openApiSeg.Length > 1 &&
			    openApiSeg[1..].All(char.IsDigit))
			{
				// OpenAPI segment should be exactly in format "v{digit}+" (e.g., "v1", "v2", "v10")
				// All characters after 'v' must be digits
				continue; // Version placeholder matches versioned path
			}

			// No match
			return false;
		}

		return true;
	}

	internal static OpenApiSchema CreateInlineSchemaWithAllValidation(IOpenApiSchema originalSchema, List<Validation.ValidationRule> rules, bool typeAppendRulesToPropertyDescription, bool appendRulesToPropertyDescription)
	{
		// Check for per-property AppendRulesToPropertyDescription setting (takes precedence over global setting)
		bool? perPropertySetting = rules.FirstOrDefault(r => r.AppendRuleToPropertyDescription.HasValue)?.AppendRuleToPropertyDescription;
		bool effectiveListRulesInDescription = perPropertySetting ?? typeAppendRulesToPropertyDescription;

		// Cast to OpenApiSchema to access properties
		OpenApiSchema? originalOpenApiSchema = originalSchema as OpenApiSchema;

		// Check if original schema has oneOf (indicates nullable type)
		// We need to preserve this structure when creating the inline schema
		bool isNullableWrapper = originalOpenApiSchema?.OneOf is not null && originalOpenApiSchema.OneOf.Count > 0;
		IOpenApiSchema? actualSchema = originalOpenApiSchema;

		if (isNullableWrapper)
		{
			// Find the non-null schema in the oneOf array
			// The nullable marker is typically an empty or minimal schema
			actualSchema = originalOpenApiSchema!.OneOf?.FirstOrDefault(s =>
			{
				if (s is OpenApiSchemaReference)
				{
					return true; // References are the actual type
				}
				if (s is OpenApiSchema schema)
				{
					// Skip minimal schemas that just mark nullability
					// These typically have no Type set and few/no other properties
					return schema.Type.HasValue || schema.Properties?.Count > 0 || schema.AllOf?.Count > 0;
				}
				return false;
			});

			// If we found a better schema to work with, update our reference
			if (actualSchema is not null && actualSchema != originalOpenApiSchema)
			{
				originalOpenApiSchema = actualSchema as OpenApiSchema;
			}
		}

		// If original schema is a reference, we need to get the type from the reference ID
		JsonSchemaType? referenceType = null;
		string? referenceFormat = null;
		bool isNullableReference = false;

		if (actualSchema is OpenApiSchemaReference schemaRef)
		{
			string? refId = schemaRef.Reference?.Id;
			if (refId is not null)
			{
				// Check if this is a nullable reference (System.Nullable`1[[...]])
				if (refId.Contains("System.Nullable`1"))
				{
					isNullableWrapper = true;
					isNullableReference = true;
				}

				if (refId.Contains("System.String"))
				{
					referenceType = JsonSchemaType.String;
				}
				else if (refId.Contains("System.Int32") || refId.Contains("System.Int64"))
				{
					referenceType = JsonSchemaType.Integer;
					referenceFormat = refId.Contains("Int64") ? "int64" : "int32";
				}
				else if (refId.Contains("System.Decimal") || refId.Contains("System.Double") || refId.Contains("System.Single"))
				{
					referenceType = JsonSchemaType.Number;
					referenceFormat = refId.Contains("Double") ? "double" : refId.Contains("Single") ? "float" : null;
				}
				else if (refId.Contains("System.Boolean"))
				{
					referenceType = JsonSchemaType.Boolean;
				}
				else if (refId.Contains("System.DateTime") || refId.Contains("System.DateTimeOffset"))
				{
					referenceType = JsonSchemaType.String;
					referenceFormat = "date-time";
				}
				else if (refId.Contains("System.Guid"))
				{
					referenceType = JsonSchemaType.String;
					referenceFormat = "uuid";
				}
			}
		}


		// Check if this is a complex object (has AllOf for schema references or has Properties for inline object definitions)
		// Complex objects should preserve their structure and only add validation descriptions
		bool isComplexObject = (originalOpenApiSchema?.AllOf?.Count > 0) ||
							   (originalOpenApiSchema?.Properties?.Count > 0 && originalOpenApiSchema.Type == JsonSchemaType.Object);

		// For complex objects (nested types with AllOf or object definitions), preserve the original schema structure
		if (isComplexObject && originalOpenApiSchema is not null)
		{
			// For complex objects, we don't want to lose the AllOf or Properties structure
			// Just return the original schema with an added description for validation rules

			// Extract custom description from DescriptionRule if present
			string? customDescription = rules
				.OfType<Validation.DescriptionRule>()
				.FirstOrDefault()?.Description;

			// Collect only applicable rule descriptions (RequiredRule for complex objects)
			List<string> ruleDescriptions = [];
			if (effectiveListRulesInDescription)
			{
				foreach (Validation.RequiredRule _ in rules.OfType<Validation.RequiredRule>())
				{
					ruleDescriptions.Add("Required");
				}
			}

			// Build the complete description
			List<string> descriptionParts = [];

			if (!string.IsNullOrEmpty(customDescription))
			{
				descriptionParts.Add(customDescription);
			}

			if (appendRulesToPropertyDescription && ruleDescriptions.Count > 0 && effectiveListRulesInDescription)
			{
				string rulesSection = "Validation rules:\n" + string.Join("\n", ruleDescriptions.Distinct().Select(msg => $"- {msg}"));
				descriptionParts.Add(rulesSection);
			}

			// Create a new schema that preserves the original structure but adds the description
			OpenApiSchema complexSchema = new()
			{
				AllOf = originalOpenApiSchema.AllOf,
				Properties = originalOpenApiSchema.Properties,
				Type = originalOpenApiSchema.Type,
				Format = originalOpenApiSchema.Format,
				Items = originalOpenApiSchema.Items,
				AdditionalProperties = originalOpenApiSchema.AdditionalProperties,
				Extensions = originalOpenApiSchema.Extensions, // Preserve extensions (e.g., enum info)
				Description = descriptionParts.Count > 0 ? string.Join("\n\n", descriptionParts) : originalOpenApiSchema.Description
			};

			return complexSchema;
		}

		// For simple properties, create a new inline schema with all validation rules
		// Get the type from the first rule (all rules for same property should have same type)
		JsonSchemaType? schemaType = rules.Select(GetSchemaType).FirstOrDefault(t => t is not null);
		string? format = rules.Select(GetSchemaFormat).FirstOrDefault(f => f is not null);

		// If no type was determined from rules, try to get it from the original schema or reference
		if (!schemaType.HasValue)
		{
			if (originalOpenApiSchema is not null)
			{
				schemaType = originalOpenApiSchema.Type;
				format ??= originalOpenApiSchema.Format;
			}
			else if (referenceType.HasValue)
			{
				schemaType = referenceType;
				format ??= referenceFormat;
			}
		}



		// Create inline schema - set properties after creation to avoid initialization issues
		OpenApiSchema newInlineSchema = new();

		// Set type and format separately
		if (schemaType.HasValue)
		{
			newInlineSchema.Type = schemaType.Value;
		}
		if (format is not null)
		{
			newInlineSchema.Format = format;
		}

		// Extract custom description from DescriptionRule if present
		string? customDescription2 = rules
			.OfType<Validation.DescriptionRule>()
			.FirstOrDefault()?.Description;

		// Collect all rule descriptions (excluding DescriptionRule and EnumRule)
		List<string> ruleDescriptions2 = [];

		// Apply all rules to this schema
		foreach (Validation.ValidationRule rule in rules)
		{
			// Skip DescriptionRule - it's handled separately
			if (rule is Validation.DescriptionRule)
			{
				continue;
			}

			// Get human-readable description for this rule (only if effectiveListRulesInDescription is true)
			// Skip EnumRule error messages as enum info is added differently
			if (effectiveListRulesInDescription && rule is not Validation.EnumRule)
			{
				string? ruleDescription = rule.ErrorMessage;
				if (!string.IsNullOrEmpty(ruleDescription))
				{
					ruleDescriptions2.Add(ruleDescription);
				}
			}

			// Apply rule to schema (for non-custom and non-description rules)
			if (!IsCustomRule(rule))
			{
				ApplyRuleToSchema(rule, newInlineSchema);
			}
		}

		// Build the complete description: custom description + validation rules + enum info
		List<string> descriptionParts2 = [];

		// If the schema already has an enum description from EnumSchemaTransformer, preserve it
		string? enumDescription = null;
		if (!string.IsNullOrEmpty(newInlineSchema.Description) && newInlineSchema.Description.StartsWith("Enum:"))
		{
			enumDescription = newInlineSchema.Description;
		}

		// Add enum description first if present
		if (!string.IsNullOrEmpty(enumDescription))
		{
			descriptionParts2.Add(enumDescription);
		}
		// Add custom description if present
		else if (!string.IsNullOrEmpty(customDescription2))
		{
			descriptionParts2.Add(customDescription2);
		}

		// Add validation rules section if any exist (and if effectiveListRulesInDescription is true)
		if (appendRulesToPropertyDescription && ruleDescriptions2.Count > 0 && effectiveListRulesInDescription)
		{
			string rulesSection = "Validation rules:\n" + string.Join("\n", ruleDescriptions2.Distinct().Select(msg => $"- {msg}"));
			descriptionParts2.Add(rulesSection);
		}

		// Set the final description
		if (descriptionParts2.Count > 0)
		{
			newInlineSchema.Description = string.Join("\n\n", descriptionParts2);
		}


		// Preserve any Extensions from the original schema (e.g., enum info from EnumSchemaTransformer)
		if (originalOpenApiSchema?.Extensions is not null && originalOpenApiSchema.Extensions.Count > 0)
		{
			newInlineSchema.Extensions ??= new Dictionary<string, IOpenApiExtension>();
			foreach (KeyValuePair<string, IOpenApiExtension> extension in originalOpenApiSchema.Extensions)
			{
				newInlineSchema.Extensions[extension.Key] = extension.Value;
			}
		}

		// If the original schema was nullable (oneOf wrapper), recreate that structure
		if (isNullableWrapper && !isNullableReference)
		{
			// Wrap the inline schema in a oneOf with a nullable option
			// The nullable option is represented as an empty schema with the "nullable" extension set to true
			// This matches ASP.NET Core's built-in behavior for nullable types
			OpenApiSchema nullableSchema = new()
			{
				Extensions = new Dictionary<string, IOpenApiExtension>
				{
					["nullable"] = new JsonNodeExtension(JsonValue.Create(true)!)
				}
			};

			return new OpenApiSchema
			{
				OneOf =
				[
					nullableSchema,
					newInlineSchema
				]
			};
		}

		return newInlineSchema;
	}

	static bool IsCustomRule(Validation.ValidationRule rule)
	{
		Type ruleType = rule.GetType();
		return ruleType.IsGenericType && ruleType.GetGenericTypeDefinition() == typeof(Validation.CustomRule<>);
	}

	static void ApplyRuleToSchema(Validation.ValidationRule rule, OpenApiSchema schema)
	{
		switch (rule)
		{
			case Validation.RequiredRule:
				// Required is handled at the parent schema level
				break;

			case Validation.StringLengthRule stringLengthRule:
				if (stringLengthRule.MinLength.HasValue)
				{
					schema.MinLength = stringLengthRule.MinLength.Value;
				}
				if (stringLengthRule.MaxLength.HasValue)
				{
					schema.MaxLength = stringLengthRule.MaxLength.Value;
				}
				break;

			case Validation.PatternRule patternRule:
				schema.Pattern = patternRule.Pattern;
				break;

			case Validation.EmailRule:
				schema.Format = "email";
				break;

			case Validation.UrlRule:
				schema.Format = "uri";
				break;

			case Validation.RangeRule<int> intRangeRule:
				if (intRangeRule.Minimum.HasValue)
				{
					schema.Minimum = intRangeRule.Minimum.Value.ToString();
					schema.ExclusiveMinimum = intRangeRule.ExclusiveMinimum.ToString().ToLowerInvariant();
				}
				if (intRangeRule.Maximum.HasValue)
				{
					schema.Maximum = intRangeRule.Maximum.Value.ToString();
					schema.ExclusiveMaximum = intRangeRule.ExclusiveMaximum.ToString().ToLowerInvariant();
				}
				break;

			case Validation.RangeRule<decimal> decimalRangeRule:
				if (decimalRangeRule.Minimum.HasValue)
				{
					schema.Minimum = decimalRangeRule.Minimum.Value.ToString();
					schema.ExclusiveMinimum = decimalRangeRule.ExclusiveMinimum.ToString().ToLowerInvariant();
				}
				if (decimalRangeRule.Maximum.HasValue)
				{
					schema.Maximum = decimalRangeRule.Maximum.Value.ToString();
					schema.ExclusiveMaximum = decimalRangeRule.ExclusiveMaximum.ToString().ToLowerInvariant();
				}
				break;

			case Validation.RangeRule<double> doubleRangeRule:
				if (doubleRangeRule.Minimum.HasValue)
				{
					schema.Minimum = doubleRangeRule.Minimum.Value.ToString();
					schema.ExclusiveMinimum = doubleRangeRule.ExclusiveMinimum.ToString().ToLowerInvariant();
				}
				if (doubleRangeRule.Maximum.HasValue)
				{
					schema.Maximum = doubleRangeRule.Maximum.Value.ToString();
					schema.ExclusiveMaximum = doubleRangeRule.ExclusiveMaximum.ToString().ToLowerInvariant();
				}
				break;

			case Validation.RangeRule<float> floatRangeRule:
				if (floatRangeRule.Minimum.HasValue)
				{
					schema.Minimum = floatRangeRule.Minimum.Value.ToString();
					schema.ExclusiveMinimum = floatRangeRule.ExclusiveMinimum.ToString().ToLowerInvariant();
				}
				if (floatRangeRule.Maximum.HasValue)
				{
					schema.Maximum = floatRangeRule.Maximum.Value.ToString();
					schema.ExclusiveMaximum = floatRangeRule.ExclusiveMaximum.ToString().ToLowerInvariant();
				}
				break;

			case Validation.RangeRule<long> longRangeRule:
				if (longRangeRule.Minimum.HasValue)
				{
					schema.Minimum = longRangeRule.Minimum.Value.ToString();
					schema.ExclusiveMinimum = longRangeRule.ExclusiveMinimum.ToString().ToLowerInvariant();
				}
				if (longRangeRule.Maximum.HasValue)
				{
					schema.Maximum = longRangeRule.Maximum.Value.ToString();
					schema.ExclusiveMaximum = longRangeRule.ExclusiveMaximum.ToString().ToLowerInvariant();
				}
				break;

			case Validation.EnumRule enumRule:
				EnrichSchemaWithEnumValues(schema, enumRule.EnumType);
				break;
		}
	}

	static JsonSchemaType? GetSchemaType(Validation.ValidationRule rule)
	{
		return rule switch
		{
			Validation.StringLengthRule => JsonSchemaType.String,
			Validation.PatternRule => JsonSchemaType.String,
			Validation.EmailRule => JsonSchemaType.String,
			Validation.UrlRule => JsonSchemaType.String,
			Validation.EnumRule enumRule => GetEnumRuleSchemaType(enumRule),
			Validation.RangeRule<int> => JsonSchemaType.Integer,
			Validation.RangeRule<long> => JsonSchemaType.Integer,
			Validation.RangeRule<decimal> => JsonSchemaType.Number,
			Validation.RangeRule<double> => JsonSchemaType.Number,
			Validation.RangeRule<float> => JsonSchemaType.Number,
			_ => null
		};
	}

	static JsonSchemaType GetEnumRuleSchemaType(Validation.EnumRule enumRule)
	{
		// Determine schema type based on the property type, not the enum type
		// - string properties (IsEnumName) should be JsonSchemaType.String
		// - int properties (IsInEnum on int) should be JsonSchemaType.Integer
		// - enum properties (IsInEnum on TEnum) should be JsonSchemaType.Integer (enum's underlying type)

		Type propertyType = enumRule.PropertyType;

		// Handle nullable types
		Type actualType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

		// If property type is string, return string schema type
		if (actualType == typeof(string))
		{
			return JsonSchemaType.String;
		}

		// If property type is int or an enum type, return integer schema type
		if (actualType == typeof(int) || actualType == typeof(long) ||
			actualType == typeof(short) || actualType == typeof(byte) ||
			actualType.IsEnum)
		{
			return JsonSchemaType.Integer;
		}

		// Default fallback (shouldn't happen)
		return JsonSchemaType.String;
	}

	static JsonSchemaType GetEnumSchemaType(Type enumType)
	{
		// Get the underlying type of the enum (byte, short, int, long, etc.)
		Type underlyingType = Enum.GetUnderlyingType(enumType);

		// Map to appropriate JSON schema type
		if (underlyingType == typeof(byte) || underlyingType == typeof(sbyte) ||
			underlyingType == typeof(short) || underlyingType == typeof(ushort) ||
			underlyingType == typeof(int) || underlyingType == typeof(uint) ||
			underlyingType == typeof(long) || underlyingType == typeof(ulong))
		{
			return JsonSchemaType.Integer;
		}

		// Default to string (shouldn't happen with normal enums, but just in case)
		return JsonSchemaType.String;
	}

	static string? GetSchemaFormat(Validation.ValidationRule rule)
	{
		return rule switch
		{
			Validation.EmailRule => "email",
			Validation.UrlRule => "uri",
			Validation.RangeRule<int> => "int32",
			Validation.RangeRule<long> => "int64",
			Validation.RangeRule<float> => "float",
			Validation.RangeRule<double> => "double",
			_ => null
		};
	}

	static void EnrichSchemaWithEnumValues(OpenApiSchema schema, Type enumType)
	{
		// Get all enum values and names
		Array enumValues = Enum.GetValues(enumType);
		string[] enumNames = Enum.GetNames(enumType);

		List<JsonNode> values = [];
		List<string> varNames = [];
		Dictionary<string, string> descriptions = [];

		// Determine if this is a string schema or integer schema
		bool isStringSchema = schema.Type == JsonSchemaType.String;

		for (int i = 0; i < enumValues.Length; i++)
		{
			object enumValue = enumValues.GetValue(i)!;
			string enumName = enumNames[i];

			// For string schemas, add the enum names as valid values
			// For integer schemas, add the numeric values
			if (isStringSchema)
			{
				values.Add(JsonValue.Create(enumName)!);
			}
			else
			{
				long numericValue = Convert.ToInt64(enumValue);
				values.Add(JsonValue.Create(numericValue)!);
			}

			varNames.Add(enumName);

			// Check for Description attribute
			FieldInfo? field = enumType.GetField(enumName);
			DescriptionAttribute? descriptionAttr = field?.GetCustomAttributes(typeof(DescriptionAttribute), false)
			.OfType<DescriptionAttribute>()
			.FirstOrDefault();

			if (descriptionAttr is not null && !string.IsNullOrWhiteSpace(descriptionAttr.Description))
			{
				descriptions[enumName] = descriptionAttr.Description;
			}
		}

		// Add the enum values
		schema.Extensions ??= new Dictionary<string, IOpenApiExtension>();
		schema.Extensions["enum"] = new JsonNodeExtension(new JsonArray(values.ToArray()));

		// Add x-enum-varnames extension for member names (only if different from enum values)
		// For string schemas, the names are the same as the values, so no need to duplicate
		if (!isStringSchema)
		{
			schema.Extensions["x-enum-varnames"] = new JsonNodeExtension(new JsonArray(varNames.Select(n => JsonValue.Create(n)!).ToArray()));
		}

		// Add x-enum-descriptions extension if any descriptions are present
		if (descriptions.Count > 0)
		{
			JsonObject descObj = [];
			foreach (KeyValuePair<string, string> kvp in descriptions)
			{
				descObj[kvp.Key] = kvp.Value;
			}
			schema.Extensions["x-enum-descriptions"] = new JsonNodeExtension(descObj);
		}

		// Update the description to mention enum values if not already set with better context
		if (string.IsNullOrWhiteSpace(schema.Description) || schema.Description.Contains("has a range of values"))
		{
			schema.Description = $"Enum: {string.Join(", ", varNames)}";
		}
		else if (!schema.Description.Contains("Enum:"))
		{
			// Prepend enum info to existing description
			schema.Description = $"Enum: {string.Join(", ", varNames)}\n\n{schema.Description}";
		}
	}

	/// <summary>
	/// Checks if a property schema is a reference to an enum schema in the document
	/// </summary>
	static bool IsEnumSchemaReference(IOpenApiSchema propertySchema, OpenApiDocument document)
	{
		// Handle oneOf patterns for nullable types
		if (propertySchema is OpenApiSchema schema && schema.OneOf is not null && schema.OneOf.Count > 0)
		{
			// Check if any of the oneOf schemas is an enum reference
			return schema.OneOf.Any(oneOfSchema => IsEnumSchemaReference(oneOfSchema, document));
		}

		// Check if this is a schema reference
		if (propertySchema is not OpenApiSchemaReference schemaRef)
		{
			return false;
		}

		// Get the reference ID
		string? refId = schemaRef.Reference?.Id;
		if (string.IsNullOrEmpty(refId))
		{
			return false;
		}

		// System.String, System.Int32, etc. are NOT enum schemas - they should be inlined
		// Only preserve $ref for actual enum type schemas
		// BUT System.Nullable`1[[EnumType...]] IS an enum schema if it wraps an enum
		if (refId.StartsWith("System.") && !refId.StartsWith("System.Nullable`1"))
		{
			return false;
		}

		// Look up the referenced schema in the document
		if (document.Components?.Schemas?.TryGetValue(refId, out IOpenApiSchema? referencedSchema) != true)
		{
			return false;
		}

		// Check if the referenced schema is an actual enum type schema
		// Enum type schemas have the Extensions["enum"] property with values (set by EnumSchemaTransformer)
		if (referencedSchema is OpenApiSchema enumSchema)
		{
			// Check if this is an enum schema by looking for the Extensions["enum"] property
			if (enumSchema.Extensions?.TryGetValue("enum", out IOpenApiExtension? _) == true)
			{
				return true;
			}

			// Also check the standard Enum property as a fallback
			if (enumSchema.Enum is not null && enumSchema.Enum.Count > 0)
			{
				return true;
			}
		}

		return false;
	}
}
