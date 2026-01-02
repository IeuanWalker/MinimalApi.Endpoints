using System.ComponentModel.DataAnnotations;
using ExampleApi.Data;
using Microsoft.Extensions.Validation;

namespace ExampleApi.Endpoints.Todos.PostDataAnnotation;

#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
[ValidatableType]
#pragma warning restore ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
public class RequestModel
{
	[Required]
	[StringLength(200, MinimumLength = 1)]
	public string Title { get; set; } = string.Empty;

	[Required]
	[StringLength(1000)]
	public string Description { get; set; } = string.Empty;

	public bool IsCompleted { get; set; }
	
	public TodoPriority Priority { get; set; } = TodoPriority.Low;
	
	public TodoStatus Status { get; set; } = TodoStatus.NotStarted;
}
