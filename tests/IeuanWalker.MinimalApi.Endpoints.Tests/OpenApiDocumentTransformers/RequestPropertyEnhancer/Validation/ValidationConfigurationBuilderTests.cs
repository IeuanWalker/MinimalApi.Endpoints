using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer.Validation;

namespace IeuanWalker.MinimalApi.Endpoints.Tests.OpenApiDocumentTransformers.RequestPropertyEnhancer.Validation;

public class ValidationConfigurationBuilderTests
{
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

	class Nested
	{
		public string? StringMin { get; set; }
	}

	class TestRequest
	{
		public string? Name { get; set; }
		public Nested? Nested { get; set; }
		public List<Nested>? ListNestedObject { get; set; }
	}
}
