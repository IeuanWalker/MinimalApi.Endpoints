using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace IeuanWalker.MinimalApi.Endpoints;
#pragma warning restore IDE0130 // Namespace does not match folder structure

[ExcludeFromCodeCoverage]
sealed class SecuritySchemeTransformer(
	IAuthenticationSchemeProvider authenticationSchemeProvider,
	SecuritySchemeTransformerOptions? options = null) : IOpenApiDocumentTransformer
{
	readonly SecuritySchemeTransformerOptions _options = options ?? new();

	public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
	{
		IEnumerable<AuthenticationScheme> authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();

		if (!authenticationSchemes.Any())
		{
			return;
		}

		Dictionary<string, IOpenApiSecurityScheme> securitySchemes = [];

		foreach (AuthenticationScheme scheme in authenticationSchemes)
		{
			IOpenApiSecurityScheme securityScheme = scheme.Name.ToLowerInvariant() switch
			{
				"bearer" => new OpenApiSecurityScheme
				{
					Type = SecuritySchemeType.Http,
					Scheme = "bearer",
					In = ParameterLocation.Header,
					BearerFormat = _options.BearerFormat
				},
				"basic" => new OpenApiSecurityScheme
				{
					Type = SecuritySchemeType.Http,
					Scheme = "basic",
					In = ParameterLocation.Header
				},
				"apikey" => new OpenApiSecurityScheme
				{
					Type = SecuritySchemeType.ApiKey,
					In = ParameterLocation.Header,
					Name = _options.ApiKeyHeaderName
				},
				"oauth2" => new OpenApiSecurityScheme
				{
					Type = SecuritySchemeType.OAuth2,
					Flows = new OpenApiOAuthFlows
					{
						Implicit = new OpenApiOAuthFlow
						{
							AuthorizationUrl = _options.OAuth2AuthorizationUrl,
							Scopes = new Dictionary<string, string>(_options.OAuth2Scopes)
						}
					}
				},
				"openidconnect" => new OpenApiSecurityScheme
				{
					Type = SecuritySchemeType.OpenIdConnect,
					OpenIdConnectUrl = _options.OpenIdConnectUrl
				},
				_ => new OpenApiSecurityScheme
				{
					Type = SecuritySchemeType.Http,
					Scheme = scheme.Name.ToLowerInvariant(),
					In = ParameterLocation.Header
				}
			};

			securitySchemes[scheme.Name] = securityScheme;
		}

		if (securitySchemes.Count > 0)
		{
			document.Components ??= new OpenApiComponents();
			document.Components.SecuritySchemes = securitySchemes;
		}
	}
}

/// <summary>
/// Configuration options for <see cref="SecuritySchemeTransformer"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public class SecuritySchemeTransformerOptions
{
	/// <summary>
	/// Gets or sets the bearer token format (e.g., "Json Web Token").
	/// Default: "Json Web Token"
	/// </summary>
	public string BearerFormat { get; set; } = "Json Web Token";

	/// <summary>
	/// Gets or sets the API key header name.
	/// Default: "X-API-Key"
	/// </summary>
	public string ApiKeyHeaderName { get; set; } = "X-API-Key";

	/// <summary>
	/// Gets or sets the OAuth2 authorization URL.
	/// </summary>
	public Uri OAuth2AuthorizationUrl { get; set; } = new("https://example.com/oauth/authorize");

	/// <summary>
	/// Gets or sets the OAuth2 scopes.
	/// </summary>
	public Dictionary<string, string> OAuth2Scopes { get; set; } = [];

	/// <summary>
	/// Gets or sets the OpenID Connect discovery URL.
	/// </summary>
	public Uri OpenIdConnectUrl { get; set; } = new("https://example.com/.well-known/openid-configuration");
}
