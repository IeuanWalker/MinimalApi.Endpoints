using System.Net;
using System.Net.Http.Json;
using ExampleApi.Data;
using ExampleApi.IntegrationTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using PostTodoDataAnnotation = ExampleApi.Endpoints.Todos.PostDataAnnotation;

namespace ExampleApi.IntegrationTests.Endpoints.Todos;

/// <summary>
/// Integration tests for PostTodo endpoint
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
	public async Task PostTodo_WithValidData_CreatesTodo()
	{
		// Arrange
		TestTodoStore? todoStore = _factory.Services.GetRequiredService<ITodoStore>() as TestTodoStore;
		todoStore!.Clear();

		PostTodoDataAnnotation.RequestModel request = new()
		{
			Title = "New Test Todo",
			Description = "Test Description"
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos/DataAnnotation", request);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		PostTodoDataAnnotation.ResponseModel? createdTodo = await response.Content.ReadFromJsonAsync<PostTodoDataAnnotation.ResponseModel>();
		createdTodo.ShouldNotBeNull();
		createdTodo.Title.ShouldBe("New Test Todo");
		createdTodo.IsCompleted.ShouldBeFalse();
		createdTodo.Id.ShouldBeGreaterThan(0);

		// Verify it was actually stored
		IEnumerable<Todo> allTodos = await todoStore.GetAllAsync(CancellationToken.None);
		allTodos.ShouldContain(t => t.Id == createdTodo.Id);
	}

	[Fact]
	public async Task PostTodo_WithInvalidData_ReturnsBadRequest()
	{
		// Arrange
		PostTodoDataAnnotation.RequestModel request = new()
		{
			Title = "", // Invalid - empty title
			Description = "Test"
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos/DataAnnotation", request);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task PostTodo_WithDuplicateTitle_ReturnsConflict()
	{
		// Arrange
		TestTodoStore? todoStore = _factory.Services.GetRequiredService<ITodoStore>() as TestTodoStore;
		todoStore!.Clear();

		Todo existingTodo = TestHelpers.CreateTestTodo("Duplicate Title");
		todoStore.SeedData(existingTodo);

		PostTodoDataAnnotation.RequestModel request = new()
		{
			Title = "Duplicate Title",
			Description = "Test"
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos/DataAnnotation", request);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
	}
}
