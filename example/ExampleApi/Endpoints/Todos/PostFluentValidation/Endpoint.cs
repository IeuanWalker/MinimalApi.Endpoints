using FluentValidation;
using FluentValidation.Results;
using IeuanWalker.MinimalApi.Endpoints;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ExampleApi.Endpoints.Todos.PostFluentValidation;

public class Endpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapPost("/todos/fluent-validation", ExecuteAsync)
			.WithName("CreateTodoWithFluentValidation")
			.WithSummary("Create a new todo with FluentValidation")
			.WithDescription("Creates a new todo item with comprehensive validation using FluentValidation rules")
			.WithTags("Todos")
			.WithFluentValidationSchema<RequestModel>() // This will automatically generate OpenAPI schema from validation rules
			.WithResponse<TodoResponse>(201, "Todo created successfully")
			.WithResponse(400, "Validation failed")
			.WithResponse(500, "Internal server error");
	}

	private static async Task<Results<Created<TodoResponse>, ValidationProblem>> ExecuteAsync(
		RequestModel request,
		IValidator<RequestModel> validator)
	{
		// Validate the request
		ValidationResult validationResult = await validator.ValidateAsync(request);
		if (!validationResult.IsValid)
		{
			return TypedResults.ValidationProblem(validationResult.ToDictionary());
		}

		// Process the request (mock implementation)
		TodoResponse todo = new TodoResponse
		{
			Id = Guid.NewGuid(),
			Title = request.Title,
			Description = request.Description,
			IsCompleted = request.IsCompleted,
			CreatedAt = DateTime.UtcNow
		};

		return TypedResults.Created($"/todos/{todo.Id}", todo);
	}
}

public class TodoResponse
{
	public Guid Id { get; set; }
	public string Title { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public bool IsCompleted { get; set; }
	public DateTime CreatedAt { get; set; }
}
