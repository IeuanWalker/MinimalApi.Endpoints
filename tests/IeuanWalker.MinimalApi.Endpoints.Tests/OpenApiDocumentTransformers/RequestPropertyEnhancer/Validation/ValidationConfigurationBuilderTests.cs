using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer.Validation;

namespace IeuanWalker.MinimalApi.Endpoints.Tests.OpenApiDocumentTransformers.RequestPropertyEnhancer.Validation;

public class ValidationConfigurationBuilderTests
{
	#region Basic Build Tests

	[Fact]
	public void Build_CollectsRulesAndOperations_ForSimpleNestedAndArrayProperties()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> builder = new();

		PropertyValidationBuilder<TestRequest, string?> nameBuilder = builder.Property(x => x.Name).Required("name required");
		nameBuilder.Alter("name required", "new name message");

		builder.Property(x => x.Nested!.StringMin).MinLength(3);

		builder.Property(x => x.ListNestedObject![0].StringMin).MaxLength(10);

		// Act
		ValidationConfiguration<TestRequest> config = builder.Build();

		// Assert
		config.Rules.ShouldNotBeNull();
		config.Rules.Count.ShouldBeGreaterThanOrEqualTo(3);

		config.Rules.ShouldContain(r => r is RequiredRule && ((RequiredRule)r).PropertyName == "Name" && ((RequiredRule)r).ErrorMessage == "name required");
		config.Rules.ShouldContain(r => r is StringLengthRule && ((StringLengthRule)r).PropertyName == "Nested.StringMin" && ((StringLengthRule)r).MinLength == 3);
		config.Rules.ShouldContain(r => r is StringLengthRule && ((StringLengthRule)r).PropertyName == "ListNestedObject[*].StringMin" && ((StringLengthRule)r).MaxLength == 10);

		config.OperationsByProperty.ShouldNotBeNull();
		config.OperationsByProperty.ContainsKey("Name").ShouldBeTrue();
		IReadOnlyList<ValidationRuleOperation> ops = config.OperationsByProperty["Name"];
		ops.Count.ShouldBe(1);
		ops[0].ShouldBeOfType<AlterOperation>();
		AlterOperation alter = (AlterOperation)ops[0];
		alter.OldErrorMessage.ShouldBe("name required");
		alter.NewErrorMessage.ShouldBe("new name message");
	}

	[Fact]
	public void Build_AppliesPerPropertyAppendRulesToPropertyDescription_OverridesGlobalSetting()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> builder = new();

		builder.AppendRulesToPropertyDescription(false);
		builder.Property(x => x.Name).Required().AppendRulesToPropertyDescription(true);

		// Act
		ValidationConfiguration<TestRequest> config = builder.Build();

		// Assert
		config.AppendRulesToPropertyDescription.ShouldBeFalse();

		RequiredRule? nameRule = config.Rules.OfType<RequiredRule>().FirstOrDefault(r => r.PropertyName == "Name");
		nameRule.ShouldNotBeNull();
		nameRule.AppendRuleToPropertyDescription.ShouldBe(true);
	}

	[Fact]
	public void Build_NoPropertiesConfigured_ReturnsEmptyRules()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> builder = new();

		// Act
		ValidationConfiguration<TestRequest> config = builder.Build();

		// Assert
		config.Rules.ShouldBeEmpty();
		config.OperationsByProperty.ShouldBeEmpty();
	}

	[Fact]
	public void Build_DefaultAppendRulesToPropertyDescription_IsTrue()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> builder = new();

		// Act
		ValidationConfiguration<TestRequest> config = builder.Build();

		// Assert
		config.AppendRulesToPropertyDescription.ShouldBeTrue();
	}

	#endregion

	#region Property Selector Tests

	[Fact]
	public void Property_SimpleProperty_ReturnsBuilder()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> builder = new();

		// Act
		PropertyValidationBuilder<TestRequest, string?> result = builder.Property(x => x.Name);

		// Assert
		result.ShouldNotBeNull();
	}

	[Fact]
	public void Property_NestedProperty_GeneratesCorrectPath()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> builder = new();

		// Act
		builder.Property(x => x.Nested!.StringMin).Required();
		ValidationConfiguration<TestRequest> config = builder.Build();

		// Assert
		config.Rules.Count.ShouldBe(1);
		config.Rules[0].PropertyName.ShouldBe("Nested.StringMin");
	}

	[Fact]
	public void Property_DeeplyNestedProperty_GeneratesCorrectPath()
	{
		// Arrange
		ValidationConfigurationBuilder<DeepRequest> builder = new();

		// Act
		builder.Property(x => x.Level1!.Level2!.Level3!.Value).Required();
		ValidationConfiguration<DeepRequest> config = builder.Build();

		// Assert
		config.Rules.Count.ShouldBe(1);
		config.Rules[0].PropertyName.ShouldBe("Level1.Level2.Level3.Value");
	}

	[Fact]
	public void Property_ArrayIndexer_GeneratesWildcardPath()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> builder = new();

		// Act
		builder.Property(x => x.ListNestedObject![0].StringMin).Required();
		ValidationConfiguration<TestRequest> config = builder.Build();

		// Assert
		config.Rules.Count.ShouldBe(1);
		config.Rules[0].PropertyName.ShouldBe("ListNestedObject[*].StringMin");
	}

	[Fact]
	public void Property_InvalidExpression_Throws()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> builder = new();

		// Act & Assert
		Should.Throw<ArgumentException>(() => builder.Property(x => x.Name!.ToUpper()));
	}

	#endregion

	#region AppendRulesToPropertyDescription Tests

	[Fact]
	public void AppendRulesToPropertyDescription_True_SetsGlobalFlag()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> builder = new();

		// Act
		builder.AppendRulesToPropertyDescription(true);
		ValidationConfiguration<TestRequest> config = builder.Build();

		// Assert
		config.AppendRulesToPropertyDescription.ShouldBeTrue();
	}

	[Fact]
	public void AppendRulesToPropertyDescription_False_SetsGlobalFlag()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> builder = new();

		// Act
		builder.AppendRulesToPropertyDescription(false);
		ValidationConfiguration<TestRequest> config = builder.Build();

		// Assert
		config.AppendRulesToPropertyDescription.ShouldBeFalse();
	}

	[Fact]
	public void AppendRulesToPropertyDescription_ReturnsBuilderForChaining()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> builder = new();

		// Act
		ValidationConfigurationBuilder<TestRequest> result = builder.AppendRulesToPropertyDescription(true);

		// Assert
		result.ShouldBeSameAs(builder);
	}

	#endregion

	#region Multiple Properties Tests

	[Fact]
	public void Build_MultipleProperties_CollectsAllRules()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> builder = new();

		builder.Property(x => x.Name).Required();
		builder.Property(x => x.Age).GreaterThan(0);
		builder.Property(x => x.Email).Email();

		// Act
		ValidationConfiguration<TestRequest> config = builder.Build();

		// Assert
		config.Rules.Count.ShouldBe(3);
		config.Rules.ShouldContain(r => r.PropertyName == "Name");
		config.Rules.ShouldContain(r => r.PropertyName == "Age");
		config.Rules.ShouldContain(r => r.PropertyName == "Email");
	}

	[Fact]
	public void Build_SamePropertyMultipleTimes_CollectsAllRules()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> builder = new();

		builder.Property(x => x.Name).Required();
		builder.Property(x => x.Name).MinLength(2);
		builder.Property(x => x.Name).MaxLength(100);

		// Act
		ValidationConfiguration<TestRequest> config = builder.Build();

		// Assert
		config.Rules.Count.ShouldBe(3);
		config.Rules.Where(r => r.PropertyName == "Name").Count().ShouldBe(3);
	}

	#endregion

	#region Operations Collection Tests

	[Fact]
	public void Build_OperationsFromMultipleProperties_CollectedSeparately()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> builder = new();

		builder.Property(x => x.Name).Required().Alter("Is required", "Name is mandatory");
		builder.Property(x => x.Email).Email().Remove("Must be a valid email address");

		// Act
		ValidationConfiguration<TestRequest> config = builder.Build();

		// Assert
		config.OperationsByProperty.Count.ShouldBe(2);
		config.OperationsByProperty.ContainsKey("Name").ShouldBeTrue();
		config.OperationsByProperty.ContainsKey("Email").ShouldBeTrue();
		config.OperationsByProperty["Name"].Count.ShouldBe(1);
		config.OperationsByProperty["Email"].Count.ShouldBe(1);
	}

	[Fact]
	public void Build_PropertyWithNoOperations_NotInOperationsCollection()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> builder = new();

		builder.Property(x => x.Name).Required(); // No operations
		builder.Property(x => x.Email).Email().Remove("Must be a valid email address"); // Has operation

		// Act
		ValidationConfiguration<TestRequest> config = builder.Build();

		// Assert
		config.OperationsByProperty.ContainsKey("Name").ShouldBeFalse();
		config.OperationsByProperty.ContainsKey("Email").ShouldBeTrue();
	}

	#endregion

	#region Test Classes

	class Nested
	{
		public string? StringMin { get; set; }
	}

	class TestRequest
	{
		public string? Name { get; set; }
		public int Age { get; set; }
		public string? Email { get; set; }
		public Nested? Nested { get; set; }
		public List<Nested>? ListNestedObject { get; set; }
	}

	class DeepRequest
	{
		public Level1Class? Level1 { get; set; }
	}

	class Level1Class
	{
		public Level2Class? Level2 { get; set; }
	}

	class Level2Class
	{
		public Level3Class? Level3 { get; set; }
	}

	class Level3Class
	{
		public string? Value { get; set; }
	}

	#endregion
}
