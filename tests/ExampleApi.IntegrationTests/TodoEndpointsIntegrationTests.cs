using System.Net;
using System.Net.Http.Json;
using ExampleApi.Endpoints.Todos.GetAll;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ExampleApi.IntegrationTests;

public class TodoEndpointsIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
	readonly WebApplicationFactory<Program> _factory;

	public TodoEndpointsIntegrationTests(WebApplicationFactory<Program> factory)
	{
		_factory = factory;
	}

	[Fact]
	public async Task GetAllTodos_ReturnsSuccess()
	{
		// Arrange
		HttpClient client = _factory.CreateClient();

		// Act
		HttpResponseMessage response = await client.GetAsync("/api/v1/todos");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		
		ResponseModel[]? todos = await response.Content.ReadFromJsonAsync<ResponseModel[]>();
		todos.Should().NotBeNull();
		todos.Should().NotBeEmpty();
	}

	[Fact]
	public async Task GetTodoById_WithValidId_ReturnsSuccess()
	{
		// Arrange
		HttpClient client = _factory.CreateClient();

		// Act
		HttpResponseMessage response = await client.GetAsync("/api/v1/todos/1");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task GetTodoById_WithInvalidId_ReturnsNotFound()
	{
		// Arrange
		HttpClient client = _factory.CreateClient();

		// Act
		HttpResponseMessage response = await client.GetAsync("/api/v1/todos/999");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task CreateTodo_WithValidData_ReturnsSuccess()
	{
		// Arrange
		HttpClient client = _factory.CreateClient();
		object newTodo = new
		{
			title = "New Todo",
			description = "Test Description",
			isCompleted = false
		};

		// Act
		HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/todos/fluent-validation", newTodo);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task CreateTodo_WithInvalidData_ReturnsBadRequest()
	{
		// Arrange
		HttpClient client = _factory.CreateClient();
		object newTodo = new
		{
			title = "", // Empty title should fail validation
			description = "Test Description",
			isCompleted = false
		};

		// Act
		HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/todos/fluent-validation", newTodo);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}
}
