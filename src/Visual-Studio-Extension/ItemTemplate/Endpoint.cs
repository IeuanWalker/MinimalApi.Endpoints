using IeuanWalker.MinimalApi.Endpoints;
using Microsoft.AspNetCore.Http.HttpResults;

namespace $rootnamespace$;

#if (withRequest && withResponse)
public class $fileinputname$ : IEndpoint<RequestModel, Results<Ok<ResponseModel>, NotFound>>
#endif
#if (withRequest && !withResponse)
public class $fileinputname$ : IEndpointWithoutResponse<RequestModel>
#endif
#if (!withRequest && withResponse)
public class $fileinputname$ : IEndpointWithoutRequest<Results<Ok<ResponseModel>, NotFound>>
#endif
#if (!withRequest && !withResponse)
public class $fileinputname$ : IEndpoint
#endif
{
	// Add dependencies via constructor injection
	// Example: readonly IMyService _myService;

	// public $fileinputname$(IMyService myService)
	// {
	//     _myService = myService;
	// }

	public static void Configure(RouteHandlerBuilder builder)
	{
		builder
#if (httpVerb == "Get")
			.Get("/route-path")
#endif
#if (httpVerb == "Post")
			.Post("/route-path")
#endif
#if (httpVerb == "Put")
			.Put("/route-path")
#endif
#if (httpVerb == "Delete")
			.Delete("/route-path")
#endif
#if (httpVerb == "Patch")
			.Patch("/route-path")
#endif
			.WithName("$fileinputname$")
			.WithSummary("Summary for $fileinputname$")
			.WithDescription("Detailed description for $fileinputname$");
	}

#if (withRequest && withResponse)
	public async Task<Results<Ok<ResponseModel>, NotFound>> Handle(RequestModel request, CancellationToken ct)
#endif
#if (withRequest && !withResponse)
	public async Task Handle(RequestModel request, CancellationToken ct)
#endif
#if (!withRequest && withResponse)
	public async Task<Results<Ok<ResponseModel>, NotFound>> Handle(CancellationToken ct)
#endif
#if (!withRequest && !withResponse)
	public async Task Handle(CancellationToken ct)
#endif
	{
		// TODO: Implement endpoint logic
		throw new NotImplementedException();

		// Example success response:
		// return TypedResults.Ok(new ResponseModel { /* properties */ });

		// Example not found response:
		// return TypedResults.NotFound();
	}
}
