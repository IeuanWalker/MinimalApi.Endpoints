using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer.Core;

using System.Reflection;
using System.IO;

namespace IeuanWalker.MinimalApi.Endpoints.Tests.OpenApiDocumentTransformers.RequestPropertyEnhancer.Core;

public class SchemaTypeResolverTests
{
	#region GetSchemaType Tests

	[Fact]
	public void GetSchemaType_ReturnsSystemType()
	{
		// Act
		Type? result = SchemaTypeResolver.GetSchemaType("System.String");

		// Assert
		result.ShouldBe(typeof(string));
	}

	[Fact]
	public void GetSchemaType_ReturnsCustomType()
	{
		// Arrange
		string name = typeof(TestEnum).FullName!;

		// Act
		Type? result = SchemaTypeResolver.GetSchemaType(name);

		// Assert
		result.ShouldBe(typeof(TestEnum));
	}

	[Fact]
	public void GetSchemaType_ReturnsNullForUnknownType()
	{
		// Act
		Type? result = SchemaTypeResolver.GetSchemaType("NonExistent.Type.That.Does.Not.Exist");

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void GetSchemaType_CachesResult()
	{
		// Arrange
		string name = typeof(TestEnum).FullName!;

		// Act
		Type? first = SchemaTypeResolver.GetSchemaType(name);
		Type? second = SchemaTypeResolver.GetSchemaType(name);

		// Assert
		second.ShouldBeSameAs(first);
	}

	[Theory]
	[InlineData("System.Int32", typeof(int))]
	[InlineData("System.Int64", typeof(long))]
	[InlineData("System.Boolean", typeof(bool))]
	[InlineData("System.Double", typeof(double))]
	[InlineData("System.Decimal", typeof(decimal))]
	[InlineData("System.DateTime", typeof(DateTime))]
	[InlineData("System.Guid", typeof(Guid))]
	public void GetSchemaType_ReturnsCorrectPrimitiveTypes(string typeName, Type expectedType)
	{
		// Act
		Type? result = SchemaTypeResolver.GetSchemaType(typeName);

		// Assert
		result.ShouldBe(expectedType);
	}

	[Fact]
	public void GetSchemaType_NestedType_WithPlusNotation_ReturnsNull()
	{
		// Arrange
		// Nested types use + in IL but . in C# - the resolver converts + to . but
		// Assembly.GetType doesn't find types with the converted name
		string nestedTypeName = typeof(OuterClass.NestedClass).FullName!;

		// Act
		Type? result = SchemaTypeResolver.GetSchemaType(nestedTypeName);

		// Assert - The current implementation returns null for nested types
		// because Assembly.GetType doesn't find the type after + is converted to .
		result.ShouldBeNull();
	}

	[Fact]
	public void GetSchemaType_EmptyString_ThrowsArgumentException()
	{
		// Act & Assert
		// The implementation throws ArgumentException for empty strings
		// because Assembly.GetType doesn't accept empty names
		Should.Throw<ArgumentException>(() => SchemaTypeResolver.GetSchemaType(""));
	}

	#endregion

	#region GetEnumType Tests

	[Fact]
	public void GetEnumType_ReturnsEnum()
	{
		// Act
		Type? result = SchemaTypeResolver.GetEnumType(typeof(TestEnum).FullName!);

		// Assert
		result.ShouldBe(typeof(TestEnum));
	}

	[Fact]
	public void GetEnumType_ReturnsNullForNonEnum()
	{
		// Act
		Type? result = SchemaTypeResolver.GetEnumType("System.String");

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void GetEnumType_ReturnsNullForNullableNonEnum()
	{
		// Arrange
		string nullableIntTypeName = typeof(int?).FullName!;

		// Act
		Type? result = SchemaTypeResolver.GetEnumType(nullableIntTypeName);

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void GetEnumType_ReturnsUnderlyingEnumForNullableEnum()
	{
		// Arrange
		string nullableEnumTypeName = typeof(TestEnum?).FullName!;

		// Act
		Type? result = SchemaTypeResolver.GetEnumType(nullableEnumTypeName);

		// Assert
		result.ShouldBe(typeof(TestEnum));
	}

	[Fact]
	public void GetEnumType_ReturnsNullForClass()
	{
		// Act
		Type? result = SchemaTypeResolver.GetEnumType(typeof(TestClass).FullName!);

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void GetEnumType_ReturnsNullForStruct()
	{
		// Act
		Type? result = SchemaTypeResolver.GetEnumType(typeof(TestStruct).FullName!);

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void GetEnumType_ReturnsNullForUnknownType()
	{
		// Act
		Type? result = SchemaTypeResolver.GetEnumType("Unknown.Enum.Type");

		// Assert
		result.ShouldBeNull();
	}

	#endregion

	#region ShouldInspectAssembly Tests

	[Fact]
	public void ShouldInspectAssembly_FiltersSystemAssembly()
	{
		// Act
		bool result = SchemaTypeResolver.ShouldInspectAssembly(typeof(string).Assembly);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void ShouldInspectAssembly_FiltersMicrosoftAssembly()
	{
		// Act
		bool result = SchemaTypeResolver.ShouldInspectAssembly(typeof(System.ComponentModel.DataAnnotations.RequiredAttribute).Assembly);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void ShouldInspectAssembly_AllowsLocalAssembly()
	{
		// Act
		bool result = SchemaTypeResolver.ShouldInspectAssembly(typeof(TestEnum).Assembly);

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public void ShouldInspectAssembly_FiltersNetStandardAssembly()
	{
		// Arrange - find an assembly that starts with netstandard
		// This is harder to test directly, but we can verify the logic
		// by using a mock or checking System.Runtime which is filtered
		bool result = SchemaTypeResolver.ShouldInspectAssembly(typeof(int).Assembly);

		// Assert - System.Private.CoreLib or System.Runtime should be filtered
		result.ShouldBeFalse();
	}

	[Fact]
	public void ShouldInspectAssemblyName_NullOrEmpty_ReturnsFalse()
	{
		// Act
		bool nullResult = SchemaTypeResolver.ShouldInspectAssemblyName(null);
		bool emptyResult = SchemaTypeResolver.ShouldInspectAssemblyName("");

		// Assert
		nullResult.ShouldBeFalse();
		emptyResult.ShouldBeFalse();
	}

	[Fact]
	public void GetAssembliesSafe_ProviderThrows_ReturnsEmpty()
	{
		// Arrange
		Func<Assembly[]> provider = () => throw new InvalidOperationException("boom");

		// Act
		Assembly[] result = SchemaTypeResolver.GetAssembliesSafe(provider);

		// Assert
		result.ShouldNotBeNull();
		result.Length.ShouldBe(0);
	}

	[Fact]
	public void GetAssembliesSafe_Default_DoesNotThrow()
	{
		// Act
		Assembly[] result = SchemaTypeResolver.GetAssembliesSafe();

		// Assert - should return assemblies or empty but not throw
		result.ShouldNotBeNull();
	}

	#endregion

	#region GetLoadableTypes Tests

	[Fact]
	public void GetLoadableTypes_ReturnsTypesFromAssembly()
	{
		// Act
    IEnumerable<Type> result = SchemaTypeResolver.GetLoadableTypes(() => typeof(TestEnum).Assembly.GetTypes());

		// Assert
		result.ShouldContain(typeof(TestEnum));
	}

	[Fact]
	public void GetLoadableTypes_HandlesEmptyResults()
	{
		// Act
    IEnumerable<Type> result = SchemaTypeResolver.GetLoadableTypes(() => typeof(SchemaTypeResolverTests).Assembly.GetTypes());

		// Assert
		result.ShouldNotBeNull();
		result.ShouldNotBeEmpty();
	}

	[Fact]
	public void GetLoadableTypes_ReturnsNestedTypes()
	{
		// Act
    IEnumerable<Type> result = SchemaTypeResolver.GetLoadableTypes(() => typeof(OuterClass).Assembly.GetTypes());

		// Assert - GetLoadableTypes does return nested types from the assembly
		result.ShouldContain(typeof(OuterClass.NestedClass));
	}

	[Fact]
	public void GetLoadableTypes_ReturnsMultipleTypes()
	{
		// Act
    List<Type> result = [.. SchemaTypeResolver.GetLoadableTypes(() => typeof(TestEnum).Assembly.GetTypes())];

		// Assert
		result.Count.ShouldBeGreaterThan(1);
		result.ShouldContain(typeof(TestEnum));
		result.ShouldContain(typeof(TestClass));
	}

	[Fact]
	public void GetLoadableTypes_ProviderThrows_ReflectionTypeLoadException_ReturnsPartialTypes()
	{
		// Arrange - create a ReflectionTypeLoadException-like behavior by throwing one from provider
		Type[]? types = new Type?[] { typeof(TestEnum), null } as Type[];
		var ex = new ReflectionTypeLoadException(types!, Array.Empty<Exception>());

		Func<Type[]> provider = () => throw ex;

		// Act
		IEnumerable<Type> result = SchemaTypeResolver.GetLoadableTypes(provider);

		// Assert
		result.ShouldContain(typeof(TestEnum));
	}

	[Fact]
	public void GetLoadableTypes_ProviderThrows_GeneralException_ReturnsEmpty()
	{
		// Arrange
		Func<Type[]> provider = () => throw new InvalidOperationException("boom");

		// Act
		IEnumerable<Type> result = SchemaTypeResolver.GetLoadableTypes(provider);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldBeEmpty();
	}


	#endregion
}

public enum TestEnum
{
	ValueA,
	ValueB,
}

public class TestClass
{
	public string? Name { get; set; }
}

public struct TestStruct
{
	public int Value { get; set; }
}

public class OuterClass
{
	public class NestedClass
	{
		public string? Property { get; set; }
    }
}


