using IeuanWalker.MinimalApi.Endpoints;
using Microsoft.AspNetCore.Http.HttpResults;

namespace $rootnamespace$;

$if$ ($withRequest$ == true && $withResponse$ == true)public class $fileinputname$Endpoint : IEndpoint<RequestModel, Results<Ok<ResponseModel>, NotFound>>$endif$
$if$ ($withRequest$ == true && $withResponse$ == false)public class $fileinputname$Endpoint : IEndpointWithoutResponse<RequestModel>$endif$
$if$ ($withRequest$ == false && $withResponse$ == true)public class $fileinputname$Endpoint : IEndpointWithoutRequest<Results<Ok<ResponseModel>, NotFound>>$endif$
$if$ ($withRequest$ == false && $withResponse$ == false)public class $fileinputname$Endpoint : IEndpoint$endif$
{
	// Add dependencies via constructor injection
	// Example: readonly IMyService _myService;

	// public $fileinputname$Endpoint(IMyService myService)
	// {
	//     _myService = myService;
	// }

	public static void Configure(RouteHandlerBuilder builder)
	{
		builder
$if$ ($httpVerb$ == Get)			.Get("$route$")$endif$
$if$ ($httpVerb$ == Post)			.Post("$route$")$endif$
$if$ ($httpVerb$ == Put)			.Put("$route$")$endif$
$if$ ($httpVerb$ == Delete)			.Delete("$route$")$endif$
$if$ ($httpVerb$ == Patch)			.Patch("$route$")$endif$
			.WithName("$fileinputname$")
			.WithSummary("Summary for $fileinputname$")
			.WithDescription("Detailed description for $fileinputname$");
	}

$if$ ($withRequest$ == true && $withResponse$ == true)	public async Task<Results<Ok<ResponseModel>, NotFound>> Handle(RequestModel request, CancellationToken ct)$endif$
$if$ ($withRequest$ == true && $withResponse$ == false)	public async Task Handle(RequestModel request, CancellationToken ct)$endif$
$if$ ($withRequest$ == false && $withResponse$ == true)	public async Task<Results<Ok<ResponseModel>, NotFound>> Handle(CancellationToken ct)$endif$
$if$ ($withRequest$ == false && $withResponse$ == false)	public async Task Handle(CancellationToken ct)$endif$
	{
		// TODO: Implement endpoint logic
		throw new NotImplementedException();

		// Example success response:
		// return TypedResults.Ok(new ResponseModel { /* properties */ });

		// Example not found response:
		// return TypedResults.NotFound();
	}
}
