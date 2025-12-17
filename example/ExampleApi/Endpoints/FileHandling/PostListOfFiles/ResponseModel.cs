namespace ExampleApi.Endpoints.FileHandling.PostListOfFiles;

public sealed class ResponseModel
{
	public required string FileName { get; set; }
	public required string PropertyName { get; set; }
	public int Size { get; set; }
}
