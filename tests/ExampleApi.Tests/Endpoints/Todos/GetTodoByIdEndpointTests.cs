using ExampleApi.Data;
using ExampleApi.Endpoints.Todos.GetById;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ExampleApi.Tests.Endpoints.Todos;

public class GetTodoByIdEndpointTests
{
	[Fact]
	public async Task Handle_WithExistingId_ReturnsOkWithTodo()
	{
		// Arrange
		ITodoStore todoStore = Substitute.For<ITodoStore>();
		Todo existingTodo = new()
		{
			Id = 1,
			Title = "Test Todo",
			Description = "Test Description",
			IsCompleted = false,
			CreatedAt = DateTime.UtcNow
		};

		todoStore
			.GetByIdAsync(1, Arg.Any<CancellationToken>())
			.Returns(existingTodo);

		GetTodoByIdEndpoint endpoint = new(todoStore);
		RequestModel request = new() { Id = 1 };

		// Act
		Results<Ok<ResponseModel>, NoContent> result = await endpoint.Handle(request, CancellationToken.None);

		// Assert
		await Verify(result);
	}

	[Fact]
	public async Task Handle_WithNonExistingId_ReturnsNotFound()
	{
		// Arrange
		ITodoStore todoStore = Substitute.For<ITodoStore>();
		todoStore
			.GetByIdAsync(999, Arg.Any<CancellationToken>())
			.Returns((Todo?)null);

		GetTodoByIdEndpoint endpoint = new(todoStore);
		RequestModel request = new() { Id = 999 };

		// Act
		Results<Ok<ResponseModel>, NoContent> result = await endpoint.Handle(request, CancellationToken.None);

		// Assert
		await Verify(result);
	}
}
