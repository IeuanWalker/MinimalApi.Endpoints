using System.Net;
using System.Net.Http.Json;
using ExampleApi.Data;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using PostTodoFluentValidation = ExampleApi.Endpoints.Todos.PostFluentValidation;

namespace ExampleApi.IntegrationTests.Endpoints.Todos;

/// <summary>
/// Integration tests for PostTodo endpoint with FluentValidation
/// </summary>
public class PostTodoFluentValidationTests : IClassFixture<ExampleApiWebApplicationFactory>
{
	readonly ExampleApiWebApplicationFactory _factory;
	readonly HttpClient _client;

	public PostTodoFluentValidationTests(ExampleApiWebApplicationFactory factory)
	{
		_factory = factory;
		_client = _factory.CreateClient();
	}

	[Fact]
	public async Task PostTodoFluentValidation_WithValidData_CreatesSuccessfully()
	{
		// Arrange
		TestTodoStore? todoStore = _factory.Services.GetRequiredService<ITodoStore>() as TestTodoStore;
		todoStore!.Clear();

		PostTodoFluentValidation.RequestModel request = new()
		{
			Title = "Valid Todo Title",
			Description = "Valid Description",
			NestedObject = new PostTodoFluentValidation.NestedObjectModel
			{
				Name = "Valid Name",
				Age = 25
			},
			NestedObject2 = []  // Empty list is allowed
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos/FluentValidation", request);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		PostTodoFluentValidation.ResponseModel? createdTodo = await response.Content.ReadFromJsonAsync<PostTodoFluentValidation.ResponseModel>();
		createdTodo.ShouldNotBeNull();
		createdTodo.Title.ShouldBe("Valid Todo Title");
	}

	[Fact]
	public async Task PostTodoFluentValidation_WithEmptyTitle_ReturnsBadRequest()
	{
		// Arrange
		PostTodoFluentValidation.RequestModel request = new()
		{
			Title = "", // ? Invalid - empty title
			Description = "Test Description",
			NestedObject = new PostTodoFluentValidation.NestedObjectModel
			{
				Name = "Valid Name",
				Age = 25
			},
			NestedObject2 = []
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos/FluentValidation", request);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

		string content = await response.Content.ReadAsStringAsync();
		content.ShouldContain("title", Case.Insensitive);
	}

	[Fact]
	public async Task PostTodoFluentValidation_WithNullTitle_ReturnsBadRequest()
	{
		// Arrange
		PostTodoFluentValidation.RequestModel request = new()
		{
			Title = null!, // ? Invalid - null title
			Description = "Test Description",
			NestedObject = new PostTodoFluentValidation.NestedObjectModel
			{
				Name = "Valid Name",
				Age = 25
			},
			NestedObject2 = []
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos/FluentValidation", request);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}
}
