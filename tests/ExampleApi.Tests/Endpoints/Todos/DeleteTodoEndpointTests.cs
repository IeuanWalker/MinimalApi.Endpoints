using ExampleApi.Data;
using ExampleApi.Endpoints.Todos.Delete;

namespace ExampleApi.Tests.Endpoints.Todos;

public class DeleteTodoEndpointTests
{
	[Fact]
	public async Task Handle_WithExistingId_CallsDeleteAsyncWithCorrectId()
	{
		// Arrange
		ITodoStore todoStore = Substitute.For<ITodoStore>();
		todoStore
			.DeleteAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
			.Returns(true);

		DeleteTodoEndpoint endpoint = new(todoStore);
		RequestModel request = new() { Id = 42 };

		// Act
		await endpoint.Handle(request, CancellationToken.None);

		// Assert
		await todoStore
			.Received(1)
			.DeleteAsync(42, Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_WithNonExistingId_CallsDeleteAsync()
	{
		// Arrange
		ITodoStore todoStore = Substitute.For<ITodoStore>();
		todoStore
			.DeleteAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
			.Returns(false);

		DeleteTodoEndpoint endpoint = new(todoStore);
		RequestModel request = new() { Id = 999 };

		// Act
		await endpoint.Handle(request, CancellationToken.None);

		// Assert
		await todoStore
			.Received(1)
			.DeleteAsync(999, Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_CallsDeleteAsyncOnlyOnce()
	{
		// Arrange
		ITodoStore todoStore = Substitute.For<ITodoStore>();
		todoStore
			.DeleteAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
			.Returns(true);

		DeleteTodoEndpoint endpoint = new(todoStore);
		RequestModel request = new() { Id = 5 };

		// Act
		await endpoint.Handle(request, CancellationToken.None);

		// Assert
		await todoStore
			.Received(1)
			.DeleteAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
	}
}
