using System.Text.Json.Nodes;
using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer;
using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer.Validation;
using Microsoft.OpenApi;
using ValidationRule = IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer.Validation.ValidationRule;

namespace IeuanWalker.MinimalApi.Endpoints.Tests.OpenApiDocumentTransformers.RequestPropertyEnhancer;

public partial class ValidationDocumentTransformerTests
{
	#region Test Helpers

	static OpenApiDocument CreateTestDocument()
	{
		return new OpenApiDocument
		{
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>()
			}
		};
	}

	static OpenApiSchema CreateStringSchema()
	{
		return new OpenApiSchema
		{
			Type = JsonSchemaType.String
		};
	}

	static OpenApiSchema CreateIntegerSchema()
	{
		return new OpenApiSchema
		{
			Type = JsonSchemaType.Integer,
			Format = "int32"
		};
	}

	static OpenApiSchema CreateNumberSchema()
	{
		return new OpenApiSchema
		{
			Type = JsonSchemaType.Number,
			Format = "double"
		};
	}

	static OpenApiSchema CreateArraySchema(IOpenApiSchema itemsSchema)
	{
		return new OpenApiSchema
		{
			Type = JsonSchemaType.Array,
			Items = itemsSchema
		};
	}

	static OpenApiSchema CreateNullableWrapperSchema(OpenApiSchema innerSchema)
	{
		return new OpenApiSchema
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
				innerSchema
			]
		};
	}

	#endregion

	#region CreateInlineSchemaWithAllValidation - RequiredRule Tests

	[Fact]
	public void CreateInlineSchemaWithAllValidation_WithRequiredRule_SetsDescriptionWhenAppendRulesEnabled()
	{
		// Arrange
		OpenApiDocument document = CreateTestDocument();
		OpenApiSchema originalSchema = CreateStringSchema();
		List<ValidationRule> rules = [new RequiredRule("Name")];

		// Act
		OpenApiSchema result = ValidationDocumentTransformer.CreateInlineSchemaWithAllValidation(
			originalSchema,
			rules,
			typeAppendRulesToPropertyDescription: true,
			appendRulesToPropertyDescription: true,
			document);

		// Assert
		result.Type.ShouldBe(JsonSchemaType.String);
		result.Description.ShouldNotBeNull();
		result.Description.ShouldContain("Is required");
	}

	[Fact]
	public void CreateInlineSchemaWithAllValidation_WithRequiredRule_NoDescriptionWhenAppendRulesDisabled()
	{
		// Arrange
		OpenApiDocument document = CreateTestDocument();
		OpenApiSchema originalSchema = CreateStringSchema();
		List<ValidationRule> rules = [new RequiredRule("Name")];

		// Act
		OpenApiSchema result = ValidationDocumentTransformer.CreateInlineSchemaWithAllValidation(
			originalSchema,
			rules,
			typeAppendRulesToPropertyDescription: false,
			appendRulesToPropertyDescription: false,
			document);

		// Assert
		result.Type.ShouldBe(JsonSchemaType.String);
		result.Description.ShouldBeNull();
	}

	[Fact]
	public void CreateInlineSchemaWithAllValidation_WithRequiredRuleAndCustomMessage_UsesCustomMessage()
	{
		// Arrange
		OpenApiDocument document = CreateTestDocument();
		OpenApiSchema originalSchema = CreateStringSchema();
		List<ValidationRule> rules = [new RequiredRule("Name", "This field is mandatory")];

		// Act
		OpenApiSchema result = ValidationDocumentTransformer.CreateInlineSchemaWithAllValidation(
			originalSchema,
			rules,
			typeAppendRulesToPropertyDescription: true,
			appendRulesToPropertyDescription: true,
			document);

		// Assert
		result.Description.ShouldNotBeNull();
		result.Description.ShouldContain("This field is mandatory");
	}

	#endregion

	#region CreateInlineSchemaWithAllValidation - StringLengthRule Tests

	[Fact]
	public void CreateInlineSchemaWithAllValidation_WithMinLengthOnly_SetsMinLength()
	{
		// Arrange
		OpenApiDocument document = CreateTestDocument();
		OpenApiSchema originalSchema = CreateStringSchema();
		List<ValidationRule> rules = [new StringLengthRule("Name", minLength: 5, maxLength: null)];

		// Act
		OpenApiSchema result = ValidationDocumentTransformer.CreateInlineSchemaWithAllValidation(
			originalSchema,
			rules,
			typeAppendRulesToPropertyDescription: true,
			appendRulesToPropertyDescription: true,
			document);

		// Assert
		result.MinLength.ShouldBe(5);
		result.MaxLength.ShouldBeNull();
	}

	[Fact]
	public void CreateInlineSchemaWithAllValidation_WithMaxLengthOnly_SetsMaxLength()
	{
		// Arrange
		OpenApiDocument document = CreateTestDocument();
		OpenApiSchema originalSchema = CreateStringSchema();
		List<ValidationRule> rules = [new StringLengthRule("Name", minLength: null, maxLength: 100)];

		// Act
		OpenApiSchema result = ValidationDocumentTransformer.CreateInlineSchemaWithAllValidation(
			originalSchema,
			rules,
			typeAppendRulesToPropertyDescription: true,
			appendRulesToPropertyDescription: true,
			document);

		// Assert
		result.MinLength.ShouldBeNull();
		result.MaxLength.ShouldBe(100);
	}

	[Fact]
	public void CreateInlineSchemaWithAllValidation_WithBothMinAndMaxLength_SetsBothConstraints()
	{
		// Arrange
		OpenApiDocument document = CreateTestDocument();
		OpenApiSchema originalSchema = CreateStringSchema();
		List<ValidationRule> rules = [new StringLengthRule("Name", minLength: 3, maxLength: 50)];

		// Act
		OpenApiSchema result = ValidationDocumentTransformer.CreateInlineSchemaWithAllValidation(
			originalSchema,
			rules,
			typeAppendRulesToPropertyDescription: true,
			appendRulesToPropertyDescription: true,
			document);

		// Assert
		result.MinLength.ShouldBe(3);
		result.MaxLength.ShouldBe(50);
		result.Description.ShouldNotBeNull();
		result.Description.ShouldContain("at least 3 characters");
		result.Description.ShouldContain("less than 50 characters");
	}

	#endregion

	#region CreateInlineSchemaWithAllValidation - PatternRule Tests

	[Fact]
	public void CreateInlineSchemaWithAllValidation_WithPatternRule_SetsPattern()
	{
		// Arrange
		OpenApiDocument document = CreateTestDocument();
		OpenApiSchema originalSchema = CreateStringSchema();
		List<ValidationRule> rules = [new PatternRule("Code", @"^[A-Z]{3}-\d{4}$")];

		// Act
		OpenApiSchema result = ValidationDocumentTransformer.CreateInlineSchemaWithAllValidation(
			originalSchema,
			rules,
			typeAppendRulesToPropertyDescription: true,
			appendRulesToPropertyDescription: true,
			document);

		// Assert
		result.Type.ShouldBe(JsonSchemaType.String);
		result.Pattern.ShouldBe(@"^[A-Z]{3}-\d{4}$");
		result.Description.ShouldNotBeNull();
		result.Description.ShouldContain("Must match pattern");
	}

	[Fact]
	public void CreateInlineSchemaWithAllValidation_WithPatternRuleCustomMessage_UsesCustomMessage()
	{
		// Arrange
		OpenApiDocument document = CreateTestDocument();
		OpenApiSchema originalSchema = CreateStringSchema();
		List<ValidationRule> rules = [new PatternRule("Code", @"^\d+$", "Must contain only digits")];

		// Act
		OpenApiSchema result = ValidationDocumentTransformer.CreateInlineSchemaWithAllValidation(
			originalSchema,
			rules,
			typeAppendRulesToPropertyDescription: true,
			appendRulesToPropertyDescription: true,
			document);

		// Assert
		result.Description.ShouldNotBeNull();
		result.Description.ShouldContain("Must contain only digits");
	}

	#endregion

	#region CreateInlineSchemaWithAllValidation - EmailRule Tests

	[Fact]
	public void CreateInlineSchemaWithAllValidation_WithEmailRule_SetsEmailFormat()
	{
		// Arrange
		OpenApiDocument document = CreateTestDocument();
		OpenApiSchema originalSchema = CreateStringSchema();
		List<ValidationRule> rules = [new EmailRule("Email")];

		// Act
		OpenApiSchema result = ValidationDocumentTransformer.CreateInlineSchemaWithAllValidation(
			originalSchema,
			rules,
			typeAppendRulesToPropertyDescription: true,
			appendRulesToPropertyDescription: true,
			document);

		// Assert
		result.Type.ShouldBe(JsonSchemaType.String);
		result.Format.ShouldBe("email");
		result.Description.ShouldNotBeNull();
		result.Description.ShouldContain("valid email address");
	}

	[Fact]
	public void CreateInlineSchemaWithAllValidation_WithEmailRuleCustomMessage_UsesCustomMessage()
	{
		// Arrange
		OpenApiDocument document = CreateTestDocument();
		OpenApiSchema originalSchema = CreateStringSchema();
		List<ValidationRule> rules = [new EmailRule("ContactEmail", "Please provide a valid email")];

		// Act
		OpenApiSchema result = ValidationDocumentTransformer.CreateInlineSchemaWithAllValidation(
			originalSchema,
			rules,
			typeAppendRulesToPropertyDescription: true,
			appendRulesToPropertyDescription: true,
			document);

		// Assert
		result.Description.ShouldNotBeNull();
		result.Description.ShouldContain("Please provide a valid email");
	}

	#endregion

	#region CreateInlineSchemaWithAllValidation - UrlRule Tests

	[Fact]
	public void CreateInlineSchemaWithAllValidation_WithUrlRule_SetsUriFormat()
	{
		// Arrange
		OpenApiDocument document = CreateTestDocument();
		OpenApiSchema originalSchema = CreateStringSchema();
		List<ValidationRule> rules = [new UrlRule("Website")];

		// Act
		OpenApiSchema result = ValidationDocumentTransformer.CreateInlineSchemaWithAllValidation(
			originalSchema,
			rules,
			typeAppendRulesToPropertyDescription: true,
			appendRulesToPropertyDescription: true,
			document);

		// Assert
		result.Type.ShouldBe(JsonSchemaType.String);
		result.Format.ShouldBe("uri");
		result.Description.ShouldNotBeNull();
		result.Description.ShouldContain("valid URL");
	}

	#endregion

	#region CreateInlineSchemaWithAllValidation - RangeRule Tests

	[Fact]
	public void CreateInlineSchemaWithAllValidation_WithIntRangeRuleMinOnly_SetsMinimum()
	{
		// Arrange
		OpenApiDocument document = CreateTestDocument();
		OpenApiSchema originalSchema = CreateIntegerSchema();
		List<ValidationRule> rules = [new RangeRule<int>("Age", minimum: 18)];

		// Act
		OpenApiSchema result = ValidationDocumentTransformer.CreateInlineSchemaWithAllValidation(
			originalSchema,
			rules,
			typeAppendRulesToPropertyDescription: true,
			appendRulesToPropertyDescription: true,
			document);

		// Assert
		result.Type.ShouldBe(JsonSchemaType.Integer);
		result.Minimum.ShouldBe("18");
		result.Maximum.ShouldBeNull();
	}

	[Fact]
	public void CreateInlineSchemaWithAllValidation_WithIntRangeRuleMaxOnly_SetsMaximum()
	{
		// Arrange
		OpenApiDocument document = CreateTestDocument();
		OpenApiSchema originalSchema = CreateIntegerSchema();
		List<ValidationRule> rules = [new RangeRule<int>("Age", maximum: 120)];

		// Act
		OpenApiSchema result = ValidationDocumentTransformer.CreateInlineSchemaWithAllValidation(
			originalSchema,
			rules,
			typeAppendRulesToPropertyDescription: true,
			appendRulesToPropertyDescription: true,
			document);

		// Assert
		result.Maximum.ShouldBe("120");
		result.Minimum.ShouldBeNull();
	}

	[Fact]
	public void CreateInlineSchemaWithAllValidation_WithIntRangeRuleBothBounds_SetsBothMinAndMax()
	{
		// Arrange
		OpenApiDocument document = CreateTestDocument();
		OpenApiSchema originalSchema = CreateIntegerSchema();
		List<ValidationRule> rules = [new RangeRule<int>("Age", minimum: 18, maximum: 120)];

		// Act
		OpenApiSchema result = ValidationDocumentTransformer.CreateInlineSchemaWithAllValidation(
			originalSchema,
			rules,
			typeAppendRulesToPropertyDescription: true,
			appendRulesToPropertyDescription: true,
			document);

		// Assert
		result.Minimum.ShouldBe("18");
		result.Maximum.ShouldBe("120");
	}

	[Fact]
	public void CreateInlineSchemaWithAllValidation_WithExclusiveMinimum_SetsExclusiveMinimum()
	{
		// Arrange
		OpenApiDocument document = CreateTestDocument();
		OpenApiSchema originalSchema = CreateIntegerSchema();
		List<ValidationRule> rules = [new RangeRule<int>("Value", minimum: 0, exclusiveMinimum: true)];

		// Act
		OpenApiSchema result = ValidationDocumentTransformer.CreateInlineSchemaWithAllValidation(
			originalSchema,
			rules,
			typeAppendRulesToPropertyDescription: true,
			appendRulesToPropertyDescription: true,
			document);

		// Assert
		result.ExclusiveMinimum.ShouldBe("0");
		result.Minimum.ShouldBeNull();
	}

	[Fact]
	public void CreateInlineSchemaWithAllValidation_WithExclusiveMaximum_SetsExclusiveMaximum()
	{
		// Arrange
		OpenApiDocument document = CreateTestDocument();
		OpenApiSchema originalSchema = CreateIntegerSchema();
		List<ValidationRule> rules = [new RangeRule<int>("Value", maximum: 100, exclusiveMaximum: true)];

		// Act
		OpenApiSchema result = ValidationDocumentTransformer.CreateInlineSchemaWithAllValidation(
			originalSchema,
			rules,
			typeAppendRulesToPropertyDescription: true,
			appendRulesToPropertyDescription: true,
			document);

		// Assert
		result.ExclusiveMaximum.ShouldBe("100");
		result.Maximum.ShouldBeNull();
	}

	[Fact]
	public void CreateInlineSchemaWithAllValidation_WithDecimalRangeRule_SetsNumericConstraints()
	{
		// Arrange
		OpenApiDocument document = CreateTestDocument();
		OpenApiSchema originalSchema = CreateNumberSchema();
		List<ValidationRule> rules = [new RangeRule<decimal>("Price", minimum: 0.01m, maximum: 999.99m)];

		// Act
		OpenApiSchema result = ValidationDocumentTransformer.CreateInlineSchemaWithAllValidation(
			originalSchema,
			rules,
			typeAppendRulesToPropertyDescription: true,
			appendRulesToPropertyDescription: true,
			document);

		// Assert
		result.Type.ShouldBe(JsonSchemaType.Number);
		result.Minimum.ShouldBe("0.01");
		result.Maximum.ShouldBe("999.99");
	}

	[Fact]
	public void CreateInlineSchemaWithAllValidation_WithLongRangeRule_SetsInt64Constraints()
	{
		// Arrange
		OpenApiDocument document = CreateTestDocument();
		OpenApiSchema originalSchema = new() { Type = JsonSchemaType.Integer, Format = "int64" };
		List<ValidationRule> rules = [new RangeRule<long>("Id", minimum: 1L, maximum: 9999999999L)];

		// Act
		OpenApiSchema result = ValidationDocumentTransformer.CreateInlineSchemaWithAllValidation(
			originalSchema,
			rules,
			typeAppendRulesToPropertyDescription: true,
			appendRulesToPropertyDescription: true,
			document);

		// Assert
		result.Type.ShouldBe(JsonSchemaType.Integer);
		result.Minimum.ShouldBe("1");
		result.Maximum.ShouldBe("9999999999");
	}

	[Fact]
	public void CreateInlineSchemaWithAllValidation_WithDoubleRangeRule_SetsDoubleConstraints()
	{
		// Arrange
		OpenApiDocument document = CreateTestDocument();
		OpenApiSchema originalSchema = CreateNumberSchema();
		List<ValidationRule> rules = [new RangeRule<double>("Temperature", minimum: -273.15, maximum: 1000.0)];

		// Act
		OpenApiSchema result = ValidationDocumentTransformer.CreateInlineSchemaWithAllValidation(
			originalSchema,
			rules,
			typeAppendRulesToPropertyDescription: true,
			appendRulesToPropertyDescription: true,
			document);

		// Assert
		result.Type.ShouldBe(JsonSchemaType.Number);
		result.Minimum.ShouldBe("-273.15");
		result.Maximum.ShouldBe("1000");
	}

	[Fact]
	public void CreateInlineSchemaWithAllValidation_WithFloatRangeRule_SetsFloatConstraints()
	{
		// Arrange
		OpenApiDocument document = CreateTestDocument();
		OpenApiSchema originalSchema = new() { Type = JsonSchemaType.Number, Format = "float" };
		List<ValidationRule> rules = [new RangeRule<float>("Score", minimum: 0.0f, maximum: 100.0f)];

		// Act
		OpenApiSchema result = ValidationDocumentTransformer.CreateInlineSchemaWithAllValidation(
			originalSchema,
			rules,
			typeAppendRulesToPropertyDescription: true,
			appendRulesToPropertyDescription: true,
			document);

		// Assert
		result.Type.ShouldBe(JsonSchemaType.Number);
		result.Minimum.ShouldBe("0");
		result.Maximum.ShouldBe("100");
	}

	#endregion

	#region CreateInlineSchemaWithAllValidation - DescriptionRule Tests

	[Fact]
	public void CreateInlineSchemaWithAllValidation_WithDescriptionRule_SetsDescription()
	{
		// Arrange
		OpenApiDocument document = CreateTestDocument();
		OpenApiSchema originalSchema = CreateStringSchema();
		List<ValidationRule> rules = [new DescriptionRule("Name", "The user's full legal name")];

		// Act
		OpenApiSchema result = ValidationDocumentTransformer.CreateInlineSchemaWithAllValidation(
			originalSchema,
			rules,
			typeAppendRulesToPropertyDescription: true,
			appendRulesToPropertyDescription: true,
			document);

		// Assert
		result.Description.ShouldBe("The user's full legal name");
	}

	[Fact]
	public void CreateInlineSchemaWithAllValidation_WithDescriptionAndOtherRules_CombinesDescription()
	{
		// Arrange
		OpenApiDocument document = CreateTestDocument();
		OpenApiSchema originalSchema = CreateStringSchema();
		List<ValidationRule> rules =
		[
			new DescriptionRule("Name", "The user's full name"),
			new RequiredRule("Name"),
			new StringLengthRule("Name", minLength: 2, maxLength: 100)
		];

		// Act
		OpenApiSchema result = ValidationDocumentTransformer.CreateInlineSchemaWithAllValidation(
			originalSchema,
			rules,
			typeAppendRulesToPropertyDescription: true,
			appendRulesToPropertyDescription: true,
			document);

		// Assert
		result.Description.ShouldNotBeNull();
		result.Description.ShouldContain("The user's full name");
		result.Description.ShouldContain("Is required");
		result.MinLength.ShouldBe(2);
		result.MaxLength.ShouldBe(100);
	}

	#endregion

	#region CreateInlineSchemaWithAllValidation - CustomRule Tests

	[Fact]
	public void CreateInlineSchemaWithAllValidation_WithCustomRule_AddsToDescription()
	{
		// Arrange
		OpenApiDocument document = CreateTestDocument();
		OpenApiSchema originalSchema = CreateStringSchema();
		List<ValidationRule> rules = [new CustomRule<string>("Field", "Must be a valid ISBN number")];

		// Act
		OpenApiSchema result = ValidationDocumentTransformer.CreateInlineSchemaWithAllValidation(
			originalSchema,
			rules,
			typeAppendRulesToPropertyDescription: true,
			appendRulesToPropertyDescription: true,
			document);

		// Assert
		result.Description.ShouldNotBeNull();
		result.Description.ShouldContain("Must be a valid ISBN number");
	}

	[Fact]
	public void CreateInlineSchemaWithAllValidation_WithCustomRule_DoesNotModifySchemaConstraints()
	{
		// Arrange
		OpenApiDocument document = CreateTestDocument();
		OpenApiSchema originalSchema = CreateStringSchema();
		List<ValidationRule> rules = [new CustomRule<string>("Field", "Custom validation message")];

		// Act
		OpenApiSchema result = ValidationDocumentTransformer.CreateInlineSchemaWithAllValidation(
			originalSchema,
			rules,
			typeAppendRulesToPropertyDescription: true,
			appendRulesToPropertyDescription: true,
			document);

		// Assert
		result.Type.ShouldBe(JsonSchemaType.String);
		result.MinLength.ShouldBeNull();
		result.MaxLength.ShouldBeNull();
		result.Pattern.ShouldBeNull();
		result.Minimum.ShouldBeNull();
		result.Maximum.ShouldBeNull();
	}

	#endregion

	#region CreateInlineSchemaWithAllValidation - EnumRule Tests

	[Fact]
	public void CreateInlineSchemaWithAllValidation_WithEnumRuleForStringProperty_EnrichesSchemaWithEnumValues()
	{
		// Arrange
		OpenApiDocument document = CreateTestDocument();
		OpenApiSchema originalSchema = CreateStringSchema();
		List<ValidationRule> rules = [new EnumRule("Status", typeof(TestStatus), typeof(string))];

		// Act
		OpenApiSchema result = ValidationDocumentTransformer.CreateInlineSchemaWithAllValidation(
			originalSchema,
			rules,
			typeAppendRulesToPropertyDescription: true,
			appendRulesToPropertyDescription: true,
			document);

		// Assert
		result.Type.ShouldBe(JsonSchemaType.String);
		result.Description.ShouldNotBeNull();
		result.Description.ShouldContain("Enum:");
	}

	[Fact]
	public void CreateInlineSchemaWithAllValidation_WithEnumRuleForIntProperty_SetsIntegerType()
	{
		// Arrange
		OpenApiDocument document = CreateTestDocument();
		OpenApiSchema originalSchema = CreateIntegerSchema();
		List<ValidationRule> rules = [new EnumRule("Priority", typeof(TestPriority), typeof(int))];

		// Act
		OpenApiSchema result = ValidationDocumentTransformer.CreateInlineSchemaWithAllValidation(
			originalSchema,
			rules,
			typeAppendRulesToPropertyDescription: true,
			appendRulesToPropertyDescription: true,
			document);

		// Assert
		result.Type.ShouldBe(JsonSchemaType.Integer);
	}

	#endregion

	#region CreateInlineSchemaWithAllValidation - Nullable Wrapper Tests

	[Fact]
	public void CreateInlineSchemaWithAllValidation_WithNullableWrapperSchema_PreservesNullableStructure()
	{
		// Arrange
		OpenApiDocument document = CreateTestDocument();
		OpenApiSchema innerSchema = CreateStringSchema();
		OpenApiSchema originalSchema = CreateNullableWrapperSchema(innerSchema);
		List<ValidationRule> rules = [new StringLengthRule("Name", minLength: 1, maxLength: 50)];

		// Act
		OpenApiSchema result = ValidationDocumentTransformer.CreateInlineSchemaWithAllValidation(
			originalSchema,
			rules,
			typeAppendRulesToPropertyDescription: true,
			appendRulesToPropertyDescription: true,
			document);

		// Assert
		result.OneOf.ShouldNotBeNull();
		result.OneOf.Count.ShouldBe(2);
	}

	#endregion

	#region CreateInlineSchemaWithAllValidation - Array Schema Tests

	[Fact]
	public void CreateInlineSchemaWithAllValidation_WithArraySchema_PreservesArrayType()
	{
		// Arrange
		OpenApiDocument document = CreateTestDocument();
		OpenApiSchema itemSchema = CreateStringSchema();
		OpenApiSchema originalSchema = CreateArraySchema(itemSchema);
		List<ValidationRule> rules = [new RequiredRule("Items")];

		// Act
		OpenApiSchema result = ValidationDocumentTransformer.CreateInlineSchemaWithAllValidation(
			originalSchema,
			rules,
			typeAppendRulesToPropertyDescription: true,
			appendRulesToPropertyDescription: true,
			document);

		// Assert
		result.Type.ShouldBe(JsonSchemaType.Array);
		result.Items.ShouldNotBeNull();
	}

	#endregion

	#region CreateInlineSchemaWithAllValidation - Multiple Rules Tests

	[Fact]
	public void CreateInlineSchemaWithAllValidation_WithMultipleRules_AppliesAllRules()
	{
		// Arrange
		OpenApiDocument document = CreateTestDocument();
		OpenApiSchema originalSchema = CreateStringSchema();
		List<ValidationRule> rules =
		[
			new RequiredRule("Email"),
			new StringLengthRule("Email", minLength: 5, maxLength: 255),
			new EmailRule("Email")
		];

		// Act
		OpenApiSchema result = ValidationDocumentTransformer.CreateInlineSchemaWithAllValidation(
			originalSchema,
			rules,
			typeAppendRulesToPropertyDescription: true,
			appendRulesToPropertyDescription: true,
			document);

		// Assert
		result.Type.ShouldBe(JsonSchemaType.String);
		result.Format.ShouldBe("email");
		result.MinLength.ShouldBe(5);
		result.MaxLength.ShouldBe(255);
		result.Description.ShouldNotBeNull();
		result.Description.ShouldContain("Is required");
		result.Description.ShouldContain("valid email address");
	}

	[Fact]
	public void CreateInlineSchemaWithAllValidation_WithNumericRulesOnInteger_AppliesAllConstraints()
	{
		// Arrange
		OpenApiDocument document = CreateTestDocument();
		OpenApiSchema originalSchema = CreateIntegerSchema();
		List<ValidationRule> rules =
		[
			new RequiredRule("Quantity"),
			new RangeRule<int>("Quantity", minimum: 1, maximum: 1000)
		];

		// Act
		OpenApiSchema result = ValidationDocumentTransformer.CreateInlineSchemaWithAllValidation(
			originalSchema,
			rules,
			typeAppendRulesToPropertyDescription: true,
			appendRulesToPropertyDescription: true,
			document);

		// Assert
		result.Type.ShouldBe(JsonSchemaType.Integer);
		result.Minimum.ShouldBe("1");
		result.Maximum.ShouldBe("1000");
		result.Description.ShouldNotBeNull();
		result.Description.ShouldContain("Is required");
	}

	#endregion

	#region CreateInlineSchemaWithAllValidation - AppendRulesToPropertyDescription Behavior Tests

	[Fact]
	public void CreateInlineSchemaWithAllValidation_WithPerPropertyAppendSettingTrue_UsesPerPropertySetting()
	{
		// Arrange
		OpenApiDocument document = CreateTestDocument();
		OpenApiSchema originalSchema = CreateStringSchema();
		RequiredRule rule = new("Name") { AppendRuleToPropertyDescription = true };
		List<ValidationRule> rules = [rule];

		// Act
		OpenApiSchema result = ValidationDocumentTransformer.CreateInlineSchemaWithAllValidation(
			originalSchema,
			rules,
			typeAppendRulesToPropertyDescription: false,
			appendRulesToPropertyDescription: true,
			document);

		// Assert
		result.Description.ShouldNotBeNull();
		result.Description.ShouldContain("Is required");
	}

	[Fact]
	public void CreateInlineSchemaWithAllValidation_WithPerPropertyAppendSettingFalse_OverridesTypeLevel()
	{
		// Arrange
		OpenApiDocument document = CreateTestDocument();
		OpenApiSchema originalSchema = CreateStringSchema();
		RequiredRule rule = new("Name") { AppendRuleToPropertyDescription = false };
		List<ValidationRule> rules = [rule];

		// Act
		OpenApiSchema result = ValidationDocumentTransformer.CreateInlineSchemaWithAllValidation(
			originalSchema,
			rules,
			typeAppendRulesToPropertyDescription: true,
			appendRulesToPropertyDescription: true,
			document);

		// Assert
		result.Description.ShouldBeNull();
	}

	[Fact]
	public void CreateInlineSchemaWithAllValidation_WhenGlobalAppendDisabled_NoValidationRulesInDescription()
	{
		// Arrange
		OpenApiDocument document = CreateTestDocument();
		OpenApiSchema originalSchema = CreateStringSchema();
		List<ValidationRule> rules =
		[
			new RequiredRule("Field"),
			new StringLengthRule("Field", minLength: 1, maxLength: 50)
		];

		// Act
		OpenApiSchema result = ValidationDocumentTransformer.CreateInlineSchemaWithAllValidation(
			originalSchema,
			rules,
			typeAppendRulesToPropertyDescription: true,
			appendRulesToPropertyDescription: false,
			document);

		// Assert
		result.Description.ShouldBeNull();
		result.MinLength.ShouldBe(1);
		result.MaxLength.ShouldBe(50);
	}

	#endregion

	#region CreateInlineSchemaWithAllValidation - Extensions Preservation Tests

	[Fact]
	public void CreateInlineSchemaWithAllValidation_WithExtensions_PreservesOriginalExtensions()
	{
		// Arrange
		OpenApiDocument document = CreateTestDocument();
		OpenApiSchema originalSchema = new()
		{
			Type = JsonSchemaType.String,
			Extensions = new Dictionary<string, IOpenApiExtension>
			{
				["x-custom"] = new JsonNodeExtension(System.Text.Json.Nodes.JsonValue.Create("value")!)
			}
		};
		List<ValidationRule> rules = [new RequiredRule("Field")];

		// Act
		OpenApiSchema result = ValidationDocumentTransformer.CreateInlineSchemaWithAllValidation(
			originalSchema,
			rules,
			typeAppendRulesToPropertyDescription: true,
			appendRulesToPropertyDescription: true,
			document);

		// Assert
		result.Extensions.ShouldNotBeNull();
		result.Extensions.ShouldContainKey("x-custom");
	}

	#endregion

	#region CreateInlineSchemaWithAllValidation - Format Preservation Tests

	[Fact]
	public void CreateInlineSchemaWithAllValidation_WithExistingFormat_PreservesFormat()
	{
		// Arrange
		OpenApiDocument document = CreateTestDocument();
		OpenApiSchema originalSchema = new()
		{
			Type = JsonSchemaType.String,
			Format = "date-time"
		};
		List<ValidationRule> rules = [new RequiredRule("Timestamp")];

		// Act
		OpenApiSchema result = ValidationDocumentTransformer.CreateInlineSchemaWithAllValidation(
			originalSchema,
			rules,
			typeAppendRulesToPropertyDescription: true,
			appendRulesToPropertyDescription: true,
			document);

		// Assert
		result.Format.ShouldBe("date-time");
	}

	[Fact]
	public void CreateInlineSchemaWithAllValidation_WithEmailRule_OverridesFormat()
	{
		// Arrange
		OpenApiDocument document = CreateTestDocument();
		OpenApiSchema originalSchema = new()
		{
			Type = JsonSchemaType.String,
			Format = "date-time"
		};
		List<ValidationRule> rules = [new EmailRule("Contact")];

		// Act
		OpenApiSchema result = ValidationDocumentTransformer.CreateInlineSchemaWithAllValidation(
			originalSchema,
			rules,
			typeAppendRulesToPropertyDescription: true,
			appendRulesToPropertyDescription: true,
			document);

		// Assert
		result.Format.ShouldBe("email");
	}

	#endregion

	#region Transformer Property Tests

	[Fact]
	public void ValidationDocumentTransformer_DefaultProperties_HaveExpectedValues()
	{
		// Arrange & Act
		ValidationDocumentTransformer transformer = new();

		// Assert
		transformer.AutoDocumentFluentValidation.ShouldBeTrue();
		transformer.AutoDocumentDataAnnotationValidation.ShouldBeTrue();
		transformer.AppendRulesToPropertyDescription.ShouldBeTrue();
	}

	[Fact]
	public void ValidationDocumentTransformer_Properties_CanBeModified()
	{
		// Arrange
		ValidationDocumentTransformer transformer = new()
		{
			AutoDocumentFluentValidation = false,
			AutoDocumentDataAnnotationValidation = false,
			AppendRulesToPropertyDescription = false
		};

		// Assert
		transformer.AutoDocumentFluentValidation.ShouldBeFalse();
		transformer.AutoDocumentDataAnnotationValidation.ShouldBeFalse();
		transformer.AppendRulesToPropertyDescription.ShouldBeFalse();
	}

	#endregion
}

#region Test Enums

public enum TestStatus
{
	Active,
	Inactive,
	Pending
}

public enum TestPriority
{
	Low = 1,
	Medium = 2,
	High = 3
}

#endregion
