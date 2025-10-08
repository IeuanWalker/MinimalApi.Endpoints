using System.Diagnostics.CodeAnalysis;
using ExampleApi.Data;
using ExampleApi.Infrastructure;
using IeuanWalker.MinimalApi.Endpoints;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ExampleApi.Endpoints.Todos.GetById;

public class GetTodoByIdEndpoint : IEndpoint<RequestModel, Results<Ok<ResponseModel>, NoContent>>
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

	public async Task<Results<Ok<ResponseModel>, NoContent>> Handle(RequestModel request, CancellationToken ct)
	{
		Todo? result = await _todoStore.GetByIdAsync(request.Id, ct);

		if (result is null)
		{
			return TypedResults.NoContent();
		}

		return TypedResults.Ok(new ResponseModel
		{
			Id = result.Id,
			Title = result.Title,
			Description = result.Description,
			IsCompleted = result.IsCompleted,
			CreatedAt = result.CreatedAt,
			UpdatedAt = result.UpdatedAt
		});
	}
}
