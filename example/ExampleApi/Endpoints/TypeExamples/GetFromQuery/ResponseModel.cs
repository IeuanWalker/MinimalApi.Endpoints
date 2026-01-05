using System.Diagnostics.CodeAnalysis;

namespace ExampleApi.Endpoints.TypeExamples.GetFromQuery;

[ExcludeFromCodeCoverage]
public class ResponseModel
{
	public string Message { get; set; } = string.Empty;
	public int ProvidedParametersCount { get; set; }
	public Dictionary<string, string> ReceivedValues { get; set; } = new();
}
