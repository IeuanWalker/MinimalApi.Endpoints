using ExampleApi.Data;
using ExampleApi.Endpoints.Todos.GetExport;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ExampleApi.Tests.Endpoints.Todos;

public class GetExportEndpointTests
{
	[Fact]
	public async Task Handle_ReturnsNoContent_WhenNoTodos()
	{
		// Arrange
		ITodoStore todoStore = Substitute.For<ITodoStore>();
		todoStore
			.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Array.Empty<Todo>());

		GetExportEndpoint endpoint = new(todoStore);

		// Act
		Results<FileContentHttpResult, NoContent> result = await endpoint.Handle(CancellationToken.None);

		// Assert - result is a Results<FileContentHttpResult, NoContent> wrapper; check for NoContent
		await Verify(result);
	}

	[Fact]
	public async Task Handle_ReturnsFile_WhenTodosExist()
	{
		// Arrange
		ITodoStore todoStore = Substitute.For<ITodoStore>();
		Todo t = new() { Id = 1, Title = "T", Description = "D", IsCompleted = false, CreatedAt = DateTime.UtcNow };
		todoStore
			.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(new[] { t });

		GetExportEndpoint endpoint = new(todoStore);

		// Act
		Results<FileContentHttpResult, NoContent> result = await endpoint.Handle(CancellationToken.None);

		// Assert - ensure File result case
		await Verify(result);
	}
}
