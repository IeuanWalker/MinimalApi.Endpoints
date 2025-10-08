using ExampleApi.Data;
using ExampleApi.Endpoints.Todos.Delete;
using NSubstitute;

namespace ExampleApi.Tests;

public class DeleteTodoEndpointTests
{
	[Fact]
	public async Task Handle_CallsDeleteAsync()
	{
		// Arrange
		ITodoStore todoStore = Substitute.For<ITodoStore>();
		todoStore.DeleteAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
			.Returns(true);

		DeleteTodoEndpoint endpoint = new(todoStore);
		RequestModel request = new() { Id = 1 };

		// Act
		await endpoint.Handle(request, CancellationToken.None);

		// Assert
		await todoStore.Received(1).DeleteAsync(1, Arg.Any<CancellationToken>());
	}
}
