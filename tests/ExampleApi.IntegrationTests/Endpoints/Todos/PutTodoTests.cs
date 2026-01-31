using System.Net;
using System.Net.Http.Json;
using ExampleApi.Data;
using Microsoft.Extensions.DependencyInjection;

namespace ExampleApi.IntegrationTests.Endpoints.Todos;

/// <summary>
/// Integration tests for PutTodo endpoint
/// </summary>
public class PutTodoTests : IClassFixture<ExampleApiWebApplicationFactory>
{
	readonly ExampleApiWebApplicationFactory _factory;
	readonly HttpClient _client;

	public PutTodoTests(ExampleApiWebApplicationFactory factory)
	{
		_factory = factory;
		_client = _factory.CreateClient();
	}

	[Fact]
	public async Task PutTodo_WithValidData_UpdatesTodo()
	{
		// Arrange
		TestTodoStore? todoStore = _factory.Services.GetRequiredService<ITodoStore>() as TestTodoStore;
		todoStore!.Clear();

		Todo originalTodo = TestHelpers.CreateTestTodo("Original Title");
		todoStore.SeedData(originalTodo);

		var updateRequest = new
		{
			Title = "Updated Title",
			Description = "Updated Description",
			IsCompleted = true
		};

		// Act
		HttpResponseMessage response = await _client.PutAsJsonAsync($"/api/v1/todos/{originalTodo.Id}", updateRequest, TestContext.Current.CancellationToken);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);

		Todo? updatedTodo = await todoStore.GetByIdAsync(originalTodo.Id, CancellationToken.None);
		updatedTodo.ShouldNotBeNull();
		updatedTodo.Title.ShouldBe("Updated Title");
		updatedTodo.Description.ShouldBe("Updated Description");
		updatedTodo.IsCompleted.ShouldBeTrue();
	}
}
