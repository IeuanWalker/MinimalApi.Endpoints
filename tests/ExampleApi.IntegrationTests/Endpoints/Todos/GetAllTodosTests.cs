using System.Net;
using System.Net.Http.Json;
using ExampleApi.Data;
using Microsoft.Extensions.DependencyInjection;

namespace ExampleApi.IntegrationTests.Endpoints.Todos;

/// <summary>
/// Integration tests for GetAllTodos endpoint
/// </summary>
public class GetAllTodosTests : IClassFixture<ExampleApiWebApplicationFactory>
{
	readonly ExampleApiWebApplicationFactory _factory;
	readonly HttpClient _client;

	public GetAllTodosTests(ExampleApiWebApplicationFactory factory)
	{
		_factory = factory;
		_client = _factory.CreateClient();
	}

	[Fact]
	public async Task GetAllTodos_WhenNoTodos_ReturnsEmptyList()
	{
		// Arrange - Clear any existing data
		TestTodoStore? todoStore = _factory.Services.GetRequiredService<ITodoStore>() as TestTodoStore;
		todoStore!.Clear();

		// Act
		HttpResponseMessage response = await _client.GetAsync("/api/v1/todos", TestContext.Current.CancellationToken);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		ExampleApi.Endpoints.Todos.GetAll.ResponseModel[]? todos = await response.Content.ReadFromJsonAsync<ExampleApi.Endpoints.Todos.GetAll.ResponseModel[]>(TestContext.Current.CancellationToken);
		todos.ShouldNotBeNull();
		todos.ShouldBeEmpty();
	}

	[Fact]
	public async Task GetAllTodos_WithExistingTodos_ReturnsAllTodos()
	{
		// Arrange
		TestTodoStore? todoStore = _factory.Services.GetRequiredService<ITodoStore>() as TestTodoStore;
		todoStore!.Clear();

		Todo todo1 = TestHelpers.CreateTestTodo("Test Todo 1", isCompleted: false);
		Todo todo2 = TestHelpers.CreateTestTodo("Test Todo 2", isCompleted: true);
		todoStore.SeedData(todo1, todo2);

		// Act
		HttpResponseMessage response = await _client.GetAsync("/api/v1/todos", TestContext.Current.CancellationToken);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		ExampleApi.Endpoints.Todos.GetAll.ResponseModel[]? todos = await response.Content.ReadFromJsonAsync<ExampleApi.Endpoints.Todos.GetAll.ResponseModel[]>(TestContext.Current.CancellationToken);
		todos.ShouldNotBeNull();
		todos.Length.ShouldBe(2);
		todos.ShouldContain(t => t.Title == "Test Todo 1" && !t.IsCompleted);
		todos.ShouldContain(t => t.Title == "Test Todo 2" && t.IsCompleted);
	}
}
