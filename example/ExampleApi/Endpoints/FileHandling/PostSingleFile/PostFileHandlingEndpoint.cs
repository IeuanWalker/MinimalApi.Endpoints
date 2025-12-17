using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.FileHandling.PostSingleFile;

public class PostFileHandlingEndpoint : IEndpoint<IFormFile, ResponseModel>
{
	public static void Configure(RouteHandlerBuilder builder)
	{
		builder
			.Post("api/v{version:apiVersion}/FileHandling/SingleFile")
			.RequestFromForm()
			.DisableAntiforgery();
	}

	public Task<ResponseModel> Handle(IFormFile request, CancellationToken ct)
	{
		ResponseModel response = new()
		{
			FileName = request.FileName,
			PropertyName = request.Name,
			Size = (int)request.Length,
		};

		return Task.FromResult(response);
	}
}
