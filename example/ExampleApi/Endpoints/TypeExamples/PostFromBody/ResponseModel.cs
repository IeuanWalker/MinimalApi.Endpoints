using System.Diagnostics.CodeAnalysis;

namespace ExampleApi.Endpoints.TypeExamples.PostFromBody;

[ExcludeFromCodeCoverage]
public class ResponseModel
{
	public string Message { get; set; } = string.Empty;
	public int ProcessedPropertiesCount { get; set; }
}
