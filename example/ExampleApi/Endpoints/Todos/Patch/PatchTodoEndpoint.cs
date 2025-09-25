using ExampleApi.Infrastructure;
using ExampleApi.Models;
using ExampleApi.Services;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Todos.Patch;

public class PatchTodoEndpoint : IEndpoint<RequestModel, ResponseModel?>
{
	readonly ITodoStore _todoStore;

	public PatchTodoEndpoint(ITodoStore todoStore)
	{
		_todoStore = todoStore;
	}

	public static void Configure(RouteHandlerBuilder builder)
	{
		builder
			.MapGroup<TodoEndpointGroup>()
			.Patch("/{id:int}")
			.RequestAsParameters()
			.WithSummary("Partially update a todo")
			.WithDescription("Updates specific fields of an existing todo item")
			.Version(1.0);
	}

	public async Task<ResponseModel?> HandleAsync(RequestModel request, CancellationToken ct)
	{
		Todo? updatedTodo = await _todoStore.PatchAsync(request.Id, todo =>
		{
			if (request.Body.Title is not null)
			{
				todo.Title = request.Body.Title;
			}

			if (request.Body.Description is not null)
			{
				todo.Description = request.Body.Description;
			}

			if (request.Body.IsCompleted.HasValue)
			{
				todo.IsCompleted = request.Body.IsCompleted.Value;
			}
		}, ct);

		return updatedTodo is null
			? null
			: new ResponseModel
			{
				Id = updatedTodo.Id,
				Title = updatedTodo.Title,
				Description = updatedTodo.Description,
				IsCompleted = updatedTodo.IsCompleted,
				CreatedAt = updatedTodo.CreatedAt,
				UpdatedAt = updatedTodo.UpdatedAt
			};
	}
}
