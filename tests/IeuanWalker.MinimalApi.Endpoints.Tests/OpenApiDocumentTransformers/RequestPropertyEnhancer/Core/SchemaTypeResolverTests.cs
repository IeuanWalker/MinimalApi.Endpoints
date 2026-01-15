using System.Reflection;
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
		first.ShouldBeSameAs(second);
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
		// Arrange
		Assembly systemAssembly = typeof(string).Assembly;

		// Act
		bool result = SchemaTypeResolver.ShouldInspectAssembly(systemAssembly);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void ShouldInspectAssembly_FiltersMicrosoftAssembly()
	{
		// Arrange
		Assembly microsoftAssembly = typeof(System.ComponentModel.DataAnnotations.RequiredAttribute).Assembly;

		// Act
		bool result = SchemaTypeResolver.ShouldInspectAssembly(microsoftAssembly);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void ShouldInspectAssembly_AllowsLocalAssembly()
	{
		// Arrange
		Assembly testAssembly = Assembly.GetExecutingAssembly();

		// Act
		bool result = SchemaTypeResolver.ShouldInspectAssembly(testAssembly);

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public void GetLoadableTypes_ReturnsTypesFromAssembly()
	{
		// Arrange
		Assembly asm = Assembly.GetExecutingAssembly();

		// Act
		IEnumerable<Type> result = SchemaTypeResolver.GetLoadableTypes(asm);

		// Assert
		result.ShouldContain(typeof(TestEnum));
	}

	[Fact]
	public void GetLoadableTypes_ReturnsEmptyForInvalidAssembly()
	{
		// Arrange
		Assembly asm = Assembly.GetExecutingAssembly();

		// Act
		IEnumerable<Type> result = SchemaTypeResolver.GetLoadableTypes(asm);

		// Assert
		result.ShouldNotBeNull();
		result.ShouldNotBeEmpty();
	}

	public enum TestEnum
	{
		ValueA,
		ValueB,
	}
}
