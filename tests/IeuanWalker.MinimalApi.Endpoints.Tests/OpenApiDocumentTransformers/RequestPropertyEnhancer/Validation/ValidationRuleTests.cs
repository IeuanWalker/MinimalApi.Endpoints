using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer.Validation;

namespace IeuanWalker.MinimalApi.Endpoints.Tests.OpenApiDocumentTransformers.RequestPropertyEnhancer.Validation;

public class ValidationRuleTests
{
	[Fact]
	public void RequiredRule_SetsPropertyNameAndDefaultErrorMessage()
	{
		// Arrange
		string propName = "Name";

		// Act
		RequiredRule rule = new(propName);

		// Assert
		Assert.Equal(propName, rule.PropertyName);
		Assert.Equal("Is required", rule.ErrorMessage);
	}

	[Fact]
	public void StringLengthRule_ThrowsWhenBothNull()
	{
		// Arrange
		string propName = "Description";

		// Act / Assert
		Assert.Throws<ArgumentException>(() => new StringLengthRule(propName, null, null));
	}

	[Fact]
	public void StringLengthRule_SetsDefaults_ForMinAndMax()
	{
		// Arrange
		string propName = "Title";

		// Act
		StringLengthRule both = new(propName, 2, 10);
		StringLengthRule minOnly = new(propName, 3, null);
		StringLengthRule maxOnly = new(propName, null, 5);

		// Assert
		Assert.Equal(2, both.MinLength);
		Assert.Equal(10, both.MaxLength);
		Assert.Contains("at least 2", both.ErrorMessage);

		Assert.Equal(3, minOnly.MinLength);
		Assert.Null(minOnly.MaxLength);
		Assert.Contains("3 characters or more", minOnly.ErrorMessage);

		Assert.Null(maxOnly.MinLength);
		Assert.Equal(5, maxOnly.MaxLength);
		Assert.Contains("5 characters or fewer", maxOnly.ErrorMessage);
	}

	[Fact]
	public void PatternRule_ThrowsForInvalidPattern_AndSetsDefaultMessage()
	{
		// Arrange
		string propName = "Code";

		// Act / Assert
		Assert.ThrowsAny<ArgumentException>(() => new PatternRule(propName, ""));

		// Act
		PatternRule rule = new(propName, "^[A-Z]{3}$");

		// Assert
		Assert.Equal("^[A-Z]{3}$", rule.Pattern);
		Assert.Contains("Must match pattern", rule.ErrorMessage);
	}

	[Fact]
	public void EmailRule_DefaultMessage()
	{
		// Arrange
		string propName = "Email";

		// Act
		EmailRule rule = new(propName);

		// Assert
		Assert.Equal(propName, rule.PropertyName);
		Assert.Equal("Must be a valid email address", rule.ErrorMessage);
	}

	[Fact]
	public void UrlRule_DefaultMessage()
	{
		// Arrange
		string propName = "Website";

		// Act
		UrlRule rule = new(propName);

		// Assert
		Assert.Equal(propName, rule.PropertyName);
		Assert.Equal("Must be a valid URL", rule.ErrorMessage);
	}

	[Fact]
	public void RangeRule_ThrowsWhenBothNull()
	{
		// Arrange
		string propName = "Amount";

		// Act / Assert
		Assert.Throws<ArgumentException>(() => new RangeRule<int>(propName, null, null));
	}

	[Fact]
	public void RangeRule_SetsDefaults_ForMinAndMax()
	{
		// Arrange
		string propName = "Count";

		// Act
		RangeRule<int> both = new(propName, 1, 5);
		RangeRule<int> minOnly = new(propName, 2, null, exclusiveMinimum: true);
		RangeRule<int> maxOnly = new(propName, null, 10, exclusiveMaximum: true);

		// Assert
		Assert.Equal(1, both.Minimum);
		Assert.Equal(5, both.Maximum);
		Assert.Contains("Must be", both.ErrorMessage);

		Assert.Equal(2, minOnly.Minimum);
		Assert.True(minOnly.ExclusiveMinimum);
		Assert.Contains(">", minOnly.ErrorMessage);

		Assert.Equal(10, maxOnly.Maximum);
		Assert.True(maxOnly.ExclusiveMaximum);
		Assert.Contains("<", maxOnly.ErrorMessage);
	}

	[Fact]
	public void CustomRule_RequiresErrorMessage()
	{
		// Arrange
		string propName = "CustomProp";

		// Act / Assert
		Assert.ThrowsAny<ArgumentException>(() => new CustomRule<string>(propName, null!));
	}

	[Fact]
	public void DescriptionRule_RequiresDescription()
	{
		// Arrange
		string propName = "Desc";

		// Act / Assert
		Assert.ThrowsAny<ArgumentException>(() => new DescriptionRule(propName, null!));
	}

	[Fact]
	public void EnumRule_ValidatesEnumTypeAndSetsDefaults()
	{
		// Arrange
		string propName = "Status";

		// Act / Assert - invalid enum type
		Assert.Throws<ArgumentException>(() => new EnumRule(propName, typeof(string), typeof(string)));

		// Act - valid enum
		EnumRule rule = new(propName, typeof(DayOfWeek), typeof(int));

		// Assert
		Assert.Equal(typeof(DayOfWeek), rule.EnumType);
		Assert.Equal(typeof(int), rule.PropertyType);
		Assert.Contains(propName, rule.ErrorMessage);
		Assert.Contains("{value}", rule.ErrorMessage);
	}
}
