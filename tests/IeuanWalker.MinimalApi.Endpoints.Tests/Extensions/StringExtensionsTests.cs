using IeuanWalker.MinimalApi.Endpoints.Extensions;

namespace IeuanWalker.MinimalApi.Endpoints.Tests.Extensions;

public class StringExtensionsTests
{
	[Theory]
	[InlineData("HelloWorld", "helloWorld")]
	[InlineData("H", "h")]
	[InlineData("hello", "hello")]
	[InlineData("", "")]
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
}
