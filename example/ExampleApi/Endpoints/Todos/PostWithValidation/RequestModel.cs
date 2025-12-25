namespace ExampleApi.Endpoints.Todos.PostWithValidation;

public record RequestModel
{
	public string Title { get; set; } = string.Empty;
	public string? Description { get; set; }
	public string Email { get; set; } = string.Empty;
	public int Priority { get; set; }
	public DateTime? DueDate { get; set; }
	public bool IsCompleted { get; set; }
}
