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
			Description = "Valid Description"
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos", request, TestContext.Current.CancellationToken);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		ResponseModel? createdTodo = await response.Content.ReadFromJsonAsync<ResponseModel>(TestContext.Current.CancellationToken);
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
			Description = "Test Description"
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos", request, TestContext.Current.CancellationToken);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

		string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
		content.ShouldContain("title", Case.Insensitive);
	}

	[Fact]
	public async Task PostTodoFluentValidation_WithNullTitle_ReturnsBadRequest()
	{
		// Arrange
		RequestModel request = new()
		{
			Title = null!, // ? Invalid - null title
			Description = "Test Description"
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos", request, TestContext.Current.CancellationToken);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
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
			Description = "" // Invalid - empty description
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos", request, TestContext.Current.CancellationToken);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

		string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

		// Should contain multiple validation errors
		content.ShouldContain("title", Case.Insensitive);
		content.ShouldContain("description", Case.Insensitive);
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
			Description = "Valid Description"
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos", request, TestContext.Current.CancellationToken);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

		string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
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
			Description = tooLongDescription
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos", request, TestContext.Current.CancellationToken);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

		string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
		content.ShouldContain("description", Case.Insensitive);
	}
}
