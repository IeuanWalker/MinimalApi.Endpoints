using System.Net;
using System.Net.Http.Json;
using ExampleApi.Data;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using PostTodoDataAnnotation = ExampleApi.Endpoints.Todos.PostDataAnnotation;

namespace ExampleApi.IntegrationTests.Endpoints.Todos;

/// <summary>
/// Integration tests for PostTodo endpoint with DataAnnotation validation
/// </summary>
public class PostTodoDataAnnotationTests : IClassFixture<ExampleApiWebApplicationFactory>
{
	readonly ExampleApiWebApplicationFactory _factory;
	readonly HttpClient _client;

	public PostTodoDataAnnotationTests(ExampleApiWebApplicationFactory factory)
	{
		_factory = factory;
		_client = _factory.CreateClient();
	}

	[Fact]
	public async Task PostTodoDataAnnotation_WithValidData_CreatesSuccessfully()
	{
		// Arrange
		TestTodoStore? todoStore = _factory.Services.GetRequiredService<ITodoStore>() as TestTodoStore;
		todoStore!.Clear();

		PostTodoDataAnnotation.RequestModel request = new()
		{
			Title = "Valid Todo Title",
			Description = "Valid Description"
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos/DataAnnotation", request);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		PostTodoDataAnnotation.ResponseModel? createdTodo = await response.Content.ReadFromJsonAsync<PostTodoDataAnnotation.ResponseModel>();
		createdTodo.ShouldNotBeNull();
		createdTodo.Title.ShouldBe("Valid Todo Title");
	}

	[Fact]
	public async Task PostTodoDataAnnotation_WithInvalidData_ReturnsBadRequest()
	{
		// Arrange
		PostTodoDataAnnotation.RequestModel request = new()
		{
			Title = "", // Empty title should fail data annotation validation
			Description = "Test"
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos/DataAnnotation", request);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public async Task PostTodoDataAnnotation_WithInvalidTitles_ReturnsBadRequest(string? invalidTitle)
	{
		// Arrange
		var request = new { Title = invalidTitle, Description = "Test" };

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/todos/DataAnnotation", request);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task PostTodoDataAnnotation_WithMalformedJson_ReturnsBadRequest()
	{
		// Arrange
		const string malformedJson = "{ \"title\": \"Test\", \"isCompleted\": }"; // Missing value for isCompleted

		// Act
		using StringContent content = new(malformedJson, null, "application/json");
		HttpResponseMessage response = await _client.PostAsync("/api/v1/todos/DataAnnotation", content);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task PostTodoDataAnnotation_WithWrongContentType_ReturnsUnsupportedMediaType()
	{
		// Arrange
		const string xmlContent = "<todo><title>Test</title></todo>";

		// Act
		using StringContent content = new(xmlContent, null, "application/xml");
		HttpResponseMessage response = await _client.PostAsync("/api/v1/todos/DataAnnotation", content);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.UnsupportedMediaType);
	}
}
