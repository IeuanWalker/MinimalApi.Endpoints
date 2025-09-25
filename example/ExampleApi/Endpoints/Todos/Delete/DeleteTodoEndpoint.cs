using ExampleApi.Infrastructure;
using ExampleApi.Services;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Todos.Delete;

public class DeleteTodoEndpoint : IEndpointWithoutResponse<RequestModel>
{
	readonly ITodoStore _todoStore;

	public DeleteTodoEndpoint(ITodoStore todoStore)
	{
		_todoStore = todoStore;
	}

	public static void Configure(RouteHandlerBuilder builder)
	{
		builder
			.MapGroup<TodoEndpointGroup>()
			.Delete("/{id:int}")
			.RequestAsParameters()
			.WithSummary("Delete a todo")
			.WithDescription("Deletes an existing todo item")
			.Version(1.0);
	}

	public async Task HandleAsync(RequestModel request, CancellationToken ct)
	{
		await _todoStore.DeleteAsync(request.Id, ct);
	}
}
