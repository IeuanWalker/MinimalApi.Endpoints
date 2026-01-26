using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace IeuanWalker.MinimalApi.Endpoints.Tests;

public class ValidationErrorsTests
{
	[Fact]
	public void HasErrors_WhenEmpty_ReturnsFalse()
	{
		// Arrange
		ValidationErrors<TestModel> errors = new();

		// Act
		bool result = errors.HasErrors();

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void HasErrors_WhenErrorsAdded_ReturnsTrue()
	{
		// Arrange
		ValidationErrors<TestModel> errors = new();

		// Act
		errors.Add("Name", "Name is required");

		// Assert
		errors.HasErrors().ShouldBeTrue();
	}

	[Fact]
	public void Add_WithStringKey_AppendsMultipleMessages()
	{
		// Arrange
		ValidationErrors<TestModel> errors = new();

		// Act
		errors.Add("Name", "Name is required", "Name must be at least 3 characters");

		// Assert
		HttpValidationProblemDetails problemDetails = errors.ToProblemDetails();
		problemDetails.Errors["Name"].ShouldBe(["Name is required", "Name must be at least 3 characters"]);
	}

	[Fact]
	public void Add_WithExpression_UsesPropertyPath()
	{
		// Arrange
		ValidationErrors<TestModel> errors = new();

		// Act
		errors.Add(x => x.Name, "Name is required");

		// Assert
		HttpValidationProblemDetails problemDetails = errors.ToProblemDetails();
		problemDetails.Errors.ShouldContainKey("Name");
	}

	[Fact]
	public void Add_WithNestedExpression_UsesPropertyPath()
	{
		// Arrange
		ValidationErrors<TestModel> errors = new();

		// Act
		errors.Add(x => x.Nested.Description, "Description is required");

		// Assert
		HttpValidationProblemDetails problemDetails = errors.ToProblemDetails();
		problemDetails.Errors.ShouldContainKey("Nested.Description");
	}

	[Fact]
	public void Add_WithUnaryExpression_UsesPropertyPath()
	{
		// Arrange
		ValidationErrors<TestModel> errors = new();

		// Act
		errors.Add(x => x.Number, "Number is required");

		// Assert
		HttpValidationProblemDetails problemDetails = errors.ToProblemDetails();
		problemDetails.Errors.ShouldContainKey("Number");
	}

	[Fact]
	public void Add_WithIndexerExpression_UsesPropertyPath()
	{
		// Arrange
		ValidationErrors<TestModel> errors = new();

		// Act
		errors.Add(x => x.Map["key"], "Map entry is invalid");

		// Assert
		HttpValidationProblemDetails problemDetails = errors.ToProblemDetails();
		problemDetails.Errors.ShouldContainKey("Map[\"key\"]");
	}

	[Fact]
	public void Add_WithNestedIndexerExpression_UsesPropertyPath()
	{
		// Arrange
		ValidationErrors<TestModel> errors = new();

		// Act
		errors.Add(x => x.Nested.Map["key"], "Nested map entry is invalid");

		// Assert
		HttpValidationProblemDetails problemDetails = errors.ToProblemDetails();
		problemDetails.Errors.ShouldContainKey("Nested.Map[\"key\"]");
	}

	[Fact]
	public void Add_WithNestedObjectProperty_UsesPropertyPath()
	{
		// Arrange
		ValidationErrors<TestModel> errors = new();

		// Act
		errors.Add(x => x.Nested.Detail.Name, "Nested detail name is required");

		// Assert
		HttpValidationProblemDetails problemDetails = errors.ToProblemDetails();
		problemDetails.Errors.ShouldContainKey("Nested.Detail.Name");
	}

	[Fact]
	public void Add_WithListOfObjectsProperty_UsesPropertyPath()
	{
		// Arrange
		ValidationErrors<TestModel> errors = new();

		// Act
		errors.Add(x => x.Items[0].Name, "First item name is required");

		// Assert
		HttpValidationProblemDetails problemDetails = errors.ToProblemDetails();
		problemDetails.Errors.ShouldContainKey("Items[0].Name");
	}

	[Fact]
	public void Add_WithListOfObjectsThirdItem_UsesPropertyPath()
	{
		// Arrange
		ValidationErrors<TestModel> errors = new();

		// Act
		errors.Add(x => x.Items[2].Name, "Third item name is required");

		// Assert
		HttpValidationProblemDetails problemDetails = errors.ToProblemDetails();
		problemDetails.Errors.ShouldContainKey("Items[2].Name");
	}

	[Fact]
	public void Add_WithInvalidExpression_ThrowsArgumentException()
	{
		// Arrange
		ValidationErrors<TestModel> errors = new();

		// Act & Assert
		Should.Throw<ArgumentException>(() => errors.Add(x => new { x.Name }, "Invalid"));
	}

	[Fact]
	public void Add_WithNoMessages_ThrowsArgumentException()
	{
		// Arrange
		ValidationErrors<TestModel> errors = new();

		// Act & Assert
		ArgumentException ex = Should.Throw<ArgumentException>(() => errors.Add("Name"));
		ex.ParamName.ShouldBe("messages");
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void Add_WithInvalidKey_ThrowsArgumentException(string? key)
	{
		// Arrange
		ValidationErrors<TestModel> errors = new();

		// Act & Assert
		ArgumentException ex = Should.Throw<ArgumentException>(() => errors.Add(key!, "Name is required"));
		ex.ParamName.ShouldBe("key");
	}

	[Fact]
	public void Add_WithExpressionNoMessages_ThrowsArgumentException()
	{
		// Arrange
		ValidationErrors<TestModel> errors = new();

		// Act & Assert
		ArgumentException ex = Should.Throw<ArgumentException>(() => errors.Add(x => x.Name));
		ex.ParamName.ShouldBe("messages");
	}

	[Fact]
	public void ToProblemDetails_ReturnsAllErrors()
	{
		// Arrange
		ValidationErrors<TestModel> errors = new();

		// Act
		errors
			.Add("Name", "Name is required")
			.Add("Nested.Description", "Description is required");

		// Assert
		HttpValidationProblemDetails problemDetails = errors.ToProblemDetails();
		problemDetails.Errors.Count.ShouldBe(2);
		problemDetails.Errors["Name"].ShouldBe(["Name is required"]);
		problemDetails.Errors["Nested.Description"].ShouldBe(["Description is required"]);
	}

	[Fact]
	public void ToProblemResponse_ReturnsProblemResult()
	{
		// Arrange
		ValidationErrors<TestModel> errors = new();
		errors.Add("Name", "Name is required");

		// Act
		ProblemHttpResult result = errors.ToProblemResponse();

		// Assert
		result.ShouldNotBeNull();
		result.ProblemDetails.ShouldBeOfType<HttpValidationProblemDetails>();
	}

	public class TestModel
	{
		public string Name { get; set; } = string.Empty;
		public int Number { get; set; }
		public NestedModel Nested { get; set; } = new();
		public Dictionary<string, string> Map { get; set; } = new();
		public List<ItemModel> Items { get; set; } = [];
	}

	public class NestedModel
	{
		public string Description { get; set; } = string.Empty;
		public Dictionary<string, string> Map { get; set; } = new();
		public DetailModel Detail { get; set; } = new();
	}

	public class DetailModel
	{
		public string Name { get; set; } = string.Empty;
	}

	public class ItemModel
	{
		public string Name { get; set; } = string.Empty;
	}
}
