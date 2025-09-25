﻿using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Todos;

public class TodoEndpointGroup : IEndpointGroup
{
	public static RouteGroupBuilder Configure(WebApplication app)
	{
		return app.MapGroup("api/v{version:apiVersion}/todos");
	}
}
