using ExampleApi.Data;
using ExampleApi.Endpoints.Todos.Post;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ExampleApi.Tests.Endpoints.Todos;

public class PostTodoEndpointTests
{
	[Fact]
	public async Task Handle_ReturnsConflict_WhenTitleExists()
	{
		// Arrange
		ITodoStore todoStore = Substitute.For<ITodoStore>();
		Todo existing = new() { Id = 1, Title = "Exists", Description = "D", IsCompleted = false, CreatedAt = DateTime.UtcNow };
		todoStore
			.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(new[] { existing });

		PostTodoEndpoint endpoint = new(todoStore);
		RequestModel request = new() { Title = "Exists", Description = "D" };

		// Act
		Results<Ok<ResponseModel>, Conflict> result = await endpoint.Handle(request, CancellationToken.None);

		// Assert
		await Verify(result);

		await todoStore
			.DidNotReceive()
			.CreateAsync(Arg.Any<Todo>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_CreatesTodo_WhenTitleDoesNotExist()
	{
		// Arrange
		ITodoStore todoStore = Substitute.For<ITodoStore>();
		Todo created = new() { Id = 10, Title = "New", Description = "Desc", CreatedAt = DateTime.UtcNow };
		todoStore
			.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Array.Empty<Todo>());
		todoStore
			.CreateAsync(Arg.Any<Todo>(), Arg.Any<CancellationToken>())
			.Returns(created);

		PostTodoEndpoint endpoint = new(todoStore);
		RequestModel request = new() { Title = "New", Description = "Desc" };

		// Act
		_ = await endpoint.Handle(request, CancellationToken.None);

		// Assert
		await todoStore
			.Received(1)
			.CreateAsync(Arg.Any<Todo>(), Arg.Any<CancellationToken>());
	}
}
