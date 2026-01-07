namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer.Validation;

/// <summary>
/// Metadata that stores validation configuration for an endpoint
/// Used by OpenAPI document transformers to apply validation rules to schemas
/// </summary>
sealed class ValidationMetadata<TRequest> : IValidationMetadata
{
	public ValidationConfiguration<TRequest> Configuration { get; }

	/// <inheritdoc />
	public Type RequestType => typeof(TRequest);

	/// <inheritdoc />
	IValidationConfiguration IValidationMetadata.Configuration => Configuration;

	public ValidationMetadata(ValidationConfiguration<TRequest> configuration)
	{
		Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
	}
}
