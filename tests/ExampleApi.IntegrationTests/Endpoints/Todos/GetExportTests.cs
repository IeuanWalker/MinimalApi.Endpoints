using ExampleApi.Data;
using Microsoft.Extensions.DependencyInjection;

namespace ExampleApi.IntegrationTests.Endpoints.Todos;

/// <summary>
/// Integration tests for GetExport endpoint
/// </summary>
public class GetExportTests : IClassFixture<ExampleApiWebApplicationFactory>
{
	readonly ExampleApiWebApplicationFactory _factory;
	readonly HttpClient _client;

	public GetExportTests(ExampleApiWebApplicationFactory factory)
	{
		_factory = factory;
		_client = _factory.CreateClient();
	}

	[Fact]
	public async Task GetExport_WhenNoTodos_ReturnsNoContent()
	{
		// Arrange - Clear any existing data
		TestTodoStore? todoStore = _factory.Services.GetRequiredService<ITodoStore>() as TestTodoStore;
		todoStore!.Clear();

		// Act
		HttpResponseMessage response = await _client.GetAsync("/api/v1/todos/export", TestContext.Current.CancellationToken);

		// Assert
		await Verify(response)
			.IgnoreMember("Content-Length");
	}

	[Fact]
	public async Task GetExport_WithTodos_ReturnsHtmlFile()
	{
		// Arrange
		TestTodoStore? todoStore = _factory.Services.GetRequiredService<ITodoStore>() as TestTodoStore;
		todoStore!.Clear();

		Todo todo1 = TestHelpers.CreateTestTodo("Test Todo 1", "Description 1", isCompleted: false);
		Todo todo2 = TestHelpers.CreateTestTodo("Test Todo 2", "Description 2", isCompleted: true);
		todoStore.SeedData(todo1, todo2);

		// Act
		HttpResponseMessage response = await _client.GetAsync("/api/v1/todos/export", TestContext.Current.CancellationToken);

		// Assert
		await Verify(response)
			.IgnoreMember("Content-Length");
	}

	[Fact]
	public async Task GetExport_WithSpecialCharacters_ProperlyEncodesHtml()
	{
		// Arrange
		TestTodoStore? todoStore = _factory.Services.GetRequiredService<ITodoStore>() as TestTodoStore;
		todoStore!.Clear();

		// Create item with special characters that need HTML encoding
		Todo todoWithSpecialChars = TestHelpers.CreateTestTodo(
			"Test <script>alert('xss')</script> & \"quotes\"",
			"Description with & ampersand < > brackets");
		todoStore.SeedData(todoWithSpecialChars);

		// Act
		HttpResponseMessage response = await _client.GetAsync("/api/v1/todos/export", TestContext.Current.CancellationToken);

		// Assert
		await Verify(response)
			.IgnoreMember("Content-Length");
	}

	[Fact]
	public async Task GetExport_FilenamFormat_IsCorrect()
	{
		// Arrange
		TestTodoStore? todoStore = _factory.Services.GetRequiredService<ITodoStore>() as TestTodoStore;
		todoStore!.Clear();

		Todo todo = TestHelpers.CreateTestTodo("Test Todo");
		todoStore.SeedData(todo);

		// Act
		HttpResponseMessage response = await _client.GetAsync("/api/v1/todos/export", TestContext.Current.CancellationToken);

		// Assert
		await Verify(response)
			.IgnoreMember("Content-Length");
	}

	[Fact]
	public async Task GetExport_LargeTodoSet_PerformsWell()
	{
		// Arrange - Test with a larger dataset
		TestTodoStore? todoStore = _factory.Services.GetRequiredService<ITodoStore>() as TestTodoStore;
		todoStore!.Clear();

		List<Todo> largeTodoSet = [];
		for (int i = 1; i <= 100; i++)
		{
			largeTodoSet.Add(TestHelpers.CreateTestTodo($"Todo {i}", $"Description for todo {i}", i % 2 == 0));
		}
		todoStore.SeedData([.. largeTodoSet]);

		// Act
		using CancellationTokenSource cts = new(TimeSpan.FromSeconds(10)); // 10 second timeout
		HttpResponseMessage response = await _client.GetAsync("/api/v1/todos/export", cts.Token);

		// Assert
		await Verify(response)
			.IgnoreMember("Content-Length");
	}
}
