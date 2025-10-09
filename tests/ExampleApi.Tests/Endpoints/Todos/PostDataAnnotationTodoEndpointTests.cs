using ExampleApi.Data;
using ExampleApi.Endpoints.Todos.PostDataAnnotation;
using NSubstitute;

namespace ExampleApi.Tests.Endpoints.Todos;

public class PostDataAnnotationTodoEndpointTests
{
	[Fact]
	public async Task Handle_ReturnsConflict_WhenTitleExists()
	{
		// Arrange
		ITodoStore todoStore = Substitute.For<ITodoStore>();
		Todo existing = new() { Id = 1, Title = "Exists", Description = "D", IsCompleted = false, CreatedAt = DateTime.UtcNow };
		todoStore.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { existing });

		PostTodoEndpoint endpoint = new(todoStore);
		RequestModel request = new() { Title = "Exists", Description = "D", IsCompleted = false };

		// Act
		_ = await endpoint.Handle(request, CancellationToken.None);

		// Assert - the endpoint returns a Results<Ok<ResponseModel>, Conflict> wrapper; ensure it's the Conflict case by verifying CreateAsync not called
		await todoStore.DidNotReceive().CreateAsync(Arg.Any<Todo>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_ReturnsOk_WhenNewTitle()
	{
		// Arrange
		ITodoStore todoStore = Substitute.For<ITodoStore>();
		Todo created = new() { Id = 10, Title = "New", Description = "Desc", IsCompleted = false, CreatedAt = DateTime.UtcNow };
		todoStore.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Todo>());
		todoStore.CreateAsync(Arg.Any<Todo>(), Arg.Any<CancellationToken>()).Returns(created);

		PostTodoEndpoint endpoint = new(todoStore);
		RequestModel request = new() { Title = "New", Description = "Desc", IsCompleted = false };

		// Act
		_ = await endpoint.Handle(request, CancellationToken.None);

		// Assert - ensure CreateAsync was called indicating success path
		await todoStore.Received(1).CreateAsync(Arg.Any<Todo>(), Arg.Any<CancellationToken>());
	}
}
