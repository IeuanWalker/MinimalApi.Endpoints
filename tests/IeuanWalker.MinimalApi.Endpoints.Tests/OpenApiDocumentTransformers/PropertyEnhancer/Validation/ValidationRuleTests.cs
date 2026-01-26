using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.PropertyEnhancer.Validation;

namespace IeuanWalker.MinimalApi.Endpoints.Tests.OpenApiDocumentTransformers.PropertyEnhancer.Validation;

public class ValidationRuleTests
{
	#region RequiredRule Tests

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
	public void RequiredRule_WithCustomErrorMessage_UsesCustomMessage()
	{
		// Arrange
		string propName = "Name";
		string customMessage = "This field is mandatory";

		// Act
		RequiredRule rule = new(propName, customMessage);

		// Assert
		rule.PropertyName.ShouldBe(propName);
		rule.ErrorMessage.ShouldBe(customMessage);
	}

	[Fact]
	public void RequiredRule_NullPropertyName_ThrowsArgumentException()
	{
		// Act & Assert
		Should.Throw<ArgumentException>(() => new RequiredRule(null!));
	}

	[Fact]
	public void RequiredRule_EmptyPropertyName_ThrowsArgumentException()
	{
		// Act & Assert
		Should.Throw<ArgumentException>(() => new RequiredRule(""));
	}

	[Fact]
	public void RequiredRule_WhitespacePropertyName_ThrowsArgumentException()
	{
		// Act & Assert
		Should.Throw<ArgumentException>(() => new RequiredRule("   "));
	}

	[Fact]
	public void RequiredRule_WhitespaceErrorMessage_UsesDefaultMessage()
	{
		// Act
		RequiredRule rule = new("Name", "   ");

		// Assert
		rule.ErrorMessage.ShouldBe("Is required");
	}

	#endregion

	#region StringLengthRule Tests

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
	public void StringLengthRule_WithCustomErrorMessage_UsesCustomMessage()
	{
		// Arrange
		string customMessage = "Name must be between 5 and 100 characters";

		// Act
		StringLengthRule rule = new("Name", 5, 100, customMessage);

		// Assert
		rule.ErrorMessage.ShouldBe(customMessage);
	}

	[Fact]
	public void StringLengthRule_ZeroMinLength_SetsCorrectly()
	{
		// Act
		StringLengthRule rule = new("Name", 0, 100);

		// Assert
		rule.MinLength.ShouldBe(0);
		rule.MaxLength.ShouldBe(100);
	}

	[Fact]
	public void StringLengthRule_ZeroMaxLength_SetsCorrectly()
	{
		// Act
		StringLengthRule rule = new("Name", null, 0);

		// Assert
		rule.MinLength.ShouldBeNull();
		rule.MaxLength.ShouldBe(0);
	}

	#endregion

	#region PatternRule Tests

	[Fact]
	public void PatternRule_ThrowsForEmptyPattern()
	{
		// Arrange
		string propName = "Code";

		// Act / Assert
		Should.Throw<ArgumentException>(() => new PatternRule(propName, ""));
	}

	[Fact]
	public void PatternRule_ThrowsForNullPattern()
	{
		// Arrange
		string propName = "Code";

		// Act / Assert
		Should.Throw<ArgumentException>(() => new PatternRule(propName, null!));
	}

	[Fact]
	public void PatternRule_ThrowsForWhitespacePattern()
	{
		// Arrange
		string propName = "Code";

		// Act / Assert
		Should.Throw<ArgumentException>(() => new PatternRule(propName, "   "));
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
	public void PatternRule_WithCustomErrorMessage_UsesCustomMessage()
	{
		// Act
		PatternRule rule = new("Code", @"^\d{5}$", "Must be a 5-digit ZIP code");

		// Assert
		rule.ErrorMessage.ShouldBe("Must be a 5-digit ZIP code");
	}

	#endregion

	#region EmailRule Tests

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
	public void EmailRule_WithCustomErrorMessage_UsesCustomMessage()
	{
		// Act
		EmailRule rule = new("Email", "Please enter a valid email");

		// Assert
		rule.ErrorMessage.ShouldBe("Please enter a valid email");
	}

	#endregion

	#region UrlRule Tests

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
	public void UrlRule_WithCustomErrorMessage_UsesCustomMessage()
	{
		// Act
		UrlRule rule = new("Website", "Enter a valid website URL");

		// Assert
		rule.ErrorMessage.ShouldBe("Enter a valid website URL");
	}

	#endregion

	#region RangeRule Tests

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
	public void RangeRule_AllExclusiveCombinations_GeneratesCorrectMessages()
	{
		// Arrange
		string propName = "Value";

		// Act - All combinations of exclusive flags
		RangeRule<int> bothInclusive = new(propName, 1, 10, false, false);
		RangeRule<int> minExclusive = new(propName, 1, 10, true, false);
		RangeRule<int> maxExclusive = new(propName, 1, 10, false, true);
		RangeRule<int> bothExclusive = new(propName, 1, 10, true, true);

		// Assert
		bothInclusive.ErrorMessage.ShouldContain(">=");
		bothInclusive.ErrorMessage.ShouldContain("<=");

		minExclusive.ErrorMessage.ShouldContain("> 1");
		minExclusive.ErrorMessage.ShouldContain("<= 10");

		maxExclusive.ErrorMessage.ShouldContain(">= 1");
		maxExclusive.ErrorMessage.ShouldContain("< 10");

		bothExclusive.ErrorMessage.ShouldContain("> 1");
		bothExclusive.ErrorMessage.ShouldContain("< 10");
	}

	[Fact]
	public void RangeRule_WithCustomErrorMessage_UsesCustomMessage()
	{
		// Act
		RangeRule<decimal> rule = new("Price", 0.01m, 1000m, false, false, "Price must be between $0.01 and $1000");

		// Assert
		rule.ErrorMessage.ShouldBe("Price must be between $0.01 and $1000");
	}

	[Fact]
	public void RangeRule_SupportsDecimalType()
	{
		// Act
		RangeRule<decimal> rule = new("Price", 0.01m, 999.99m);

		// Assert
		rule.Minimum.ShouldBe(0.01m);
		rule.Maximum.ShouldBe(999.99m);
	}

	[Fact]
	public void RangeRule_SupportsDoubleType()
	{
		// Act
		RangeRule<double> rule = new("Rating", 0.0, 5.0);

		// Assert
		rule.Minimum.ShouldBe(0.0);
		rule.Maximum.ShouldBe(5.0);
	}

	[Fact]
	public void RangeRule_SupportsLongType()
	{
		// Act
		RangeRule<long> rule = new("BigNumber", 0L, long.MaxValue);

		// Assert
		rule.Minimum.ShouldBe(0L);
		rule.Maximum.ShouldBe(long.MaxValue);
	}

	#endregion

	#region CustomRule Tests

	[Fact]
	public void CustomRule_RequiresErrorMessage()
	{
		// Arrange
		string propName = "CustomProp";

		// Act / Assert
		Should.Throw<ArgumentException>(() => new CustomRule<string>(propName, null!));
	}

	[Fact]
	public void CustomRule_EmptyErrorMessage_Throws()
	{
		// Act / Assert
		Should.Throw<ArgumentException>(() => new CustomRule<string>("Prop", ""));
	}

	[Fact]
	public void CustomRule_WhitespaceErrorMessage_Throws()
	{
		// Act / Assert
		Should.Throw<ArgumentException>(() => new CustomRule<string>("Prop", "   "));
	}

	[Fact]
	public void CustomRule_ValidInput_SetsProperties()
	{
		// Act
		CustomRule<int> rule = new("Age", "Age must be a positive number");

		// Assert
		rule.PropertyName.ShouldBe("Age");
		rule.ErrorMessage.ShouldBe("Age must be a positive number");
	}

	#endregion

	#region DescriptionRule Tests

	[Fact]
	public void DescriptionRule_RequiresDescription()
	{
		// Arrange
		string propName = "Desc";

		// Act / Assert
		Should.Throw<ArgumentException>(() => new DescriptionRule(propName, null!));
	}

	[Fact]
	public void DescriptionRule_EmptyDescription_Throws()
	{
		// Act / Assert
		Should.Throw<ArgumentException>(() => new DescriptionRule("Prop", ""));
	}

	[Fact]
	public void DescriptionRule_WhitespaceDescription_Throws()
	{
		// Act / Assert
		Should.Throw<ArgumentException>(() => new DescriptionRule("Prop", "   "));
	}

	[Fact]
	public void DescriptionRule_ValidInput_SetsProperties()
	{
		// Act
		DescriptionRule rule = new("Name", "The user's full name");

		// Assert
		rule.PropertyName.ShouldBe("Name");
		rule.Description.ShouldBe("The user's full name");
	}

	#endregion

	#region EnumRule Tests

	[Fact]
	public void EnumRule_ThrowsForInvalidEnumType()
	{
		// Arrange
		string propName = "Status";

		// Act / Assert
		Should.Throw<ArgumentException>(() => new EnumRule(propName, typeof(string), typeof(string)));
	}

	[Fact]
	public void EnumRule_ThrowsForNullEnumType()
	{
		// Act / Assert
		Should.Throw<ArgumentNullException>(() => new EnumRule("Status", null!, typeof(int)));
	}

	[Fact]
	public void EnumRule_ThrowsForNullPropertyType()
	{
		// Act / Assert
		Should.Throw<ArgumentNullException>(() => new EnumRule("Status", typeof(DayOfWeek), null!));
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

	[Fact]
	public void EnumRule_WithCustomErrorMessage_UsesCustomMessage()
	{
		// Act
		EnumRule rule = new("Status", typeof(DayOfWeek), typeof(int), "Invalid status value");

		// Assert
		rule.ErrorMessage.ShouldBe("Invalid status value");
	}

	[Fact]
	public void EnumRule_WithStringPropertyType_SetsPropertyType()
	{
		// Act
		EnumRule rule = new("Status", typeof(DayOfWeek), typeof(string));

		// Assert
		rule.PropertyType.ShouldBe(typeof(string));
	}

	#endregion

	#region AppendRuleToPropertyDescription Tests

	[Fact]
	public void ValidationRule_AppendRuleToPropertyDescription_DefaultsToNull()
	{
		// Act
		RequiredRule rule = new("Name");

		// Assert
		rule.AppendRuleToPropertyDescription.ShouldBeNull();
	}

	[Fact]
	public void ValidationRule_AppendRuleToPropertyDescription_CanBeSetViaInit()
	{
		// Act
		RequiredRule rule = new("Name") { AppendRuleToPropertyDescription = false };

		// Assert
		rule.AppendRuleToPropertyDescription.ShouldBe(false);
	}

	#endregion
}
