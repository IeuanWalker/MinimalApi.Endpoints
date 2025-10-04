using ExampleApi.Endpoints.Todos.GetById;
using ExampleApi.Models;
using ExampleApi.Services;
using FluentAssertions;
using NSubstitute;

namespace ExampleApi.Tests;

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
		result.Should().NotBeNull();
		result!.Id.Should().Be(1);
		result.Title.Should().Be("Test Todo");
		result.Description.Should().Be("Test Description");
		result.IsCompleted.Should().BeFalse();
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
		result.Should().BeNull();
	}
}
