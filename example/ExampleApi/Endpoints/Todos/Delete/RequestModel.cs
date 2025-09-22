using Microsoft.AspNetCore.Mvc;

namespace ExampleApi.Endpoints.Todos.Delete;

public class RequestModel
{
    [FromRoute(Name = "id")]
    public int Id { get; set; }
}