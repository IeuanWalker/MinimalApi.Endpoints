using ExampleApi.Data;
using ExampleApi.Endpoints.Todos.GetById;
using NSubstitute;
using Shouldly;

namespace ExampleApi.Tests.Endpoints.Todos;

public class GetTodoByIdEndpointTests
{
	[Fact]
	public async Task Handle_WithExistingId_ReturnsTodo()
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

		todoStore.GetByIdAsync(1, Arg.Any<CancellationToken>())
			.Returns(existingTodo);

		GetTodoByIdEndpoint endpoint = new(todoStore);
		RequestModel request = new() { Id = 1 };

		// Act
		ResponseModel? result = await endpoint.Handle(request, CancellationToken.None);

		// Assert
		result.ShouldNotBeNull();
		result.Id.ShouldBe(1);
		result.Title.ShouldBe("Test Todo");
		result.Description.ShouldBe("Test Description");
		result.IsCompleted.ShouldBeFalse();
	}

	[Fact]
	public async Task Handle_WithNonExistingId_ReturnsNull()
	{
		// Arrange
		ITodoStore todoStore = Substitute.For<ITodoStore>();
		todoStore.GetByIdAsync(999, Arg.Any<CancellationToken>())
			.Returns((Todo?)null);

		GetTodoByIdEndpoint endpoint = new(todoStore);
		RequestModel request = new() { Id = 999 };

		// Act
		ResponseModel? result = await endpoint.Handle(request, CancellationToken.None);

		// Assert
		result.ShouldBeNull();
	}
}
