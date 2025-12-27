namespace IeuanWalker.MinimalApi.Endpoints.Validation;

/// <summary>
/// Metadata that stores validation configuration for an endpoint
/// Used by OpenAPI document transformers to apply validation rules to schemas
/// </summary>
sealed class ValidationMetadata<TRequest>
{
	public ValidationConfiguration<TRequest> Configuration { get; }

	public ValidationMetadata(ValidationConfiguration<TRequest> configuration)
	{
		Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
	}
}
