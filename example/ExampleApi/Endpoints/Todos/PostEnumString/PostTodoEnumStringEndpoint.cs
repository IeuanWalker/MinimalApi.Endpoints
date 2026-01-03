using ExampleApi.Data;
using IeuanWalker.MinimalApi.Endpoints;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ExampleApi.Endpoints.Todos.PostEnumString;

public class PostTodoEnumStringEndpoint : IEndpoint<RequestModel, Results<Ok<ResponseModel>, Conflict>>
{
	readonly ITodoStore _todoStore;

	public PostTodoEnumStringEndpoint(ITodoStore todoStore)
	{
		_todoStore = todoStore;
	}

	public static void Configure(RouteHandlerBuilder builder)
	{
		builder
			.Group<TodoEndpointGroup>()
			.Post("/EnumString")
			.WithName("PostTodoEnumString")
			.WithSummary("Create a new todo using enum strings/ints")
			.WithDescription("Demonstrates FluentValidation IsEnumName and IsInEnum validators with OpenAPI enum enrichment");
	}

	public async Task<Results<Ok<ResponseModel>, Conflict>> Handle(RequestModel request, CancellationToken cancellationToken)
	{
		// Parse the priority name to enum
		if (!Enum.TryParse<TodoPriority>(request.PriorityNameAsString, ignoreCase: true, out TodoPriority priority))
		{
			return TypedResults.Conflict();
		}

		// Parse the status name to enum
		if (!Enum.TryParse<TodoStatus>(request.StatusName, ignoreCase: true, out TodoStatus status))
		{
			return TypedResults.Conflict();
		}

		var todo = new Todo
		{
			Title = request.Title,
			Description = $"Created with priority: {priority}, status: {status}",
			Priority = priority,
			Status = status,
			IsCompleted = status == TodoStatus.Completed
		};

		Todo created = await _todoStore.CreateAsync(todo, cancellationToken);
		return TypedResults.Ok(ResponseModel.FromTodo(created));
	}
}
