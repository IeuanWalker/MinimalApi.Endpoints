using ExampleApi.Endpoints.Todos.GetAll;
using ExampleApi.Models;
using ExampleApi.Services;
using FluentAssertions;
using NSubstitute;

namespace ExampleApi.Tests;

public class GetAllTodosEndpointTests
{
	[Fact]
	public async Task Handle_ReturnsAllTodos()
	{
		// Arrange
		ITodoStore todoStore = Substitute.For<ITodoStore>();
		List<Todo> todos =
		[
			new Todo { Id = 1, Title = "Todo 1", Description = "Description 1", IsCompleted = false, CreatedAt = DateTime.UtcNow },
			new Todo { Id = 2, Title = "Todo 2", Description = "Description 2", IsCompleted = true, CreatedAt = DateTime.UtcNow.AddDays(-1) }
		];

		todoStore.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(todos);

		GetAllTodosEndpoint endpoint = new(todoStore);

		// Act
		ResponseModel[] result = await endpoint.Handle(CancellationToken.None);

		// Assert
		result.Should().HaveCount(2);
		result[0].Id.Should().Be(1);
		result[0].Title.Should().Be("Todo 1");
		result[1].Id.Should().Be(2);
		result[1].Title.Should().Be("Todo 2");
	}

	[Fact]
	public async Task Handle_ReturnsEmptyArray_WhenNoTodos()
	{
		// Arrange
		ITodoStore todoStore = Substitute.For<ITodoStore>();
		todoStore.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns([]);

		GetAllTodosEndpoint endpoint = new(todoStore);

		// Act
		ResponseModel[] result = await endpoint.Handle(CancellationToken.None);

		// Assert
		result.Should().BeEmpty();
	}
}
