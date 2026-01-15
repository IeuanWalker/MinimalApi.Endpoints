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
		rule.PropertyName.ShouldBe(propName);
		rule.ErrorMessage.ShouldBe("Is required");
	}

	[Fact]
	public void StringLengthRule_ThrowsWhenBothNull()
	{
		// Arrange
		string propName = "Description";

		// Act / Assert
		Should.Throw<ArgumentException>(() => new StringLengthRule(propName, null, null));
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
		both.MinLength.ShouldBe(2);
		both.MaxLength.ShouldBe(10);
		both.ErrorMessage.ShouldContain("at least 2");

		minOnly.MinLength.ShouldBe(3);
		minOnly.MaxLength.ShouldBeNull();
		minOnly.ErrorMessage.ShouldContain("3 characters or more");

		maxOnly.MinLength.ShouldBeNull();
		maxOnly.MaxLength.ShouldBe(5);
		maxOnly.ErrorMessage.ShouldContain("5 characters or fewer");
	}

	[Fact]
	public void PatternRule_ThrowsForEmptyPattern()
	{
		// Arrange
		string propName = "Code";

		// Act / Assert
		Should.Throw<ArgumentException>(() => new PatternRule(propName, ""));
	}

	[Fact]
	public void PatternRule_SetsPatternAndDefaultErrorMessage()
	{
		// Arrange
		string propName = "Code";

		// Act
		PatternRule rule = new(propName, "^[A-Z]{3}$");

		// Assert
		rule.Pattern.ShouldBe("^[A-Z]{3}$");
		rule.ErrorMessage.ShouldContain("Must match pattern");
	}

	[Fact]
	public void EmailRule_DefaultMessage()
	{
		// Arrange
		string propName = "Email";

		// Act
		EmailRule rule = new(propName);

		// Assert
		rule.PropertyName.ShouldBe(propName);
		rule.ErrorMessage.ShouldBe("Must be a valid email address");
	}

	[Fact]
	public void UrlRule_DefaultMessage()
	{
		// Arrange
		string propName = "Website";

		// Act
		UrlRule rule = new(propName);

		// Assert
		rule.PropertyName.ShouldBe(propName);
		rule.ErrorMessage.ShouldBe("Must be a valid URL");
	}

	[Fact]
	public void RangeRule_ThrowsWhenBothNull()
	{
		// Arrange
		string propName = "Amount";

		// Act / Assert
		Should.Throw<ArgumentException>(() => new RangeRule<int>(propName, null, null));
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
		both.Minimum.ShouldBe(1);
		both.Maximum.ShouldBe(5);
		both.ErrorMessage.ShouldContain("Must be");

		minOnly.Minimum.ShouldBe(2);
		minOnly.ExclusiveMinimum.ShouldBeTrue();
		minOnly.ErrorMessage.ShouldContain(">");

		maxOnly.Maximum.ShouldBe(10);
		maxOnly.ExclusiveMaximum.ShouldBeTrue();
		maxOnly.ErrorMessage.ShouldContain("<");
	}

	[Fact]
	public void CustomRule_RequiresErrorMessage()
	{
		// Arrange
		string propName = "CustomProp";

		// Act / Assert
		Should.Throw<ArgumentException>(() => new CustomRule<string>(propName, null!));
	}

	[Fact]
	public void DescriptionRule_RequiresDescription()
	{
		// Arrange
		string propName = "Desc";

		// Act / Assert
		Should.Throw<ArgumentException>(() => new DescriptionRule(propName, null!));
	}

	[Fact]
	public void EnumRule_ThrowsForInvalidEnumType()
	{
		// Arrange
		string propName = "Status";

		// Act / Assert
		Should.Throw<ArgumentException>(() => new EnumRule(propName, typeof(string), typeof(string)));
	}

	[Fact]
	public void EnumRule_SetsEnumTypeAndDefaultErrorMessage()
	{
		// Arrange
		string propName = "Status";

		// Act
		EnumRule rule = new(propName, typeof(DayOfWeek), typeof(int));

		// Assert
		rule.EnumType.ShouldBe(typeof(DayOfWeek));
		rule.PropertyType.ShouldBe(typeof(int));
		rule.ErrorMessage.ShouldContain(propName);
		rule.ErrorMessage.ShouldContain("{value}");
	}
}
