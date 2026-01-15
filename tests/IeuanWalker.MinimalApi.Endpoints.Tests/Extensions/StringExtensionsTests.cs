using IeuanWalker.MinimalApi.Endpoints.Extensions;

namespace IeuanWalker.MinimalApi.Endpoints.Tests.Extensions;

public class StringExtensionsTests
{
	[Theory]
	[InlineData("HelloWorld", "helloWorld")]
	[InlineData("H", "h")]
	[InlineData("hello", "hello")]
	[InlineData("", "")]
	[InlineData("A", "a")]
	[InlineData("a", "a")]
	[InlineData("Z", "z")]
	public void ToCamelCase_ConvertsFirstCharacterToLower(string input, string expected)
	{
		// Act
		string result = input.ToCamelCase();

		// Assert
		result.ShouldBe(expected);
	}

	[Fact]
	public void ToCamelCase_NullInput_ReturnsNull()
	{
		// Arrange
		string? input = null;

		// Act
		string? result = input!.ToCamelCase();

		// Assert
		result.ShouldBeNull();
	}

	[Theory]
	[InlineData("123abc", "123abc")]
	[InlineData("_underscore", "_underscore")]
	[InlineData("$dollar", "$dollar")]
	[InlineData("@symbol", "@symbol")]
	[InlineData("#hash", "#hash")]
	public void ToCamelCase_SpecialCharactersOrNumbers_ReturnsSameString(string input, string expected)
	{
		// Act
		string result = input.ToCamelCase();

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData(" ", " ")]
	[InlineData("  ", "  ")]
	[InlineData("\t", "\t")]
	[InlineData("\n", "\n")]
	public void ToCamelCase_WhitespaceStrings_ReturnsSameString(string input, string expected)
	{
		// Act
		string result = input.ToCamelCase();

		// Assert
		result.ShouldBe(expected);
	}

	[Fact]
	public void ToCamelCase_UnicodeUppercaseFirst_ConvertsToLowercase()
	{
		// Arrange
		string input = "Übung"; // German word with umlaut

		// Act
		string result = input.ToCamelCase();

		// Assert
		result.ShouldBe("übung");
	}

	[Fact]
	public void ToCamelCase_UnicodeAlreadyLowercase_ReturnsSame()
	{
		// Arrange
		string input = "éclair";

		// Act
		string result = input.ToCamelCase();

		// Assert
		result.ShouldBe("éclair");
	}

	[Theory]
	[InlineData("ABC", "aBC")]
	[InlineData("URL", "uRL")]
	[InlineData("ID", "iD")]
	public void ToCamelCase_AllUppercase_OnlyConvertsFirstCharacter(string input, string expected)
	{
		// Act
		string result = input.ToCamelCase();

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("PascalCase", "pascalCase")]
	[InlineData("CamelCaseTest", "camelCaseTest")]
	[InlineData("TestProperty", "testProperty")]
	public void ToCamelCase_PascalCaseStrings_ConvertsToCamelCase(string input, string expected)
	{
		// Act
		string result = input.ToCamelCase();

		// Assert
		result.ShouldBe(expected);
	}

	[Fact]
	public void ToCamelCase_StringWithSpaces_PreservesSpaces()
	{
		// Arrange
		string input = "Hello World";

		// Act
		string result = input.ToCamelCase();

		// Assert
		result.ShouldBe("hello World");
	}

	[Fact]
	public void ToCamelCase_LongString_ConvertsOnlyFirstCharacter()
	{
		// Arrange
		string input = new('A', 1000);

		// Act
		string result = input.ToCamelCase();

		// Assert
		result[0].ShouldBe('a');
		result.Length.ShouldBe(1000);
		result[1..].ShouldBe(new string('A', 999));
	}
}
