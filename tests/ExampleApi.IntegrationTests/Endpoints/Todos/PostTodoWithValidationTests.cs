using System.Net;
using System.Net.Http.Json;
using ExampleApi.Data;
using Microsoft.Extensions.DependencyInjection;
using PostTodoWithValidation = ExampleApi.Endpoints.Todos.PostWithValidation;

namespace ExampleApi.IntegrationTests.Endpoints.Todos;

/// <summary>
/// Integration tests for PostTodo endpoint with WithValidation extension method
/// </summary>
public class PostTodoWithValidationTests : IClassFixture<ExampleApiWebApplicationFactory>
{
	readonly ExampleApiWebApplicationFactory _factory;
	readonly HttpClient _client;

	public PostTodoWithValidationTests(ExampleApiWebApplicationFactory factory)
	{
		_factory = factory;
		_client = _factory.CreateClient();
	}

	[Fact]
	public async Task PostTodoWithValidation_WithValidData_CreatesSuccessfully()
	{
		// Arrange
		TestTodoStore? todoStore = _factory.Services.GetRequiredService<ITodoStore>() as TestTodoStore;
		todoStore!.Clear();

		PostTodoWithValidation.RequestModel request = new()
		{
			Title = "Valid Todo Title",
			Description = "Valid Description",
			Email = "test@example.com",
			Priority = 5,
			DueDate = DateTime.Now.AddDays(7),
			IsCompleted = false
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos/WithValidation", request);

		// Assert - Debug output
		if (response.StatusCode != HttpStatusCode.OK)
		{
			string content = await response.Content.ReadAsStringAsync();
			Console.WriteLine($"Unexpected status: {response.StatusCode}");
			Console.WriteLine($"Response content: {content}");
		}

		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		PostTodoWithValidation.ResponseModel? createdTodo = await response.Content.ReadFromJsonAsync<PostTodoWithValidation.ResponseModel>();
		createdTodo.ShouldNotBeNull();
		createdTodo.Title.ShouldBe("Valid Todo Title");
	}

	[Fact]
	public async Task PostTodoWithValidation_WithEmptyTitle_ReturnsBadRequest()
	{
		// Arrange
		PostTodoWithValidation.RequestModel request = new()
		{
			Title = "", // Invalid - empty title
			Description = "Test Description",
			Email = "test@example.com",
			Priority = 5,
			DueDate = DateTime.Now.AddDays(7)
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos/WithValidation", request);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
		string content = await response.Content.ReadAsStringAsync();
		content.ShouldContain("Title", Case.Insensitive);
	}

	[Fact]
	public async Task PostTodoWithValidation_WithTitleTooLong_ReturnsBadRequest()
	{
		// Arrange
		PostTodoWithValidation.RequestModel request = new()
		{
			Title = new string('x', 201), // Invalid - exceeds 200 chars
			Description = "Test Description",
			Email = "test@example.com",
			Priority = 5
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos/WithValidation", request);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
		string content = await response.Content.ReadAsStringAsync();
		content.ShouldContain("Title", Case.Insensitive);
	}

	[Fact]
	public async Task PostTodoWithValidation_WithInvalidEmail_ReturnsBadRequest()
	{
		// Arrange
		PostTodoWithValidation.RequestModel request = new()
		{
			Title = "Valid Title",
			Description = "Test Description",
			Email = "not-an-email", // Invalid email format
			Priority = 5
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos/WithValidation", request);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
		string content = await response.Content.ReadAsStringAsync();
		content.ShouldContain("Email", Case.Insensitive);
	}

	[Fact]
	public async Task PostTodoWithValidation_WithPriorityTooHigh_ReturnsBadRequest()
	{
		// Arrange
		PostTodoWithValidation.RequestModel request = new()
		{
			Title = "Valid Title",
			Description = "Test Description",
			Email = "test@example.com",
			Priority = 11 // Invalid - exceeds maximum of 10
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos/WithValidation", request);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
		string content = await response.Content.ReadAsStringAsync();
		content.ShouldContain("Priority", Case.Insensitive);
	}

	[Fact]
	public async Task PostTodoWithValidation_WithPriorityNegative_ReturnsBadRequest()
	{
		// Arrange
		PostTodoWithValidation.RequestModel request = new()
		{
			Title = "Valid Title",
			Description = "Test Description",
			Email = "test@example.com",
			Priority = -1 // Invalid - below minimum of 0
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos/WithValidation", request);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
		string content = await response.Content.ReadAsStringAsync();
		content.ShouldContain("Priority", Case.Insensitive);
	}

	[Fact]
	public async Task PostTodoWithValidation_WithPastDueDate_ReturnsBadRequest()
	{
		// Arrange
		PostTodoWithValidation.RequestModel request = new()
		{
			Title = "Valid Title",
			Description = "Test Description",
			Email = "test@example.com",
			Priority = 5,
			DueDate = DateTime.Now.AddDays(-1) // Invalid - in the past
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos/WithValidation", request);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
		string content = await response.Content.ReadAsStringAsync();
		content.ShouldContain("Due date must be in the future", Case.Insensitive);
	}

	[Fact]
	public async Task PostTodoWithValidation_HighPriorityWithoutDueDate_ReturnsBadRequest()
	{
		// Arrange
		PostTodoWithValidation.RequestModel request = new()
		{
			Title = "Valid Title",
			Description = "Test Description",
			Email = "test@example.com",
			Priority = 9, // High priority (>= 8)
			DueDate = null // Invalid - high priority items need a due date
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos/WithValidation", request);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
		string content = await response.Content.ReadAsStringAsync();
		content.ShouldContain("High priority items", Case.Insensitive);
		content.ShouldContain("due date", Case.Insensitive);
	}

	[Fact]
	public async Task PostTodoWithValidation_HighPriorityWithDueDate_CreatesSuccessfully()
	{
		// Arrange
		TestTodoStore? todoStore = _factory.Services.GetRequiredService<ITodoStore>() as TestTodoStore;
		todoStore!.Clear();

		PostTodoWithValidation.RequestModel request = new()
		{
			Title = "High Priority Todo",
			Description = "Urgent task",
			Email = "test@example.com",
			Priority = 9, // High priority (>= 8)
			DueDate = DateTime.Now.AddDays(1), // Valid - has due date
			IsCompleted = false
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos/WithValidation", request);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		PostTodoWithValidation.ResponseModel? createdTodo = await response.Content.ReadFromJsonAsync<PostTodoWithValidation.ResponseModel>();
		createdTodo.ShouldNotBeNull();
		createdTodo.Title.ShouldBe("High Priority Todo");
	}

	[Fact]
	public async Task PostTodoWithValidation_WithMultipleValidationErrors_ReturnsBadRequest()
	{
		// Arrange
		PostTodoWithValidation.RequestModel request = new()
		{
			Title = "", // Invalid - empty
			Description = "Test",
			Email = "bad-email", // Invalid - not an email
			Priority = 15 // Invalid - exceeds maximum
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos/WithValidation", request);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
		string content = await response.Content.ReadAsStringAsync();
		
		// Should have multiple validation errors
		content.ShouldContain("Title", Case.Insensitive);
		content.ShouldContain("Email", Case.Insensitive);
		content.ShouldContain("Priority", Case.Insensitive);
	}

	[Fact]
	public async Task PostTodoWithValidation_WithOptionalFieldsOmitted_CreatesSuccessfully()
	{
		// Arrange
		TestTodoStore? todoStore = _factory.Services.GetRequiredService<ITodoStore>() as TestTodoStore;
		todoStore!.Clear();

		PostTodoWithValidation.RequestModel request = new()
		{
			Title = "Minimal Todo",
			Description = null, // Optional
			Email = "test@example.com",
			Priority = 3,
			DueDate = null, // Optional (for low priority)
			IsCompleted = false
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos/WithValidation", request);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		PostTodoWithValidation.ResponseModel? createdTodo = await response.Content.ReadFromJsonAsync<PostTodoWithValidation.ResponseModel>();
		createdTodo.ShouldNotBeNull();
		createdTodo.Title.ShouldBe("Minimal Todo");
	}
}
