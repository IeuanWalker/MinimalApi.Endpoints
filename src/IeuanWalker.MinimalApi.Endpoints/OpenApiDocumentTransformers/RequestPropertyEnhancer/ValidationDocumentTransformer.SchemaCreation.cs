using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Nodes;
using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer.Core;
using IeuanWalker.MinimalApi.Endpoints.Validation;
using Microsoft.OpenApi;
using ValidationRule = IeuanWalker.MinimalApi.Endpoints.Validation.ValidationRule;

namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer;

partial class ValidationDocumentTransformer
{
	internal static OpenApiSchema CreateInlineSchemaWithAllValidation(IOpenApiSchema originalSchema, List<ValidationRule> rules, bool typeAppendRulesToPropertyDescription, bool appendRulesToPropertyDescription, OpenApiDocument document)
	{
		bool? perPropertySetting = rules.FirstOrDefault(r => r.AppendRuleToPropertyDescription.HasValue)?.AppendRuleToPropertyDescription;
		bool effectiveListRulesInDescription = perPropertySetting ?? typeAppendRulesToPropertyDescription;

		OpenApiSchemaHelper.TryAsOpenApiSchema(originalSchema, out OpenApiSchema? originalOpenApiSchema);

		bool isNullableWrapper = originalOpenApiSchema?.OneOf is not null && originalOpenApiSchema.OneOf.Count > 0;
		IOpenApiSchema? actualSchema = originalOpenApiSchema;
		IOpenApiSchema? typeSourceSchema = null;

		if (isNullableWrapper)
		{
			actualSchema = originalOpenApiSchema!.OneOf?.FirstOrDefault(s =>
			{
				if (s is OpenApiSchemaReference)
				{
					return true;
				}
				if (OpenApiSchemaHelper.TryAsOpenApiSchema(s, out OpenApiSchema? schema))
				{
					return schema!.Type.HasValue || schema.Properties?.Count > 0 || schema.AllOf?.Count > 0 ||
						   schema.MaxLength.HasValue || schema.MinLength.HasValue ||
						   schema.Maximum is not null || schema.Minimum is not null;
				}
				return false;
			});

			typeSourceSchema = originalOpenApiSchema!.OneOf?.FirstOrDefault(s => s is OpenApiSchemaReference);

			if (actualSchema is not null && actualSchema != originalOpenApiSchema)
			{
				originalOpenApiSchema = actualSchema as OpenApiSchema;
			}
		}

		actualSchema ??= originalSchema;

		JsonSchemaType? referenceType = null;
		string? referenceFormat = null;
		bool isNullableReference = false;
		OpenApiSchema? resolvedReferenceSchema = null;

		if (actualSchema is OpenApiSchemaReference schemaRef)
		{
			string? refId = schemaRef.Reference?.Id;
			if (refId is not null)
			{
				if (document.Components?.Schemas?.TryGetValue(refId, out IOpenApiSchema? referencedSchema) == true)
				{
					resolvedReferenceSchema = referencedSchema as OpenApiSchema;
				}

				if (SchemaConstants.IsNullableType(refId))
				{
					isNullableWrapper = true;
					isNullableReference = true;
				}

				(referenceType, referenceFormat) = DetermineTypeFromRefId(refId);
			}
		}

		if (typeSourceSchema is OpenApiSchemaReference typeSrcRef && typeSrcRef != actualSchema && !referenceType.HasValue)
		{
			string? refId = typeSrcRef.Reference?.Id;
			if (refId is not null && SchemaConstants.IsCollectionType(refId))
			{
				referenceType = JsonSchemaType.Array;
			}
		}

		bool isCustomTypeReference = false;
		OpenApiSchemaReference? customTypeRef = null;
		if (actualSchema is OpenApiSchemaReference schemaRef2)
		{
			string? refId = schemaRef2.Reference?.Id;
			if (refId is not null && !SchemaConstants.IsSystemType(refId))
			{
				isCustomTypeReference = true;
				customTypeRef = schemaRef2;
			}
		}

		if (isCustomTypeReference && customTypeRef is not null)
		{
			return CreateCustomTypeReferenceSchema(customTypeRef, rules, effectiveListRulesInDescription, appendRulesToPropertyDescription, document);
		}

		bool isComplexObject = (originalOpenApiSchema?.AllOf?.Count > 0) ||
							   (originalOpenApiSchema?.Properties?.Count > 0 && originalOpenApiSchema.Type == JsonSchemaType.Object);

		if (isComplexObject && originalOpenApiSchema is not null)
		{
			return CreateComplexObjectSchema(originalOpenApiSchema, rules, effectiveListRulesInDescription, appendRulesToPropertyDescription);
		}

		JsonSchemaType? schemaType = null;
		string? format = null;

		bool isArrayType = resolvedReferenceSchema?.Type == JsonSchemaType.Array ||
						   originalOpenApiSchema?.Type == JsonSchemaType.Array ||
						   referenceType == JsonSchemaType.Array;

		if (isArrayType)
		{
			schemaType = originalOpenApiSchema?.Type ?? resolvedReferenceSchema?.Type ?? referenceType;
			format = originalOpenApiSchema?.Format ?? resolvedReferenceSchema?.Format ?? referenceFormat;
		}
		else
		{
			schemaType = rules.Select(GetSchemaType).FirstOrDefault(t => t is not null);
			format = rules.Select(GetSchemaFormat).FirstOrDefault(f => f is not null);

			if (!schemaType.HasValue)
			{
				if (referenceType.HasValue)
				{
					schemaType = referenceType;
					format ??= referenceFormat;
				}
				else if (originalOpenApiSchema?.Type is not null)
				{
					schemaType = originalOpenApiSchema.Type;
					format ??= originalOpenApiSchema.Format;
				}
				else if (resolvedReferenceSchema?.Type is not null)
				{
					schemaType = resolvedReferenceSchema.Type;
					format ??= resolvedReferenceSchema.Format;
				}
			}

			format ??= referenceFormat ?? resolvedReferenceSchema?.Format ?? originalOpenApiSchema?.Format;
		}

		OpenApiSchema newInlineSchema = new();

		if (schemaType.HasValue)
		{
			newInlineSchema.Type = schemaType.Value;
		}
		else if (isArrayType || originalOpenApiSchema?.Items is not null || resolvedReferenceSchema?.Items is not null)
		{
			newInlineSchema.Type = JsonSchemaType.Array;
		}

		if (format is not null)
		{
			newInlineSchema.Format = format;
		}

		SetArrayItems(newInlineSchema, originalOpenApiSchema, resolvedReferenceSchema, actualSchema, typeSourceSchema, isArrayType, document);

		if (originalOpenApiSchema?.Enum is not null && originalOpenApiSchema.Enum.Count > 0)
		{
			newInlineSchema.Enum = originalOpenApiSchema.Enum;
		}
		else if (resolvedReferenceSchema?.Enum is not null && resolvedReferenceSchema.Enum.Count > 0)
		{
			newInlineSchema.Enum = resolvedReferenceSchema.Enum;
		}

		string? customDescription = rules.OfType<DescriptionRule>().FirstOrDefault()?.Description;
		List<string> ruleDescriptions = [];

		foreach (ValidationRule rule in rules)
		{
			if (rule is DescriptionRule)
			{
				continue;
			}

			if (effectiveListRulesInDescription && rule is not EnumRule)
			{
				string? ruleDescription = rule.ErrorMessage;
				if (!string.IsNullOrEmpty(ruleDescription))
				{
					ruleDescriptions.Add(ruleDescription);
				}
			}

			if (!IsCustomRule(rule))
			{
				ApplyRuleToSchema(rule, newInlineSchema);
			}
		}

		List<string> descriptionParts = [];

		string? enumDescription = null;
		if (!string.IsNullOrEmpty(newInlineSchema.Description) && newInlineSchema.Description.StartsWith("Enum:"))
		{
			enumDescription = newInlineSchema.Description;
		}

		if (!string.IsNullOrEmpty(enumDescription))
		{
			descriptionParts.Add(enumDescription);
		}
		else if (!string.IsNullOrEmpty(customDescription))
		{
			descriptionParts.Add(customDescription);
		}

		if (appendRulesToPropertyDescription && ruleDescriptions.Count > 0 && effectiveListRulesInDescription)
		{
			string rulesSection = "Validation rules:\n" + string.Join("\n", ruleDescriptions.Distinct().Select(msg => $"- {msg}"));
			descriptionParts.Add(rulesSection);
		}

		if (descriptionParts.Count > 0)
		{
			newInlineSchema.Description = string.Join("\n\n", descriptionParts);
		}

		if (originalOpenApiSchema?.Extensions is not null && originalOpenApiSchema.Extensions.Count > 0)
		{
			newInlineSchema.Extensions ??= new Dictionary<string, IOpenApiExtension>();
			foreach (KeyValuePair<string, IOpenApiExtension> extension in originalOpenApiSchema.Extensions)
			{
				newInlineSchema.Extensions[extension.Key] = extension.Value;
			}
		}

		if (isNullableWrapper && !isNullableReference)
		{
			newInlineSchema.Extensions?.Remove(OpenApiConstants.NullableExtension);
			if (newInlineSchema.Extensions?.Count == 0)
			{
				newInlineSchema.Extensions = null;
			}

			return new OpenApiSchema
			{
				OneOf =
				[
					OpenApiSchemaHelper.CreateNullableMarker(),
					newInlineSchema
				]
			};
		}

		return newInlineSchema;
	}

	static (JsonSchemaType? type, string? format) DetermineTypeFromRefId(string refId)
	{
		if (refId.EndsWith(SchemaConstants.ArraySuffix) || SchemaConstants.IsCollectionType(refId))
		{
			return (JsonSchemaType.Array, null);
		}
		if (refId.Contains(SchemaConstants.SystemString) && !refId.Contains(SchemaConstants.ArraySuffix))
		{
			return (JsonSchemaType.String, null);
		}
		if (refId.Contains(SchemaConstants.SystemInt32))
		{
			return (JsonSchemaType.Integer, SchemaConstants.FormatInt32);
		}
		if (refId.Contains(SchemaConstants.SystemInt64))
		{
			return (JsonSchemaType.Integer, SchemaConstants.FormatInt64);
		}
		if (refId.Contains(SchemaConstants.SystemDecimal) || refId.Contains(SchemaConstants.SystemDouble) || refId.Contains(SchemaConstants.SystemSingle))
		{
			string? fmt = refId.Contains(SchemaConstants.SystemDouble) ? SchemaConstants.FormatDouble :
						  refId.Contains(SchemaConstants.SystemSingle) ? SchemaConstants.FormatFloat : null;
			return (JsonSchemaType.Number, fmt);
		}
		if (refId.Contains(SchemaConstants.SystemBoolean))
		{
			return (JsonSchemaType.Boolean, null);
		}
		if (refId.Contains(SchemaConstants.SystemDateTime) || refId.Contains(SchemaConstants.SystemDateTimeOffset))
		{
			return (JsonSchemaType.String, SchemaConstants.FormatDateTime);
		}
		if (refId.Contains(SchemaConstants.SystemGuid))
		{
			return (JsonSchemaType.String, SchemaConstants.FormatUuid);
		}
		return (null, null);
	}

	static OpenApiSchema CreateCustomTypeReferenceSchema(OpenApiSchemaReference customTypeRef, List<ValidationRule> rules, bool effectiveListRulesInDescription, bool appendRulesToPropertyDescription, OpenApiDocument document)
	{
		string? customDescription = rules.OfType<DescriptionRule>().FirstOrDefault()?.Description;
		List<string> ruleDescriptions = [];

		if (effectiveListRulesInDescription)
		{
			foreach (RequiredRule _ in rules.OfType<RequiredRule>())
			{
				ruleDescriptions.Add("Is required");
			}
		}

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

		string? refId = customTypeRef.Reference?.Id;
		if (refId is not null &&
			document.Components?.Schemas?.TryGetValue(refId, out IOpenApiSchema? referencedSchema) == true &&
			referencedSchema is OpenApiSchema refSchema &&
			refSchema.Enum?.Count > 0)
		{
			return new OpenApiSchema
			{
				Type = refSchema.Type,
				Format = refSchema.Format,
				Enum = refSchema.Enum,
				Extensions = refSchema.Extensions,
				Description = descriptionParts.Count > 0 ? string.Join("\n\n", descriptionParts) : refSchema.Description
			};
		}

		return new OpenApiSchema
		{
			AllOf = [customTypeRef],
			Description = descriptionParts.Count > 0 ? string.Join("\n\n", descriptionParts) : null
		};
	}

	static OpenApiSchema CreateComplexObjectSchema(OpenApiSchema originalSchema, List<ValidationRule> rules, bool effectiveListRulesInDescription, bool appendRulesToPropertyDescription)
	{
		string? customDescription = rules.OfType<DescriptionRule>().FirstOrDefault()?.Description;
		List<string> ruleDescriptions = [];

		if (effectiveListRulesInDescription)
		{
			foreach (RequiredRule _ in rules.OfType<RequiredRule>())
			{
				ruleDescriptions.Add("Required");
			}
		}

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

		return new OpenApiSchema
		{
			AllOf = originalSchema.AllOf,
			Properties = originalSchema.Properties,
			Type = originalSchema.Type,
			Format = originalSchema.Format,
			Items = originalSchema.Items,
			AdditionalProperties = originalSchema.AdditionalProperties,
			Extensions = originalSchema.Extensions,
			Description = descriptionParts.Count > 0 ? string.Join("\n\n", descriptionParts) : originalSchema.Description
		};
	}

	static void SetArrayItems(OpenApiSchema newSchema, OpenApiSchema? originalSchema, OpenApiSchema? resolvedSchema, IOpenApiSchema? actualSchema, IOpenApiSchema? typeSourceSchema, bool isArrayType, OpenApiDocument document)
	{
		if (originalSchema?.Items is not null)
		{
			newSchema.Items = originalSchema.Items;
			EnsureArrayType(newSchema);
		}
		else if (resolvedSchema?.Items is not null)
		{
			newSchema.Items = resolvedSchema.Items;
			EnsureArrayType(newSchema);
		}
		else if (isArrayType && actualSchema is OpenApiSchemaReference arraySchemaRef)
		{
			SetArrayItemsFromCollectionRef(newSchema, arraySchemaRef.Reference?.Id, document);
		}
		else if (isArrayType && typeSourceSchema is OpenApiSchemaReference typeSrcRef)
		{
			SetArrayItemsFromCollectionRef(newSchema, typeSrcRef.Reference?.Id, document);
		}
	}

	static void SetArrayItemsFromCollectionRef(OpenApiSchema newSchema, string? refId, OpenApiDocument document)
	{
		if (refId is null || !SchemaConstants.IsCollectionType(refId))
		{
			return;
		}

		int startIdx = refId.IndexOf("[[");// TODO: handle multi-dimensional arrays
		int endIdx = refId.IndexOf(',', startIdx);
		if (startIdx >= 0 && endIdx > startIdx)
		{
			string elementType = refId.Substring(startIdx + 2, endIdx - startIdx - 2);
			OpenApiSchemaReference elementRef = new(elementType, document, null);
			newSchema.Items = elementRef;
			EnsureArrayType(newSchema);
		}
	}

	static void EnsureArrayType(OpenApiSchema schema)
	{
		if (!schema.Type.HasValue || schema.Type == JsonSchemaType.Null)
		{
			schema.Type = JsonSchemaType.Array;
		}
	}

	static bool IsCustomRule(ValidationRule rule)
	{
		Type ruleType = rule.GetType();
		return ruleType.IsGenericType && ruleType.GetGenericTypeDefinition() == typeof(IeuanWalker.MinimalApi.Endpoints.Validation.CustomRule<>);
	}

	static void ApplyRuleToSchema(ValidationRule rule, OpenApiSchema schema)
	{
		switch (rule)
		{
			case RequiredRule:
				break;

			case StringLengthRule stringLengthRule:
				if (stringLengthRule.MinLength.HasValue)
				{
					schema.MinLength = stringLengthRule.MinLength.Value;
				}
				if (stringLengthRule.MaxLength.HasValue)
				{
					schema.MaxLength = stringLengthRule.MaxLength.Value;
				}
				break;

			case PatternRule patternRule:
				schema.Pattern = patternRule.Pattern;
				break;

			case EmailRule:
				schema.Format = SchemaConstants.FormatEmail;
				break;

			case UrlRule:
				schema.Format = SchemaConstants.FormatUri;
				break;

			case IeuanWalker.MinimalApi.Endpoints.Validation.RangeRule<int> intRangeRule:
				ApplyRangeRule(schema, intRangeRule);
				break;

			case IeuanWalker.MinimalApi.Endpoints.Validation.RangeRule<decimal> decimalRangeRule:
				ApplyRangeRule(schema, decimalRangeRule);
				break;

			case IeuanWalker.MinimalApi.Endpoints.Validation.RangeRule<double> doubleRangeRule:
				ApplyRangeRule(schema, doubleRangeRule);
				break;

			case IeuanWalker.MinimalApi.Endpoints.Validation.RangeRule<float> floatRangeRule:
				ApplyRangeRule(schema, floatRangeRule);
				break;

			case IeuanWalker.MinimalApi.Endpoints.Validation.RangeRule<long> longRangeRule:
				ApplyRangeRule(schema, longRangeRule);
				break;

			case EnumRule enumRule:
				EnrichSchemaWithEnumValues(schema, enumRule.EnumType);
				break;
		}
	}

	static void ApplyRangeRule<T>(OpenApiSchema schema, IeuanWalker.MinimalApi.Endpoints.Validation.RangeRule<T> rangeRule) where T : struct, IComparable<T>
	{
		if (rangeRule.Minimum.HasValue)
		{
			if (rangeRule.ExclusiveMinimum)
			{
				schema.ExclusiveMinimum = rangeRule.Minimum.Value.ToString();
			}
			else
			{
				schema.Minimum = rangeRule.Minimum.Value.ToString();
			}
		}
		if (rangeRule.Maximum.HasValue)
		{
			if (rangeRule.ExclusiveMaximum)
			{
				schema.ExclusiveMaximum = rangeRule.Maximum.Value.ToString();
			}
			else
			{
				schema.Maximum = rangeRule.Maximum.Value.ToString();
			}
		}
	}

	static JsonSchemaType? GetSchemaType(ValidationRule rule)
	{
		return rule switch
		{
			StringLengthRule => null,
			PatternRule or EmailRule or UrlRule => JsonSchemaType.String,
			EnumRule enumRule => GetEnumRuleSchemaType(enumRule),
			IeuanWalker.MinimalApi.Endpoints.Validation.RangeRule<int> or IeuanWalker.MinimalApi.Endpoints.Validation.RangeRule<long> => JsonSchemaType.Integer,
			IeuanWalker.MinimalApi.Endpoints.Validation.RangeRule<decimal> or IeuanWalker.MinimalApi.Endpoints.Validation.RangeRule<double> or IeuanWalker.MinimalApi.Endpoints.Validation.RangeRule<float> => JsonSchemaType.Number,
			_ => null
		};
	}

	static JsonSchemaType GetEnumRuleSchemaType(EnumRule enumRule)
	{
		Type propertyType = enumRule.PropertyType;
		Type actualType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

		if (actualType == typeof(string))
		{
			return JsonSchemaType.String;
		}

		if (actualType == typeof(int) || actualType == typeof(long) ||
			actualType == typeof(short) || actualType == typeof(byte) ||
			actualType.IsEnum)
		{
			return JsonSchemaType.Integer;
		}

		return JsonSchemaType.String;
	}

	static string? GetSchemaFormat(ValidationRule rule)
	{
		return rule switch
		{
			EmailRule => SchemaConstants.FormatEmail,
			UrlRule => SchemaConstants.FormatUri,
			IeuanWalker.MinimalApi.Endpoints.Validation.RangeRule<int> => SchemaConstants.FormatInt32,
			IeuanWalker.MinimalApi.Endpoints.Validation.RangeRule<long> => SchemaConstants.FormatInt64,
			IeuanWalker.MinimalApi.Endpoints.Validation.RangeRule<float> => SchemaConstants.FormatFloat,
			IeuanWalker.MinimalApi.Endpoints.Validation.RangeRule<double> => SchemaConstants.FormatDouble,
			_ => null
		};
	}

	static void EnrichSchemaWithEnumValues(OpenApiSchema schema, Type enumType)
	{
		Array enumValues = Enum.GetValues(enumType);
		string[] enumNames = Enum.GetNames(enumType);

		List<JsonNode> values = [];
		List<string> varNames = [];
		Dictionary<string, string> descriptions = [];

		bool isStringSchema = schema.Type == JsonSchemaType.String;

		for (int i = 0; i < enumValues.Length; i++)
		{
			object enumValue = enumValues.GetValue(i)!;
			string enumName = enumNames[i];

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

			FieldInfo? field = enumType.GetField(enumName);
			DescriptionAttribute? descriptionAttr = field?.GetCustomAttributes(typeof(DescriptionAttribute), false)
				.OfType<DescriptionAttribute>()
				.FirstOrDefault();

			if (descriptionAttr is not null && !string.IsNullOrWhiteSpace(descriptionAttr.Description))
			{
				descriptions[enumName] = descriptionAttr.Description;
			}
		}


		schema.Extensions ??= new Dictionary<string, IOpenApiExtension>();
		schema.Extensions[SchemaConstants.EnumExtension] = new JsonNodeExtension(new JsonArray(values.ToArray()));

		if (!isStringSchema)
		{
			schema.Extensions[SchemaConstants.EnumVarNamesExtension] = new JsonNodeExtension(new JsonArray(varNames.Select(n => JsonValue.Create(n)!).ToArray()));
		}

		if (descriptions.Count > 0)
		{
			JsonObject descObj = [];
			foreach (KeyValuePair<string, string> kvp in descriptions)
			{
				descObj[kvp.Key] = kvp.Value;
			}
			schema.Extensions[SchemaConstants.EnumDescriptionsExtension] = new JsonNodeExtension(descObj);
		}

		// Always set enum description, replacing any validation error messages
		// Check for common validation error patterns that should be replaced
		bool shouldReplaceDescription = string.IsNullOrWhiteSpace(schema.Description) ||
			schema.Description.Contains("has a range of values") ||
			schema.Description.StartsWith("Validation rules:");

		if (shouldReplaceDescription)
		{
			schema.Description = $"Enum: {string.Join(", ", varNames)}";
		}
		else if (!schema.Description?.Contains("Enum:") ?? false)
		{
			schema.Description = $"Enum: {string.Join(", ", varNames)}\n\n{schema.Description}";
		}
	}
}
