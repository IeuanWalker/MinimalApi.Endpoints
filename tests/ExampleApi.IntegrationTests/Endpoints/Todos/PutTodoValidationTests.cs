using System.Net;
using System.Net.Http.Json;
using ExampleApi.Data;
using ExampleApi.IntegrationTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ExampleApi.IntegrationTests.Endpoints.Todos;

/// <summary>
/// Integration tests for PutTodo endpoint validation scenarios
/// </summary>
public class PutTodoValidationTests : IClassFixture<ExampleApiWebApplicationFactory>
{
	readonly ExampleApiWebApplicationFactory _factory;
	readonly HttpClient _client;

	public PutTodoValidationTests(ExampleApiWebApplicationFactory factory)
	{
		_factory = factory;
		_client = _factory.CreateClient();
	}

	[Fact]
	public async Task PutTodo_WithInvalidData_ReturnsBadRequest()
	{
		// Arrange
		TestTodoStore? todoStore = _factory.Services.GetRequiredService<ITodoStore>() as TestTodoStore;
		todoStore!.Clear();

		Todo existingTodo = TestHelpers.CreateTestTodo("Existing Todo");
		todoStore.SeedData(existingTodo);

		var invalidUpdateRequest = new
		{
			Title = "", // Invalid empty title
			Description = "Updated Description",
			IsCompleted = true
		};

		// Act
		HttpResponseMessage response = await _client.PutAsJsonAsync($"/api/v1/todos/{existingTodo.Id}", invalidUpdateRequest);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}
}
