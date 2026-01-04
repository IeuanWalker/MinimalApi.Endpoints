using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.Validation.PostWithValidationAlterRemove;

public class RequestModel
{
	public string Alter { get; set; } = string.Empty;
	public string Remove1 { get; set; } = string.Empty;
	public string RemoveAll { get; set; } = string.Empty;
}
