using System.Diagnostics.CodeAnalysis;
using ExampleApi.Data;
using ExampleApi.Infrastructure;
using IeuanWalker.MinimalApi.Endpoints;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ExampleApi.Endpoints.Todos.Put;

public class PutTodoEndpoint : IEndpoint<RequestModel, Results<Ok<ResponseModel>, Conflict>>
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

	public async Task<Results<Ok<ResponseModel>, Conflict>> Handle(RequestModel request, CancellationToken ct)
	{
		Todo todo = new()
		{
			Title = request.Body.Title,
			Description = request.Body.Description,
			IsCompleted = request.Body.IsCompleted
		};

		Todo? updatedTodo = await _todoStore.UpdateAsync(request.Id, todo, ct);

		if (updatedTodo is null)
		{
			return TypedResults.Conflict();
		}

		return TypedResults.Ok(new ResponseModel
		{
			Id = updatedTodo.Id,
			Title = updatedTodo.Title,
			Description = updatedTodo.Description,
			IsCompleted = updatedTodo.IsCompleted,
			CreatedAt = updatedTodo.CreatedAt,
			UpdatedAt = updatedTodo.UpdatedAt
		});
	}
}
