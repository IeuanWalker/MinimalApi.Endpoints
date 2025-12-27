using System.Diagnostics.CodeAnalysis;
using IeuanWalker.MinimalApi.Endpoints.Validation;
using Microsoft.AspNetCore.Builder;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace IeuanWalker.MinimalApi.Endpoints;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Extension methods for adding validation to endpoints
/// </summary>
[ExcludeFromCodeCoverage]
public static class ValidationExtensions
{
	/// <summary>
	/// Adds validation rules to the endpoint for OpenAPI schema documentation.
	/// This enables declarative validation constraints that appear in the generated OpenAPI specification.
	/// Note: This does NOT perform runtime validation - it only updates the OpenAPI documentation.
	/// </summary>
	/// <typeparam name="TRequest">The type of the request model to validate</typeparam>
	/// <param name="builder">The route handler builder</param>
	/// <param name="configure">Action to configure validation rules</param>
	/// <returns>The route handler builder for method chaining</returns>
	/// <example>
	/// <code>
	/// app.MapPost("/todos", async (TodoRequest request) => { ... })
	///    .WithValidation&lt;TodoRequest&gt;(config =>
	///    {
	///        config.Property(x => x.Title)
	///            .Required()
	///            .MinLength(1)
	///            .MaxLength(200);
	///
	///        config.Property(x => x.Email)
	///            .Email();
	///    });
	/// </code>
	/// </example>
	[ExcludeFromCodeCoverage]
	public static RouteHandlerBuilder WithValidation<TRequest>(
		this RouteHandlerBuilder builder,
		Action<ValidationConfigurationBuilder<TRequest>> configure)
	{
		ArgumentNullException.ThrowIfNull(builder);
		ArgumentNullException.ThrowIfNull(configure);

		ValidationConfigurationBuilder<TRequest> configBuilder = new();
		configure(configBuilder);
		ValidationConfiguration<TRequest> configuration = configBuilder.Build();

		// Store validation configuration as endpoint metadata for OpenAPI generation
		builder.WithMetadata(new ValidationMetadata<TRequest>(configuration));

		return builder;
	}
}
