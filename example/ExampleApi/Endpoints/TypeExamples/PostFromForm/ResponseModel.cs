using System.Diagnostics.CodeAnalysis;

namespace ExampleApi.Endpoints.TypeExamples.PostFromForm;

[ExcludeFromCodeCoverage]
public class ResponseModel
{
	public string Message { get; set; } = string.Empty;
	public int ProcessedPropertiesCount { get; set; }
	public int UploadedFilesCount { get; set; }
}
