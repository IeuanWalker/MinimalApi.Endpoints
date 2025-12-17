using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.FileHandling.ListOfFiles;

public class PostFileHandlingEndpoint : IEndpoint<IFormFileCollection, IEnumerable<ResponseModel>>
{
	public static void Configure(RouteHandlerBuilder builder)
	{
		builder
			.Post("api/v{version:apiVersion}/FileHandling/ListOfFiles")
			.RequestFromForm()
			.DisableAntiforgery();
	}

	public Task<IEnumerable<ResponseModel>> Handle(IFormFileCollection request, CancellationToken ct)
	{
		IEnumerable<ResponseModel> response = request.Select(x => new ResponseModel
		{
			FileName = x.FileName,
			PropertyName = x.Name,
			Size = (int)x.Length
		});

		return Task.FromResult(response);
	}
}
