namespace ExampleApi.Endpoints.Todos.PostWithValidation;

public record RequestModel
{
	// String validations
	public string Title { get; set; } = string.Empty;
	public string? Description { get; set; }
	public string Email { get; set; } = string.Empty;
	public string? Url { get; set; }
	public string? PhoneNumber { get; set; }
	
	// Numeric validations
	public int Priority { get; set; }
	public decimal? Budget { get; set; }
	public double? Rating { get; set; }
	
	// Date/Time validations
	public DateTime? DueDate { get; set; }
	public DateTimeOffset? CreatedAt { get; set; }
	
	// Boolean
	public bool IsCompleted { get; set; }
	
	// Array validation
	public List<string>? Tags { get; set; }
	
	// Enum validation
	public TodoStatus? Status { get; set; }
}

public enum TodoStatus
{
	NotStarted,
	InProgress,
	Completed,
	Cancelled
}
