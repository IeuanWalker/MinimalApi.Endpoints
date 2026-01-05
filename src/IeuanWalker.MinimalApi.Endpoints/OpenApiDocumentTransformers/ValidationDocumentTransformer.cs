using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Nodes;
using IeuanWalker.MinimalApi.Endpoints.Extensions;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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

			// Create inline schema for all properties with validation rules (including just Required)
			// This ensures type information is explicit in the schema rather than relying on references
			if (propertyRules.Any())
			{
				if (schema.Properties.TryGetValue(propertyKey, out IOpenApiSchema? propertySchemaInterface))
				{
					// Check if this property is a reference to an enum schema OR has inline enum values
					// Enum schemas should NOT be modified - we preserve the $ref or inline enum information
					if (IsEnumSchemaReference(propertySchemaInterface, document) || IsInlineEnumSchema(propertySchemaInterface))
					{
						// Don't modify enum properties - preserve the $ref or inline enum info
						// EnumRule validation is redundant since the enum schema itself defines valid values
						continue;
					}

					// Create inline schema with all validation constraints for this property
					schema.Properties[propertyKey] = CreateInlineSchemaWithAllValidation(propertySchemaInterface, [.. propertyRules], typeAppendRulesToPropertyDescription, appendRulesToPropertyDescription, document);
				}
			}
		}

		// Set required properties on the schema
		if (requiredProperties.Count > 0)
		{
			schema.Required = new HashSet<string>(requiredProperties);
		}

		// Inline primitive type references for ALL properties (including those without validation rules)
		// This ensures explicit types in OpenAPI documentation instead of requiring clients to resolve $ref links
		foreach (KeyValuePair<string, IOpenApiSchema> property in schema.Properties.ToList())
		{
			// Skip properties we already processed (they have validation rules)
			if (rulesByProperty.Any(g => g.Key.ToCamelCase() == property.Key))
			{
				continue;
			}

			// Skip enum properties - preserve the $ref or inline enum info
			if (IsEnumSchemaReference(property.Value, document) || IsInlineEnumSchema(property.Value))
			{
				continue;
			}

			// Inline primitive type references for better documentation
			schema.Properties[property.Key] = InlinePrimitiveTypeReference(property.Value, document);
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

			if (matchingParamType is null)
			{
				continue;
			}

			if (mapping.TryGetValue(routePattern, out Type existingType))
			{
				// Log a warning when a collision is detected
				if (existingType != matchingParamType)
				{
					ILogger logger = context.ApplicationServices.GetService(typeof(ILogger<ValidationDocumentTransformer>)) as ILogger ?? NullLogger.Instance;
					LogRoutePatternCollision(logger, routePattern, existingType.Name, matchingParamType.Name);
				}
			}
			else
			{
				// Only set the mapping if this route pattern hasn't been mapped yet
				// This prevents collisions when multiple endpoints share the same route pattern
				mapping[routePattern] = matchingParamType;
			}
		}

		return mapping;
	}

	[LoggerMessage(Level = LogLevel.Warning, Message = "Route pattern '{RoutePattern}' is shared by multiple endpoints with different request types. Using validation rules from '{FirstType}', ignoring '{SecondType}'. Consider using different route patterns or HTTP methods to avoid validation rule conflicts.")]
	static partial void LogRoutePatternCollision(ILogger logger, string routePattern, string firstType, string secondType);

	static void ApplyValidationToParameters(OpenApiDocument document, Type requestType, List<Validation.ValidationRule> rules, Dictionary<string, Type> endpointToRequestType, bool typeAppendRulesToPropertyDescription, bool appendRulesToPropertyDescription)
	{
		if (document.Paths is null || rules.Count == 0)
		{
			return;
		}

		// Build a lookup cache for this invocation to optimize repeated PathsMatch calls
		// This provides O(1) lookup instead of O(M) linear search for each path
		Dictionary<string, Type?> pathMatchCache = new(StringComparer.OrdinalIgnoreCase);

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

			// Try to find matching endpoint - use cache to avoid redundant PathsMatch calls
			if (!pathMatchCache.TryGetValue(pathPattern, out Type? pathRequestType))
			{
				// Cache miss - perform the search and cache the result
				pathRequestType = endpointToRequestType
					.Where(mapping => PathsMatch(pathPattern, mapping.Key))
					.Select(mapping => mapping.Value)
					.FirstOrDefault();

				pathMatchCache[pathPattern] = pathRequestType;
			}

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
						// No validation rules for this parameter, but we should still inline primitive types
						// for better OpenAPI documentation clarity
						if (parameter.Schema is not null && !IsEnumSchemaReference(parameter.Schema, document) && !IsInlineEnumSchema(parameter.Schema))
						{
							parameter.Schema = InlinePrimitiveTypeReference(parameter.Schema, document);
						}
						continue;
					}

					// Check if any rule for this property is RequiredRule
					bool hasRequiredRule = propertyRules.Any(r => r is Validation.RequiredRule);
					if (hasRequiredRule)
					{
						parameter.Required = true;
					}

					// Create inline schema for all parameters with validation rules (including just Required)
					// This ensures type information is explicit in the schema rather than relying on references
					if (propertyRules.Count > 0 && parameter.Schema is not null)
					{
						// Check if this parameter is a reference to an enum schema OR has inline enum values
						// Enum schemas should NOT be modified - we preserve the $ref or inline enum information
						if (IsEnumSchemaReference(parameter.Schema, document) || IsInlineEnumSchema(parameter.Schema))
						{
							// Don't modify enum properties - preserve the $ref or inline enum info
							continue;
						}

						// Create inline schema with all validation constraints for this parameter
						parameter.Schema = CreateInlineSchemaWithAllValidation(parameter.Schema, [.. propertyRules], typeAppendRulesToPropertyDescription, appendRulesToPropertyDescription, document);
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

	internal static OpenApiSchema CreateInlineSchemaWithAllValidation(IOpenApiSchema originalSchema, List<Validation.ValidationRule> rules, bool typeAppendRulesToPropertyDescription, bool appendRulesToPropertyDescription, OpenApiDocument document)
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
		IOpenApiSchema? typeSourceSchema = null; // Schema to extract type info from (may be different from actualSchema)

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
					// But also look for schemas with maxLength/minLength as those are validation rules
					return schema.Type.HasValue || schema.Properties?.Count > 0 || schema.AllOf?.Count > 0 || 
					       schema.MaxLength.HasValue || schema.MinLength.HasValue ||
					       schema.Maximum is not null || schema.Minimum is not null;
				}
				return false;
			});

			// Also find any schema reference in the oneOf for type information
			// (In case actualSchema has validation but type info is in a different oneOf element)
			typeSourceSchema = originalOpenApiSchema!.OneOf?.FirstOrDefault(s => s is OpenApiSchemaReference);

			// If we found a better schema to work with, update our reference
			if (actualSchema is not null && actualSchema != originalOpenApiSchema)
			{
				originalOpenApiSchema = actualSchema as OpenApiSchema;
			}
		}

		// If we still don't have an actualSchema but originalSchema is a reference, use it
		actualSchema ??= originalSchema;

		// If original schema is a reference, we need to get the type from the reference ID
		JsonSchemaType? referenceType = null;
		string? referenceFormat = null;
		bool isNullableReference = false;
		OpenApiSchema? resolvedReferenceSchema = null;

		// Check actualSchema for reference type info
		if (actualSchema is OpenApiSchemaReference schemaRef)
		{
			string? refId = schemaRef.Reference?.Id;
			if (refId is not null)
			{
				// Try to resolve the reference to get the actual schema
				if (document.Components?.Schemas?.TryGetValue(refId, out IOpenApiSchema? referencedSchema) == true)
				{
					resolvedReferenceSchema = referencedSchema as OpenApiSchema;
				}

				// Check if this is a nullable reference (System.Nullable`1[[...]])
				if (refId.Contains("System.Nullable`1"))
				{
					isNullableWrapper = true;
					isNullableReference = true;
				}

				// Check for array types (e.g., System.String[], System.Int32[])
				if (refId.EndsWith("[]"))
				{
					referenceType = JsonSchemaType.Array;
				}
				else if (refId.Contains("System.Collections.Generic.List`1") ||
						 refId.Contains("System.Collections.Generic.IEnumerable`1") ||
						 refId.Contains("System.Collections.Generic.ICollection`1"))
				{
					referenceType = JsonSchemaType.Array;
				}
				else if (refId.Contains("System.String") && !refId.Contains("[]"))
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

		// Also check typeSourceSchema if it's different from actualSchema (for nullable types with validation)
		if (typeSourceSchema is not null && typeSourceSchema != actualSchema && typeSourceSchema is OpenApiSchemaReference typeSrcRef)
		{
			string? refId = typeSrcRef.Reference?.Id;
			if (refId is not null && !referenceType.HasValue)
			{
				// Check for array types in the typeSourceSchema
				if (refId.EndsWith("[]"))
				{
					referenceType = JsonSchemaType.Array;
				}
				else if (refId.Contains("System.Collections.Generic.List`1") ||
						 refId.Contains("System.Collections.Generic.IEnumerable`1") ||
						 refId.Contains("System.Collections.Generic.ICollection`1"))
				{
					referenceType = JsonSchemaType.Array;
				}
			}
		}


		// Check if actualSchema is a reference to a custom type (not a System.* primitive)
		// If so, we should preserve the reference and only add validation description
		bool isCustomTypeReference = false;
		OpenApiSchemaReference? customTypeRef = null;
		if (actualSchema is OpenApiSchemaReference schemaRef2)
		{
			string? refId = schemaRef2.Reference?.Id;
			if (refId is not null && !refId.StartsWith("System."))
			{
				// This is a custom type reference - preserve it
				isCustomTypeReference = true;
				customTypeRef = schemaRef2;
			}
		}

		// For custom type references, preserve the $ref and only add validation description
		if (isCustomTypeReference && customTypeRef is not null)
		{
			// Extract custom description from DescriptionRule if present
			string? customDescription = rules
				.OfType<Validation.DescriptionRule>()
				.FirstOrDefault()?.Description;

			// Collect only applicable rule descriptions (RequiredRule for custom type references)
			List<string> ruleDescriptions = [];
			if (effectiveListRulesInDescription)
			{
				foreach (Validation.RequiredRule _ in rules.OfType<Validation.RequiredRule>())
				{
					ruleDescriptions.Add("Is required");
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

			// For custom type references, we need to check if it's an enum and enrich with values
			string? refId2 = customTypeRef.Reference?.Id;
			if (refId2 is not null && document.Components?.Schemas?.TryGetValue(refId2, out IOpenApiSchema? referencedSchema2) == true)
			{
				if (referencedSchema2 is OpenApiSchema refSchema && refSchema.Enum?.Count > 0)
				{
					// This is an enum type - create an inline schema with enum values
					OpenApiSchema enumInlineSchema = new()
					{
						Type = refSchema.Type,
						Format = refSchema.Format,
						Enum = refSchema.Enum,
						Extensions = refSchema.Extensions,
						Description = descriptionParts.Count > 0 ? string.Join("\n\n", descriptionParts) : refSchema.Description
					};
					return enumInlineSchema;
				}
			}

			// Not an enum - just return the reference with updated description
			// We cannot modify OpenApiSchemaReference, so return a schema with AllOf containing the reference
			OpenApiSchema refWrapperSchema = new()
			{
				AllOf = [customTypeRef],
				Description = descriptionParts.Count > 0 ? string.Join("\n\n", descriptionParts) : null
			};

			return refWrapperSchema;
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
		// Strategy:
		// - For array types: prioritize schema type to preserve array structure
		// - For other types: prioritize validation rules type, then schema type
		JsonSchemaType? schemaType = null;
		string? format = null;

		// Check if this is an array type based on resolved schema or reference
		bool isArrayType = resolvedReferenceSchema?.Type == JsonSchemaType.Array ||
						   originalOpenApiSchema?.Type == JsonSchemaType.Array ||
						   referenceType == JsonSchemaType.Array;

		if (isArrayType)
		{
			// For arrays, prioritize schema type to preserve array structure
			if (originalOpenApiSchema?.Type is not null)
			{
				schemaType = originalOpenApiSchema.Type;
				format = originalOpenApiSchema.Format;
			}
			else if (resolvedReferenceSchema?.Type is not null)
			{
				schemaType = resolvedReferenceSchema.Type;
				format = resolvedReferenceSchema.Format;
			}
			else if (referenceType.HasValue)
			{
				schemaType = referenceType;
				format = referenceFormat;
			}
		}
		else
		{
			// For non-array types, get type from validation rules first (original behavior)
			schemaType = rules.Select(GetSchemaType).FirstOrDefault(t => t is not null);
			format = rules.Select(GetSchemaFormat).FirstOrDefault(f => f is not null);

			// If no type from rules, try reference type detection, then schema types
			// Prioritize referenceType for primitive types (Int32, String, etc.) as it's more reliable
			if (!schemaType.HasValue)
			{
				// First try reference type detection (most reliable for primitives)
				if (referenceType.HasValue)
				{
					schemaType = referenceType;
					format ??= referenceFormat;
				}
				// Then try original schema
				else if (originalOpenApiSchema?.Type is not null)
				{
					schemaType = originalOpenApiSchema.Type;
					format ??= originalOpenApiSchema.Format;
				}
				// Finally try resolved reference schema
				else if (resolvedReferenceSchema?.Type is not null)
				{
					schemaType = resolvedReferenceSchema.Type;
					format ??= resolvedReferenceSchema.Format;
				}
			}
			
			// If format still not set, try to get it from all sources
			if (format is null)
			{
				format = referenceFormat ?? resolvedReferenceSchema?.Format ?? originalOpenApiSchema?.Format;
			}
		}

		// Create inline schema - set properties after creation to avoid initialization issues
		OpenApiSchema newInlineSchema = new();

		// Set type and format separately
		if (schemaType.HasValue)
		{
			newInlineSchema.Type = schemaType.Value;
		}
		// For arrays, ensure type is always set even if schemaType wasn't determined
		else if (isArrayType)
		{
			newInlineSchema.Type = JsonSchemaType.Array;
		}
		
		if (format is not null)
		{
			newInlineSchema.Format = format;
		}

		// Preserve Items for array types from the original or resolved schema
		// BUT inline primitive type references for better OpenAPI documentation
		if (originalOpenApiSchema?.Items is not null)
		{
			newInlineSchema.Items = InlinePrimitiveTypeReference(originalOpenApiSchema.Items, document);
		}
		else if (resolvedReferenceSchema?.Items is not null)
		{
			newInlineSchema.Items = InlinePrimitiveTypeReference(resolvedReferenceSchema.Items, document);
		}
		else if (isArrayType && actualSchema is OpenApiSchemaReference arraySchemaRef)
		{
			// For List<T> references, extract the element type from the reference ID
			string? refId = arraySchemaRef.Reference?.Id;
			if (refId is not null && (refId.Contains("System.Collections.Generic.List`1") ||
									  refId.Contains("System.Collections.Generic.IEnumerable`1") ||
									  refId.Contains("System.Collections.Generic.ICollection`1")))
			{
				// Extract the element type from the generic parameter
				// Format: System.Collections.Generic.List`1[[ElementType, Assembly, ...]]
				int startIdx = refId.IndexOf("[[");
				int endIdx = refId.IndexOf(',', startIdx);
				if (startIdx >= 0 && endIdx > startIdx)
				{
					string elementType = refId.Substring(startIdx + 2, endIdx - startIdx - 2);
					// Create a reference to the element type schema (will be inlined if it's a primitive)
					OpenApiSchemaReference elementRef = new(elementType, document, null);
					newInlineSchema.Items = InlinePrimitiveTypeReference(elementRef, document);
				}
			}
		}
		else if (isArrayType && typeSourceSchema is OpenApiSchemaReference typeSrcRef2)
		{
			// Handle case where type info is in a different oneOf element than actualSchema
			// (e.g., nullable List<T> with validation rules)
			string? refId = typeSrcRef2.Reference?.Id;
			if (refId is not null && (refId.Contains("System.Collections.Generic.List`1") ||
									  refId.Contains("System.Collections.Generic.IEnumerable`1") ||
									  refId.Contains("System.Collections.Generic.ICollection`1")))
			{
				// Extract the element type from the generic parameter
				int startIdx = refId.IndexOf("[[");
				int endIdx = refId.IndexOf(',', startIdx);
				if (startIdx >= 0 && endIdx > startIdx)
				{
					string elementType = refId.Substring(startIdx + 2, endIdx - startIdx - 2);
					// Create a reference to the element type schema (will be inlined if it's a primitive)
					OpenApiSchemaReference elementRef = new(elementType, document, null);
					newInlineSchema.Items = InlinePrimitiveTypeReference(elementRef, document);
				}
			}
		}

		// Preserve Enum values from the original or resolved schema (for enum types)
		if (originalOpenApiSchema?.Enum is not null && originalOpenApiSchema.Enum.Count > 0)
		{
			newInlineSchema.Enum = originalOpenApiSchema.Enum;
		}
		else if (resolvedReferenceSchema?.Enum is not null && resolvedReferenceSchema.Enum.Count > 0)
		{
			newInlineSchema.Enum = resolvedReferenceSchema.Enum;
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
					if (intRangeRule.ExclusiveMinimum)
					{
						schema.ExclusiveMinimum = intRangeRule.Minimum.Value.ToString();
					}
					else
					{
						schema.Minimum = intRangeRule.Minimum.Value.ToString();
					}
				}
				if (intRangeRule.Maximum.HasValue)
				{
					if (intRangeRule.ExclusiveMaximum)
					{
						schema.ExclusiveMaximum = intRangeRule.Maximum.Value.ToString();
					}
					else
					{
						schema.Maximum = intRangeRule.Maximum.Value.ToString();
					}
				}
				break;

			case Validation.RangeRule<decimal> decimalRangeRule:
				if (decimalRangeRule.Minimum.HasValue)
				{
					if (decimalRangeRule.ExclusiveMinimum)
					{
						schema.ExclusiveMinimum = decimalRangeRule.Minimum.Value.ToString();
					}
					else
					{
						schema.Minimum = decimalRangeRule.Minimum.Value.ToString();
					}
				}
				if (decimalRangeRule.Maximum.HasValue)
				{
					if (decimalRangeRule.ExclusiveMaximum)
					{
						schema.ExclusiveMaximum = decimalRangeRule.Maximum.Value.ToString();
					}
					else
					{
						schema.Maximum = decimalRangeRule.Maximum.Value.ToString();
					}
				}
				break;

			case Validation.RangeRule<double> doubleRangeRule:
				if (doubleRangeRule.Minimum.HasValue)
				{
					if (doubleRangeRule.ExclusiveMinimum)
					{
						schema.ExclusiveMinimum = doubleRangeRule.Minimum.Value.ToString();
					}
					else
					{
						schema.Minimum = doubleRangeRule.Minimum.Value.ToString();
					}
				}
				if (doubleRangeRule.Maximum.HasValue)
				{
					if (doubleRangeRule.ExclusiveMaximum)
					{
						schema.ExclusiveMaximum = doubleRangeRule.Maximum.Value.ToString();
					}
					else
					{
						schema.Maximum = doubleRangeRule.Maximum.Value.ToString();
					}
				}
				break;

			case Validation.RangeRule<float> floatRangeRule:
				if (floatRangeRule.Minimum.HasValue)
				{
					if (floatRangeRule.ExclusiveMinimum)
					{
						schema.ExclusiveMinimum = floatRangeRule.Minimum.Value.ToString();
					}
					else
					{
						schema.Minimum = floatRangeRule.Minimum.Value.ToString();
					}
				}
				if (floatRangeRule.Maximum.HasValue)
				{
					if (floatRangeRule.ExclusiveMaximum)
					{
						schema.ExclusiveMaximum = floatRangeRule.Maximum.Value.ToString();
					}
					else
					{
						schema.Maximum = floatRangeRule.Maximum.Value.ToString();
					}
				}
				break;

			case Validation.RangeRule<long> longRangeRule:
				if (longRangeRule.Minimum.HasValue)
				{
					if (longRangeRule.ExclusiveMinimum)
					{
						schema.ExclusiveMinimum = longRangeRule.Minimum.Value.ToString();
					}
					else
					{
						schema.Minimum = longRangeRule.Minimum.Value.ToString();
					}
				}
				if (longRangeRule.Maximum.HasValue)
				{
					if (longRangeRule.ExclusiveMaximum)
					{
						schema.ExclusiveMaximum = longRangeRule.Maximum.Value.ToString();
					}
					else
					{
						schema.Maximum = longRangeRule.Maximum.Value.ToString();
					}
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
			// StringLengthRule can apply to both strings and arrays (collections)
			// Return null to let the schema type be determined from the property's actual type
			Validation.StringLengthRule => null,
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

	static bool IsInlineEnumSchema(IOpenApiSchema schema)
	{
		// Check if this is an inline enum schema (has Enum property with values)
		if (schema is OpenApiSchema openApiSchema)
		{
			// Inline enum schemas have the Enum property set with valid values
			if (openApiSchema.Enum is not null && openApiSchema.Enum.Count > 0)
			{
				return true;
			}

			// Also check if there's a oneOf pattern (nullable enum)
			if (openApiSchema.OneOf is not null && openApiSchema.OneOf.Count > 0)
			{
				// Check if any of the oneOf schemas is an inline enum
				return openApiSchema.OneOf.Any(IsInlineEnumSchema);
			}
		}

		return false;
	}

	/// <summary>
	/// Converts primitive type references (like System.Int32, System.String) to inline schemas
	/// with explicit type and format information. This improves OpenAPI documentation by making
	/// types explicit instead of requiring clients to resolve $ref links.
	/// </summary>
	static IOpenApiSchema InlinePrimitiveTypeReference(IOpenApiSchema itemSchema, OpenApiDocument document)
	{
		// If not a reference, return as-is
		if (itemSchema is not OpenApiSchemaReference schemaRef)
		{
			return itemSchema;
		}

		string? refId = schemaRef.Reference?.Id;
		if (string.IsNullOrEmpty(refId))
		{
			return itemSchema;
		}

		// Only inline primitive system types, not custom types
		if (!refId.StartsWith("System."))
		{
			return itemSchema;
		}

		// Create inline schema for primitive types
		OpenApiSchema inlineSchema = new();

		if (refId.Contains("System.String") && !refId.Contains("[]"))
		{
			inlineSchema.Type = JsonSchemaType.String;
		}
		else if (refId.Contains("System.Int32"))
		{
			inlineSchema.Type = JsonSchemaType.Integer;
			inlineSchema.Format = "int32";
		}
		else if (refId.Contains("System.Int64"))
		{
			inlineSchema.Type = JsonSchemaType.Integer;
			inlineSchema.Format = "int64";
		}
		else if (refId.Contains("System.Decimal"))
		{
			inlineSchema.Type = JsonSchemaType.Number;
			// decimal doesn't have a standard format, leave null
		}
		else if (refId.Contains("System.Double"))
		{
			inlineSchema.Type = JsonSchemaType.Number;
			inlineSchema.Format = "double";
		}
		else if (refId.Contains("System.Single"))
		{
			inlineSchema.Type = JsonSchemaType.Number;
			inlineSchema.Format = "float";
		}
		else if (refId.Contains("System.Boolean"))
		{
			inlineSchema.Type = JsonSchemaType.Boolean;
		}
		else if (refId.Contains("System.DateTime") || refId.Contains("System.DateTimeOffset"))
		{
			inlineSchema.Type = JsonSchemaType.String;
			inlineSchema.Format = "date-time";
		}
		else if (refId.Contains("System.Guid"))
		{
			inlineSchema.Type = JsonSchemaType.String;
			inlineSchema.Format = "uuid";
		}
		else
		{
			// For other types (non-primitives), keep the reference
			return itemSchema;
		}

		return inlineSchema;
	}
}
