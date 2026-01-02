using ExampleApi.Data;
using FluentValidation;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Todos.PostEnumString;

public class RequestModel
{
	public string Title { get; set; } = string.Empty;
	public string PriorityName { get; set; } = string.Empty;
	public string StatusName { get; set; } = string.Empty;
}

public class RequestModelValidator : Validator<RequestModel>
{
	public RequestModelValidator()
	{
		RuleFor(x => x.Title)
			.NotEmpty()
			.MaximumLength(200);

		RuleFor(x => x.PriorityName)
			.NotEmpty()
			.IsEnumName(typeof(TodoPriority), caseSensitive: false);

		RuleFor(x => x.StatusName)
			.NotEmpty()
			.IsEnumName(typeof(TodoStatus), caseSensitive: false);
	}
}

public class ResponseModel
{
	public int Id { get; set; }
	public string Title { get; set; } = string.Empty;
	public TodoPriority Priority { get; set; }
	public TodoStatus Status { get; set; }
	public DateTime CreatedAt { get; set; }

	public static ResponseModel FromTodo(Todo todo)
	{
		return new ResponseModel
		{
			Id = todo.Id,
			Title = todo.Title,
			Priority = todo.Priority,
			Status = todo.Status,
			CreatedAt = todo.CreatedAt
		};
	}
}
