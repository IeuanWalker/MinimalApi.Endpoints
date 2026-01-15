using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer.Validation;

namespace IeuanWalker.MinimalApi.Endpoints.Tests.OpenApiDocumentTransformers.RequestPropertyEnhancer.Validation;

public class PropertyValidationBuilderTests
{
	static PropertyValidationBuilder<object, string> CreateBuilder(string propertyName) => new(propertyName);

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
	public void Length_InvalidArguments_ThrowArgumentException()
	{
		// Arrange
		PropertyValidationBuilder<object, string> builder = CreateBuilder("prop");

		// Act + Assert
		Should.Throw<ArgumentException>(() => builder.Length(5, 2, null));
		Should.Throw<ArgumentException>(() => builder.Length(-1, 2, null));
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
	public void Between_MinGreaterThanMax_Throws()
	{
		// Arrange
		PropertyValidationBuilder<object, string> builder = CreateBuilder("prop");

		// Act + Assert
		Should.Throw<ArgumentException>(() => builder.Between<int>(5, 1, null));
	}
}
