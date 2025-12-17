namespace ExampleApi.Endpoints.FileHandling.ListOfFiles;

public sealed class ResponseModel
{
	public required string FileName { get; set; }
	public required string PropertyName { get; set; }
	public int Size { get; set; }
}
