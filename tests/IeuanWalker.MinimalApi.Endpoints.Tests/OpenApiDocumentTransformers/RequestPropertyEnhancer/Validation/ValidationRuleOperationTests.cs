namespace IeuanWalker.MinimalApi.Endpoints.Tests.OpenApiDocumentTransformers.RequestPropertyEnhancer.Validation;

using System;
using System.Collections.Generic;
using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer.Validation;
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
		Assert.Single(rules);
		Assert.Equal("New message", rules[0].ErrorMessage);
	}

	[Fact]
	public void AlterOperation_Throws_When_OldMessage_NotFound()
	{
		// Arrange
		List<ValidationRule> rules = [new RequiredRule("Name", "Some message")];
		AlterOperation op = new("Not found", "New message");

		// Act & Assert
		ArgumentException ex = Assert.Throws<ArgumentException>(() => op.Apply(rules));
		Assert.Contains("No validation rule exists with error message", ex.Message);
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
		Assert.Single(rules);
		Assert.Equal("Keep me", rules[0].ErrorMessage);
	}

	[Fact]
	public void RemoveOperation_Throws_When_NotFound()
	{
		// Arrange
		List<ValidationRule> rules = [new RequiredRule("Name", "Some message")];
		RemoveOperation op = new("Does not exist");

		// Act & Assert
		ArgumentException ex = Assert.Throws<ArgumentException>(() => op.Apply(rules));
		Assert.Contains("No validation rule exists with error message", ex.Message);
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
		Assert.Empty(rules);
	}

	[Fact]
	public void RemoveAllOperation_Throws_When_Empty()
	{
		// Arrange
		List<ValidationRule> rules = [];
		RemoveAllOperation op = new();

		// Act & Assert
		InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => op.Apply(rules));
		Assert.Equal("No validation rules exist to remove.", ex.Message);
	}
}
