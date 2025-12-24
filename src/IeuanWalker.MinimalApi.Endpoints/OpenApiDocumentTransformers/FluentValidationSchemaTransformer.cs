using System.Text.Json.Nodes;
using FluentValidation;
using FluentValidation.Validators;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers;

/// <summary>
/// Transforms OpenAPI schemas by enriching them with FluentValidation rules.
/// Extracts validation constraints from FluentValidation validators and applies them to the OpenAPI schema.
/// </summary>
public class FluentValidationSchemaTransformer : IOpenApiSchemaTransformer
{
	readonly IServiceProvider _serviceProvider;

	public FluentValidationSchemaTransformer(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
	}

	public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
	{
		// Get the validator for this type from DI
		Type validatorType = typeof(IValidator<>).MakeGenericType(context.JsonTypeInfo.Type);
		object? validator = _serviceProvider.GetService(validatorType);

		if (validator is null)
		{
			// No validator registered for this type
			return Task.CompletedTask;
		}

		// Get the validator descriptor to access validation rules
		IValidator typedValidator = (IValidator)validator;
		IValidatorDescriptor descriptor = typedValidator.CreateDescriptor();

		// Track which properties are required
		HashSet<string> requiredProperties = [];

		// Process each validation rule
		foreach (IValidationRule rule in descriptor.GetRulesForMember(""))
		{
			ProcessRule(rule, schema, requiredProperties);
		}

		// Apply required properties to schema
		if (requiredProperties.Count > 0)
		{
			schema.Required ??= new HashSet<string>();
			foreach (string prop in requiredProperties)
			{
				schema.Required.Add(prop);
			}
		}

		// Add extension to indicate schema is enriched with FluentValidation
		schema.Extensions ??= new Dictionary<string, IOpenApiExtension>();
		schema.Extensions["x-validation-source"] = new JsonNodeExtension(JsonValue.Create("FluentValidation"));

		return Task.CompletedTask;
	}

	static void ProcessRule(IValidationRule rule, OpenApiSchema schema, HashSet<string> requiredProperties)
	{
		string? propertyName = rule.PropertyName;
		if (string.IsNullOrEmpty(propertyName))
		{
			return;
		}

		// Convert property name to camelCase for OpenAPI
		string schemaPropertyName = ToCamelCase(propertyName);

		// Ensure the property exists in the schema
		if (schema.Properties is null || !schema.Properties.ContainsKey(schemaPropertyName))
		{
			return;
		}

		IOpenApiSchema propertySchemaInterface = schema.Properties[schemaPropertyName];
		
		// Cast to OpenApiSchema to be able to modify properties
		if (propertySchemaInterface is not OpenApiSchema propertySchema)
		{
			return;
		}

		// Process each component validator for this property
		foreach (IPropertyValidator validator in rule.Components.Select(c => c.Validator))
		{
			ApplyValidatorConstraints(validator, propertySchema, requiredProperties, schemaPropertyName);
		}
	}

	static void ApplyValidatorConstraints(
		IPropertyValidator validator,
		OpenApiSchema propertySchema,
		HashSet<string> requiredProperties,
		string propertyName)
	{
		switch (validator)
		{
			case INotNullValidator:
			case INotEmptyValidator:
				requiredProperties.Add(propertyName);
				// For strings, NotEmpty means minLength = 1
				if (IsStringSchema(propertySchema))
				{
					propertySchema.MinLength = 1;
				}
				break;

			// Check specific length validators before the general ILengthValidator
			case IMinimumLengthValidator minLengthValidator:
				if (IsStringSchema(propertySchema) && minLengthValidator.Min > 0)
				{
					propertySchema.MinLength = minLengthValidator.Min;
				}
				break;

			case IMaximumLengthValidator maxLengthValidator:
				if (IsStringSchema(propertySchema) && maxLengthValidator.Max > 0)
				{
					propertySchema.MaxLength = maxLengthValidator.Max;
				}
				break;

			case ILengthValidator lengthValidator:
				if (IsStringSchema(propertySchema))
				{
					if (lengthValidator.Max > 0)
					{
						propertySchema.MaxLength = lengthValidator.Max;
					}
					if (lengthValidator.Min > 0)
					{
						propertySchema.MinLength = lengthValidator.Min;
					}
				}
				break;

			case IComparisonValidator comparisonValidator:
				ApplyComparisonConstraints(comparisonValidator, propertySchema);
				break;

			case IBetweenValidator betweenValidator:
				ApplyBetweenConstraints(betweenValidator, propertySchema);
				break;

			case IRegularExpressionValidator regexValidator:
				if (IsStringSchema(propertySchema) && !string.IsNullOrEmpty(regexValidator.Expression))
				{
					propertySchema.Pattern = regexValidator.Expression;
				}
				break;

			case IEmailValidator:
				if (IsStringSchema(propertySchema))
				{
					propertySchema.Format = "email";
				}
				break;
		}
	}

	static void ApplyComparisonConstraints(IComparisonValidator comparisonValidator, OpenApiSchema propertySchema)
	{
		object? valueToCompare = comparisonValidator.ValueToCompare;
		if (valueToCompare is null)
		{
			return;
		}

		string? numericValue = ConvertToString(valueToCompare);
		if (numericValue is null)
		{
			return;
		}

		switch (comparisonValidator.Comparison)
		{
			case Comparison.GreaterThan:
				propertySchema.Minimum = numericValue;
				propertySchema.ExclusiveMinimum = "true";
				break;
			case Comparison.GreaterThanOrEqual:
				propertySchema.Minimum = numericValue;
				propertySchema.ExclusiveMinimum = "false";
				break;
			case Comparison.LessThan:
				propertySchema.Maximum = numericValue;
				propertySchema.ExclusiveMaximum = "true";
				break;
			case Comparison.LessThanOrEqual:
				propertySchema.Maximum = numericValue;
				propertySchema.ExclusiveMaximum = "false";
				break;
		}
	}

	static void ApplyBetweenConstraints(IBetweenValidator betweenValidator, OpenApiSchema propertySchema)
	{
		string? from = ConvertToString(betweenValidator.From);
		string? to = ConvertToString(betweenValidator.To);

		if (from is not null)
		{
			propertySchema.Minimum = from;
			propertySchema.ExclusiveMinimum = "false";
		}

		if (to is not null)
		{
			propertySchema.Maximum = to;
			propertySchema.ExclusiveMaximum = "false";
		}
	}

	static string? ConvertToString(object value)
	{
		try
		{
			return Convert.ToDecimal(value).ToString(System.Globalization.CultureInfo.InvariantCulture);
		}
		catch (InvalidCastException)
		{
			return null;
		}
		catch (FormatException)
		{
			return null;
		}
		catch (OverflowException)
		{
			return null;
		}
	}

	static bool IsStringSchema(OpenApiSchema schema)
	{
		// In OpenAPI 3.1, Type is JsonSchemaType? enum
		return schema.Type.HasValue && schema.Type.Value == JsonSchemaType.String;
	}

	static string ToCamelCase(string str)
	{
		if (string.IsNullOrEmpty(str) || char.IsLower(str[0]))
		{
			return str;
		}

		return char.ToLowerInvariant(str[0]) + str[1..];
	}
}
