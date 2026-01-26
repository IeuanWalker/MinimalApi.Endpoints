using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.PropertyEnhancer.Validation;

namespace IeuanWalker.MinimalApi.Endpoints.Tests.OpenApiDocumentTransformers.PropertyEnhancer.Validation;

public class PropertyValidationBuilderTests
{
	static PropertyValidationBuilder<object, string> CreateBuilder(string propertyName) => new(propertyName);
	static PropertyValidationBuilder<object, int> CreateIntBuilder(string propertyName) => new(propertyName);
	static PropertyValidationBuilder<object, decimal> CreateDecimalBuilder(string propertyName) => new(propertyName);

	#region Constructor Tests

	[Fact]
	public void Constructor_NullPropertyName_Throws()
	{
		// Act & Assert
		Should.Throw<ArgumentException>(() => new PropertyValidationBuilder<object, string>(null!));
	}

	[Fact]
	public void Constructor_EmptyPropertyName_Throws()
	{
		// Act & Assert
		Should.Throw<ArgumentException>(() => new PropertyValidationBuilder<object, string>(""));
	}

	[Fact]
	public void Constructor_WhitespacePropertyName_Throws()
	{
		// Act & Assert
		Should.Throw<ArgumentException>(() => new PropertyValidationBuilder<object, string>("   "));
	}

	#endregion

	#region Build Tests

	[Fact]
	public void Build_WhenAppendRulesFlagSet_AppliesFlagToAllRules()
	{
		// Arrange
		PropertyValidationBuilder<object, string> builder = CreateBuilder("myProperty");

		builder.Required("req msg");
		builder.Description("custom desc");
		builder.AppendRulesToPropertyDescription(false);

		// Act
		List<ValidationRule> rules = [.. builder.Build()];

		// Assert
		rules.ShouldNotBeEmpty();
		rules.ShouldAllBe(r => r.AppendRuleToPropertyDescription == false);
	}

	[Fact]
	public void Build_WhenAppendRulesFlagNotSet_ReturnsRulesWithNullFlag()
	{
		// Arrange
		PropertyValidationBuilder<object, string> builder = CreateBuilder("myProperty");

		builder.Custom("note");

		// Act
		List<ValidationRule> rules = [.. builder.Build()];

		// Assert
		rules.ShouldNotBeEmpty();
		rules.ShouldAllBe(r => r.AppendRuleToPropertyDescription == null);
	}

	[Fact]
	public void Build_NoRulesAdded_ReturnsEmpty()
	{
		// Arrange
		PropertyValidationBuilder<object, string> builder = CreateBuilder("myProperty");

		// Act
		List<ValidationRule> rules = [.. builder.Build()];

		// Assert
		rules.ShouldBeEmpty();
	}

	#endregion

	#region GetOperations Tests

	[Fact]
	public void GetOperations_Returns_AddedOperations()
	{
		// Arrange
		PropertyValidationBuilder<object, string> builder = CreateBuilder("myProperty");

		builder.Alter("old", "new");
		builder.Remove("remove me");
		builder.RemoveAll();

		// Act
		List<ValidationRuleOperation> ops = [.. builder.GetOperations()];

		// Assert
		ops.Count.ShouldBe(3);
		ops[0].ShouldBeOfType<AlterOperation>();
		((AlterOperation)ops[0]).OldErrorMessage.ShouldBe("old");
		((AlterOperation)ops[0]).NewErrorMessage.ShouldBe("new");

		ops[1].ShouldBeOfType<RemoveOperation>();
		((RemoveOperation)ops[1]).ErrorMessage.ShouldBe("remove me");

		ops[2].ShouldBeOfType<RemoveAllOperation>();
	}

	[Fact]
	public void GetOperations_NoOperationsAdded_ReturnsEmpty()
	{
		// Arrange
		PropertyValidationBuilder<object, string> builder = CreateBuilder("myProperty");

		// Act
		IReadOnlyList<ValidationRuleOperation> ops = builder.GetOperations();

		// Assert
		ops.ShouldBeEmpty();
	}

	#endregion

	#region Length Validation Tests

	[Fact]
	public void Length_InvalidArguments_ThrowArgumentException()
	{
		// Arrange
		PropertyValidationBuilder<object, string> builder = CreateBuilder("prop");

		// Act + Assert
		Should.Throw<ArgumentException>(() => builder.Length(5, 2, null));
		Should.Throw<ArgumentException>(() => builder.Length(-1, 2, null));
	}

	[Fact]
	public void Length_ValidArguments_AddsRule()
	{
		// Arrange
		PropertyValidationBuilder<object, string> builder = CreateBuilder("prop");

		// Act
		builder.Length(2, 10);

		// Assert
		List<ValidationRule> rules = [.. builder.Build()];
		rules.Count.ShouldBe(1);
		StringLengthRule? rule = rules[0] as StringLengthRule;
		rule.ShouldNotBeNull();
		rule.MinLength.ShouldBe(2);
		rule.MaxLength.ShouldBe(10);
	}

	[Fact]
	public void Length_EqualMinMax_IsValid()
	{
		// Arrange
		PropertyValidationBuilder<object, string> builder = CreateBuilder("prop");

		// Act
		builder.Length(5, 5);

		// Assert
		List<ValidationRule> rules = [.. builder.Build()];
		rules.Count.ShouldBe(1);
	}

	[Fact]
	public void MinMaxLength_InvalidArguments_ThrowArgumentException()
	{
		// Arrange
		PropertyValidationBuilder<object, string> builder = CreateBuilder("prop");

		// Act + Assert
		Should.Throw<ArgumentException>(() => builder.MinLength(-1, null));
		Should.Throw<ArgumentException>(() => builder.MaxLength(-1, null));
	}

	[Fact]
	public void MinLength_ZeroValue_IsValid()
	{
		// Arrange
		PropertyValidationBuilder<object, string> builder = CreateBuilder("prop");

		// Act
		builder.MinLength(0);

		// Assert
		List<ValidationRule> rules = [.. builder.Build()];
		rules.Count.ShouldBe(1);
		StringLengthRule? rule = rules[0] as StringLengthRule;
		rule.ShouldNotBeNull();
		rule.MinLength.ShouldBe(0);
	}

	[Fact]
	public void MaxLength_ZeroValue_IsValid()
	{
		// Arrange
		PropertyValidationBuilder<object, string> builder = CreateBuilder("prop");

		// Act
		builder.MaxLength(0);

		// Assert
		List<ValidationRule> rules = [.. builder.Build()];
		rules.Count.ShouldBe(1);
		StringLengthRule? rule = rules[0] as StringLengthRule;
		rule.ShouldNotBeNull();
		rule.MaxLength.ShouldBe(0);
	}

	#endregion

	#region Pattern Validation Tests

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void Pattern_NullOrWhiteSpace_Throws(string? pattern)
	{
		// Arrange
		PropertyValidationBuilder<object, string> builder = CreateBuilder("prop");

		// Act + Assert
		Should.Throw<ArgumentException>(() => builder.Pattern(pattern!, null));
	}

	[Fact]
	public void Pattern_ValidPattern_AddsRule()
	{
		// Arrange
		PropertyValidationBuilder<object, string> builder = CreateBuilder("prop");

		// Act
		builder.Pattern(@"^\d+$");

		// Assert
		List<ValidationRule> rules = [.. builder.Build()];
		rules.Count.ShouldBe(1);
		PatternRule? rule = rules[0] as PatternRule;
		rule.ShouldNotBeNull();
		rule.Pattern.ShouldBe(@"^\d+$");
	}

	#endregion

	#region Range Validation Tests

	[Fact]
	public void Between_MinGreaterThanMax_Throws()
	{
		// Arrange
		PropertyValidationBuilder<object, string> builder = CreateBuilder("prop");

		// Act + Assert
		Should.Throw<ArgumentException>(() => builder.Between<int>(5, 1, null));
	}

	[Fact]
	public void Between_ValidRange_AddsRule()
	{
		// Arrange
		PropertyValidationBuilder<object, int> builder = CreateIntBuilder("prop");

		// Act
		builder.Between(1, 100);

		// Assert
		List<ValidationRule> rules = [.. builder.Build()];
		rules.Count.ShouldBe(1);
		RangeRule<int>? rule = rules[0] as RangeRule<int>;
		rule.ShouldNotBeNull();
		rule.Minimum.ShouldBe(1);
		rule.Maximum.ShouldBe(100);
		rule.ExclusiveMinimum.ShouldBeFalse();
		rule.ExclusiveMaximum.ShouldBeFalse();
	}

	[Fact]
	public void GreaterThan_AddsExclusiveMinimumRule()
	{
		// Arrange
		PropertyValidationBuilder<object, int> builder = CreateIntBuilder("prop");

		// Act
		builder.GreaterThan(0);

		// Assert
		List<ValidationRule> rules = [.. builder.Build()];
		rules.Count.ShouldBe(1);
		RangeRule<int>? rule = rules[0] as RangeRule<int>;
		rule.ShouldNotBeNull();
		rule.Minimum.ShouldBe(0);
		rule.Maximum.ShouldBeNull();
		rule.ExclusiveMinimum.ShouldBeTrue();
	}

	[Fact]
	public void GreaterThanOrEqual_AddsInclusiveMinimumRule()
	{
		// Arrange
		PropertyValidationBuilder<object, int> builder = CreateIntBuilder("prop");

		// Act
		builder.GreaterThanOrEqual(1);

		// Assert
		List<ValidationRule> rules = [.. builder.Build()];
		rules.Count.ShouldBe(1);
		RangeRule<int>? rule = rules[0] as RangeRule<int>;
		rule.ShouldNotBeNull();
		rule.Minimum.ShouldBe(1);
		rule.ExclusiveMinimum.ShouldBeFalse();
	}

	[Fact]
	public void LessThan_AddsExclusiveMaximumRule()
	{
		// Arrange
		PropertyValidationBuilder<object, int> builder = CreateIntBuilder("prop");

		// Act
		builder.LessThan(100);

		// Assert
		List<ValidationRule> rules = [.. builder.Build()];
		rules.Count.ShouldBe(1);
		RangeRule<int>? rule = rules[0] as RangeRule<int>;
		rule.ShouldNotBeNull();
		rule.Minimum.ShouldBeNull();
		rule.Maximum.ShouldBe(100);
		rule.ExclusiveMaximum.ShouldBeTrue();
	}

	[Fact]
	public void LessThanOrEqual_AddsInclusiveMaximumRule()
	{
		// Arrange
		PropertyValidationBuilder<object, int> builder = CreateIntBuilder("prop");

		// Act
		builder.LessThanOrEqual(99);

		// Assert
		List<ValidationRule> rules = [.. builder.Build()];
		rules.Count.ShouldBe(1);
		RangeRule<int>? rule = rules[0] as RangeRule<int>;
		rule.ShouldNotBeNull();
		rule.Maximum.ShouldBe(99);
		rule.ExclusiveMaximum.ShouldBeFalse();
	}

	#endregion

	#region String Format Validation Tests

	[Fact]
	public void Email_AddsEmailRule()
	{
		// Arrange
		PropertyValidationBuilder<object, string> builder = CreateBuilder("email");

		// Act
		builder.Email();

		// Assert
		List<ValidationRule> rules = [.. builder.Build()];
		rules.Count.ShouldBe(1);
		rules[0].ShouldBeOfType<EmailRule>();
	}

	[Fact]
	public void Email_WithCustomMessage_UsesCustomMessage()
	{
		// Arrange
		PropertyValidationBuilder<object, string> builder = CreateBuilder("email");

		// Act
		builder.Email("Please enter a valid email");

		// Assert
		List<ValidationRule> rules = [.. builder.Build()];
		rules[0].ErrorMessage.ShouldBe("Please enter a valid email");
	}

	[Fact]
	public void Url_AddsUrlRule()
	{
		// Arrange
		PropertyValidationBuilder<object, string> builder = CreateBuilder("website");

		// Act
		builder.Url();

		// Assert
		List<ValidationRule> rules = [.. builder.Build()];
		rules.Count.ShouldBe(1);
		rules[0].ShouldBeOfType<UrlRule>();
	}

	#endregion

	#region Required and Custom Rules Tests

	[Fact]
	public void Required_AddsRequiredRule()
	{
		// Arrange
		PropertyValidationBuilder<object, string> builder = CreateBuilder("name");

		// Act
		builder.Required();

		// Assert
		List<ValidationRule> rules = [.. builder.Build()];
		rules.Count.ShouldBe(1);
		rules[0].ShouldBeOfType<RequiredRule>();
	}

	[Fact]
	public void Custom_AddsCustomRule()
	{
		// Arrange
		PropertyValidationBuilder<object, string> builder = CreateBuilder("field");

		// Act
		builder.Custom("Custom validation note");

		// Assert
		List<ValidationRule> rules = [.. builder.Build()];
		rules.Count.ShouldBe(1);
		CustomRule<string>? rule = rules[0] as CustomRule<string>;
		rule.ShouldNotBeNull();
		rule.ErrorMessage.ShouldBe("Custom validation note");
	}

	[Fact]
	public void Description_AddsDescriptionRule()
	{
		// Arrange
		PropertyValidationBuilder<object, string> builder = CreateBuilder("field");

		// Act
		builder.Description("This is a custom description");

		// Assert
		List<ValidationRule> rules = [.. builder.Build()];
		rules.Count.ShouldBe(1);
		DescriptionRule? rule = rules[0] as DescriptionRule;
		rule.ShouldNotBeNull();
		rule.Description.ShouldBe("This is a custom description");
	}

	#endregion

	#region Chaining Tests

	[Fact]
	public void Chaining_MultipleRules_AllRulesAdded()
	{
		// Arrange
		PropertyValidationBuilder<object, string> builder = CreateBuilder("name");

		// Act
		builder
			.Required()
			.MinLength(2)
			.MaxLength(50)
			.Pattern(@"^[A-Za-z]+$")
			.Description("User's display name");

		// Assert
		List<ValidationRule> rules = [.. builder.Build()];
		rules.Count.ShouldBe(5);
		rules[0].ShouldBeOfType<RequiredRule>();
		rules[1].ShouldBeOfType<StringLengthRule>();
		rules[2].ShouldBeOfType<StringLengthRule>();
		rules[3].ShouldBeOfType<PatternRule>();
		rules[4].ShouldBeOfType<DescriptionRule>();
	}

	[Fact]
	public void Chaining_ReturnsBuilderForFluentApi()
	{
		// Arrange
		PropertyValidationBuilder<object, string> builder = CreateBuilder("name");

		// Act
		PropertyValidationBuilder<object, string> result = builder
			.Required()
			.MinLength(1)
			.MaxLength(100);

		// Assert
		result.ShouldBeSameAs(builder);
	}

	[Fact]
	public void Chaining_RulesAndOperations_BothAdded()
	{
		// Arrange
		PropertyValidationBuilder<object, string> builder = CreateBuilder("name");

		// Act
		builder
			.Required()
			.MinLength(2)
			.Alter("Is required", "Name is required")
			.Remove("Must be 2 characters or more");

		// Assert
		List<ValidationRule> rules = [.. builder.Build()];
		rules.Count.ShouldBe(2);

		IReadOnlyList<ValidationRuleOperation> ops = builder.GetOperations();
		ops.Count.ShouldBe(2);
	}

	#endregion

	#region Decimal Type Tests

	[Fact]
	public void Between_DecimalType_AddsRule()
	{
		// Arrange
		PropertyValidationBuilder<object, decimal> builder = CreateDecimalBuilder("price");

		// Act
		builder.Between(0.01m, 999.99m);

		// Assert
		List<ValidationRule> rules = [.. builder.Build()];
		rules.Count.ShouldBe(1);
		RangeRule<decimal>? rule = rules[0] as RangeRule<decimal>;
		rule.ShouldNotBeNull();
		rule.Minimum.ShouldBe(0.01m);
		rule.Maximum.ShouldBe(999.99m);
	}

	#endregion
}
