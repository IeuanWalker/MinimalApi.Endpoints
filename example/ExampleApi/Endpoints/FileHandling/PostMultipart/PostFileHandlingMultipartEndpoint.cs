using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.FileHandling.PostMultipart;

public class PostFileHandlingMultipartEndpoint : IEndpoint<RequestModel, ResponseModel>
{
	readonly IHttpContextAccessor _context;
	public PostFileHandlingMultipartEndpoint(IHttpContextAccessor context)
	{
		_context = context ?? throw new ArgumentNullException(nameof(context));
	}
	public static void Configure(RouteHandlerBuilder builder)
	{
		builder
			.Post("api/v{version:apiVersion}/FileHandling/Multipart")
			.RequestFromForm()
			.DisableAntiforgery();
	}

	public Task<ResponseModel> Handle(RequestModel request, CancellationToken ct)
	{
		ResponseModel response = new()
		{
			SomeData = request.SomeData,
			TotalFileCount = _context.HttpContext!.Request.Form.Files.Count,
			SingleFile = MapFile(request.SingleFile),
			ReadOnlyList1 = [.. request.ReadOnlyList1.Select(MapFile)],
			ReadOnlyList2 = request.ReadOnlyList2?.Select(MapFile).ToList() ?? [],
			FileCollectionList = [.. request.FileCollectionList.Select(MapFile)],
		};

		return Task.FromResult(response);
	}

	static FileInfo MapFile(IFormFile formFile)
	{
		return new FileInfo
		{
			FileName = formFile.FileName,
			PropertyName = formFile.Name,
			Size = (int)formFile.Length,
		};
	}
}
