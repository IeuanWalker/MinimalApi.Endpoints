using System.ComponentModel;

namespace ExampleApi.Data;

public enum TodoPriority
{
	[Description("Low priority task")]
	Low = 0,
	[Description("Medium priority task")]
	Medium = 1,
	[Description("High priority task")]
	High = 2,
	[Description("Critical priority task requiring immediate attention")]
	Critical = 3
}

public enum TodoStatus
{
	[Description("Task not yet started")]
	NotStarted,
	[Description("Task is currently being worked on")]
	InProgress,
	[Description("Task has been completed")]
	Completed,
	[Description("Task has been cancelled")]
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
