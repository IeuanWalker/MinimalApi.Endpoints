using System.Net;
using System.Net.Http.Json;
using ExampleApi.Data;
using ExampleApi.IntegrationTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ExampleApi.IntegrationTests.Endpoints.Todos;

/// <summary>
/// Integration tests for PatchTodo endpoint
/// </summary>
public class PatchTodoTests : IClassFixture<ExampleApiWebApplicationFactory>
{
	readonly ExampleApiWebApplicationFactory _factory;
	readonly HttpClient _client;

	public PatchTodoTests(ExampleApiWebApplicationFactory factory)
	{
		_factory = factory;
		_client = _factory.CreateClient();
	}

	[Fact]
	public async Task PatchTodo_WithPartialData_UpdatesOnlySpecifiedFields()
	{
		// Arrange
		TestTodoStore? todoStore = _factory.Services.GetRequiredService<ITodoStore>() as TestTodoStore;
		todoStore!.Clear();

		Todo originalTodo = TestHelpers.CreateTestTodo("Original Title", "Original Description");
		todoStore.SeedData(originalTodo);

		var patchRequest = new
		{
			IsCompleted = true
			// Not updating Title or Description
		};

		// Act
		using HttpRequestMessage request = new(HttpMethod.Patch, $"/api/v1/todos/{originalTodo.Id}")
		{
			Content = JsonContent.Create(patchRequest)
		};
		HttpResponseMessage response = await _client.SendAsync(request);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);

		// Verify only IsCompleted was updated
		Todo? updatedTodo = await todoStore.GetByIdAsync(originalTodo.Id, CancellationToken.None);
		updatedTodo.ShouldNotBeNull();
		updatedTodo.Title.ShouldBe("Original Title"); // Unchanged
		updatedTodo.Description.ShouldBe("Original Description"); // Unchanged
		updatedTodo.IsCompleted.ShouldBeTrue(); // Updated
	}
}
