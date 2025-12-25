using IeuanWalker.MinimalApi.Endpoints.Validation;
using Shouldly;

namespace IeuanWalker.MinimalApi.Endpoints.Tests.Validation;

public class RangeRuleTests
{
	[Fact]
	public void RangeRule_WithValueGreaterThanMinimum_ReturnsTrue()
	{
		// Arrange
		RangeRule<int> rule = new()
		{
			PropertyName = "Priority",
			Minimum = 0,
			ExclusiveMinimum = false,
			ErrorMessage = "Priority must be greater than or equal to 0"
		};

		object value = 5;

		// Act
		bool result = rule.IsValid(value);

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public void RangeRule_WithValueEqualToMinimum_ReturnsTrue()
	{
		// Arrange
		RangeRule<int> rule = new()
		{
			PropertyName = "Priority",
			Minimum = 0,
			ExclusiveMinimum = false,
			ErrorMessage = "Priority must be greater than or equal to 0"
		};

		object value = 0;

		// Act
		bool result = rule.IsValid(value);

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public void RangeRule_WithValueLessThanMinimum_ReturnsFalse()
	{
		// Arrange
		RangeRule<int> rule = new()
		{
			PropertyName = "Priority",
			Minimum = 0,
			ExclusiveMinimum = false,
			ErrorMessage = "Priority must be greater than or equal to 0"
		};

		object value = -1;

		// Act
		bool result = rule.IsValid(value);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void RangeRule_WithValueBetweenMinAndMax_ReturnsTrue()
	{
		// Arrange
		RangeRule<int> rule = new()
		{
			PropertyName = "Priority",
			Minimum = 0,
			Maximum = 10,
			ExclusiveMinimum = false,
			ExclusiveMaximum = false,
			ErrorMessage = "Priority must be between 0 and 10"
		};

		object value = 5;

		// Act
		bool result = rule.IsValid(value);

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public void RangeRule_WithValueGreaterThanMaximum_ReturnsFalse()
	{
		// Arrange
		RangeRule<int> rule = new()
		{
			PropertyName = "Priority",
			Minimum = 0,
			Maximum = 10,
			ExclusiveMinimum = false,
			ExclusiveMaximum = false,
			ErrorMessage = "Priority must be between 0 and 10"
		};

		object value = 11;

		// Act
		bool result = rule.IsValid(value);

		// Assert
		result.ShouldBeFalse();
	}
}
