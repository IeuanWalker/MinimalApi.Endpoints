using ExampleApi.Infrastructure;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.FileHandling.PostListOfFiles;

public class PostFileHandlingListOfFilesEndpoint : IEndpoint<IFormFileCollection, IEnumerable<ResponseModel>>
{
	public static void Configure(RouteHandlerBuilder builder)
	{
		builder
			.Post("api/v{version:apiVersion}/FileHandling/ListOfFiles")
			.Version(1)
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
