using System.Diagnostics.CodeAnalysis;
using ExampleApi.Infrastructure;
using ExampleApi.Models;
using ExampleApi.Services;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Todos.GetById;

public class GetTodoByIdEndpoint : IEndpoint<RequestModel, ResponseModel?>
{
	readonly ITodoStore _todoStore;

	public GetTodoByIdEndpoint(ITodoStore todoStore)
	{
		_todoStore = todoStore;
	}

	[ExcludeFromCodeCoverage]
	public static void Configure(RouteHandlerBuilder builder)
	{
		builder
			.Group<TodoEndpointGroup>()
			.Get("/{id:int}")
			.RequestAsParameters()
			.WithSummary("Get todo by ID")
			.WithDescription("Retrieves a specific todo by its ID")
			.Version(1.0);
	}

	public async Task<ResponseModel?> Handle(RequestModel request, CancellationToken ct)
	{
		Todo? result = await _todoStore.GetByIdAsync(request.Id, ct);

		return result is null
			? null
			: new ResponseModel
			{
				Id = result.Id,
				Title = result.Title,
				Description = result.Description,
				IsCompleted = result.IsCompleted,
				CreatedAt = result.CreatedAt,
				UpdatedAt = result.UpdatedAt
			};
	}
}
