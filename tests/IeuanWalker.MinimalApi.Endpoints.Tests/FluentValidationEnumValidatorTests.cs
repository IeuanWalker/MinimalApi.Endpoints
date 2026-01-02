using FluentValidation;
using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Shouldly;
using Xunit;

namespace IeuanWalker.MinimalApi.Endpoints.Tests;

public enum TestEnumForValidator
{
	Value1,
	Value2,
	Value3
}

public class StringEnumModel
{
	public string EnumAsString { get; set; } = string.Empty;
}

public class StringEnumModelValidator : AbstractValidator<StringEnumModel>
{
	public StringEnumModelValidator()
	{
		RuleFor(x => x.EnumAsString)
			.NotEmpty()
			.IsEnumName(typeof(TestEnumForValidator), caseSensitive: false);
	}
}

public class FluentValidationEnumValidatorTests
{
	[Fact]
	public async Task TransformAsync_IsEnumName_AddsEnumValuesToStringProperty()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddSingleton<IValidator<StringEnumModel>, StringEnumModelValidator>();
		var serviceProvider = services.BuildServiceProvider();

		var transformer = new FluentValidationSchemaTransformer(serviceProvider);
		var document = CreateTestDocument<StringEnumModel>();

		// Act
		await transformer.TransformAsync(document, CreateContext(serviceProvider), CancellationToken.None);

		// Assert
		OpenApiSchema schema = GetSchema<StringEnumModel>(document);
		schema.ShouldNotBeNull();
		
		var enumStringProperty = GetPropertySchema(schema, "enumAsString");
		enumStringProperty.ShouldNotBeNull();
		
		// Should have enum extensions
		enumStringProperty.Extensions.ShouldContainKey("enum");
		enumStringProperty.Extensions.ShouldContainKey("x-enum-varnames");
		
		// Description should mention the enum values
		enumStringProperty.Description.ShouldContain("Value1");
		enumStringProperty.Description.ShouldContain("Value2");
		enumStringProperty.Description.ShouldContain("Value3");
	}

	static OpenApiDocument CreateTestDocument<T>()
	{
		var document = new OpenApiDocument
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
			}
		};

		string typeName = typeof(T).FullName!;
		var schema = new OpenApiSchema
		{
			Type = JsonSchemaType.Object,
			Properties = new Dictionary<string, IOpenApiSchema>()
		};

		// Add properties based on type
		foreach (var prop in typeof(T).GetProperties())
		{
			var propName = char.ToLowerInvariant(prop.Name[0]) + prop.Name[1..];
			IOpenApiSchema propSchema = new OpenApiSchemaReference($"System.{prop.PropertyType.Name}", null, null);
			schema.Properties[propName] = propSchema;
		}

		document.Components.Schemas[typeName] = schema;
		return document;
	}

	static OpenApiDocumentTransformerContext CreateContext(IServiceProvider serviceProvider)
	{
		return new OpenApiDocumentTransformerContext
		{
			DocumentName = "v1",
			ApplicationServices = serviceProvider,
			DescriptionGroups = []
		};
	}

	static OpenApiSchema GetSchema<T>(OpenApiDocument document)
	{
		var typeName = typeof(T).FullName!;
		return (OpenApiSchema)document.Components!.Schemas[typeName];
	}

	static OpenApiSchema GetPropertySchema(OpenApiSchema schema, string propertyName)
	{
		var property = schema.Properties[propertyName];
		return property as OpenApiSchema ?? throw new InvalidOperationException($"Property {propertyName} was not transformed to OpenApiSchema");
	}
}
