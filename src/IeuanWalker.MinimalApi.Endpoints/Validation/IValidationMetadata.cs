namespace IeuanWalker.MinimalApi.Endpoints.Validation;

/// <summary>
/// Non-generic interface for validation metadata to enable access without reflection.
/// This interface is implemented by <see cref="ValidationMetadata{TRequest}"/> to allow
/// the OpenAPI document transformers to access validation configuration without using reflection.
/// </summary>
internal interface IValidationMetadata
{
	/// <summary>
	/// Gets the type of the request model being validated.
	/// </summary>
	Type RequestType { get; }

	/// <summary>
	/// Gets the validation configuration as a non-generic interface.
	/// </summary>
	IValidationConfiguration Configuration { get; }
}

/// <summary>
/// Non-generic interface for validation configuration to enable access without reflection.
/// This interface is implemented by <see cref="ValidationConfiguration{TRequest}"/> to allow
/// the OpenAPI document transformers to access validation rules without using reflection.
/// </summary>
internal interface IValidationConfiguration
{
	/// <summary>
	/// Gets all validation rules for properties.
	/// </summary>
	IReadOnlyList<ValidationRule> Rules { get; }

	/// <summary>
	/// Gets whether to list validation rules in the property description field in OpenAPI documentation.
	/// </summary>
	bool AppendRulesToPropertyDescription { get; }

	/// <summary>
	/// Gets operations to apply to validation rules grouped by property name.
	/// </summary>
	IReadOnlyDictionary<string, IReadOnlyList<ValidationRuleOperation>> OperationsByProperty { get; }
}
