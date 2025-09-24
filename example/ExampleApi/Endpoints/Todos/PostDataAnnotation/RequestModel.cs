using System.ComponentModel.DataAnnotations;

namespace ExampleApi.Endpoints.Todos.PostDataAnnotation;

public class RequestModel
{
	[Required]
	[StringLength(200, MinimumLength = 1)]
	public string Title { get; set; } = string.Empty;

	[Required]
	[StringLength(1000)]
	public string Description { get; set; } = string.Empty;

	public bool IsCompleted { get; set; }
}
