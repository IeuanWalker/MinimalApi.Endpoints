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
	/// Cross-field validators that validate relationships between properties
	/// </summary>
	public IReadOnlyList<Func<TRequest, Dictionary<string, string[]>>> CrossFieldValidators { get; }

	internal ValidationConfiguration(
		IReadOnlyList<ValidationRule> rules,
		IReadOnlyList<Func<TRequest, Dictionary<string, string[]>>> crossFieldValidators)
	{
		Rules = rules;
		CrossFieldValidators = crossFieldValidators;
	}
}
