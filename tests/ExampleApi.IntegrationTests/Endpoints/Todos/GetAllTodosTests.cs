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
		await Verify(response)
			.IgnoreMember("Content-Length");
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
		await Verify(response)
			.IgnoreMember("Content-Length");
	}
}
