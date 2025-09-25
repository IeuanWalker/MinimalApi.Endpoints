using ExampleApi.Infrastructure;
using ExampleApi.Models;
using ExampleApi.Services;
using IeuanWalker.MinimalApi.Endpoints;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ExampleApi.Endpoints.Todos.PostFluentValidation;

public class PostTodoEndpoint : IEndpoint<RequestModel, Results<Ok<ResponseModel>, Conflict>>
{
	readonly ITodoStore _todoStore;

	public PostTodoEndpoint(ITodoStore todoStore)
	{
		_todoStore = todoStore;
	}

	public static void Configure(RouteHandlerBuilder builder)
	{
		builder
			.MapGroup<TodoEndpointGroup>()
			.Post("/FluentValidation")
			.RequestFromBody()
			.WithSummary("Create a new todo")
			.WithDescription("Creates a new todo item")
			.Version(1.0);
	}

	public async Task<Results<Ok<ResponseModel>, Conflict>> HandleAsync(RequestModel request, CancellationToken ct)
	{
		if ((await _todoStore.GetAllAsync(ct)).Any(x => x.Title.Equals(request.Title, StringComparison.InvariantCultureIgnoreCase)))
		{
			return TypedResults.Conflict();
		}

		Todo todo = new()
		{
			Title = request.Title,
			Description = request.Description,
			IsCompleted = request.IsCompleted
		};

		Todo createdTodo = await _todoStore.CreateAsync(todo, ct);

		return TypedResults.Ok(ResponseModel.FromTodo(createdTodo));
	}
}
