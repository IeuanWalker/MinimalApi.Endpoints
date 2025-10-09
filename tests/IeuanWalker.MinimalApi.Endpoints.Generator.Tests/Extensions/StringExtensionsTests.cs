using Shouldly;
using IeuanWalker.MinimalApi.Endpoints.Generator.Extensions;

namespace IeuanWalker.MinimalApi.Endpoints.Generator.Tests.Extensions;

public class StringExtensionsTests
{
	[Theory]
	[InlineData("HelloWorld", "HelloWorld")]
	[InlineData("Hello-World", "Hello_World")]
	[InlineData("Hello World", "Hello_World")]
	[InlineData("Hello@World#123", "Hello_World_123")]
	[InlineData("test.name", "test_name")]
	public void Sanitize_WithDefaultReplacement_ReplacesNonAlphanumeric(string input, string expected)
	{
		// Act
		string result = input.Sanitize();

		// Assert
		result.ShouldBe(expected);
	}

	[Fact]
	public void Sanitize_WithCustomReplacement_UsesCustomCharacter()
	{
		// Arrange
		const string input = "Hello-World";

		// Act
		string result = input.Sanitize("-");

		// Assert
		result.ShouldBe("Hello-World");
	}

	[Fact]
	public void Sanitize_WithEmptyReplacement_RemovesNonAlphanumeric()
	{
		// Arrange
		const string input = "Hello-World";

		// Act
		string result = input.Sanitize(string.Empty);

		// Assert
		result.ShouldBe("HelloWorld");
	}

	[Theory]
	[InlineData("HelloWorld", "helloWorld")]
	[InlineData("H", "h")]
	[InlineData("hello", "hello")]
	[InlineData("", "")]
	public void ToLowerFirstLetter_ConvertsFirstCharacterToLowerCase(string input, string expected)
	{
		// Act
		string result = input.ToLowerFirstLetter();

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("helloWorld", "HelloWorld")]
	[InlineData("h", "H")]
	[InlineData("Hello", "Hello")]
	[InlineData("", "")]
	public void ToUpperFirstLetter_ConvertsFirstCharacterToUpperCase(string input, string expected)
	{
		// Act
		string result = input.ToUpperFirstLetter();

		// Assert
		result.ShouldBe(expected);
	}
}
