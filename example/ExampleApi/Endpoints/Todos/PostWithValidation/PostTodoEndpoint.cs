using System.Diagnostics.CodeAnalysis;
using ExampleApi.Data;
using ExampleApi.Infrastructure;
using IeuanWalker.MinimalApi.Endpoints;
using IeuanWalker.MinimalApi.Endpoints.Validation;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ExampleApi.Endpoints.Todos.PostWithValidation;

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
			.Post("/WithValidation")
			.RequestFromBody()
			.WithSummary("Create a new todo with declarative validation")
			.WithDescription("Creates a new todo item using the WithValidation extension method for declarative validation rules")
			.Version(1.0)
			.WithValidation<RequestModel>(config =>
			{
				// String validation
				config.Property(x => x.Title)
					.Required()
					.MinLength(1)
					.MaxLength(200);

				// Optional string with max length
				config.Property(x => x.Description)
					.MaxLength(1000);

				// Email validation
				config.Property(x => x.Email)
					.Required()
					.Email();

				// Numeric range validation
				config.Property(x => x.Priority)
					.GreaterThanOrEqual(0)
					.LessThanOrEqual(10);

			// Date validation with custom rule
				config.Property(x => x.DueDate)
					.Custom(
						dueDate => dueDate == null || dueDate.Value > DateTime.Now,
						"Due date must be in the future");

				// Cross-field validation
				config.CrossField(request =>
				{
					Dictionary<string, string[]> errors = [];

					// Validate that high priority items have a due date
					if (request.Priority >= 8 && request.DueDate == null)
					{
						errors["DueDate"] = ["High priority items (8+) must have a due date"];
					}

					return errors;
				});
			});
	}

	public async Task<Results<Ok<ResponseModel>, Conflict>> Handle(RequestModel request, CancellationToken ct)
	{
		// Check for duplicate title
		if ((await _todoStore.GetAllAsync(ct)).Any(x => x.Title.Equals(request.Title, StringComparison.InvariantCultureIgnoreCase)))
		{
			return TypedResults.Conflict();
		}

		Todo todo = new()
		{
			Title = request.Title,
			Description = request.Description ?? string.Empty,
			IsCompleted = request.IsCompleted
		};

		Todo createdTodo = await _todoStore.CreateAsync(todo, ct);

		return TypedResults.Ok(ResponseModel.FromTodo(createdTodo));
	}
}
