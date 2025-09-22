using ExampleApi.Infrastructure;
using ExampleApi.Models;
using ExampleApi.Services;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Todos.GetAll;

public class GetAllTodosEndpoint : IEndpointWithoutRequest<ResponseModel[]>
{
    private readonly ITodoStore _todoStore;

    public GetAllTodosEndpoint(ITodoStore todoStore)
    {
        _todoStore = todoStore;
    }

    public static void Configure(RouteHandlerBuilder builder)
    {
        builder
            .Get("/api/v{version:apiVersion}/todos")
            .WithSummary("Get all todos")
            .WithDescription("Retrieves all todos from the store")
            .Version(1.0);
    }

    public async Task<ResponseModel[]> HandleAsync(CancellationToken ct)
    {
        IEnumerable<Todo> result = await _todoStore.GetAllAsync(ct);

        return [.. result.Select(x => new ResponseModel
        {
            Id = x.Id,
            Title = x.Title,
            Description = x.Description,
            IsCompleted = x.IsCompleted,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        })];
    }
}