using ExampleApi.Data;

namespace ExampleApi.Endpoints.Todos.Post;

public class ResponseModel
{
	public int Id { get; set; }
	public string Title { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public bool IsCompleted { get; set; }
	public TodoPriority Priority { get; set; }
	public TodoStatus Status { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime? UpdatedAt { get; set; }

	public static ResponseModel FromTodo(Todo todo)
	{
		return new ResponseModel
		{
			Id = todo.Id,
			Title = todo.Title,
			Description = todo.Description,
			IsCompleted = todo.IsCompleted,
			Priority = todo.Priority,
			Status = todo.Status,
			CreatedAt = todo.CreatedAt,
			UpdatedAt = todo.UpdatedAt
		};
	}
}
