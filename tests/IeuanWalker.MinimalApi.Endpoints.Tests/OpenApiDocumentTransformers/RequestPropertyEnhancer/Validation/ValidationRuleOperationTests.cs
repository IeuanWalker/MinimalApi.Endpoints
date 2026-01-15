namespace IeuanWalker.MinimalApi.Endpoints.Tests.OpenApiDocumentTransformers.RequestPropertyEnhancer.Validation;

using System;
using System.Collections.Generic;
using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer.Validation;
using Shouldly;
using Xunit;

public class ValidationRuleOperationTests
{
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
}
