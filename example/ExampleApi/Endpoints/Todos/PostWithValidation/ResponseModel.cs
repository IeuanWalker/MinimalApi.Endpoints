using ExampleApi.Data;

namespace ExampleApi.Endpoints.Todos.PostWithValidation;

public record ResponseModel
{
	public int Id { get; set; }
	public string Title { get; set; } = string.Empty;
	public string? Description { get; set; }
	public string Email { get; set; } = string.Empty;
	public int Priority { get; set; }
	public DateTime? DueDate { get; set; }
	public bool IsCompleted { get; set; }
	public DateTime CreatedAt { get; set; }

	public static ResponseModel FromTodo(Todo todo) => new()
	{
		Id = todo.Id,
		Title = todo.Title,
		Description = todo.Description,
		Email = string.Empty, // Not stored in Todo
		Priority = 0, // Not stored in Todo
		DueDate = null, // Not stored in Todo
		IsCompleted = todo.IsCompleted,
		CreatedAt = todo.CreatedAt
	};
}
