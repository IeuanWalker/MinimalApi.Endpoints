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
				// String validation - required with length constraints
				config.Property(x => x.Title)
					.Description("The title of the todo item")
					.Required()
					.MinLength(1)
					.MaxLength(200);

				// Optional string with max length
				config.Property(x => x.Description)
					.Description("Optional detailed description of the task")
					.MaxLength(1000);

				// Email validation - required with format
				config.Property(x => x.Email)
					.Description("Contact email for notifications about this todo")
					.Required()
					.Email();

				// URL validation - optional with format
				config.Property(x => x.Url)
					.Url();

				// Phone number with pattern validation
				config.Property(x => x.PhoneNumber)
					.Pattern(@"^\+?[1-9]\d{1,14}$", "Phone number must be in E.164 format");

				// Numeric range validation - integer
				config.Property(x => x.Priority)
					.GreaterThanOrEqual(0)
					.LessThanOrEqual(10);

				// Decimal range validation (optional, so we use Custom)
				config.Property(x => x.Budget)
					.Custom("Budget must be greater than 0");

				// Double range validation with custom implementation for nullable
				config.Property(x => x.Rating)
					.Custom("Rating must be between 0 and 5");

				// Date validation with custom rule
				config.Property(x => x.DueDate)
					.Custom("Due date must be in the future");

				// DateTimeOffset validation
				config.Property(x => x.CreatedAt)
					.Custom("Created date cannot be in the future");
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
