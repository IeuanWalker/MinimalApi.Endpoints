using ExampleApi.Data;
using ExampleApi.Endpoints.Todos.Patch;

namespace ExampleApi.Tests.Endpoints.Todos;

public class PatchTodoEndpointTests
{
	[Fact]
	public async Task Handle_WithExistingId_AppliesPatchAndReturns()
	{
		// Arrange
		ITodoStore todoStore = Substitute.For<ITodoStore>();
		Todo patched = new() { Id = 1, Title = "Patched", Description = "Patched Desc", IsCompleted = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

		todoStore
			.PatchAsync(1, Arg.Any<Action<Todo>>(), Arg.Any<CancellationToken>())
			.Returns(patched);

		PatchTodoEndpoint endpoint = new(todoStore);
		RequestModel request = new() { Id = 1, Body = new RequestBodyModel { Title = "Patched", Description = "Patched Desc", IsCompleted = true } };

		// Act
		ResponseModel? result = await endpoint.Handle(request, CancellationToken.None);

		// Assert
		await Verify(result);

		await todoStore
			.Received(1)
			.PatchAsync(1, Arg.Any<Action<Todo>>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_WithNonExistingId_ReturnsNull()
	{
		// Arrange
		ITodoStore todoStore = Substitute.For<ITodoStore>();
		todoStore
			.PatchAsync(Arg.Any<int>(), Arg.Any<Action<Todo>>(), Arg.Any<CancellationToken>())
			.Returns((Todo?)null);

		PatchTodoEndpoint endpoint = new(todoStore);
		RequestModel request = new() { Id = 999, Body = new RequestBodyModel { Title = null, Description = null, IsCompleted = null } };

		// Act
		ResponseModel? result = await endpoint.Handle(request, CancellationToken.None);

		// Assert
		result.ShouldBeNull();
	}
}
