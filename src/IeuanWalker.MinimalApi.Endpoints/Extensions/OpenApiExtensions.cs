using System.Diagnostics.CodeAnalysis;
using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace IeuanWalker.MinimalApi.Endpoints;
#pragma warning restore IDE0130 // Namespace does not match folder structure

[ExcludeFromCodeCoverage]
public static class OpenApiExtensions
{
	[ExcludeFromCodeCoverage]
	public static RouteHandlerBuilder WithResponse(this RouteHandlerBuilder builder, int statusCode, string description, string? contentType = null, params string[] additionalContentTypes)
	{
		builder.WithResponse(null, statusCode, description, contentType, additionalContentTypes);

		return builder;
	}

	[ExcludeFromCodeCoverage]
	public static RouteHandlerBuilder WithResponse<T>(this RouteHandlerBuilder builder, int statusCode, string description, string? contentType = null, params string[] additionalContentTypes)
	{
		builder.WithResponse(typeof(T), statusCode, description, contentType, additionalContentTypes);

		return builder;
	}

	[ExcludeFromCodeCoverage]
	static RouteHandlerBuilder WithResponse(this RouteHandlerBuilder builder, Type? responseType, int statusCode, string description, string? contentType = null, params string[] additionalContentTypes)
	{
		if (responseType is not null && string.IsNullOrEmpty(contentType))
		{
			contentType = "application/json";
		}


		if (contentType is null)
		{
			builder.WithMetadata(new ProducesResponseTypeMetadata(statusCode, responseType ?? typeof(void)));
		}
		else
		{
			string[] contentTypes = new string[additionalContentTypes.Length + 1];
			contentTypes[0] = contentType;
			additionalContentTypes.CopyTo(contentTypes, 1);

			builder.WithMetadata(new ProducesResponseTypeMetadata(statusCode, responseType ?? typeof(void), contentTypes));
		}

		builder.AddOpenApiOperationTransformer((operation, _, _) =>
		{
			if (operation.Responses is not null)
			{
				IOpenApiResponse? response = operation.Responses[statusCode.ToString()];
				response?.Description = description;
			}

			return Task.CompletedTask;
		});

		return builder;
	}

	/// <summary>
	/// Adds security scheme documentation to the OpenAPI document with default configuration.
	/// This will automatically document all registered authentication schemes.
	/// </summary>
	/// <example>
	/// Simple usage with defaults:
	/// <code>
	/// builder.Services.AddOpenApi(options =>
	/// {
	///     options.AddAuthenticationSchemes();
	/// });
	/// </code>
	/// </example>
	[ExcludeFromCodeCoverage]
	public static OpenApiOptions AddAuthenticationSchemes(this OpenApiOptions options)
	{
		return options.AddAuthenticationSchemes(null);
	}

	/// <summary>
	/// Adds security scheme documentation to the OpenAPI document with custom configuration.
	/// This will automatically document all registered authentication schemes with the specified options.
	/// </summary>
	/// <param name="options">The OpenAPI options (passed via AddOpenApi lambda).</param>
	/// <param name="configure">Action to customize security scheme options.</param>
	/// <returns>The OpenAPI options for method chaining.</returns>
	/// <example>
	/// Custom configuration example:
	/// <code>
	/// builder.Services.AddOpenApi(options =>
	/// {
	///     options.AddAuthenticationSchemes(o =>
	///     {
	///         o.BearerFormat = "My Custom JWT";
	///         o.ApiKeyHeaderName = "X-Custom-Key";
	///         o.OAuth2AuthorizationUrl = new Uri("https://auth.example.com/authorize");
	///         o.OAuth2Scopes = new Dictionary&lt;string, string&gt;
	///         {
	///             { "read", "Read access" },
	///             { "write", "Write access" }
	///         };
	///         o.OpenIdConnectUrl = new Uri("https://auth.example.com/.well-known/openid-configuration");
	///     });
	/// });
	/// </code>
	/// </example>
	[ExcludeFromCodeCoverage]
	public static OpenApiOptions AddAuthenticationSchemes(this OpenApiOptions options, Action<SecuritySchemeTransformerOptions>? configure)
	{
		SecuritySchemeTransformerOptions securityOptions = new();
		configure?.Invoke(securityOptions);

		options.AddDocumentTransformer(async (document, context, ct) =>
		{
			IAuthenticationSchemeProvider? authProvider = context.ApplicationServices.GetService<IAuthenticationSchemeProvider>()
				?? throw new InvalidOperationException("Authentication services are not registered. Add authentication to your application using 'builder.Services.AddAuthentication()' before calling 'AddSecuritySchemeTransformer()'.");

			SecuritySchemeTransformer transformer = new(authProvider, securityOptions);
			await transformer.TransformAsync(document, context, ct);
		});

		return options;
	}

	/// <summary>
	/// Adds authorization policy documentation to the OpenAPI document.
	/// This will automatically document all authorization policies applied to endpoints,
	/// extracting and displaying their requirements in the operation descriptions.
	/// </summary>
	/// <example>
	/// Usage with OpenAPI options:
	/// <code>
	/// builder.Services.AddOpenApi(options =>
	/// {
	///     options.AddAuthorizationPoliciesAndRequirements();
	/// });
	/// </code>
	/// </example>
	[ExcludeFromCodeCoverage]
	public static OpenApiOptions AddAuthorizationPoliciesAndRequirements(this OpenApiOptions options)
	{
		options.AddOperationTransformer<AuthorizationPoliciesAndRequirementsOperationTransformer>();
		return options;
	}

	/// <summary>
	/// Enriches OpenAPI schemas with FluentValidation rules.
	/// Extracts validation constraints from FluentValidation validators and applies them to the OpenAPI schema.
	/// This will add constraints like minLength, maxLength, minimum, maximum, pattern, format, and required fields
	/// based on the FluentValidation rules defined for your DTOs.
	/// </summary>
	/// <example>
	/// Usage with OpenAPI options:
	/// <code>
	/// builder.Services.AddOpenApi(options =>
	/// {
	///     options.AddFluentValidationSchemas();
	/// });
	/// </code>
	/// </example>
	[ExcludeFromCodeCoverage]
	public static OpenApiOptions AddFluentValidationSchemas(this OpenApiOptions options)
	{
		options.AddSchemaTransformer<OpenApiDocumentTransformers.FluentValidationSchemaTransformer>();
		return options;
	}
}
