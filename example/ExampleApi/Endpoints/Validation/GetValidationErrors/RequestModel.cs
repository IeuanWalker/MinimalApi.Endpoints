using System.Diagnostics.CodeAnalysis;

namespace ExampleApi.Endpoints.Validation.GetValidationErrors;

[ExcludeFromCodeCoverage]
public class RequestModel
{
	public string Name { get; set; } = string.Empty;
	public NestedRequest Nested { get; set; } = new();
}

[ExcludeFromCodeCoverage]
public class NestedRequest
{
	public string Description { get; set; } = string.Empty;
}
