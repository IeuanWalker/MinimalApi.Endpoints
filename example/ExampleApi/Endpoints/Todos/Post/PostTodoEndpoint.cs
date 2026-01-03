using System.Diagnostics.CodeAnalysis;
using ExampleApi.Data;
using ExampleApi.Infrastructure;
using IeuanWalker.MinimalApi.Endpoints;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ExampleApi.Endpoints.Todos.Post;

public class PostTodoEndpoint : IEndpoint<RequestModel, Results<Ok<ResponseModel>, Conflict>>
{
	readonly ITodoStore _todoStore;

	public PostTodoEndpoint(ITodoStore todoStore)
	{
		_todoStore = todoStore;
	}

	[ExcludeFromCodeCoverage]
	public static void Configure(RouteHandlerBuilder builder)
	{
		builder
			.Group<TodoEndpointGroup>()
			.Post("")
			.RequestFromBody()
			.WithSummary("Create a new todo")
			.WithDescription("Creates a new todo item")
			.Version(1.0);
	}

	public async Task<Results<Ok<ResponseModel>, Conflict>> Handle(RequestModel request, CancellationToken ct)
	{
		if ((await _todoStore.GetAllAsync(ct)).Any(x => x.Title.Equals(request.Title, StringComparison.InvariantCultureIgnoreCase)))
		{
			return TypedResults.Conflict();
		}

		Todo todo = new()
		{
			Title = request.Title,
			Description = request.Description
		};

		Todo createdTodo = await _todoStore.CreateAsync(todo, ct);

		return TypedResults.Ok(ResponseModel.FromTodo(createdTodo));
	}
}
