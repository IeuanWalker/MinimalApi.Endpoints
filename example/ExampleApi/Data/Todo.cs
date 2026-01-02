namespace ExampleApi.Data;

public enum TodoPriority
{
	Low = 0,
	Medium = 1,
	High = 2,
	Critical = 3
}

public enum TodoStatus
{
	NotStarted,
	InProgress,
	Completed,
	Cancelled
}

public class Todo
{
	public int Id { get; set; }
	public string Title { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public bool IsCompleted { get; set; }
	public TodoPriority Priority { get; set; } = TodoPriority.Low;
	public TodoStatus Status { get; set; } = TodoStatus.NotStarted;
	public DateTime CreatedAt { get; set; }
	public DateTime? UpdatedAt { get; set; }
}
