using System.Net.Http.Json;
using ExampleApi.Data;
using ExampleApi.Endpoints.Todos.Patch;
using Microsoft.Extensions.DependencyInjection;

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

		RequestBodyModel patchRequest = new()
		{
			Title = "TestTitle",
			Description = "TestDescription",
			IsCompleted = true
		};

		// Act
		using HttpRequestMessage request = new(HttpMethod.Patch, $"/api/v1/todos/{originalTodo.Id}")
		{
			Content = JsonContent.Create(patchRequest)
		};
		HttpResponseMessage response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

		// Assert
		await Verify(response)
			.IgnoreMember("Content-Length");

		// Verify only IsCompleted was updated
		Todo? updatedTodo = await todoStore.GetByIdAsync(originalTodo.Id, CancellationToken.None);
		updatedTodo.ShouldNotBeNull();
		updatedTodo.Title.ShouldBe("TestTitle");
		updatedTodo.Description.ShouldBe("TestDescription");
		updatedTodo.IsCompleted.ShouldBeTrue();
	}
}
