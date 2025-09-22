using ExampleApi.Infrastructure;
using ExampleApi.Models;
using ExampleApi.Services;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Todos.Put;

public class PutTodoEndpoint : IEndpoint<RequestModel, ResponseModel?>
{
    private readonly ITodoStore _todoStore;

    public PutTodoEndpoint(ITodoStore todoStore)
    {
        _todoStore = todoStore;
    }

    public static void Configure(RouteHandlerBuilder builder)
    {
        builder
            .Put("/api/v{version:apiVersion}/todos/{id:int}")
            .RequestAsParameters()
            .WithSummary("Update a todo")
            .WithDescription("Updates an existing todo item completely")
            .Version(1.0);
    }

    public async Task<ResponseModel?> HandleAsync(RequestModel request, CancellationToken ct)
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