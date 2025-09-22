using ExampleApi.Infrastructure;
using ExampleApi.Models;
using ExampleApi.Services;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Todos.Patch;

public class PatchTodoEndpoint : IEndpoint<RequestModel, ResponseModel?>
{
    private readonly ITodoStore _todoStore;

    public PatchTodoEndpoint(ITodoStore todoStore)
    {
        _todoStore = todoStore;
    }

    public static void Configure(RouteHandlerBuilder builder)
    {
        builder
            .Patch("/api/v{version:apiVersion}/todos/{id:int}")
            .WithSummary("Partially update a todo")
            .WithDescription("Updates specific fields of an existing todo item")
            .Version(1.0);
    }

    public async Task<ResponseModel?> HandleAsync(RequestModel request, CancellationToken ct)
    {
        Todo? updatedTodo = await _todoStore.PatchAsync(request.Id, todo =>
        {
            if(request.Title is not null)
                todo.Title = request.Title;

            if(request.Description is not null)
                todo.Description = request.Description;

            if(request.IsCompleted.HasValue)
                todo.IsCompleted = request.IsCompleted.Value;
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