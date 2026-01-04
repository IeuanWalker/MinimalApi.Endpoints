using System.Reflection;
using IeuanWalker.MinimalApi.Endpoints.Validation;

namespace IeuanWalker.MinimalApi.Endpoints.Tests;

public class PropertyValidationBuilderTests
{
	public class TestRequest
	{
		public string Name { get; set; } = string.Empty;
		public int Age { get; set; }
	}

	static List<ValidationRule> GetRules<TRequest, TProperty>(PropertyValidationBuilder<TRequest, TProperty> builder)
	{
		FieldInfo? field = builder.GetType().GetField("_rules", BindingFlags.Instance | BindingFlags.NonPublic);
		return (List<ValidationRule>)field!.GetValue(builder)!;
	}

	#region Alter Tests

	[Fact]
	public void Alter_WhenOldRuleExists_UpdatesErrorMessage()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> configBuilder = new();
		PropertyValidationBuilder<TestRequest, string> propertyBuilder = configBuilder.Property(x => x.Name);
		propertyBuilder.Required();
		propertyBuilder.MinLength(5);

		// Act
		PropertyValidationBuilder<TestRequest, string> result = propertyBuilder.Alter("Must be 5 characters or more", "Minimum 5 chars required");

		// Assert
		result.ShouldBeSameAs(propertyBuilder); // Method chaining works
		List<ValidationRule> rules = GetRules(propertyBuilder);
		ValidationRule? alteredRule = rules.FirstOrDefault(r => r.ErrorMessage == "Minimum 5 chars required");
		alteredRule.ShouldNotBeNull();
		alteredRule.ShouldBeOfType<StringLengthRule>();
	}

	[Fact]
	public void Alter_WhenOldRuleDoesNotExist_ThrowsArgumentException()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> configBuilder = new();
		PropertyValidationBuilder<TestRequest, string> propertyBuilder = configBuilder.Property(x => x.Name);
		propertyBuilder.Required();

		// Act & Assert
		ArgumentException ex = Should.Throw<ArgumentException>(() =>
			propertyBuilder.Alter("Non-existent rule", "New message"));

		ex.ParamName.ShouldBe("oldRule");
		ex.Message.ShouldContain("No validation rule exists with error message: 'Non-existent rule'");
	}

	[Fact]
	public void Alter_WhenOldRuleIsNull_ThrowsArgumentNullException()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> configBuilder = new();
		PropertyValidationBuilder<TestRequest, string> propertyBuilder = configBuilder.Property(x => x.Name);

		// Act & Assert
		Should.Throw<ArgumentNullException>(() => propertyBuilder.Alter(null!, "New message"))
			.ParamName.ShouldBe("oldRule");
	}

	[Fact]
	public void Alter_WhenOldRuleIsWhiteSpace_ThrowsArgumentException()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> configBuilder = new();
		PropertyValidationBuilder<TestRequest, string> propertyBuilder = configBuilder.Property(x => x.Name);

		// Act & Assert
		Should.Throw<ArgumentException>(() => propertyBuilder.Alter("   ", "New message"))
			.ParamName.ShouldBe("oldRule");
	}

	[Fact]
	public void Alter_WhenNewRuleIsNull_ThrowsArgumentNullException()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> configBuilder = new();
		PropertyValidationBuilder<TestRequest, string> propertyBuilder = configBuilder.Property(x => x.Name);
		propertyBuilder.Required();

		// Act & Assert
		Should.Throw<ArgumentNullException>(() => propertyBuilder.Alter("Is required", null!))
			.ParamName.ShouldBe("newRule");
	}

	[Fact]
	public void Alter_WhenNewRuleIsWhiteSpace_ThrowsArgumentException()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> configBuilder = new();
		PropertyValidationBuilder<TestRequest, string> propertyBuilder = configBuilder.Property(x => x.Name);
		propertyBuilder.Required();

		// Act & Assert
		Should.Throw<ArgumentException>(() => propertyBuilder.Alter("Is required", "   "))
			.ParamName.ShouldBe("newRule");
	}

	[Fact]
	public void Alter_WithCustomErrorMessage_UpdatesCorrectly()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> configBuilder = new();
		PropertyValidationBuilder<TestRequest, string> propertyBuilder = configBuilder.Property(x => x.Name);
		propertyBuilder.Required("Name field is mandatory");
		propertyBuilder.MinLength(3, "Too short!");

		// Act
		propertyBuilder.Alter("Too short!", "Must have at least 3 characters");

		// Assert
		
		ValidationRule? alteredRule = GetRules(propertyBuilder).FirstOrDefault(r => r.ErrorMessage == "Must have at least 3 characters");
		alteredRule.ShouldNotBeNull();
		alteredRule.ShouldBeOfType<StringLengthRule>();
	}

	#endregion

	#region Remove Tests

	[Fact]
	public void Remove_WhenRuleExists_RemovesTheRule()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> configBuilder = new();
		PropertyValidationBuilder<TestRequest, string> propertyBuilder = configBuilder.Property(x => x.Name);
		propertyBuilder.Required();
		propertyBuilder.MinLength(5);
		propertyBuilder.MaxLength(100);

		// Act
		PropertyValidationBuilder<TestRequest, string> result = propertyBuilder.Remove("Must be 5 characters or more");

		// Assert
		result.ShouldBeSameAs(propertyBuilder); // Method chaining works
		
		GetRules(propertyBuilder).Count().ShouldBe(2); // Should have Required and MaxLength only
		GetRules(propertyBuilder).ShouldNotContain(r => r.ErrorMessage == "Must be 5 characters or more");
	}

	[Fact]
	public void Remove_WhenRuleDoesNotExist_ThrowsArgumentException()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> configBuilder = new();
		PropertyValidationBuilder<TestRequest, string> propertyBuilder = configBuilder.Property(x => x.Name);
		propertyBuilder.Required();

		// Act & Assert
		ArgumentException ex = Should.Throw<ArgumentException>(() =>
			propertyBuilder.Remove("Non-existent rule"));

		ex.ParamName.ShouldBe("rule");
		ex.Message.ShouldContain("No validation rule exists with error message: 'Non-existent rule'");
	}

	[Fact]
	public void Remove_WhenRuleIsNull_ThrowsArgumentNullException()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> configBuilder = new();
		PropertyValidationBuilder<TestRequest, string> propertyBuilder = configBuilder.Property(x => x.Name);

		// Act & Assert
		Should.Throw<ArgumentNullException>(() => propertyBuilder.Remove(null!))
			.ParamName.ShouldBe("rule");
	}

	[Fact]
	public void Remove_WhenRuleIsWhiteSpace_ThrowsArgumentException()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> configBuilder = new();
		PropertyValidationBuilder<TestRequest, string> propertyBuilder = configBuilder.Property(x => x.Name);

		// Act & Assert
		Should.Throw<ArgumentException>(() => propertyBuilder.Remove("   "))
			.ParamName.ShouldBe("rule");
	}

	[Fact]
	public void Remove_WithCustomErrorMessage_RemovesCorrectly()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> configBuilder = new();
		PropertyValidationBuilder<TestRequest, string> propertyBuilder = configBuilder.Property(x => x.Name);
		propertyBuilder.Required("Custom required message");
		propertyBuilder.MinLength(3, "Too short!");
		propertyBuilder.MaxLength(50);

		// Act
		propertyBuilder.Remove("Too short!");

		// Assert
		
		GetRules(propertyBuilder).Count().ShouldBe(2); // Should have Required and MaxLength only
		GetRules(propertyBuilder).ShouldNotContain(r => r.ErrorMessage == "Too short!");
		GetRules(propertyBuilder).ShouldContain(r => r.ErrorMessage == "Custom required message");
	}

	[Fact]
	public void Remove_AllRulesOneByOne_LeavesEmptyList()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> configBuilder = new();
		PropertyValidationBuilder<TestRequest, string> propertyBuilder = configBuilder.Property(x => x.Name);
		propertyBuilder.Required();
		propertyBuilder.MinLength(5);

		// Act
		propertyBuilder.Remove("Is required");
		propertyBuilder.Remove("Must be 5 characters or more");

		// Assert
		
		GetRules(propertyBuilder).ShouldBeEmpty();
	}

	#endregion

	#region RemoveAll Tests

	[Fact]
	public void RemoveAll_WhenRulesExist_RemovesAllRules()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> configBuilder = new();
		PropertyValidationBuilder<TestRequest, string> propertyBuilder = configBuilder.Property(x => x.Name);
		propertyBuilder.Required();
		propertyBuilder.MinLength(5);
		propertyBuilder.MaxLength(100);
		propertyBuilder.Pattern("^[a-z]+$");

		// Act
		PropertyValidationBuilder<TestRequest, string> result = propertyBuilder.RemoveAll();

		// Assert
		result.ShouldBeSameAs(propertyBuilder); // Method chaining works
		
		GetRules(propertyBuilder).ShouldBeEmpty();
	}

	[Fact]
	public void RemoveAll_WhenNoRulesExist_ThrowsInvalidOperationException()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> configBuilder = new();
		PropertyValidationBuilder<TestRequest, string> propertyBuilder = configBuilder.Property(x => x.Name);

		// Act & Assert
		InvalidOperationException ex = Should.Throw<InvalidOperationException>(() => propertyBuilder.RemoveAll());
		ex.Message.ShouldBe("No validation rules exist to remove.");
	}

	[Fact]
	public void RemoveAll_AfterRemovingAllRulesIndividually_ThrowsInvalidOperationException()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> configBuilder = new();
		PropertyValidationBuilder<TestRequest, string> propertyBuilder = configBuilder.Property(x => x.Name);
		propertyBuilder.Required();
		propertyBuilder.Remove("Is required");

		// Act & Assert
		InvalidOperationException ex = Should.Throw<InvalidOperationException>(() => propertyBuilder.RemoveAll());
		ex.Message.ShouldBe("No validation rules exist to remove.");
	}

	[Fact]
	public void RemoveAll_WithSingleRule_RemovesIt()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> configBuilder = new();
		PropertyValidationBuilder<TestRequest, string> propertyBuilder = configBuilder.Property(x => x.Name);
		propertyBuilder.Required();

		// Act
		propertyBuilder.RemoveAll();

		// Assert
		
		GetRules(propertyBuilder).ShouldBeEmpty();
	}

	#endregion

	#region Integration Tests with Multiple Operations

	[Fact]
	public void ChainedOperations_AlterThenRemove_WorksCorrectly()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> configBuilder = new();
		PropertyValidationBuilder<TestRequest, string> propertyBuilder = configBuilder.Property(x => x.Name);
		propertyBuilder.Required();
		propertyBuilder.MinLength(5);
		propertyBuilder.MaxLength(100);

		// Act
		propertyBuilder
			.Alter("Must be 5 characters or more", "Minimum 5 chars")
			.Remove("Minimum 5 chars");

		// Assert
		
		GetRules(propertyBuilder).Count().ShouldBe(2); // Should have Required and MaxLength only
		GetRules(propertyBuilder).Where(r => r is StringLengthRule sl && sl.MinLength.HasValue).ShouldBeEmpty();
	}

	[Fact]
	public void ChainedOperations_RemoveThenAlter_WorksCorrectly()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> configBuilder = new();
		PropertyValidationBuilder<TestRequest, string> propertyBuilder = configBuilder.Property(x => x.Name);
		propertyBuilder.Required();
		propertyBuilder.MinLength(5);
		propertyBuilder.MaxLength(100);

		// Act
		propertyBuilder
			.Remove("Must be 5 characters or more")
			.Alter("Must be 100 characters or fewer", "Max 100 characters");

		// Assert
		
		GetRules(propertyBuilder).Count().ShouldBe(2); // Should have Required and MaxLength only
		ValidationRule? alteredRule = GetRules(propertyBuilder).FirstOrDefault(r => r.ErrorMessage == "Max 100 characters");
		alteredRule.ShouldNotBeNull();
		alteredRule.ShouldBeOfType<StringLengthRule>();
	}

	[Fact]
	public void ChainedOperations_AlterMultipleTimes_UpdatesCorrectly()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> configBuilder = new();
		PropertyValidationBuilder<TestRequest, string> propertyBuilder = configBuilder.Property(x => x.Name);
		propertyBuilder.Required();
		propertyBuilder.MinLength(5);

		// Act
		propertyBuilder
			.Alter("Must be 5 characters or more", "Intermediate message")
			.Alter("Intermediate message", "Final message");

		// Assert
		
		ValidationRule? alteredRule = GetRules(propertyBuilder).FirstOrDefault(r => r.ErrorMessage == "Final message");
		alteredRule.ShouldNotBeNull();
		alteredRule.ShouldBeOfType<StringLengthRule>();
	}

	#endregion
}
