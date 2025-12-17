namespace ExampleApi.Endpoints.FileHandling.PostMultipart;

public class RequestModel
{
	public required string SomeData { get; set; }
	public IFormFile SingleFile { get; set; } = null!;
	public required IReadOnlyList<IFormFile> ReadOnlyList1 { get; set; }
	public required IReadOnlyList<IFormFile> ReadOnlyList2 { get; set; }
	/// <summary>
	/// Important: IFormFileCollection doesnt respect property names and binds all files in the request
	/// IFormFile and IReadOnlyList respects property names and only binds the files relevant to its property name
	/// https://github.com/dotnet/aspnetcore/issues/54999
	/// </summary>
	public required IFormFileCollection FileCollectionList { get; set; }
}
