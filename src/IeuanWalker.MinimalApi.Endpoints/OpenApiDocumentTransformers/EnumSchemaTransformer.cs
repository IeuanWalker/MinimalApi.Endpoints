using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers;

/// <summary>
/// Transforms OpenAPI document by enriching enum schemas with value information and member names.
/// This transformer adds the 'enum' array with valid values and 'x-enum-varnames' extension with member names
/// to provide better API documentation for enums.
/// </summary>
sealed class EnumSchemaTransformer : IOpenApiDocumentTransformer
{
	// Cache for type lookups to avoid repeatedly scanning all assemblies
	static readonly Dictionary<string, Type?> typeCache = [];
	static readonly Lock typeCacheLock = new();

	public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
	{
		if (document.Components?.Schemas is null)
		{
			return Task.CompletedTask;
		}

		// Process each schema in the document
		foreach (KeyValuePair<string, IOpenApiSchema> schemaEntry in document.Components.Schemas.ToList())
		{
			if (schemaEntry.Value is not OpenApiSchema schema)
			{
				continue;
			}

			// Try to find the corresponding .NET type for this schema
			Type? foundType = FindTypeForSchema(schemaEntry.Key);
			if (foundType is null)
			{
				continue;
			}

			// Check if it's a direct or nullable enum type
			Type? enumType;
			if (foundType.IsEnum)
			{
				enumType = foundType;
			}
			else
			{
				enumType = GetEnumTypeFromNullable(foundType);
				if (enumType is null)
				{
					// Not an enum or nullable enum we can handle, skip this schema
					continue;
				}
			}

			// Enrich the enum schema with values and names
			EnrichEnumSchema(schema, enumType);
		}

		return Task.CompletedTask;
	}

	static Type? FindTypeForSchema(string schemaName)
	{
		// Check cache first
		lock (typeCacheLock)
		{
			if (typeCache.TryGetValue(schemaName, out Type? cachedType))
			{
				return cachedType;
			}
		}

		// Try to find the type by its full name
		// Schema names are typically the full type name with + replaced by .
		string typeName = schemaName.Replace('+', '.');

		// Try to load the type from all loaded assemblies
		Type? foundType = AppDomain.CurrentDomain.GetAssemblies()
			.Select(assembly => assembly.GetType(typeName))
			.FirstOrDefault(type => type is not null);

		// Cache the result (including null to avoid repeated lookups)
		lock (typeCacheLock)
		{
			typeCache[schemaName] = foundType;
		}

		return foundType;
	}

	static Type? GetEnumTypeFromNullable(Type type)
	{
		// Check if it's a nullable type
		if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
		{
			// Get the underlying type
			Type underlyingType = Nullable.GetUnderlyingType(type)!;

			// Check if the underlying type is an enum
			if (underlyingType.IsEnum)
			{
				return underlyingType;
			}
		}

		return null;
	}

	static void EnrichEnumSchema(OpenApiSchema schema, Type enumType)
	{
		// Get all enum values
		Array enumValues = Enum.GetValues(enumType);
		string[] enumNames = Enum.GetNames(enumType);

		// Convert enum values to their underlying numeric values for the schema
		List<JsonNode> values = [];
		List<string> varNames = [];
		Dictionary<string, string> descriptions = [];

		for (int i = 0; i < enumValues.Length; i++)
		{
			object enumValue = enumValues.GetValue(i)!;
			string enumName = enumNames[i];

			// Convert to underlying type value (integer)
			long numericValue = Convert.ToInt64(enumValue);
			values.Add(JsonValue.Create(numericValue)!);
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

		// Add the enum values array - use JsonNodeExtension for the enum property
		schema.Extensions ??= new Dictionary<string, IOpenApiExtension>();
		schema.Extensions["enum"] = new JsonNodeExtension(new JsonArray(values.ToArray()));

		// Add x-enum-varnames extension for member names
		// This is a common extension supported by many OpenAPI tools
		schema.Extensions["x-enum-varnames"] = new JsonNodeExtension(new JsonArray(varNames.Select(n => JsonValue.Create(n)!).ToArray()));

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

		// Add a description to the schema if it doesn't have one
		if (string.IsNullOrWhiteSpace(schema.Description))
		{
			schema.Description = $"Enum: {string.Join(", ", varNames)}";
		}
	}
}
