using System.Diagnostics.CodeAnalysis;
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
	///     options.AddSecuritySchemeTransformer();
	/// });
	/// </code>
	/// </example>
	[ExcludeFromCodeCoverage]
	public static OpenApiOptions AddSecuritySchemeTransformer(this OpenApiOptions options)
	{
		return options.AddSecuritySchemeTransformer(null);
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
	///     options.AddSecuritySchemeTransformer(o =>
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
	public static OpenApiOptions AddSecuritySchemeTransformer(this OpenApiOptions options, Action<SecuritySchemeTransformerOptions>? configure)
	{
		SecuritySchemeTransformerOptions securityOptions = new();
		configure?.Invoke(securityOptions);

		_ = options.AddDocumentTransformer(async (document, context, ct) =>
		{
			IAuthenticationSchemeProvider? authProvider = context.ApplicationServices.GetService<IAuthenticationSchemeProvider>()
				?? throw new InvalidOperationException("Authentication services are not registered. Add authentication to your application using 'builder.Services.AddAuthentication()' before calling 'AddSecuritySchemeTransformer()'.");

			SecuritySchemeTransformer transformer = new(authProvider, securityOptions);
			await transformer.TransformAsync(document, context, ct);
		});

		return options;
	}
}
