using System.Net;
using System.Net.Http.Json;
using ExampleApi.Data;
using ExampleApi.Endpoints.Todos.Post;
using Microsoft.Extensions.DependencyInjection;

namespace ExampleApi.IntegrationTests.Endpoints.Todos;

/// <summary>
/// Integration tests for PostTodo endpoint with FluentValidation
/// </summary>
public class PostTodoTests : IClassFixture<ExampleApiWebApplicationFactory>
{
	readonly ExampleApiWebApplicationFactory _factory;
	readonly HttpClient _client;

	public PostTodoTests(ExampleApiWebApplicationFactory factory)
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

		RequestModel request = new()
		{
			Title = "Valid Todo Title",
			Description = "Valid Description",
			NestedObject = new NestedObjectModel
			{
				Name = "Valid Name",
				Age = 25
			},
			NestedObject2 = []  // Empty list is allowed
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos", request);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		ResponseModel? createdTodo = await response.Content.ReadFromJsonAsync<ResponseModel>();
		createdTodo.ShouldNotBeNull();
		createdTodo.Title.ShouldBe("Valid Todo Title");
	}

	[Fact]
	public async Task PostTodoFluentValidation_WithEmptyTitle_ReturnsBadRequest()
	{
		// Arrange
		RequestModel request = new()
		{
			Title = "", // ? Invalid - empty title
			Description = "Test Description",
			NestedObject = new NestedObjectModel
			{
				Name = "Valid Name",
				Age = 25
			},
			NestedObject2 = []
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos", request);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

		string content = await response.Content.ReadAsStringAsync();
		content.ShouldContain("title", Case.Insensitive);
	}

	[Fact]
	public async Task PostTodoFluentValidation_WithNullTitle_ReturnsBadRequest()
	{
		// Arrange
		RequestModel request = new()
		{
			Title = null!, // ? Invalid - null title
			Description = "Test Description",
			NestedObject = new NestedObjectModel
			{
				Name = "Valid Name",
				Age = 25
			},
			NestedObject2 = []
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos", request);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task PostTodoFluentValidation_WithInvalidNestedObject_ReturnsBadRequest()
	{
		// Arrange
		TestTodoStore? todoStore = _factory.Services.GetRequiredService<ITodoStore>() as TestTodoStore;
		todoStore!.Clear();

		RequestModel request = new()
		{
			Title = "Valid Title",
			Description = "Valid Description",
			NestedObject = new NestedObjectModel
			{
				Name = "", // Invalid - empty name
				Age = 25
			},
			NestedObject2 = []
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos", request);

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

		RequestModel request = new()
		{
			Title = "Valid Title",
			Description = "Valid Description",
			NestedObject = new NestedObjectModel
			{
				Name = "Valid Name",
				Age = -5 // Invalid - negative age
			},
			NestedObject2 = []
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos", request);

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

		RequestModel request = new()
		{
			Title = "Valid Title",
			Description = "Valid Description",
			NestedObject = new NestedObjectModel
			{
				Name = "Valid Name",
				Age = 85 // Invalid - over limit (should be less than 80)
			},
			NestedObject2 = []
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos", request);

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

		RequestModel request = new()
		{
			Title = "Valid Title",
			Description = "Valid Description",
			NestedObject = new NestedObjectModel
			{
				Name = "Valid Name",
				Age = 25
			},
			NestedObject2 =
			[
				new NestedObject2Model { Note = "Valid note" },
				new NestedObject2Model { Note = "" } // Invalid - empty note
			]
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos", request);

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

		RequestModel request = new()
		{
			Title = "", // Invalid - empty title
			Description = "", // Invalid - empty description
			NestedObject = new NestedObjectModel
			{
				Name = "", // Invalid - empty name
				Age = -1 // Invalid - negative age
			},
			NestedObject2 = []
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos", request);

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

		RequestModel request = new()
		{
			Title = tooLongTitle,
			Description = "Valid Description",
			NestedObject = new NestedObjectModel
			{
				Name = "Valid Name",
				Age = 25
			},
			NestedObject2 = []
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos", request);

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

		RequestModel request = new()
		{
			Title = "Valid Title",
			Description = tooLongDescription,
			NestedObject = new NestedObjectModel
			{
				Name = "Valid Name",
				Age = 25
			},
			NestedObject2 = []
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos", request);

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

		RequestModel request = new()
		{
			Title = "Valid Title",
			Description = "Valid Description",
			NestedObject = null!, // Invalid - null nested object
			NestedObject2 = []
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos", request);

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

		RequestModel request = new()
		{
			Title = "Valid Todo Title",
			Description = "Valid Description",
			NestedObject = new NestedObjectModel
			{
				Name = "Valid Name",
				Age = 25
			},
			NestedObject2 =
			[
				new NestedObject2Model { Note = "First note" },
				new NestedObject2Model { Note = "Second note" },
				new NestedObject2Model { Note = "Third note" }
			]
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos", request);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		ResponseModel? createdTodo = await response.Content.ReadFromJsonAsync<ResponseModel>();
		createdTodo.ShouldNotBeNull();
		createdTodo.Title.ShouldBe("Valid Todo Title");
	}
}
