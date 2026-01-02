using ExampleApi.Infrastructure;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.FileHandling.PostSingleFile;

public class PostFileHandlingSingleFileEndpoint : IEndpoint<IFormFile, ResponseModel>
{
	public static void Configure(RouteHandlerBuilder builder)
	{
		builder
			.Post("api/v{version:apiVersion}/FileHandling/SingleFile")
			.Version(1)
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
