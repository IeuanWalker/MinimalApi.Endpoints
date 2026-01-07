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
	public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
	{
		if (document.Components?.Schemas is null)
		{
			return Task.CompletedTask;
		}

		foreach (KeyValuePair<string, IOpenApiSchema> schemaEntry in document.Components.Schemas.ToList())
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

			EnrichEnumSchema(schema, enumType);
		}

		return Task.CompletedTask;
	}

	static void EnrichEnumSchema(OpenApiSchema schema, Type enumType)
	{
		Array enumValues = Enum.GetValues(enumType);
		string[] enumNames = Enum.GetNames(enumType);

		JsonArray valueArray = [];
		JsonArray varNamesArray = [];
		JsonObject? descObj = null;

		for (int i = 0; i < enumValues.Length; i++)
		{
			object enumValue = enumValues.GetValue(i)!;
			string enumName = enumNames[i];

			long numericValue = Convert.ToInt64(enumValue);
			valueArray.Add(JsonValue.Create(numericValue)!);
			varNamesArray.Add(JsonValue.Create(enumName)!);

			FieldInfo? field = enumType.GetField(enumName);
			DescriptionAttribute? descriptionAttr = field?.GetCustomAttributes(typeof(DescriptionAttribute), false)
				.OfType<DescriptionAttribute>()
				.FirstOrDefault();

			if (descriptionAttr is not null && !string.IsNullOrWhiteSpace(descriptionAttr.Description))
			{
				descObj ??= [];
				descObj[enumName] = descriptionAttr.Description;
			}
		}

		schema.Extensions ??= new Dictionary<string, IOpenApiExtension>();
		schema.Extensions["enum"] = new JsonNodeExtension(valueArray);
		schema.Extensions["x-enum-varnames"] = new JsonNodeExtension(varNamesArray);

		if (descObj is not null)
		{
			schema.Extensions["x-enum-descriptions"] = new JsonNodeExtension(descObj);
		}

		if (string.IsNullOrWhiteSpace(schema.Description))
		{
			schema.Description = $"Enum: {string.Join(", ", enumNames)}";
		}
	}
}
