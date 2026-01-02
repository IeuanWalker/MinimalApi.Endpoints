namespace IeuanWalker.MinimalApi.Endpoints.Validation;

/// <summary>
/// Compiled validation configuration for a request type
/// </summary>
public sealed class ValidationConfiguration<TRequest>
{
	/// <summary>
	/// All validation rules for properties
	/// </summary>
	public IReadOnlyList<ValidationRule> Rules { get; }

	/// <summary>
	/// Whether to list validation rules in the property description field in OpenAPI documentation
	/// </summary>
	public bool AppendRulesToPropertyDescription { get; }

	internal ValidationConfiguration(IReadOnlyList<ValidationRule> rules, bool appendRulesToPropertyDescription)
	{
		Rules = rules;
		AppendRulesToPropertyDescription = appendRulesToPropertyDescription;
	}
}
