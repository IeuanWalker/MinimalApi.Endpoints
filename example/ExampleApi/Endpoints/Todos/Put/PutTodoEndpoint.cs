using System.Diagnostics.CodeAnalysis;
using ExampleApi.Data;
using ExampleApi.Infrastructure;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Todos.Put;

public class PutTodoEndpoint : IEndpoint<RequestModel, ResponseModel?>
{
	readonly ITodoStore _todoStore;

	public PutTodoEndpoint(ITodoStore todoStore)
	{
		_todoStore = todoStore;
	}

	[ExcludeFromCodeCoverage]
	public static void Configure(RouteHandlerBuilder builder)
	{
		builder
			.Group<TodoEndpointGroup>()
			.Put("/{id:int}")
			.RequestAsParameters()
			.WithSummary("Update a todo")
			.WithDescription("Updates an existing todo item completely")
			.Version(1.0);
	}

	public async Task<ResponseModel?> Handle(RequestModel request, CancellationToken ct)
	{
		Todo todo = new()
		{
			Title = request.Body.Title,
			Description = request.Body.Description,
			IsCompleted = request.Body.IsCompleted
		};

		Todo? updatedTodo = await _todoStore.UpdateAsync(request.Id, todo, ct);

		return updatedTodo is null
			? null
			: new ResponseModel
			{
				Id = todo.Id,
				Title = todo.Title,
				Description = todo.Description,
				IsCompleted = todo.IsCompleted,
				CreatedAt = todo.CreatedAt,
				UpdatedAt = todo.UpdatedAt
			};
	}
}
