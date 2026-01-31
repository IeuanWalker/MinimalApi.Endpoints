using ExampleApi.Data;
using ExampleApi.Endpoints.Todos.Put;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ExampleApi.Tests.Endpoints.Todos;

public class PutTodoEndpointTests
{
	[Fact]
	public async Task Handle_WithExistingIdAndValidData_CallsUpdateAndReturnsOk()
	{
		// Arrange
		ITodoStore todoStore = Substitute.For<ITodoStore>();
		Todo updated = new()
		{
			Id = 5,
			Title = "Updated",
			Description = "Updated Desc",
			IsCompleted = true,
			CreatedAt = DateTime.UtcNow.AddDays(-1),
			UpdatedAt = DateTime.UtcNow
		};

		todoStore
			.UpdateAsync(5, Arg.Any<Todo>(), Arg.Any<CancellationToken>())
			.Returns(updated);

		PutTodoEndpoint endpoint = new(todoStore);
		RequestModel request = new() { Id = 5, Body = new RequestBodyModel { Title = "Updated", Description = "Updated Desc", IsCompleted = true } };

		// Act
		Results<Ok<ResponseModel>, Conflict> result = await endpoint.Handle(request, CancellationToken.None);

		// Assert
		await Verify(result);

		await todoStore
			.Received(1)
			.UpdateAsync(5, Arg.Is<Todo>(t => t.Title == "Updated" && t.Description == "Updated Desc" && t.IsCompleted), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_WithNonExistingId_ReturnsNotFound()
	{
		// Arrange
		ITodoStore todoStore = Substitute.For<ITodoStore>();
		todoStore
			.UpdateAsync(Arg.Any<int>(), Arg.Any<Todo>(), Arg.Any<CancellationToken>())
			.Returns((Todo?)null);

		PutTodoEndpoint endpoint = new(todoStore);
		RequestModel request = new() { Id = 999, Body = new RequestBodyModel { Title = "Missing", Description = "N/A", IsCompleted = false } };

		// Act
		Results<Ok<ResponseModel>, Conflict> result = await endpoint.Handle(request, CancellationToken.None);

		// Assert
		await Verify(result);
	}
}
