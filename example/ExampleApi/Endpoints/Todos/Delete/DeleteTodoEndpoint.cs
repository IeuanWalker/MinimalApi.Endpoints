using System.Diagnostics.CodeAnalysis;
using ExampleApi.Data;
using ExampleApi.Infrastructure;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Todos.Delete;

public class DeleteTodoEndpoint : IEndpointWithoutResponse<RequestModel>
{
	readonly ITodoStore _todoStore;

	public DeleteTodoEndpoint(ITodoStore todoStore)
	{
		_todoStore = todoStore;
	}

	[ExcludeFromCodeCoverage]
	public static void Configure(RouteHandlerBuilder builder)
	{
		builder
			.Group<TodoEndpointGroup>()
			.Delete("/{id:int}")
			.RequestAsParameters()
			.WithSummary("Delete a todo")
			.WithDescription("Deletes an existing todo item")
			.Version(1.0);
	}

	public async Task Handle(RequestModel request, CancellationToken ct)
	{
		await _todoStore.DeleteAsync(request.Id, ct);
	}
}
