using ExampleApi.Data;

namespace ExampleApi.Endpoints.Todos.GetAll;

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
}