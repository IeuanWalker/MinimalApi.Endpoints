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

	/// <summary>
	/// Operations to apply to validation rules grouped by property name
	/// </summary>
	internal IReadOnlyDictionary<string, IReadOnlyList<ValidationRuleOperation>> OperationsByProperty { get; }

	internal ValidationConfiguration(
		IReadOnlyList<ValidationRule> rules,
		bool appendRulesToPropertyDescription,
		IReadOnlyDictionary<string, IReadOnlyList<ValidationRuleOperation>> operationsByProperty)
	{
		Rules = rules;
		AppendRulesToPropertyDescription = appendRulesToPropertyDescription;
		OperationsByProperty = operationsByProperty;
	}
}
