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
		HttpResponseMessage response = await _client.GetAsync($"/api/v1/todos/{todo.Id}", TestContext.Current.CancellationToken);

		// Assert
		await Verify(response)
			.IgnoreMember("Content-Length");
	}

	[Fact]
	public async Task GetTodoById_WithInvalidId_ReturnsNoContent()
	{
		// Arrange
		TestTodoStore? todoStore = _factory.Services.GetRequiredService<ITodoStore>() as TestTodoStore;
		todoStore!.Clear();

		const int nonExistentId = 999;

		// Act
		HttpResponseMessage response = await _client.GetAsync($"/api/v1/todos/{nonExistentId}", TestContext.Current.CancellationToken);

		// Assert
		await Verify(response)
			.IgnoreMember("Content-Length");
	}
}
