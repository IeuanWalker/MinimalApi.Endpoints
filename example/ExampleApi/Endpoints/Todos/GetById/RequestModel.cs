using Microsoft.AspNetCore.Mvc;

namespace ExampleApi.Endpoints.Todos.GetById;

public class RequestModel
{
	[FromRoute(Name = "id")]
	public int Id { get; set; }
}