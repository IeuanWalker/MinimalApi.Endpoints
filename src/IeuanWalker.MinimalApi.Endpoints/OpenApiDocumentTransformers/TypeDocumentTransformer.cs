using System.Reflection;
using System.Text.Json.Nodes;
using IeuanWalker.MinimalApi.Endpoints.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Routing;
using Microsoft.OpenApi;

namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers;

/// <summary>
/// OpenAPI document transformer that ensures all property types and formats are correctly documented.
/// This transformer runs before other transformers to establish a baseline of correct type information.
/// It handles:
/// - Schema properties (primitives, arrays, nested objects)
/// - Query and path parameters
/// - IFormFile and IFormFileCollection types
/// - Nullable types
/// </summary>
sealed class TypeDocumentTransformer : IOpenApiDocumentTransformer
{
	public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
	{
		// Step 1: Fix all schema property types
		FixSchemaPropertyTypes(document);

		// Step 2: Fix nullable array structures (arrays with nullable items should be nullable arrays instead)
		FixNullableArrays(document);

		// Step 3: Fix double-wrapped arrays (defensive cleanup)
		FixDoubleWrappedArrays(document);

		// Step 4: Build endpoint-to-request-type mapping
		Dictionary<string, Type> endpointToRequestType = BuildEndpointToRequestTypeMapping(context, document);

		// Step 5: Fix parameter types (query/path parameters)
		FixParameterTypes(document, endpointToRequestType);

		return Task.CompletedTask;
	}

	static void FixSchemaPropertyTypes(OpenApiDocument document)
	{
		if (document.Components?.Schemas is null)
		{
			return;
		}

		// Process each schema in the document
		foreach (KeyValuePair<string, IOpenApiSchema> schemaEntry in document.Components.Schemas.ToList())
		{
			if (schemaEntry.Value is not OpenApiSchema schema)
			{
				continue;
			}

			// Try to find the .NET type for this schema
			Type? schemaType = FindTypeForSchema(schemaEntry.Key);

			// Fix properties in this schema
			if (schema.Properties is not null && schema.Properties.Count > 0)
			{
				foreach (KeyValuePair<string, IOpenApiSchema> property in schema.Properties.ToList())
				{
					// Get the property's actual .NET type if possible
					PropertyInfo? propertyInfo = schemaType?.GetProperty(property.Key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
					
					schema.Properties[property.Key] = FixSchemaType(property.Value, document, propertyInfo?.PropertyType);
				}
			}

			// Fix array item types
			if (schema.Items is not null)
			{
				schema.Items = FixSchemaType(schema.Items, document, null);
			}

			// Fix the schema itself (for primitives defined at the schema level)
			FixInlineSchemaType(schema, schemaEntry.Key);
		}
	}

	static void FixNullableArrays(OpenApiDocument document)
	{
		if (document.Components?.Schemas is null)
		{
			return;
		}

		// Process each schema in the document
		foreach (KeyValuePair<string, IOpenApiSchema> schemaEntry in document.Components.Schemas.ToList())
		{
			if (schemaEntry.Value is not OpenApiSchema schema)
			{
				continue;
			}

			// Fix properties that are nullable arrays
			if (schema.Properties is not null && schema.Properties.Count > 0)
			{
				foreach (KeyValuePair<string, IOpenApiSchema> property in schema.Properties.ToList())
				{
					schema.Properties[property.Key] = FixNullableArraySchema(property.Value);
				}
			}
		}
	}

	static void FixDoubleWrappedArrays(OpenApiDocument document)
	{
		if (document.Components?.Schemas is null)
		{
			return;
		}

		// Process each schema in the document
		foreach (KeyValuePair<string, IOpenApiSchema> schemaEntry in document.Components.Schemas.ToList())
		{
			if (schemaEntry.Value is not OpenApiSchema schema)
			{
				continue;
			}

			// Fix properties that might have double-wrapped arrays
			if (schema.Properties is not null && schema.Properties.Count > 0)
			{
				foreach (KeyValuePair<string, IOpenApiSchema> property in schema.Properties.ToList())
				{
					schema.Properties[property.Key] = UnwrapDoubleArrays(property.Value);
				}
			}
		}
	}

	static IOpenApiSchema UnwrapDoubleArrays(IOpenApiSchema schema)
	{
		if (schema is not OpenApiSchema openApiSchema)
		{
			return schema;
		}

		// Check if this is an array with items that are also an array
		// BUT only unwrap if the inner array's items are a reference to a non-array type (not a primitive array type)
		// This prevents unwrapping legitimate nested arrays like int[][] or List<List<T>>
		if (openApiSchema.Type == JsonSchemaType.Array &&
			openApiSchema.Items is OpenApiSchema itemsSchema &&
			itemsSchema.Type == JsonSchemaType.Array &&
			itemsSchema.Items is not null)
		{
			// Only unwrap if the innermost items are a reference to a complex type (not a primitive or another array)
			if (itemsSchema.Items is OpenApiSchemaReference ||
				(itemsSchema.Items is OpenApiSchema innerSchema && 
				 innerSchema.Type == JsonSchemaType.Object))
			{
				// This is likely an incorrectly double-wrapped array - unwrap it
				return new OpenApiSchema
				{
					Type = JsonSchemaType.Array,
					Items = itemsSchema.Items
				};
			}
		}

		// Check if this is a oneOf with an array that has double-wrapped items
		if (openApiSchema.OneOf is not null && openApiSchema.OneOf.Count > 0)
		{
			for (int i = 0; i < openApiSchema.OneOf.Count; i++)
			{
				if (openApiSchema.OneOf[i] is OpenApiSchema oneOfSchema &&
					oneOfSchema.Type == JsonSchemaType.Array &&
					oneOfSchema.Items is OpenApiSchema itemsSchema2 &&
					itemsSchema2.Type == JsonSchemaType.Array &&
					itemsSchema2.Items is not null)
				{
					// Only unwrap if the innermost items are a reference to a complex type (not a primitive or another array)
					if (itemsSchema2.Items is OpenApiSchemaReference ||
						(itemsSchema2.Items is OpenApiSchema innerSchema &&
						 innerSchema.Type == JsonSchemaType.Object))
					{
						// This oneOf element has an incorrectly double-wrapped array - unwrap it
						openApiSchema.OneOf[i] = new OpenApiSchema
						{
							Type = JsonSchemaType.Array,
							Items = itemsSchema2.Items
						};
					}
				}
			}
		}

		return schema;
	}

	static IOpenApiSchema FixNullableArraySchema(IOpenApiSchema schema)
	{
		if (schema is not OpenApiSchema openApiSchema)
		{
			return schema;
		}

		// Check if this is an array with items that have oneOf for nullable
		if (openApiSchema.Type == JsonSchemaType.Array && 
			openApiSchema.Items is OpenApiSchema itemsSchema && 
			itemsSchema.OneOf is not null && 
			itemsSchema.OneOf.Count == 2)
		{
			// Check if one of the oneOf elements is just a nullable marker (no type set)
			IOpenApiSchema? typeSchema = null;
			
			foreach (IOpenApiSchema oneOfSchema in itemsSchema.OneOf)
			{
				if (oneOfSchema is OpenApiSchema os && os.Type.HasValue && os.Type != JsonSchemaType.Null)
				{
					typeSchema = os;
					break;
				}
			}
			
			// If we found a type schema, this is a nullable array - restructure it
			if (typeSchema is not null)
			{
				// Create new array with corrected items (non-nullable)
				OpenApiSchema correctedArraySchema = new()
				{
					Type = JsonSchemaType.Array,
					Items = typeSchema
				};
				
				// Wrap the array in oneOf for nullable
				OpenApiSchema nullableArraySchema = new()
				{
					OneOf =
					[
						new OpenApiSchema
						{
							Extensions = new Dictionary<string, IOpenApiExtension>
							{
								["nullable"] = new JsonNodeExtension(JsonValue.Create(true)!)
							}
						},
						correctedArraySchema
					]
				};
				return nullableArraySchema;
			}
		}

		return schema;
	}

	static IOpenApiSchema FixSchemaType(IOpenApiSchema schema, OpenApiDocument document, Type? actualPropertyType = null)
	{
		// If we know the actual property type and it's an array/collection, ensure the schema reflects that
		if (actualPropertyType is not null && schema is OpenApiSchema openApiSchema)
		{
			Type actualType = Nullable.GetUnderlyingType(actualPropertyType) ?? actualPropertyType;
			bool isNullable = Nullable.GetUnderlyingType(actualPropertyType) is not null;
			
			// Check if this is an array or collection type
			bool isArrayOrCollection = actualType.IsArray || 
				(actualType.IsGenericType && (
					actualType.GetGenericTypeDefinition() == typeof(List<>) ||
					actualType.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
					actualType.GetGenericTypeDefinition() == typeof(ICollection<>) ||
					actualType.GetGenericTypeDefinition() == typeof(IReadOnlyList<>) ||
					actualType.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>)));
			
			if (isArrayOrCollection)
			{
				// Check if the schema already represents a nullable collection properly (for reference types like List<T>?)
				// If it has oneOf with a nullable marker and either an array or a collection reference, skip our processing
				if (openApiSchema.OneOf is not null && openApiSchema.OneOf.Count == 2)
				{
					bool hasNullableMarker = openApiSchema.OneOf.Any(s => 
						s is OpenApiSchema os && !os.Type.HasValue);
					bool hasArray = openApiSchema.OneOf.Any(s => 
						s is OpenApiSchema os && os.Type == JsonSchemaType.Array);
					bool hasCollectionRef = openApiSchema.OneOf.Any(s => 
						s is OpenApiSchemaReference sr && sr.Reference?.Id is not null && 
						(sr.Reference.Id.Contains("System.Collections.Generic.List") ||
						 sr.Reference.Id.Contains("System.Collections.Generic.IEnumerable") ||
						 sr.Reference.Id.Contains("System.Collections.Generic.ICollection") ||
						 sr.Reference.Id.Contains("System.Collections.Generic.IReadOnlyList") ||
						 sr.Reference.Id.Contains("System.Collections.Generic.IReadOnlyCollection") ||
						 sr.Reference.Id.Contains("[]")));
					
					if (hasNullableMarker && (hasArray || hasCollectionRef))
					{
						// This is already a nullable array/collection
						// Don't wrap it again, but let the normal oneOf processing below handle inlining
						// We just need to skip the array wrapping logic for this case
						return schema;
					}
				}
				
				// Case 1: Schema doesn't have type: array yet, needs wrapping
				// But only wrap if this is truly an element type, not a complex structure
				if (openApiSchema.Type != JsonSchemaType.Array && openApiSchema.Items is null)
				{
					// Skip wrapping if the schema has properties (it's a complex object, not an element type)
					if (openApiSchema.Properties is not null && openApiSchema.Properties.Count > 0)
					{
						// This is a complex object schema, not an element type - don't wrap
						return openApiSchema;
					}
					
					// The current schema represents the element type
					// Check if it has a oneOf wrapper (this would be wrong for array element types)
					IOpenApiSchema itemsSchema = openApiSchema;
					
					// If the schema has oneOf for nullable, unwrap it since the array itself is nullable, not the elements
					if (isNullable && openApiSchema.OneOf is not null && openApiSchema.OneOf.Count > 0)
					{
						// Find the non-nullable schema in the oneOf
						IOpenApiSchema? nonNullableSchema = openApiSchema.OneOf
							.FirstOrDefault(s => s is OpenApiSchema os && os.Extensions is not null && !os.Extensions.ContainsKey("nullable"));
						if (nonNullableSchema is not null)
						{
							itemsSchema = nonNullableSchema;
						}
					}
					
					// Create array schema with the corrected items schema
					OpenApiSchema arraySchema = new()
					{
						Type = JsonSchemaType.Array,
						Items = itemsSchema
					};
					
					// For nullable arrays/collections, wrap in oneOf
					if (isNullable)
					{
						OpenApiSchema nullableArraySchema = new()
						{
							OneOf =
							[
								new OpenApiSchema
								{
									Extensions = new Dictionary<string, IOpenApiExtension>
									{
										["nullable"] = new JsonNodeExtension(JsonValue.Create(true)!)
									}
								},
								arraySchema
							]
						};
						return nullableArraySchema;
					}
					
					return arraySchema;
				}
				// Case 2: Schema already has type: array - check if items have wrong nullable structure
				// This handles cases where the framework generates array with nullable items when it should be nullable array
				else if (openApiSchema.Type == JsonSchemaType.Array)
				{
					IOpenApiSchema? items = openApiSchema.Items;
					
					// Check if items have a oneOf for nullable - this indicates the array is nullable, not the items
					if (items is OpenApiSchema itemsSchema && itemsSchema.OneOf is not null && itemsSchema.OneOf.Count == 2)
					{
						// Check if one of the oneOf elements is just a nullable marker (has no type set)
						IOpenApiSchema? nullableMarker = null;
						IOpenApiSchema? typeSchema = null;
						
						foreach (IOpenApiSchema oneOfSchema in itemsSchema.OneOf)
						{
							if (oneOfSchema is OpenApiSchema os)
							{
								// Check if this is a nullable marker (no type, just extensions or nothing)
								if (!os.Type.HasValue || os.Type == JsonSchemaType.Null)
								{
									nullableMarker = os;
								}
								else
								{
									typeSchema = os;
								}
							}
						}
						
						// If we found both a nullable marker and a type schema, this is a nullable array
						if (nullableMarker is not null && typeSchema is not null)
						{
							// Create new array with corrected items (non-nullable)
							OpenApiSchema correctedArraySchema = new()
							{
								Type = JsonSchemaType.Array,
								Items = typeSchema
							};
							
							// Wrap the array in oneOf for nullable
							OpenApiSchema nullableArraySchema = new()
							{
								OneOf =
								[
									new OpenApiSchema
									{
										Extensions = new Dictionary<string, IOpenApiExtension>
										{
											["nullable"] = new JsonNodeExtension(JsonValue.Create(true)!)
										}
									},
									correctedArraySchema
								]
							};
							return nullableArraySchema;
						}
					}
				}
			}
		}

		// Handle OpenApiSchemaReference - inline primitive types
		if (schema is OpenApiSchemaReference schemaRef)
		{
			return InlinePrimitiveTypeReference(schemaRef, document);
		}

		// Handle OpenApiSchema - fix type information
		if (schema is OpenApiSchema openApiSchemaToFix)
		{
			// Fix oneOf patterns (nullable types)
			if (openApiSchemaToFix.OneOf is not null && openApiSchemaToFix.OneOf.Count > 0)
			{
				// Fix each schema in oneOf
				for (int i = 0; i < openApiSchemaToFix.OneOf.Count; i++)
				{
					openApiSchemaToFix.OneOf[i] = FixSchemaType(openApiSchemaToFix.OneOf[i], document, null);
				}
			}

			// Fix array item types
			if (openApiSchemaToFix.Items is not null)
			{
				openApiSchemaToFix.Items = FixSchemaType(openApiSchemaToFix.Items, document, null);
			}

			// Fix nested properties
			if (openApiSchemaToFix.Properties is not null && openApiSchemaToFix.Properties.Count > 0)
			{
				foreach (KeyValuePair<string, IOpenApiSchema> property in openApiSchemaToFix.Properties.ToList())
				{
					openApiSchemaToFix.Properties[property.Key] = FixSchemaType(property.Value, document, null);
				}
			}

			return openApiSchemaToFix;
		}

		return schema;
	}

	static void FixInlineSchemaType(OpenApiSchema schema, string schemaName)
	{
		// For schemas that don't have a type set, try to infer it from the schema name
		// This is particularly important for IFormFile and other special types
		if (!schema.Type.HasValue || schema.Type == JsonSchemaType.Null)
		{
			// Check for IFormFile types
			if (schemaName.Contains("Microsoft.AspNetCore.Http.IFormFile"))
			{
				schema.Type = JsonSchemaType.String;
				schema.Format = "binary";
			}
			else if (schemaName.Contains("Microsoft.AspNetCore.Http.IFormFileCollection"))
			{
				schema.Type = JsonSchemaType.Array;
				schema.Items = new OpenApiSchema
				{
					Type = JsonSchemaType.String,
					Format = "binary"
				};
			}
		}
	}

	static void FixParameterTypes(OpenApiDocument document, Dictionary<string, Type> endpointToRequestType)
	{
		if (document.Paths is null)
		{
			return;
		}

		// Build a lookup cache for path matching
		Dictionary<string, Type?> pathMatchCache = new(StringComparer.OrdinalIgnoreCase);

		// Iterate through all paths and operations
		foreach (KeyValuePair<string, IOpenApiPathItem> pathItem in document.Paths)
		{
			if (pathItem.Value is not OpenApiPathItem pathItemValue)
			{
				continue;
			}

			string pathPattern = pathItem.Key;

			// Try to find matching endpoint - use cache to avoid redundant PathsMatch calls
			if (!pathMatchCache.TryGetValue(pathPattern, out Type? pathRequestType))
			{
				pathRequestType = endpointToRequestType
					.Where(mapping => PathsMatch(pathPattern, mapping.Key))
					.Select(mapping => mapping.Value)
					.FirstOrDefault();

				pathMatchCache[pathPattern] = pathRequestType;
			}

			foreach (KeyValuePair<HttpMethod, OpenApiOperation> operation in pathItemValue.Operations ?? [])
			{
				if (operation.Value.Parameters is null || operation.Value.Parameters.Count == 0)
				{
					continue;
				}

				// Process each parameter
				foreach (OpenApiParameter parameter in operation.Value.Parameters.Cast<OpenApiParameter>())
				{
					if (parameter.Schema is null || string.IsNullOrEmpty(parameter.Name))
					{
						continue;
					}

					// Fix the parameter schema type
					parameter.Schema = FixParameterSchemaType(parameter.Schema, parameter.Name, pathRequestType);
				}
			}
		}
	}

	static IOpenApiSchema FixParameterSchemaType(IOpenApiSchema schema, string parameterName, Type? requestType)
	{
		// Try to get the actual property type from the request model
		if (requestType is not null)
		{
			PropertyInfo? property = requestType.GetProperty(parameterName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
			if (property is not null)
			{
				return CreateSchemaFromType(property.PropertyType);
			}
		}

		// If we can't determine the type from the request model, fix the schema as-is
		if (schema is OpenApiSchemaReference schemaRef)
		{
			return InlinePrimitiveTypeReferenceForParameter(schemaRef);
		}

		if (schema is OpenApiSchema openApiSchema)
		{
			EnsureSchemaHasType(openApiSchema);
		}

		return schema;
	}

	static OpenApiSchema CreateSchemaFromType(Type type)
	{
		// Handle nullable types
		Type actualType = Nullable.GetUnderlyingType(type) ?? type;
		bool isNullable = Nullable.GetUnderlyingType(type) is not null;

		OpenApiSchema schema = new();

		// Handle arrays and collections
		if (actualType.IsArray)
		{
			schema.Type = JsonSchemaType.Array;
			Type elementType = actualType.GetElementType()!;
			schema.Items = CreateSchemaFromType(elementType);
		}
		else if (actualType.IsGenericType &&
				 (actualType.GetGenericTypeDefinition() == typeof(List<>) ||
				  actualType.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
				  actualType.GetGenericTypeDefinition() == typeof(ICollection<>) ||
				  actualType.GetGenericTypeDefinition() == typeof(IReadOnlyList<>) ||
				  actualType.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>)))
		{
			schema.Type = JsonSchemaType.Array;
			Type elementType = actualType.GetGenericArguments()[0];
			schema.Items = CreateSchemaFromType(elementType);
		}
		// Handle primitive types
		else if (actualType == typeof(string))
		{
			schema.Type = JsonSchemaType.String;
		}
		else if (actualType == typeof(int))
		{
			schema.Type = JsonSchemaType.Integer;
			schema.Format = "int32";
		}
		else if (actualType == typeof(long))
		{
			schema.Type = JsonSchemaType.Integer;
			schema.Format = "int64";
		}
		else if (actualType == typeof(short))
		{
			schema.Type = JsonSchemaType.Integer;
			schema.Format = "int32";
		}
		else if (actualType == typeof(byte))
		{
			schema.Type = JsonSchemaType.Integer;
			schema.Format = "int32";
		}
		else if (actualType == typeof(decimal))
		{
			schema.Type = JsonSchemaType.Number;
		}
		else if (actualType == typeof(double))
		{
			schema.Type = JsonSchemaType.Number;
			schema.Format = "double";
		}
		else if (actualType == typeof(float))
		{
			schema.Type = JsonSchemaType.Number;
			schema.Format = "float";
		}
		else if (actualType == typeof(bool))
		{
			schema.Type = JsonSchemaType.Boolean;
		}
		else if (actualType == typeof(DateTime) || actualType == typeof(DateTimeOffset))
		{
			schema.Type = JsonSchemaType.String;
			schema.Format = "date-time";
		}
		else if (actualType == typeof(DateOnly))
		{
			schema.Type = JsonSchemaType.String;
			schema.Format = "date";
		}
		else if (actualType == typeof(TimeOnly))
		{
			schema.Type = JsonSchemaType.String;
			schema.Format = "time";
		}
		else if (actualType == typeof(Guid))
		{
			schema.Type = JsonSchemaType.String;
			schema.Format = "uuid";
		}
		else if (actualType == typeof(IFormFile))
		{
			schema.Type = JsonSchemaType.String;
			schema.Format = "binary";
		}
		else if (actualType == typeof(IFormFileCollection))
		{
			schema.Type = JsonSchemaType.Array;
			schema.Items = new OpenApiSchema
			{
				Type = JsonSchemaType.String,
				Format = "binary"
			};
		}
		else if (actualType.IsEnum)
		{
			// Let the framework handle enum types based on user configuration
			// (can be integer or string depending on serializer settings)
			// Don't modify the schema - let the EnumSchemaTransformer handle it
		}
		// For other complex types, don't override - let the framework handle them
		// This preserves whatever the framework has already decided for the type

		// Wrap in oneOf for nullable types (except string which is already nullable in JSON)
		if (isNullable && actualType != typeof(string))
		{
			OpenApiSchema nullableSchema = new()
			{
				OneOf =
				[
					new OpenApiSchema
					{
						Extensions = new Dictionary<string, IOpenApiExtension>
						{
							["nullable"] = new JsonNodeExtension(System.Text.Json.Nodes.JsonValue.Create(true)!)
						}
					},
					schema
				]
			};
			return nullableSchema;
		}

		return schema;
	}

	static void EnsureSchemaHasType(OpenApiSchema schema)
	{
		// If the schema doesn't have a type, try to infer it
		if (!schema.Type.HasValue || schema.Type == JsonSchemaType.Null)
		{
			// If it has Items, it's an array
			if (schema.Items is not null)
			{
				schema.Type = JsonSchemaType.Array;
			}
			// If it has Properties, it's an object
			else if (schema.Properties is not null && schema.Properties.Count > 0)
			{
				schema.Type = JsonSchemaType.Object;
			}
		}
	}

	static IOpenApiSchema InlinePrimitiveTypeReference(OpenApiSchemaReference schemaRef, OpenApiDocument document)
	{
		string? refId = schemaRef.Reference?.Id;
		if (string.IsNullOrEmpty(refId))
		{
			return schemaRef;
		}

		// Only inline primitive system types and special ASP.NET types, not custom types
		if (!refId.StartsWith("System.") && !refId.StartsWith("Microsoft.AspNetCore.Http.") && !refId.Equals("IFormFile", StringComparison.Ordinal) && !refId.Equals("IFormFileCollection", StringComparison.Ordinal))
		{
			return schemaRef;
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
		else if (refId.Contains("System.Int16"))
		{
			inlineSchema.Type = JsonSchemaType.Integer;
			inlineSchema.Format = "int32";
		}
		else if (refId.Contains("System.Byte"))
		{
			inlineSchema.Type = JsonSchemaType.Integer;
			inlineSchema.Format = "int32";
		}
		else if (refId.Contains("System.Decimal"))
		{
			inlineSchema.Type = JsonSchemaType.Number;
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
		else if (refId.Contains("System.DateOnly"))
		{
			inlineSchema.Type = JsonSchemaType.String;
			inlineSchema.Format = "date";
		}
		else if (refId.Contains("System.TimeOnly"))
		{
			inlineSchema.Type = JsonSchemaType.String;
			inlineSchema.Format = "time";
		}
		else if (refId.Contains("System.Guid"))
		{
			inlineSchema.Type = JsonSchemaType.String;
			inlineSchema.Format = "uuid";
		}
		else if (refId.Contains("Microsoft.AspNetCore.Http.IFormFile") || refId.Equals("IFormFile", StringComparison.Ordinal))
		{
			inlineSchema.Type = JsonSchemaType.String;
			inlineSchema.Format = "binary";
		}
		else if (refId.Contains("Microsoft.AspNetCore.Http.IFormFileCollection") || refId.Equals("IFormFileCollection", StringComparison.Ordinal))
		{
			inlineSchema.Type = JsonSchemaType.Array;
			inlineSchema.Items = new OpenApiSchema
			{
				Type = JsonSchemaType.String,
				Format = "binary"
			};
		}
		// Handle array types
		else if (refId.EndsWith("[]"))
		{
			inlineSchema.Type = JsonSchemaType.Array;
			// Extract element type from the array reference
			string elementRefId = refId[..^2]; // Remove "[]"
			OpenApiSchemaReference elementRef = new(elementRefId, document, null);
			inlineSchema.Items = InlinePrimitiveTypeReference(elementRef, document);
		}
		// Handle List<T> and other generic collection types
		else if (refId.Contains("System.Collections.Generic.List`1") ||
				 refId.Contains("System.Collections.Generic.IEnumerable`1") ||
				 refId.Contains("System.Collections.Generic.ICollection`1") ||
				 refId.Contains("System.Collections.Generic.IReadOnlyList`1") ||
				 refId.Contains("System.Collections.Generic.IReadOnlyCollection`1"))
		{
			inlineSchema.Type = JsonSchemaType.Array;
			// Extract element type from generic parameter
			int startIdx = refId.IndexOf("[[");
			int endIdx = refId.IndexOf(',', startIdx);
			if (startIdx >= 0 && endIdx > startIdx)
			{
				string elementType = refId.Substring(startIdx + 2, endIdx - startIdx - 2);
				OpenApiSchemaReference elementRef = new(elementType, document, null);
				inlineSchema.Items = InlinePrimitiveTypeReference(elementRef, document);
			}
		}
		else
		{
			// For other types (non-primitives), keep the reference
			return schemaRef;
		}

		return inlineSchema;
	}

	static IOpenApiSchema InlinePrimitiveTypeReferenceForParameter(OpenApiSchemaReference schemaRef)
	{
		string? refId = schemaRef.Reference?.Id;
		if (string.IsNullOrEmpty(refId))
		{
			return schemaRef;
		}

		// Only inline primitive system types
		if (!refId.StartsWith("System."))
		{
			return schemaRef;
		}

		OpenApiSchema inlineSchema = new();

		if (refId.Contains("System.String"))
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
		else if (refId.Contains("System.Int16"))
		{
			inlineSchema.Type = JsonSchemaType.Integer;
			inlineSchema.Format = "int32";
		}
		else if (refId.Contains("System.Byte"))
		{
			inlineSchema.Type = JsonSchemaType.Integer;
			inlineSchema.Format = "int32";
		}
		else if (refId.Contains("System.Decimal"))
		{
			inlineSchema.Type = JsonSchemaType.Number;
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
		else if (refId.Contains("System.DateOnly"))
		{
			inlineSchema.Type = JsonSchemaType.String;
			inlineSchema.Format = "date";
		}
		else if (refId.Contains("System.TimeOnly"))
		{
			inlineSchema.Type = JsonSchemaType.String;
			inlineSchema.Format = "time";
		}
		else if (refId.Contains("System.Guid"))
		{
			inlineSchema.Type = JsonSchemaType.String;
			inlineSchema.Format = "uuid";
		}
		else
		{
			return schemaRef;
		}

		return inlineSchema;
	}

	static Dictionary<string, Type> BuildEndpointToRequestTypeMapping(OpenApiDocumentTransformerContext context, OpenApiDocument document)
	{
		Dictionary<string, Type> mapping = new(StringComparer.OrdinalIgnoreCase);

		// Get all endpoints
		if (context.ApplicationServices.GetService(typeof(EndpointDataSource)) is not EndpointDataSource endpointDataSource)
		{
			return mapping;
		}

		// Collect all request types from schemas
		HashSet<Type> requestTypes = [];
		if (document.Components?.Schemas is not null)
		{
			foreach (string schemaName in document.Components.Schemas.Keys)
			{
				Type? type = FindTypeForSchema(schemaName);
				if (type is not null)
				{
					requestTypes.Add(type);
				}
			}
		}

		foreach (Microsoft.AspNetCore.Http.Endpoint endpoint in endpointDataSource.Endpoints)
		{
			if (endpoint is not RouteEndpoint routeEndpoint)
			{
				continue;
			}

			string? routePattern = routeEndpoint.RoutePattern.RawText;
			if (string.IsNullOrEmpty(routePattern))
			{
				continue;
			}

			// Look for the handler method in metadata
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
				.FirstOrDefault(paramType => requestTypes.Contains(paramType));

			if (matchingParamType is not null)
			{
				mapping[routePattern] = matchingParamType;
			}
		}

		return mapping;
	}

	static Type? FindTypeForSchema(string schemaName)
	{
		// Try to find the type by its full name
		string typeName = schemaName.Replace('+', '.');

		// Try to load the type from all loaded assemblies
		return AppDomain.CurrentDomain.GetAssemblies()
			.Select(assembly => assembly.GetType(typeName))
			.FirstOrDefault(type => type is not null);
	}

	static bool PathsMatch(string openApiPath, string routePattern)
	{
		// Direct match
		if (string.Equals(openApiPath, routePattern, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		// Normalize both paths for comparison
		string normalizedOpenApi = openApiPath.Trim('/').ToLowerInvariant();
		string normalizedRoute = routePattern.Trim('/').ToLowerInvariant();

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

			// Check for version parameter matching
			if (routeSeg.StartsWith("v{") &&
				routeSeg.Contains("version", StringComparison.OrdinalIgnoreCase) &&
				routeSeg.EndsWith('}') &&
				openApiSeg.StartsWith('v') &&
				openApiSeg.Length > 1 &&
				openApiSeg[1..].All(char.IsDigit))
			{
				continue;
			}

			// No match
			return false;
		}

		return true;
	}
}
