using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ExampleApi.Endpoints.Todos.Put;

public class RequestModel
{
    [FromRoute(Name = "id")]
    public int Id { get; set; }
    
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;
    
    public bool IsCompleted { get; set; }
}