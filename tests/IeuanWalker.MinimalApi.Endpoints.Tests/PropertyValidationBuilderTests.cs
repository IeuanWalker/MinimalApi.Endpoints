using System.Reflection;
using IeuanWalker.MinimalApi.Endpoints.Validation;

namespace IeuanWalker.MinimalApi.Endpoints.Tests;

public class PropertyValidationBuilderTests
{
	public class TestRequest
	{
		public string Name { get; set; } = string.Empty;
		public int Age { get; set; }
	}

	// Helper to check if operations are stored
	static int GetOperationsCount<TRequest, TProperty>(PropertyValidationBuilder<TRequest, TProperty> builder)
	{
		MethodInfo? method = builder.GetType().GetMethod("GetOperations", BindingFlags.Instance | BindingFlags.NonPublic);
		object? result = method?.Invoke(builder, null);
		if (result is System.Collections.ICollection collection)
		{
			return collection.Count;
		}
		return 0;
	}

	#region Alter Tests

	[Fact]
	public void Alter_StoresOperation()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> configBuilder = new();
		PropertyValidationBuilder<TestRequest, string> propertyBuilder = configBuilder.Property(x => x.Name);

		// Act
		PropertyValidationBuilder<TestRequest, string> result = propertyBuilder.Alter("Old message", "New message");

		// Assert
		result.ShouldBeSameAs(propertyBuilder); // Method chaining works
		GetOperationsCount(propertyBuilder).ShouldBe(1);
	}

	[Fact]
	public void Alter_WithNullOldRule_ThrowsArgumentNullException()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> configBuilder = new();
		PropertyValidationBuilder<TestRequest, string> propertyBuilder = configBuilder.Property(x => x.Name);

		// Act & Assert
		Should.Throw<ArgumentNullException>(() => propertyBuilder.Alter(null!, "New message"));
	}

	[Fact]
	public void Alter_WithWhitespaceOldRule_ThrowsArgumentException()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> configBuilder = new();
		PropertyValidationBuilder<TestRequest, string> propertyBuilder = configBuilder.Property(x => x.Name);

		// Act & Assert
		Should.Throw<ArgumentException>(() => propertyBuilder.Alter("   ", "New message"));
	}

	[Fact]
	public void Alter_WithNullNewRule_ThrowsArgumentNullException()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> configBuilder = new();
		PropertyValidationBuilder<TestRequest, string> propertyBuilder = configBuilder.Property(x => x.Name);

		// Act & Assert
		Should.Throw<ArgumentNullException>(() => propertyBuilder.Alter("Old message", null!));
	}

	[Fact]
	public void Alter_WithWhitespaceNewRule_ThrowsArgumentException()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> configBuilder = new();
		PropertyValidationBuilder<TestRequest, string> propertyBuilder = configBuilder.Property(x => x.Name);

		// Act & Assert
		Should.Throw<ArgumentException>(() => propertyBuilder.Alter("Old message", "   "));
	}

	#endregion

	#region Remove Tests

	[Fact]
	public void Remove_StoresOperation()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> configBuilder = new();
		PropertyValidationBuilder<TestRequest, string> propertyBuilder = configBuilder.Property(x => x.Name);

		// Act
		PropertyValidationBuilder<TestRequest, string> result = propertyBuilder.Remove("Message to remove");

		// Assert
		result.ShouldBeSameAs(propertyBuilder); // Method chaining works
		GetOperationsCount(propertyBuilder).ShouldBe(1);
	}

	[Fact]
	public void Remove_WithNullRule_ThrowsArgumentNullException()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> configBuilder = new();
		PropertyValidationBuilder<TestRequest, string> propertyBuilder = configBuilder.Property(x => x.Name);

		// Act & Assert
		Should.Throw<ArgumentNullException>(() => propertyBuilder.Remove(null!))
			.ParamName.ShouldBe("errorMessage");
	}

	[Fact]
	public void Remove_WithWhitespaceRule_ThrowsArgumentException()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> configBuilder = new();
		PropertyValidationBuilder<TestRequest, string> propertyBuilder = configBuilder.Property(x => x.Name);

		// Act & Assert
		Should.Throw<ArgumentException>(() => propertyBuilder.Remove("   "));
	}

	#endregion

	#region RemoveAll Tests

	[Fact]
	public void RemoveAll_StoresOperation()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> configBuilder = new();
		PropertyValidationBuilder<TestRequest, string> propertyBuilder = configBuilder.Property(x => x.Name);

		// Act
		PropertyValidationBuilder<TestRequest, string> result = propertyBuilder.RemoveAll();

		// Assert
		result.ShouldBeSameAs(propertyBuilder); // Method chaining works
		GetOperationsCount(propertyBuilder).ShouldBe(1);
	}

	#endregion

	#region Integration Tests with Multiple Operations

	[Fact]
	public void ChainedOperations_StoresMultipleOperationsInOrder()
	{
		// Arrange
		ValidationConfigurationBuilder<TestRequest> configBuilder = new();
		PropertyValidationBuilder<TestRequest, string> propertyBuilder = configBuilder.Property(x => x.Name);

		// Act
		propertyBuilder
			.Alter("Message 1", "Modified 1")
			.Remove("Message 2")
			.Alter("Message 3", "Modified 3")
			.RemoveAll();

		// Assert
		GetOperationsCount(propertyBuilder).ShouldBe(4);
	}

	#endregion
}
