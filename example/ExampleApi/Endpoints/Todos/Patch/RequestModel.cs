using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ExampleApi.Endpoints.Todos.Patch;

public class RequestModel
{
    [FromRoute(Name = "id")]
    public int Id { get; set; }
    
    [StringLength(200, MinimumLength = 1)]
    public string? Title { get; set; }
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    public bool? IsCompleted { get; set; }
}