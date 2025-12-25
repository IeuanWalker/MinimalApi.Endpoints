using FluentValidation;
using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Shouldly;
using Xunit;

namespace IeuanWalker.MinimalApi.Endpoints.Tests;

// Test models - defined outside the test class so they have proper type names
public class StringValidationModel
{
	public string Name { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string Pattern { get; set; } = string.Empty;
	public string LengthRange { get; set; } = string.Empty;
}

public class NumericValidationModel
{
	public int IntValue { get; set; }
	public decimal DecimalValue { get; set; }
	public double DoubleValue { get; set; }
}

public class ComplexValidationModel
{
	public string RequiredString { get; set; } = string.Empty;
	public int? OptionalInt { get; set; }
	public NestedModel Nested { get; set; } = new();
	public List<NestedModel> NestedList { get; set; } = [];
}

public class NestedModel
{
	public string NestedProperty { get; set; } = string.Empty;
}

// Validators
public class StringValidationModelValidator : AbstractValidator<StringValidationModel>
{
	public StringValidationModelValidator()
	{
		RuleFor(x => x.Name)
			.NotEmpty()
			.MinimumLength(2)
			.MaximumLength(50);

		RuleFor(x => x.Email)
			.EmailAddress();

		RuleFor(x => x.Pattern)
			.Matches(@"^[A-Z][a-z]+$");

		RuleFor(x => x.LengthRange)
			.Length(5, 15);
	}
}

public class NumericValidationModelValidator : AbstractValidator<NumericValidationModel>
{
	public NumericValidationModelValidator()
	{
		RuleFor(x => x.IntValue)
			.GreaterThan(0)
			.LessThan(100);

		RuleFor(x => x.DecimalValue)
			.GreaterThanOrEqualTo(0.0m)
			.LessThanOrEqualTo(1000.0m);

		RuleFor(x => x.DoubleValue)
			.InclusiveBetween(0.0, 100.0);
	}
}

public class NestedModelValidator : AbstractValidator<NestedModel>
{
	public NestedModelValidator()
	{
		RuleFor(x => x.NestedProperty)
			.NotEmpty()
			.MaximumLength(100);
	}
}

public class ComplexValidationModelValidator : AbstractValidator<ComplexValidationModel>
{
	public ComplexValidationModelValidator()
	{
		RuleFor(x => x.RequiredString)
			.NotNull()
			.NotEmpty();

		RuleFor(x => x.Nested)
			.NotNull()
			.SetValidator(new NestedModelValidator());

		RuleForEach(x => x.NestedList)
			.SetValidator(new NestedModelValidator());
	}
}

public class FluentValidationSchemaTransformerTests
{

	[Fact]
	public async Task TransformAsync_StringNotEmpty_AddsRequiredAndMinLength()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddSingleton<IValidator<StringValidationModel>, StringValidationModelValidator>();
		var serviceProvider = services.BuildServiceProvider();

		var transformer = new FluentValidationSchemaTransformer(serviceProvider);
		var document = CreateTestDocument<StringValidationModel>();

		// Act
		await transformer.TransformAsync(document, CreateContext(), CancellationToken.None);

		// Assert
		var schema = GetSchema<StringValidationModel>(document);
		schema.Required.ShouldNotBeNull();
		schema.Required.ShouldContain("name");

		var nameProperty = GetPropertySchema(schema, "name");
		nameProperty.Type.ShouldBe(JsonSchemaType.String);
		nameProperty.MinLength.ShouldBe(2);
	}

	[Fact]
	public async Task TransformAsync_StringMaximumLength_AddsMaxLength()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddSingleton<IValidator<StringValidationModel>, StringValidationModelValidator>();
		var serviceProvider = services.BuildServiceProvider();

		var transformer = new FluentValidationSchemaTransformer(serviceProvider);
		var document = CreateTestDocument<StringValidationModel>();

		// Act
		await transformer.TransformAsync(document, CreateContext(), CancellationToken.None);

		// Assert
		var schema = GetSchema<StringValidationModel>(document);
		var nameProperty = GetPropertySchema(schema, "name");
		nameProperty.MaxLength.ShouldBe(50);
	}

	[Fact]
	public async Task TransformAsync_EmailAddress_AddsEmailFormat()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddSingleton<IValidator<StringValidationModel>, StringValidationModelValidator>();
		var serviceProvider = services.BuildServiceProvider();

		var transformer = new FluentValidationSchemaTransformer(serviceProvider);
		var document = CreateTestDocument<StringValidationModel>();

		// Act
		await transformer.TransformAsync(document, CreateContext(), CancellationToken.None);

		// Assert
		var schema = GetSchema<StringValidationModel>(document);
		var emailProperty = GetPropertySchema(schema, "email");
		emailProperty.Format.ShouldBe("email");
	}

	[Fact]
	public async Task TransformAsync_Matches_AddsPattern()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddSingleton<IValidator<StringValidationModel>, StringValidationModelValidator>();
		var serviceProvider = services.BuildServiceProvider();

		var transformer = new FluentValidationSchemaTransformer(serviceProvider);
		var document = CreateTestDocument<StringValidationModel>();

		// Act
		await transformer.TransformAsync(document, CreateContext(), CancellationToken.None);

		// Assert
		var schema = GetSchema<StringValidationModel>(document);
		var patternProperty = GetPropertySchema(schema, "pattern");
		patternProperty.Pattern.ShouldBe(@"^[A-Z][a-z]+$");
	}

	[Fact]
	public async Task TransformAsync_Length_AddsBothMinAndMaxLength()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddSingleton<IValidator<StringValidationModel>, StringValidationModelValidator>();
		var serviceProvider = services.BuildServiceProvider();

		var transformer = new FluentValidationSchemaTransformer(serviceProvider);
		var document = CreateTestDocument<StringValidationModel>();

		// Act
		await transformer.TransformAsync(document, CreateContext(), CancellationToken.None);

		// Assert
		var schema = GetSchema<StringValidationModel>(document);
		var lengthProperty = GetPropertySchema(schema, "lengthRange");
		lengthProperty.MinLength.ShouldBe(5);
		lengthProperty.MaxLength.ShouldBe(15);
	}

	[Fact]
	public async Task TransformAsync_GreaterThan_AddsExclusiveMinimum()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddSingleton<IValidator<NumericValidationModel>, NumericValidationModelValidator>();
		var serviceProvider = services.BuildServiceProvider();

		var transformer = new FluentValidationSchemaTransformer(serviceProvider);
		var document = CreateTestDocument<NumericValidationModel>();

		// Act
		await transformer.TransformAsync(document, CreateContext(), CancellationToken.None);

		// Assert
		var schema = GetSchema<NumericValidationModel>(document);
		schema.Extensions.ShouldNotBeNull("Schema should have extensions");
		schema.Extensions.ShouldContainKey("x-validation-source");
		
		// Check if property was transformed from reference to inline schema
		var intProperty = schema.Properties["intValue"];
		intProperty.ShouldNotBeNull("intValue property should exist");
		intProperty.ShouldBeOfType<OpenApiSchema>("Property should be transformed to OpenApiSchema");
		
		var intSchema = (OpenApiSchema)intProperty;
		intSchema.Type.ShouldBe(JsonSchemaType.Integer);
		intSchema.Minimum.ShouldBe("0");
		intSchema.ExclusiveMinimum.ShouldBe("true");
	}

	[Fact]
	public async Task TransformAsync_LessThan_AddsExclusiveMaximum()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddSingleton<IValidator<NumericValidationModel>, NumericValidationModelValidator>();
		var serviceProvider = services.BuildServiceProvider();

		var transformer = new FluentValidationSchemaTransformer(serviceProvider);
		var document = CreateTestDocument<NumericValidationModel>();

		// Act
		await transformer.TransformAsync(document, CreateContext(), CancellationToken.None);

		// Assert
		var schema = GetSchema<NumericValidationModel>(document);
		var intProperty = GetPropertySchema(schema, "intValue");
		intProperty.Maximum.ShouldBe("100");
		intProperty.ExclusiveMaximum.ShouldBe("true");
	}

	[Fact]
	public async Task TransformAsync_GreaterThanOrEqual_AddsInclusiveMinimum()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddSingleton<IValidator<NumericValidationModel>, NumericValidationModelValidator>();
		var serviceProvider = services.BuildServiceProvider();

		var transformer = new FluentValidationSchemaTransformer(serviceProvider);
		var document = CreateTestDocument<NumericValidationModel>();

		// Act
		await transformer.TransformAsync(document, CreateContext(), CancellationToken.None);

		// Assert
		var schema = GetSchema<NumericValidationModel>(document);
		var decimalProperty = GetPropertySchema(schema, "decimalValue");
		decimalProperty.Minimum.ShouldBe("0");
		decimalProperty.ExclusiveMinimum.ShouldBe("false");
	}

	[Fact]
	public async Task TransformAsync_LessThanOrEqual_AddsInclusiveMaximum()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddSingleton<IValidator<NumericValidationModel>, NumericValidationModelValidator>();
		var serviceProvider = services.BuildServiceProvider();

		var transformer = new FluentValidationSchemaTransformer(serviceProvider);
		var document = CreateTestDocument<NumericValidationModel>();

		// Act
		await transformer.TransformAsync(document, CreateContext(), CancellationToken.None);

		// Assert
		var schema = GetSchema<NumericValidationModel>(document);
		var decimalProperty = GetPropertySchema(schema, "decimalValue");
		decimalProperty.Maximum.ShouldBe("1000");
		decimalProperty.ExclusiveMaximum.ShouldBe("false");
	}

	[Fact]
	public async Task TransformAsync_InclusiveBetween_AddsMinAndMaxInclusive()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddSingleton<IValidator<NumericValidationModel>, NumericValidationModelValidator>();
		var serviceProvider = services.BuildServiceProvider();

		var transformer = new FluentValidationSchemaTransformer(serviceProvider);
		var document = CreateTestDocument<NumericValidationModel>();

		// Act
		await transformer.TransformAsync(document, CreateContext(), CancellationToken.None);

		// Assert
		var schema = GetSchema<NumericValidationModel>(document);
		var doubleProperty = GetPropertySchema(schema, "doubleValue");
		doubleProperty.Minimum.ShouldBe("0");
		doubleProperty.ExclusiveMinimum.ShouldBe("false");
		doubleProperty.Maximum.ShouldBe("100");
		doubleProperty.ExclusiveMaximum.ShouldBe("false");
	}

	[Fact]
	public async Task TransformAsync_NotNull_AddsToRequired()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddSingleton<IValidator<ComplexValidationModel>, ComplexValidationModelValidator>();
		var serviceProvider = services.BuildServiceProvider();

		var transformer = new FluentValidationSchemaTransformer(serviceProvider);
		var document = CreateTestDocument<ComplexValidationModel>();

		// Act
		await transformer.TransformAsync(document, CreateContext(), CancellationToken.None);

		// Assert
		var schema = GetSchema<ComplexValidationModel>(document);
		schema.Required.ShouldNotBeNull();
		schema.Required.ShouldContain("requiredString");
		schema.Required.ShouldContain("nested");
	}

	[Fact]
	public async Task TransformAsync_ComplexType_PreservesReference()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddSingleton<IValidator<ComplexValidationModel>, ComplexValidationModelValidator>();
		services.AddSingleton<IValidator<NestedModel>, NestedModelValidator>();
		var serviceProvider = services.BuildServiceProvider();

		var transformer = new FluentValidationSchemaTransformer(serviceProvider);
		var document = CreateTestDocument<ComplexValidationModel>();

		// Act
		await transformer.TransformAsync(document, CreateContext(), CancellationToken.None);

		// Assert
		var schema = GetSchema<ComplexValidationModel>(document);
		var nestedProperty = schema.Properties["nested"];
		nestedProperty.ShouldBeOfType<OpenApiSchemaReference>();
	}

	[Fact]
	public async Task TransformAsync_AddsValidationSourceExtension()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddSingleton<IValidator<StringValidationModel>, StringValidationModelValidator>();
		var serviceProvider = services.BuildServiceProvider();

		var transformer = new FluentValidationSchemaTransformer(serviceProvider);
		var document = CreateTestDocument<StringValidationModel>();

		// Act
		await transformer.TransformAsync(document, CreateContext(), CancellationToken.None);

		// Assert
		var schema = GetSchema<StringValidationModel>(document);
		schema.Extensions.ShouldNotBeNull();
		schema.Extensions.ShouldContainKey("x-validation-source");
	}

	[Fact]
	public async Task TransformAsync_NoValidator_DoesNotModifySchema()
	{
		// Arrange
		var services = new ServiceCollection();
		var serviceProvider = services.BuildServiceProvider();

		var transformer = new FluentValidationSchemaTransformer(serviceProvider);
		var document = CreateTestDocument<StringValidationModel>();

		// Act
		await transformer.TransformAsync(document, CreateContext(), CancellationToken.None);

		// Assert
		var schema = GetSchema<StringValidationModel>(document);
		(schema.Extensions is null || schema.Extensions.Count == 0).ShouldBeTrue();
		(schema.Required is null || schema.Required.Count == 0).ShouldBeTrue();
	}

	[Fact]
	public async Task TransformAsync_NestedValidator_ProcessesBothSchemas()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddSingleton<IValidator<ComplexValidationModel>, ComplexValidationModelValidator>();
		services.AddSingleton<IValidator<NestedModel>, NestedModelValidator>();
		var serviceProvider = services.BuildServiceProvider();

		var transformer = new FluentValidationSchemaTransformer(serviceProvider);
		var document = CreateTestDocument<ComplexValidationModel>();
		AddSchemaToDocument<NestedModel>(document);

		// Act
		await transformer.TransformAsync(document, CreateContext(), CancellationToken.None);

		// Assert
		var parentSchema = GetSchema<ComplexValidationModel>(document);
		parentSchema.Extensions.ShouldContainKey("x-validation-source");

		var nestedSchema = GetSchema<NestedModel>(document);
		nestedSchema.Extensions.ShouldContainKey("x-validation-source");
		nestedSchema.Required.ShouldContain("nestedProperty");
	}

	[Fact]
	public async Task TransformAsync_NullDocument_ReturnsImmediately()
	{
		// Arrange
		var services = new ServiceCollection();
		var serviceProvider = services.BuildServiceProvider();
		var transformer = new FluentValidationSchemaTransformer(serviceProvider);
		var document = new OpenApiDocument();

		// Act & Assert - Should not throw
		await transformer.TransformAsync(document, CreateContext(), CancellationToken.None);
	}

	[Fact]
	public async Task TransformAsync_EmptySchemas_ReturnsImmediately()
	{
		// Arrange
		var services = new ServiceCollection();
		var serviceProvider = services.BuildServiceProvider();
		var transformer = new FluentValidationSchemaTransformer(serviceProvider);
		var document = new OpenApiDocument
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
			}
		};

		// Act & Assert - Should not throw
		await transformer.TransformAsync(document, CreateContext(), CancellationToken.None);
	}

	// Helper methods
	static OpenApiDocument CreateTestDocument<T>()
	{
		var document = new OpenApiDocument
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
			}
		};

		AddSchemaToDocument<T>(document);
		return document;
	}

	static void AddSchemaToDocument<T>(OpenApiDocument document)
	{
		var typeName = typeof(T).FullName!;
		var schema = new OpenApiSchema
		{
			Type = JsonSchemaType.Object,
			Properties = new Dictionary<string, IOpenApiSchema>()
		};

		// Add properties based on the type
		foreach (var prop in typeof(T).GetProperties())
		{
			var propName = char.ToLowerInvariant(prop.Name[0]) + prop.Name[1..];
			var propType = prop.PropertyType;

			IOpenApiSchema propSchema;
			if (propType == typeof(string))
			{
				propSchema = new OpenApiSchemaReference("System.String", null, null);
			}
			else if (propType == typeof(int) || propType == typeof(int?))
			{
				propSchema = new OpenApiSchemaReference("System.Int32", null, null);
			}
			else if (propType == typeof(decimal))
			{
				propSchema = new OpenApiSchemaReference("System.Decimal", null, null);
			}
			else if (propType == typeof(double))
			{
				propSchema = new OpenApiSchemaReference("System.Double", null, null);
			}
			else if (propType == typeof(bool))
			{
				propSchema = new OpenApiSchemaReference("System.Boolean", null, null);
			}
			else if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(List<>))
			{
				propSchema = new OpenApiSchemaReference($"System.Collections.Generic.List`1[[{propType.GetGenericArguments()[0].FullName}]]", null, null);
			}
			else
			{
				propSchema = new OpenApiSchemaReference(propType.FullName!, null, null);
			}

			schema.Properties[propName] = propSchema;
		}

		document.Components.Schemas[typeName] = schema;
	}

	static OpenApiSchema GetSchema<T>(OpenApiDocument document)
	{
		var typeName = typeof(T).FullName!;
		return (OpenApiSchema)document.Components.Schemas[typeName];
	}

	static OpenApiSchema GetPropertySchema(OpenApiSchema schema, string propertyName)
	{
		var property = schema.Properties[propertyName];
		// After transformation, primitive types should be OpenApiSchema, not references
		return property as OpenApiSchema ?? throw new InvalidOperationException($"Property {propertyName} was not transformed to OpenApiSchema");
	}

	static OpenApiDocumentTransformerContext CreateContext()
	{
		return new OpenApiDocumentTransformerContext
		{
			DocumentName = "v1",
			ApplicationServices = null!,
			DescriptionGroups = []
		};
	}
}
