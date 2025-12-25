using System.Net;
using System.Net.Http.Json;
using ExampleApi.Data;
using Microsoft.Extensions.DependencyInjection;
using PostTodoFluentValidation = ExampleApi.Endpoints.Todos.PostFluentValidation;

namespace ExampleApi.IntegrationTests.Endpoints.Todos;

/// <summary>
/// Integration tests for PostTodo endpoint with FluentValidation
/// </summary>
public class PostTodoFluentValidationTests : IClassFixture<ExampleApiWebApplicationFactory>
{
	readonly ExampleApiWebApplicationFactory _factory;
	readonly HttpClient _client;

	public PostTodoFluentValidationTests(ExampleApiWebApplicationFactory factory)
	{
		_factory = factory;
		_client = _factory.CreateClient();
	}

	[Fact]
	public async Task PostTodoFluentValidation_WithValidData_CreatesSuccessfully()
	{
		// Arrange
		TestTodoStore? todoStore = _factory.Services.GetRequiredService<ITodoStore>() as TestTodoStore;
		todoStore!.Clear();

		PostTodoFluentValidation.RequestModel request = new()
		{
			// String validation examples
			Title = "Valid Todo Title",
			Description = "Valid Description",
			Email = "test@example.com",
			Pattern = "Hello",
			LengthRange = "12345",
			CreditCard = "4111111111111111",
			Url = "https://example.com",
			
			// Numeric validation examples
			IntGreaterThan = 5,
			IntLessThan = 50,
			IntRange = 25,
			DecimalGreaterThanOrEqual = 10.5m,
			DecimalLessThanOrEqual = 500.0m,
			DoubleInclusiveBetween = 50.0,
			DoubleExclusiveBetween = 50.0,
			PrecisionScale = 12345678.12m,
			
			// Comparison validation examples
			Equal = 42,
			NotEqual = 10,
			
			// Boolean validation
			IsCompleted = false,
			IsTest = true,
			
			// Complex type validation
			NestedObject = new PostTodoFluentValidation.NestedObjectModel
			{
				Name = "Valid Name",
				Age = 25
			},
			NestedObject2 = [],  // Empty list is allowed
			
			// Null/empty validation
			NullableString = "NotNull",
			NonEmptyString = "NotEmpty"
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos/FluentValidation", request);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		PostTodoFluentValidation.ResponseModel? createdTodo = await response.Content.ReadFromJsonAsync<PostTodoFluentValidation.ResponseModel>();
		createdTodo.ShouldNotBeNull();
		createdTodo.Title.ShouldBe("Valid Todo Title");
	}

	[Fact]
	public async Task PostTodoFluentValidation_WithEmptyTitle_ReturnsBadRequest()
	{
		// Arrange
		PostTodoFluentValidation.RequestModel request = new()
		{
			Title = "", // Invalid - empty title
			Description = "Test Description",
			Email = "test@example.com",
			Pattern = "Hello",
			LengthRange = "12345",
			CreditCard = "4111111111111111",
			Url = "https://example.com",
			IntGreaterThan = 5,
			IntLessThan = 50,
			IntRange = 25,
			DecimalGreaterThanOrEqual = 10.5m,
			DecimalLessThanOrEqual = 500.0m,
			DoubleInclusiveBetween = 50.0,
			DoubleExclusiveBetween = 50.0,
			PrecisionScale = 12345678.12m,
			Equal = 42,
			NotEqual = 10,
			NestedObject = new PostTodoFluentValidation.NestedObjectModel
			{
				Name = "Valid Name",
				Age = 25
			},
			NestedObject2 = [],
			NullableString = "NotNull",
			NonEmptyString = "NotEmpty"
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos/FluentValidation", request);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

		string content = await response.Content.ReadAsStringAsync();
		content.ShouldContain("title", Case.Insensitive);
	}

	[Fact]
	public async Task PostTodoFluentValidation_WithNullTitle_ReturnsBadRequest()
	{
		// Arrange
		PostTodoFluentValidation.RequestModel request = CreateValidRequest();
		request.Title = null!; // Invalid - null title

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos/FluentValidation", request);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task PostTodoFluentValidation_WithInvalidNestedObject_ReturnsBadRequest()
	{
		// Arrange
		TestTodoStore? todoStore = _factory.Services.GetRequiredService<ITodoStore>() as TestTodoStore;
		todoStore!.Clear();

		PostTodoFluentValidation.RequestModel request = CreateValidRequest();
		request.NestedObject = new PostTodoFluentValidation.NestedObjectModel
		{
			Name = "", // Invalid - empty name
			Age = 25
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos/FluentValidation", request);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

		string content = await response.Content.ReadAsStringAsync();
		content.ShouldContain("name", Case.Insensitive);
	}

	[Fact]
	public async Task PostTodoFluentValidation_WithInvalidAge_ReturnsBadRequest()
	{
		// Arrange
		TestTodoStore? todoStore = _factory.Services.GetRequiredService<ITodoStore>() as TestTodoStore;
		todoStore!.Clear();

		PostTodoFluentValidation.RequestModel request = CreateValidRequest();
		request.NestedObject = new PostTodoFluentValidation.NestedObjectModel
		{
			Name = "Valid Name",
			Age = -5 // Invalid - negative age
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos/FluentValidation", request);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

		string content = await response.Content.ReadAsStringAsync();
		content.ShouldContain("age", Case.Insensitive);
	}

	[Fact]
	public async Task PostTodoFluentValidation_WithAgeOverLimit_ReturnsBadRequest()
	{
		// Arrange
		TestTodoStore? todoStore = _factory.Services.GetRequiredService<ITodoStore>() as TestTodoStore;
		todoStore!.Clear();

		PostTodoFluentValidation.RequestModel request = new()
		{
			Title = "Valid Title",
			Description = "Valid Description",
			NestedObject = new PostTodoFluentValidation.NestedObjectModel
			{
				Name = "Valid Name",
				Age = 85 // Invalid - over limit (should be less than 80)
			},
			NestedObject2 = []
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos/FluentValidation", request);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

		string content = await response.Content.ReadAsStringAsync();
		content.ShouldContain("age", Case.Insensitive);
	}

	[Fact]
	public async Task PostTodoFluentValidation_WithInvalidNestedList_ReturnsBadRequest()
	{
		// Arrange
		TestTodoStore? todoStore = _factory.Services.GetRequiredService<ITodoStore>() as TestTodoStore;
		todoStore!.Clear();

		PostTodoFluentValidation.RequestModel request = CreateValidRequest();
		request.NestedObject2 =
		[
			new PostTodoFluentValidation.NestedObject2Model { Note = "Valid note" },
			new PostTodoFluentValidation.NestedObject2Model { Note = "" } // Invalid - empty note
		];

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos/FluentValidation", request);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

		string content = await response.Content.ReadAsStringAsync();
		content.ShouldContain("note", Case.Insensitive);
	}

	[Fact]
	public async Task PostTodoFluentValidation_WithMultipleValidationErrors_ReturnsAllErrors()
	{
		// Arrange
		TestTodoStore? todoStore = _factory.Services.GetRequiredService<ITodoStore>() as TestTodoStore;
		todoStore!.Clear();

		PostTodoFluentValidation.RequestModel request = CreateValidRequest();
		request.Title = ""; // Invalid - empty title
		request.Description = ""; // Invalid - empty description
		request.NestedObject = new PostTodoFluentValidation.NestedObjectModel
		{
			Name = "", // Invalid - empty name
			Age = -1 // Invalid - negative age
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos/FluentValidation", request);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

		string content = await response.Content.ReadAsStringAsync();

		// Should contain multiple validation errors
		content.ShouldContain("title", Case.Insensitive);
		content.ShouldContain("description", Case.Insensitive);
		content.ShouldContain("name", Case.Insensitive);
		content.ShouldContain("age", Case.Insensitive);
	}

	[Fact]
	public async Task PostTodoFluentValidation_WithTitleTooLong_ReturnsBadRequest()
	{
		// Arrange
		TestTodoStore? todoStore = _factory.Services.GetRequiredService<ITodoStore>() as TestTodoStore;
		todoStore!.Clear();

		string tooLongTitle = new('X', 201); // Over the 200 character limit

		PostTodoFluentValidation.RequestModel request = CreateValidRequest();
		request.Title = tooLongTitle;

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos/FluentValidation", request);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

		string content = await response.Content.ReadAsStringAsync();
		content.ShouldContain("title", Case.Insensitive);
	}

	[Fact]
	public async Task PostTodoFluentValidation_WithDescriptionTooLong_ReturnsBadRequest()
	{
		// Arrange
		TestTodoStore? todoStore = _factory.Services.GetRequiredService<ITodoStore>() as TestTodoStore;
		todoStore!.Clear();

		string tooLongDescription = new('X', 1001); // Over the 1000 character limit

		PostTodoFluentValidation.RequestModel request = CreateValidRequest();
		request.Description = tooLongDescription;

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos/FluentValidation", request);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

		string content = await response.Content.ReadAsStringAsync();
		content.ShouldContain("description", Case.Insensitive);
	}

	[Fact]
	public async Task PostTodoFluentValidation_WithNullNestedObject_ReturnsBadRequest()
	{
		// Arrange
		TestTodoStore? todoStore = _factory.Services.GetRequiredService<ITodoStore>() as TestTodoStore;
		todoStore!.Clear();

		PostTodoFluentValidation.RequestModel request = CreateValidRequest();
		request.NestedObject = null!; // Invalid - null nested object

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos/FluentValidation", request);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

		string content = await response.Content.ReadAsStringAsync();
		content.ShouldContain("NestedObject", Case.Insensitive);
	}

	[Fact]
	public async Task PostTodoFluentValidation_WithValidNestedListItems_CreatesSuccessfully()
	{
		// Arrange
		TestTodoStore? todoStore = _factory.Services.GetRequiredService<ITodoStore>() as TestTodoStore;
		todoStore!.Clear();

		PostTodoFluentValidation.RequestModel request = CreateValidRequest();
		request.NestedObject2 =
		[
			new PostTodoFluentValidation.NestedObject2Model { Note = "First note" },
			new PostTodoFluentValidation.NestedObject2Model { Note = "Second note" },
			new PostTodoFluentValidation.NestedObject2Model { Note = "Third note" }
		];

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos/FluentValidation", request);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		PostTodoFluentValidation.ResponseModel? createdTodo = await response.Content.ReadFromJsonAsync<PostTodoFluentValidation.ResponseModel>();
		createdTodo.ShouldNotBeNull();
		createdTodo.Title.ShouldBe("Valid Todo Title");
	}
	
	// Helper method to create a valid request with all required fields
	static PostTodoFluentValidation.RequestModel CreateValidRequest()
	{
		return new PostTodoFluentValidation.RequestModel
		{
			// String validation examples
			Title = "Valid Todo Title",
			Description = "Valid Description",
			Email = "test@example.com",
			Pattern = "Hello",
			LengthRange = "12345",
			CreditCard = "4111111111111111",
			Url = "https://example.com",
			
			// Numeric validation examples
			IntGreaterThan = 5,
			IntLessThan = 50,
			IntRange = 25,
			DecimalGreaterThanOrEqual = 10.5m,
			DecimalLessThanOrEqual = 500.0m,
			DoubleInclusiveBetween = 50.0,
			DoubleExclusiveBetween = 50.0,
			PrecisionScale = 12345678.12m,
			
			// Comparison validation examples
			Equal = 42,
			NotEqual = 10,
			
			// Boolean validation
			IsCompleted = false,
			IsTest = true,
			
			// Complex type validation
			NestedObject = new PostTodoFluentValidation.NestedObjectModel
			{
				Name = "Valid Name",
				Age = 25
			},
			NestedObject2 = [],
			
			// Null/empty validation
			NullableString = "NotNull",
			NonEmptyString = "NotEmpty"
		};
	}
}
