using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer.Core;

namespace IeuanWalker.MinimalApi.Endpoints.Tests.OpenApiDocumentTransformers.RequestPropertyEnhancer.Core;

public class OpenApiConstantsTests
{
	[Theory]
	[InlineData("System.String", true)]
	[InlineData("System.Int32", true)]
	[InlineData("MyNamespace.CustomType", false)]
	public void IsSystemType_Works(string input, bool expected)
	{
		// Arrange + Act
		bool result = SchemaConstants.IsSystemType(input);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("System.Nullable`1[System.Int32]", true)]
	[InlineData("System.Nullable`1[MyNamespace.Custom]", true)]
	[InlineData("System.Int32", false)]
	public void IsNullableType_Works(string input, bool expected)
	{
		// Arrange + Act
		bool result = SchemaConstants.IsNullableType(input);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("MyNamespace.Foo[]", true)]
	[InlineData("System.Collections.Generic.List`1[System.String]", true)]
	[InlineData("System.Collections.Generic.IEnumerable`1[System.String]", true)]
	[InlineData("System.String", false)]
	public void IsCollectionType_Works(string input, bool expected)
	{
		// Arrange + Act
		bool result = SchemaConstants.IsCollectionType(input);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("System.Collections.Generic.IDictionary`2[System.String,System.Int32]", true)]
	[InlineData("System.Collections.Generic.Dictionary`2[System.String,System.Int32]", true)]
	[InlineData("System.Collections.Concurrent.ConcurrentDictionary`2[System.String,System.Int32]", true)]
	[InlineData("System.Collections.Generic.List`1[System.String]", false)]
	public void IsDictionaryType_Works(string input, bool expected)
	{
		// Arrange + Act
		bool result = SchemaConstants.IsDictionaryType(input);

		// Assert
		result.ShouldBe(expected);
	}

	[Fact]
	public void ArraySuffix_Constant_IsBracketPair()
	{
		// Arrange + Act
		string result = SchemaConstants.ArraySuffix;

		// Assert
		result.ShouldBe("[]");
	}
}
