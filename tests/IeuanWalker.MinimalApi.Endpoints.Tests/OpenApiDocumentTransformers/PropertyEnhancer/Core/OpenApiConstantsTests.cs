using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.PropertyEnhancer.Core;

namespace IeuanWalker.MinimalApi.Endpoints.Tests.OpenApiDocumentTransformers.PropertyEnhancer.Core;

public class OpenApiConstantsTests
{
	#region IsSystemType Tests

	[Theory]
	[InlineData("System.String", true)]
	[InlineData("System.Int32", true)]
	[InlineData("System.Int64", true)]
	[InlineData("System.Boolean", true)]
	[InlineData("System.DateTime", true)]
	[InlineData("System.Guid", true)]
	[InlineData("System.Decimal", true)]
	[InlineData("MyNamespace.CustomType", false)]
	[InlineData("Custom.System.Type", false)]
	[InlineData("", false)]
	[InlineData("Sys", false)]
	public void IsSystemType_Works(string input, bool expected)
	{
		// Arrange + Act
		bool result = SchemaConstants.IsSystemType(input);

		// Assert
		result.ShouldBe(expected);
	}

	#endregion

	#region IsNullableType Tests

	[Theory]
	[InlineData("System.Nullable`1[System.Int32]", true)]
	[InlineData("System.Nullable`1[MyNamespace.Custom]", true)]
	[InlineData("System.Nullable`1[[MyEnum, MyAssembly]]", true)]
	[InlineData("System.Int32", false)]
	[InlineData("System.Nullable", false)]
	[InlineData("System.Nullable`2[A,B]", false)]
	[InlineData("", false)]
	public void IsNullableType_Works(string input, bool expected)
	{
		// Arrange + Act
		bool result = SchemaConstants.IsNullableType(input);

		// Assert
		result.ShouldBe(expected);
	}

	#endregion

	#region IsCollectionType Tests

	[Theory]
	[InlineData("MyNamespace.Foo[]", true)]
	[InlineData("System.Collections.Generic.List`1[System.String]", true)]
	[InlineData("System.Collections.Generic.IEnumerable`1[System.String]", true)]
	[InlineData("System.Collections.Generic.ICollection`1[System.Int32]", true)]
	[InlineData("System.Collections.Generic.IReadOnlyList`1[System.String]", true)]
	[InlineData("System.Collections.Generic.IReadOnlyCollection`1[System.Object]", true)]
	[InlineData("int[]", true)]
	[InlineData("System.String", false)]
	[InlineData("System.Collections.Generic.Dictionary`2[System.String,System.Int32]", false)]
	[InlineData("", false)]
	public void IsCollectionType_Works(string input, bool expected)
	{
		// Arrange + Act
		bool result = SchemaConstants.IsCollectionType(input);

		// Assert
		result.ShouldBe(expected);
	}

	#endregion

	#region IsDictionaryType Tests

	[Theory]
	[InlineData("System.Collections.Generic.IDictionary`2[System.String,System.Int32]", true)]
	[InlineData("System.Collections.Generic.Dictionary`2[System.String,System.Int32]", true)]
	[InlineData("System.Collections.Concurrent.ConcurrentDictionary`2[System.String,System.Int32]", true)]
	[InlineData("System.Collections.Generic.IReadOnlyDictionary`2[System.String,System.Object]", true)]
	[InlineData("System.Collections.Generic.List`1[System.String]", false)]
	[InlineData("System.String", false)]
	[InlineData("", false)]
	public void IsDictionaryType_Works(string input, bool expected)
	{
		// Arrange + Act
		bool result = SchemaConstants.IsDictionaryType(input);

		// Assert
		result.ShouldBe(expected);
	}

	#endregion

	#region Constant Values Tests

	[Fact]
	public void ArraySuffix_Constant_IsBracketPair()
	{
		// Arrange + Act
		string result = SchemaConstants.ArraySuffix;

		// Assert
		result.ShouldBe("[]");
	}

	[Fact]
	public void NullableExtension_Constant_IsNullable()
	{
		// Assert
		SchemaConstants.NullableExtension.ShouldBe("nullable");
	}

	[Fact]
	public void EnumExtension_Constant_IsEnum()
	{
		// Assert
		SchemaConstants.EnumExtension.ShouldBe("enum");
	}

	[Fact]
	public void EnumVarNamesExtension_Constant_HasCorrectValue()
	{
		// Assert
		SchemaConstants.EnumVarNamesExtension.ShouldBe("x-enum-varnames");
	}

	[Fact]
	public void EnumDescriptionsExtension_Constant_HasCorrectValue()
	{
		// Assert
		SchemaConstants.EnumDescriptionsExtension.ShouldBe("x-enum-descriptions");
	}

	[Fact]
	public void SystemPrefix_Constant_HasCorrectValue()
	{
		// Assert
		SchemaConstants.SystemPrefix.ShouldBe("System.");
	}

	[Fact]
	public void NullableTypePrefix_Constant_HasCorrectValue()
	{
		// Assert
		SchemaConstants.NullableTypePrefix.ShouldBe("System.Nullable`1");
	}

	[Fact]
	public void AspNetCoreHttpPrefix_Constant_HasCorrectValue()
	{
		// Assert
		SchemaConstants.AspNetCoreHttpPrefix.ShouldBe("Microsoft.AspNetCore.Http.");
	}

	[Theory]
	[InlineData(nameof(SchemaConstants.SystemString), "System.String")]
	[InlineData(nameof(SchemaConstants.SystemInt32), "System.Int32")]
	[InlineData(nameof(SchemaConstants.SystemInt64), "System.Int64")]
	[InlineData(nameof(SchemaConstants.SystemInt16), "System.Int16")]
	[InlineData(nameof(SchemaConstants.SystemByte), "System.Byte")]
	[InlineData(nameof(SchemaConstants.SystemDecimal), "System.Decimal")]
	[InlineData(nameof(SchemaConstants.SystemDouble), "System.Double")]
	[InlineData(nameof(SchemaConstants.SystemSingle), "System.Single")]
	[InlineData(nameof(SchemaConstants.SystemBoolean), "System.Boolean")]
	[InlineData(nameof(SchemaConstants.SystemDateTime), "System.DateTime")]
	[InlineData(nameof(SchemaConstants.SystemDateTimeOffset), "System.DateTimeOffset")]
	[InlineData(nameof(SchemaConstants.SystemDateOnly), "System.DateOnly")]
	[InlineData(nameof(SchemaConstants.SystemTimeOnly), "System.TimeOnly")]
	[InlineData(nameof(SchemaConstants.SystemGuid), "System.Guid")]
	[InlineData(nameof(SchemaConstants.SystemUri), "System.Uri")]
	public void TypeConstant_MatchesExpectedValue(string constantName, string expected)
	{
		// Act
		string? actual = constantName switch
		{
			nameof(SchemaConstants.SystemString) => SchemaConstants.SystemString,
			nameof(SchemaConstants.SystemInt32) => SchemaConstants.SystemInt32,
			nameof(SchemaConstants.SystemInt64) => SchemaConstants.SystemInt64,
			nameof(SchemaConstants.SystemInt16) => SchemaConstants.SystemInt16,
			nameof(SchemaConstants.SystemByte) => SchemaConstants.SystemByte,
			nameof(SchemaConstants.SystemDecimal) => SchemaConstants.SystemDecimal,
			nameof(SchemaConstants.SystemDouble) => SchemaConstants.SystemDouble,
			nameof(SchemaConstants.SystemSingle) => SchemaConstants.SystemSingle,
			nameof(SchemaConstants.SystemBoolean) => SchemaConstants.SystemBoolean,
			nameof(SchemaConstants.SystemDateTime) => SchemaConstants.SystemDateTime,
			nameof(SchemaConstants.SystemDateTimeOffset) => SchemaConstants.SystemDateTimeOffset,
			nameof(SchemaConstants.SystemDateOnly) => SchemaConstants.SystemDateOnly,
			nameof(SchemaConstants.SystemTimeOnly) => SchemaConstants.SystemTimeOnly,
			nameof(SchemaConstants.SystemGuid) => SchemaConstants.SystemGuid,
			nameof(SchemaConstants.SystemUri) => SchemaConstants.SystemUri,
			_ => null
		};

		// Assert
		actual.ShouldBe(expected);
	}

	[Theory]
	[InlineData(nameof(SchemaConstants.FormatInt32), "int32")]
	[InlineData(nameof(SchemaConstants.FormatInt64), "int64")]
	[InlineData(nameof(SchemaConstants.FormatFloat), "float")]
	[InlineData(nameof(SchemaConstants.FormatDouble), "double")]
	[InlineData(nameof(SchemaConstants.FormatDateTime), "date-time")]
	[InlineData(nameof(SchemaConstants.FormatDate), "date")]
	[InlineData(nameof(SchemaConstants.FormatTime), "time")]
	[InlineData(nameof(SchemaConstants.FormatUuid), "uuid")]
	[InlineData(nameof(SchemaConstants.FormatEmail), "email")]
	[InlineData(nameof(SchemaConstants.FormatUri), "uri")]
	[InlineData(nameof(SchemaConstants.FormatBinary), "binary")]
	public void FormatConstant_MatchesExpectedValue(string constantName, string expected)
	{
		// Act
		string? actual = constantName switch
		{
			nameof(SchemaConstants.FormatInt32) => SchemaConstants.FormatInt32,
			nameof(SchemaConstants.FormatInt64) => SchemaConstants.FormatInt64,
			nameof(SchemaConstants.FormatFloat) => SchemaConstants.FormatFloat,
			nameof(SchemaConstants.FormatDouble) => SchemaConstants.FormatDouble,
			nameof(SchemaConstants.FormatDateTime) => SchemaConstants.FormatDateTime,
			nameof(SchemaConstants.FormatDate) => SchemaConstants.FormatDate,
			nameof(SchemaConstants.FormatTime) => SchemaConstants.FormatTime,
			nameof(SchemaConstants.FormatUuid) => SchemaConstants.FormatUuid,
			nameof(SchemaConstants.FormatEmail) => SchemaConstants.FormatEmail,
			nameof(SchemaConstants.FormatUri) => SchemaConstants.FormatUri,
			nameof(SchemaConstants.FormatBinary) => SchemaConstants.FormatBinary,
			_ => null
		};

		// Assert
		actual.ShouldBe(expected);
	}

	[Fact]
	public void IFormFile_Constant_HasCorrectValue()
	{
		// Assert
		SchemaConstants.IFormFile.ShouldBe("Microsoft.AspNetCore.Http.IFormFile");
	}

	[Fact]
	public void IFormFileCollection_Constant_HasCorrectValue()
	{
		// Assert
		SchemaConstants.IFormFileCollection.ShouldBe("Microsoft.AspNetCore.Http.IFormFileCollection");
	}

	#endregion

	#region Edge Cases

	[Fact]
	public void IsSystemType_WithMixedCase_ReturnsFalse()
	{
		// Arrange
		string input = "SYSTEM.String";

		// Act
		bool result = SchemaConstants.IsSystemType(input);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void IsCollectionType_WithNestedGeneric_ReturnsTrue()
	{
		// Arrange
		string input = "System.Collections.Generic.List`1[System.Collections.Generic.List`1[System.String]]";

		// Act
		bool result = SchemaConstants.IsCollectionType(input);

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public void IsDictionaryType_WithNestedDictionary_ReturnsTrue()
	{
		// Arrange
		string input = "System.Collections.Generic.Dictionary`2[System.String,System.Collections.Generic.Dictionary`2[System.Int32,System.Object]]";

		// Act
		bool result = SchemaConstants.IsDictionaryType(input);

		// Assert
		result.ShouldBeTrue();
	}

	#endregion
}
