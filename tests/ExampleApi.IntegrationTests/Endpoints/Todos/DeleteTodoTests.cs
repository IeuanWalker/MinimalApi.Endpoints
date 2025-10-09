using System.Net;
using ExampleApi.Data;
using ExampleApi.IntegrationTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ExampleApi.IntegrationTests.Endpoints.Todos;

/// <summary>
/// Integration tests for DeleteTodo endpoint
/// </summary>
public class DeleteTodoTests : IClassFixture<ExampleApiWebApplicationFactory>
{
	readonly ExampleApiWebApplicationFactory _factory;
	readonly HttpClient _client;

	public DeleteTodoTests(ExampleApiWebApplicationFactory factory)
	{
		_factory = factory;
		_client = _factory.CreateClient();
	}

	[Fact]
	public async Task DeleteTodo_WithValidId_DeletesTodo()
	{
		// Arrange
		TestTodoStore? todoStore = _factory.Services.GetRequiredService<ITodoStore>() as TestTodoStore;
		todoStore!.Clear();

		Todo todo = TestHelpers.CreateTestTodo("Test Todo");
		todoStore.SeedData(todo);

		// Act
		HttpResponseMessage response = await _client.DeleteAsync($"/api/v1/todos/{todo.Id}");

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);

		// Verify it was actually deleted
		Todo? deletedTodo = await todoStore.GetByIdAsync(todo.Id, CancellationToken.None);
		deletedTodo.ShouldBeNull();
	}

	[Fact]
	public async Task DeleteTodo_WithInvalidId_ReturnsOk()
	{
		// Arrange
		TestTodoStore? todoStore = _factory.Services.GetRequiredService<ITodoStore>() as TestTodoStore;
		todoStore!.Clear();

		const int nonExistentId = 999;

		// Act
		HttpResponseMessage response = await _client.DeleteAsync($"/api/v1/todos/{nonExistentId}");

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
	}
}
