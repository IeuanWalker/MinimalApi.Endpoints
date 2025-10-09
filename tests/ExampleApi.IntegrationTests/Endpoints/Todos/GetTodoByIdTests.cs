using System.Net;
using System.Net.Http.Json;
using ExampleApi.Data;
using Microsoft.Extensions.DependencyInjection;

namespace ExampleApi.IntegrationTests.Endpoints.Todos;

/// <summary>
/// Integration tests for GetTodoById endpoint
/// </summary>
public class GetTodoByIdTests : IClassFixture<ExampleApiWebApplicationFactory>
{
	readonly ExampleApiWebApplicationFactory _factory;
	readonly HttpClient _client;

	public GetTodoByIdTests(ExampleApiWebApplicationFactory factory)
	{
		_factory = factory;
		_client = _factory.CreateClient();
	}

	[Fact]
	public async Task GetTodoById_WithValidId_ReturnsTodo()
	{
		// Arrange
		TestTodoStore? todoStore = _factory.Services.GetRequiredService<ITodoStore>() as TestTodoStore;
		todoStore!.Clear();

		Todo todo = TestHelpers.CreateTestTodo("Test Todo");
		todoStore.SeedData(todo);

		// Act
		HttpResponseMessage response = await _client.GetAsync($"/api/v1/todos/{todo.Id}");

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		ExampleApi.Endpoints.Todos.GetById.ResponseModel? returnedTodo = await response.Content.ReadFromJsonAsync<ExampleApi.Endpoints.Todos.GetById.ResponseModel>();
		returnedTodo.ShouldNotBeNull();
		returnedTodo!.Id.ShouldBe(todo.Id);
		returnedTodo.Title.ShouldBe("Test Todo");
		returnedTodo.IsCompleted.ShouldBeFalse();
	}

	[Fact]
	public async Task GetTodoById_WithInvalidId_ReturnsNoContent()
	{
		// Arrange
		TestTodoStore? todoStore = _factory.Services.GetRequiredService<ITodoStore>() as TestTodoStore;
		todoStore!.Clear();

		const int nonExistentId = 999;

		// Act
		HttpResponseMessage response = await _client.GetAsync($"/api/v1/todos/{nonExistentId}");

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
	}
}
