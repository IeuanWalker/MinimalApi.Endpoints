using System.Net;
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
		HttpResponseMessage response = await _client.GetAsync("/api/v1/todos/export");

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
		string content = await response.Content.ReadAsStringAsync();
		content.ShouldBeEmpty();
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
		HttpResponseMessage response = await _client.GetAsync("/api/v1/todos/export");

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		response.Content.Headers.ContentType?.MediaType.ShouldBe("text/html");

		// Verify Content-Disposition header for file download
		response.Content.Headers.ContentDisposition.ShouldNotBeNull();
		response.Content.Headers.ContentDisposition.DispositionType.ShouldBe("attachment");
		response.Content.Headers.ContentDisposition.FileName.ShouldStartWith("todos-");
		response.Content.Headers.ContentDisposition.FileName.ShouldEndWith(".html");

		string htmlContent = await response.Content.ReadAsStringAsync();

		// Verify HTML structure
		htmlContent.ShouldContain("<!DOCTYPE html>");
		htmlContent.ShouldContain("<title>Todo Export</title>");
		htmlContent.ShouldContain("<h1>Todo Export</h1>");
		htmlContent.ShouldContain("<table>");

		// Verify todo data is present
		htmlContent.ShouldContain("Test Todo 1");
		htmlContent.ShouldContain("Description 1");
		htmlContent.ShouldContain("Test Todo 2");
		htmlContent.ShouldContain("Description 2");
		htmlContent.ShouldContain("No"); // For completed status of todo1
		htmlContent.ShouldContain("Yes"); // For completed status of todo2

		// Verify HTML encoding is working (should not contain raw HTML)
		htmlContent.ShouldNotContain("<script");
		htmlContent.ShouldNotContain("javascript:");
	}

	[Fact]
	public async Task GetExport_WithSpecialCharacters_ProperlyEncodesHtml()
	{
		// Arrange
		TestTodoStore? todoStore = _factory.Services.GetRequiredService<ITodoStore>() as TestTodoStore;
		todoStore!.Clear();

		// Create todo with special characters that need HTML encoding
		Todo todoWithSpecialChars = TestHelpers.CreateTestTodo(
			"Test <script>alert('xss')</script> & \"quotes\"",
			"Description with & ampersand < > brackets");
		todoStore.SeedData(todoWithSpecialChars);

		// Act
		HttpResponseMessage response = await _client.GetAsync("/api/v1/todos/export");

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		string htmlContent = await response.Content.ReadAsStringAsync();

		// Verify HTML encoding worked properly
		htmlContent.ShouldContain("&lt;script&gt;");
		htmlContent.ShouldContain("&amp;");
		htmlContent.ShouldContain("&quot;");

		// Should NOT contain unencoded dangerous content
		htmlContent.ShouldNotContain("<script>alert");
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
		HttpResponseMessage response = await _client.GetAsync("/api/v1/todos/export");

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);

		string? fileName = response.Content.Headers.ContentDisposition?.FileName;
		fileName.ShouldNotBeNull();

		// Verify filename format: todos-YYYY-MM-DD-HH-mm-ss.html
		fileName.ShouldStartWith("todos-");
		fileName.ShouldEndWith(".html");

		// Extract date part and verify it's a valid format
		string datePart = fileName.Substring(6, fileName.Length - 11); // Remove "todos-" and ".html"
		datePart.Length.ShouldBe(19); // YYYY-MM-DD-HH-mm-ss format
		datePart.ShouldMatch(@"^\d{4}-\d{2}-\d{2}-\d{2}-\d{2}-\d{2}$");
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
		todoStore.SeedData(largeTodoSet.ToArray());

		// Act
		using CancellationTokenSource cts = new(TimeSpan.FromSeconds(10)); // 10 second timeout
		HttpResponseMessage response = await _client.GetAsync("/api/v1/todos/export", cts.Token);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		response.Content.Headers.ContentType?.MediaType.ShouldBe("text/html");

		string htmlContent = await response.Content.ReadAsStringAsync();
		htmlContent.ShouldNotBeEmpty();

		// Verify all todos are present
		htmlContent.ShouldContain("Todo 1");
		htmlContent.ShouldContain("Todo 50");
		htmlContent.ShouldContain("Todo 100");

		// Verify the content size is reasonable (should be larger for more todos)
		htmlContent.Length.ShouldBeGreaterThan(10000); // Rough estimate for 100 todos
	}
}
