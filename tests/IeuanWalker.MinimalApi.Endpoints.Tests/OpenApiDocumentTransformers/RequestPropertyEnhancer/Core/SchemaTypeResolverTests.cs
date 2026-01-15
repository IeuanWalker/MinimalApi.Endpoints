using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer.Core;

namespace IeuanWalker.MinimalApi.Endpoints.Tests.OpenApiDocumentTransformers.RequestPropertyEnhancer.Core;

public class SchemaTypeResolverTests
{
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
	public void GetLoadableTypes_ReturnsTypesFromAssembly()
	{
		// Act
		IEnumerable<Type> result = SchemaTypeResolver.GetLoadableTypes(typeof(TestEnum).Assembly);

		// Assert
		result.ShouldContain(typeof(TestEnum));
	}

	[Fact]
	public void GetLoadableTypes_HandlesEmptyResults()
	{
		// Act
		IEnumerable<Type> result = SchemaTypeResolver.GetLoadableTypes(typeof(SchemaTypeResolverTests).Assembly);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldNotBeEmpty();
	}
}

public enum TestEnum
{
	ValueA,
	ValueB,
}
