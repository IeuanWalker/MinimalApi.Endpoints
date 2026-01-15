namespace IeuanWalker.MinimalApi.Endpoints.Tests.OpenApiDocumentTransformers.RequestPropertyEnhancer.Validation;

using System;
using System.Collections.Generic;
using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer.Validation;
using Shouldly;
using Xunit;

public class ValidationRuleOperationTests
{
	#region AlterOperation Constructor Tests

	[Fact]
	public void AlterOperation_NullOldMessage_Throws()
	{
		// Act & Assert
		Should.Throw<ArgumentException>(() => new AlterOperation(null!, "New message"));
	}

	[Fact]
	public void AlterOperation_EmptyOldMessage_Throws()
	{
		// Act & Assert
		Should.Throw<ArgumentException>(() => new AlterOperation("", "New message"));
	}

	[Fact]
	public void AlterOperation_WhitespaceOldMessage_Throws()
	{
		// Act & Assert
		Should.Throw<ArgumentException>(() => new AlterOperation("   ", "New message"));
	}

	[Fact]
	public void AlterOperation_NullNewMessage_Throws()
	{
		// Act & Assert
		Should.Throw<ArgumentException>(() => new AlterOperation("Old message", null!));
	}

	[Fact]
	public void AlterOperation_EmptyNewMessage_Throws()
	{
		// Act & Assert
		Should.Throw<ArgumentException>(() => new AlterOperation("Old message", ""));
	}

	[Fact]
	public void AlterOperation_WhitespaceNewMessage_Throws()
	{
		// Act & Assert
		Should.Throw<ArgumentException>(() => new AlterOperation("Old message", "   "));
	}

	#endregion

	#region AlterOperation Apply Tests

	[Fact]
	public void AlterOperation_Replaces_ErrorMessage()
	{
		// Arrange
		List<ValidationRule> rules =
		[
			new RequiredRule("Name", "Original message")
		];

		AlterOperation op = new("Original message", "New message");

		// Act
		op.Apply(rules);

		// Assert
		rules.Count.ShouldBe(1);
		rules[0].ErrorMessage.ShouldBe("New message");
	}

	[Fact]
	public void AlterOperation_Throws_When_OldMessage_NotFound()
	{
		// Arrange
		List<ValidationRule> rules = [new RequiredRule("Name", "Some message")];
		AlterOperation op = new("Not found", "New message");

		// Act & Assert
		ArgumentException ex = Should.Throw<ArgumentException>(() => op.Apply(rules));
		ex.Message.ShouldContain("No validation rule exists with error message");
	}

	[Fact]
	public void AlterOperation_OnlyAltersFirstMatchingRule()
	{
		// Arrange
		List<ValidationRule> rules =
		[
			new RequiredRule("Name", "Same message"),
			new RequiredRule("Email", "Same message")
		];

		AlterOperation op = new("Same message", "New message");

		// Act
		op.Apply(rules);

		// Assert
		rules[0].ErrorMessage.ShouldBe("New message");
		rules[1].ErrorMessage.ShouldBe("Same message"); // Second one unchanged
	}

	[Fact]
	public void AlterOperation_PreservesOtherRules()
	{
		// Arrange
		List<ValidationRule> rules =
		[
			new RequiredRule("Name", "To alter"),
			new EmailRule("Email", "Email message"),
			new UrlRule("Website", "Url message")
		];

		AlterOperation op = new("To alter", "Altered message");

		// Act
		op.Apply(rules);

		// Assert
		rules.Count.ShouldBe(3);
		rules[0].ErrorMessage.ShouldBe("Altered message");
		rules[1].ErrorMessage.ShouldBe("Email message");
		rules[2].ErrorMessage.ShouldBe("Url message");
	}

	#endregion

	#region RemoveOperation Constructor Tests

	[Fact]
	public void RemoveOperation_NullMessage_Throws()
	{
		// Act & Assert
		Should.Throw<ArgumentException>(() => new RemoveOperation(null!));
	}

	[Fact]
	public void RemoveOperation_EmptyMessage_Throws()
	{
		// Act & Assert
		Should.Throw<ArgumentException>(() => new RemoveOperation(""));
	}

	[Fact]
	public void RemoveOperation_WhitespaceMessage_Throws()
	{
		// Act & Assert
		Should.Throw<ArgumentException>(() => new RemoveOperation("   "));
	}

	#endregion

	#region RemoveOperation Apply Tests

	[Fact]
	public void RemoveOperation_Removes_Rule_By_ErrorMessage()
	{
		// Arrange
		List<ValidationRule> rules =
		[
			new RequiredRule("Name", "To remove"),
			new RequiredRule("Other", "Keep me")
		];

		RemoveOperation op = new("To remove");

		// Act
		op.Apply(rules);

		// Assert
		rules.Count.ShouldBe(1);
		rules[0].ErrorMessage.ShouldBe("Keep me");
	}

	[Fact]
	public void RemoveOperation_Throws_When_NotFound()
	{
		// Arrange
		List<ValidationRule> rules = [new RequiredRule("Name", "Some message")];
		RemoveOperation op = new("Does not exist");

		// Act & Assert
		ArgumentException ex = Should.Throw<ArgumentException>(() => op.Apply(rules));
		ex.Message.ShouldContain("No validation rule exists with error message");
	}

	[Fact]
	public void RemoveOperation_OnlyRemovesFirstMatchingRule()
	{
		// Arrange
		List<ValidationRule> rules =
		[
			new RequiredRule("Name", "Duplicate"),
			new RequiredRule("Email", "Duplicate"),
			new RequiredRule("Other", "Keep me")
		];

		RemoveOperation op = new("Duplicate");

		// Act
		op.Apply(rules);

		// Assert
		rules.Count.ShouldBe(2);
		rules[0].ErrorMessage.ShouldBe("Duplicate"); // Second "Duplicate" is now first
		rules[1].ErrorMessage.ShouldBe("Keep me");
	}

	[Fact]
	public void RemoveOperation_RemovesLastRule()
	{
		// Arrange
		List<ValidationRule> rules =
		[
			new RequiredRule("Name", "Keep me"),
			new RequiredRule("Email", "Remove me")
		];

		RemoveOperation op = new("Remove me");

		// Act
		op.Apply(rules);

		// Assert
		rules.Count.ShouldBe(1);
		rules[0].ErrorMessage.ShouldBe("Keep me");
	}

	[Fact]
	public void RemoveOperation_RemovesOnlyRule()
	{
		// Arrange
		List<ValidationRule> rules = [new RequiredRule("Name", "Only rule")];

		RemoveOperation op = new("Only rule");

		// Act
		op.Apply(rules);

		// Assert
		rules.ShouldBeEmpty();
	}

	#endregion

	#region RemoveAllOperation Apply Tests

	[Fact]
	public void RemoveAllOperation_Clears_List_When_NotEmpty()
	{
		// Arrange
		List<ValidationRule> rules = [new RequiredRule("Name", "msg")];
		RemoveAllOperation op = new();

		// Act
		op.Apply(rules);

		// Assert
		rules.ShouldBeEmpty();
	}

	[Fact]
	public void RemoveAllOperation_Throws_When_Empty()
	{
		// Arrange
		List<ValidationRule> rules = [];
		RemoveAllOperation op = new();

		// Act & Assert
		InvalidOperationException ex = Should.Throw<InvalidOperationException>(() => op.Apply(rules));
		ex.Message.ShouldBe("No validation rules exist to remove.");
	}

	[Fact]
	public void RemoveAllOperation_Clears_MultipleRules()
	{
		// Arrange
		List<ValidationRule> rules =
		[
			new RequiredRule("Name", "msg1"),
			new EmailRule("Email", "msg2"),
			new UrlRule("Website", "msg3"),
			new PatternRule("Code", @"^\d+$", "msg4")
		];
		RemoveAllOperation op = new();

		// Act
		op.Apply(rules);

		// Assert
		rules.ShouldBeEmpty();
	}

	#endregion

	#region Operation Properties Tests

	[Fact]
	public void AlterOperation_ExposesOldAndNewErrorMessages()
	{
		// Arrange
		AlterOperation op = new("Old", "New");

		// Assert
		op.OldErrorMessage.ShouldBe("Old");
		op.NewErrorMessage.ShouldBe("New");
	}

	[Fact]
	public void RemoveOperation_ExposesErrorMessage()
	{
		// Arrange
		RemoveOperation op = new("To remove");

		// Assert
		op.ErrorMessage.ShouldBe("To remove");
	}

	#endregion
}
