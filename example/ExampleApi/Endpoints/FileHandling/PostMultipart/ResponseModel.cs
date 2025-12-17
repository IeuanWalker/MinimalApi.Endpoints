namespace ExampleApi.Endpoints.FileHandling.PostMultipart;

public sealed class ResponseModel
{
	public required string SomeData { get; set; }
	public required FileInfo SingleFile { get; set; }
	public required int TotalFileCount { get; set; }
	public required List<FileInfo> ReadOnlyList1 { get; set; }
	public required List<FileInfo> ReadOnlyList2 { get; set; }
	public required List<FileInfo> FileCollectionList { get; set; }
}

public sealed class FileInfo
{
	public required string FileName { get; set; }
	public required string PropertyName { get; set; }
	public int Size { get; set; }
}
