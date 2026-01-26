using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi;
using NSubstitute;

namespace IeuanWalker.MinimalApi.Endpoints.Tests.OpenApiDocumentTransformers;

public class SecuritySchemeTransformerTests
{
	#region Constructor Tests

	[Fact]
	public void Constructor_WithNullOptions_UsesDefaultOptions()
	{
		// Arrange
		IAuthenticationSchemeProvider schemeProvider = Substitute.For<IAuthenticationSchemeProvider>();

		// Act
		SecuritySchemeTransformer transformer = new(schemeProvider, null);

		// Assert - transformer should be created without throwing
		transformer.ShouldNotBeNull();
	}

	[Fact]
	public void Constructor_WithCustomOptions_UsesProvidedOptions()
	{
		// Arrange
		IAuthenticationSchemeProvider schemeProvider = Substitute.For<IAuthenticationSchemeProvider>();
		SecuritySchemeTransformerOptions options = new()
		{
			BearerFormat = "CustomFormat",
			ApiKeyHeaderName = "X-Custom-Key"
		};

		// Act
		SecuritySchemeTransformer transformer = new(schemeProvider, options);

		// Assert
		transformer.ShouldNotBeNull();
	}

	#endregion

	#region TransformAsync - No Schemes Tests

	[Fact]
	public async Task TransformAsync_WhenNoAuthenticationSchemes_DoesNotModifyDocument()
	{
		// Arrange
		IAuthenticationSchemeProvider schemeProvider = Substitute.For<IAuthenticationSchemeProvider>();
		schemeProvider.GetAllSchemesAsync().Returns([]);

		SecuritySchemeTransformer transformer = new(schemeProvider);
		OpenApiDocument document = new();

		// Act
		await transformer.TransformAsync(document, null!, CancellationToken.None);

		// Assert
		document.Components.ShouldBeNull();
	}

	[Fact]
	public async Task TransformAsync_WhenNoAuthenticationSchemes_ReturnsEarly()
	{
		// Arrange
		IAuthenticationSchemeProvider schemeProvider = Substitute.For<IAuthenticationSchemeProvider>();
		schemeProvider.GetAllSchemesAsync().Returns([]);

		SecuritySchemeTransformer transformer = new(schemeProvider);
		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
				{
					["ExistingScheme"] = new OpenApiSecurityScheme
					{
						Type = SecuritySchemeType.Http
					}
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, null!, CancellationToken.None);

		// Assert - existing schemes should remain unchanged
		document.Components.SecuritySchemes.ShouldContainKey("ExistingScheme");
		document.Components.SecuritySchemes.Count.ShouldBe(1);
	}

	#endregion

	#region TransformAsync - Bearer Scheme Tests

	[Fact]
	public async Task TransformAsync_WithBearerScheme_AddsBearerSecurityScheme()
	{
		// Arrange
		IAuthenticationSchemeProvider schemeProvider = CreateSchemeProvider("Bearer");
		SecuritySchemeTransformer transformer = new(schemeProvider);
		OpenApiDocument document = new();

		// Act
		await transformer.TransformAsync(document, null!, CancellationToken.None);

		// Assert
		document.Components.ShouldNotBeNull();
		document.Components.SecuritySchemes.ShouldNotBeNull();
		document.Components.SecuritySchemes.ShouldContainKey("Bearer");

		IOpenApiSecurityScheme scheme = document.Components.SecuritySchemes["Bearer"];
		scheme.Type.ShouldBe(SecuritySchemeType.Http);
		scheme.Scheme.ShouldBe("bearer");
		scheme.In.ShouldBe(ParameterLocation.Header);
		scheme.BearerFormat.ShouldBe("JWT");
	}

	[Fact]
	public async Task TransformAsync_WithBearerScheme_UsesCustomBearerFormat()
	{
		// Arrange
		IAuthenticationSchemeProvider schemeProvider = CreateSchemeProvider("Bearer");
		SecuritySchemeTransformerOptions options = new()
		{
			BearerFormat = "CustomToken"
		};
		SecuritySchemeTransformer transformer = new(schemeProvider, options);
		OpenApiDocument document = new();

		// Act
		await transformer.TransformAsync(document, null!, CancellationToken.None);

		// Assert
		document.Components.ShouldNotBeNull();
		document.Components.SecuritySchemes.ShouldNotBeNull();
		IOpenApiSecurityScheme scheme = document.Components.SecuritySchemes["Bearer"];
		scheme.BearerFormat.ShouldBe("CustomToken");
	}

	[Fact]
	public async Task TransformAsync_WithBearerScheme_CaseInsensitive()
	{
		// Arrange
		IAuthenticationSchemeProvider schemeProvider = CreateSchemeProvider("BEARER");
		SecuritySchemeTransformer transformer = new(schemeProvider);
		OpenApiDocument document = new();

		// Act
		await transformer.TransformAsync(document, null!, CancellationToken.None);

		// Assert
		document.Components.ShouldNotBeNull();
		document.Components.SecuritySchemes.ShouldNotBeNull();
		document.Components.SecuritySchemes.ShouldContainKey("BEARER");
		IOpenApiSecurityScheme scheme = document.Components.SecuritySchemes["BEARER"];
		scheme.Type.ShouldBe(SecuritySchemeType.Http);
		scheme.Scheme.ShouldBe("bearer");
	}

	#endregion

	#region TransformAsync - Basic Scheme Tests

	[Fact]
	public async Task TransformAsync_WithBasicScheme_AddsBasicSecurityScheme()
	{
		// Arrange
		IAuthenticationSchemeProvider schemeProvider = CreateSchemeProvider("Basic");
		SecuritySchemeTransformer transformer = new(schemeProvider);
		OpenApiDocument document = new();

		// Act
		await transformer.TransformAsync(document, null!, CancellationToken.None);

		// Assert
		document.Components.ShouldNotBeNull();
		document.Components.SecuritySchemes.ShouldNotBeNull();
		document.Components.SecuritySchemes.ShouldContainKey("Basic");

		IOpenApiSecurityScheme scheme = document.Components.SecuritySchemes["Basic"];
		scheme.Type.ShouldBe(SecuritySchemeType.Http);
		scheme.Scheme.ShouldBe("basic");
		scheme.In.ShouldBe(ParameterLocation.Header);
	}

	[Fact]
	public async Task TransformAsync_WithBasicScheme_CaseInsensitive()
	{
		// Arrange
		IAuthenticationSchemeProvider schemeProvider = CreateSchemeProvider("BASIC");
		SecuritySchemeTransformer transformer = new(schemeProvider);
		OpenApiDocument document = new();

		// Act
		await transformer.TransformAsync(document, null!, CancellationToken.None);

		// Assert
		document.Components.ShouldNotBeNull();
		document.Components.SecuritySchemes.ShouldNotBeNull();
		IOpenApiSecurityScheme scheme = document.Components.SecuritySchemes["BASIC"];
		scheme.Scheme.ShouldBe("basic");
	}

	#endregion

	#region TransformAsync - ApiKey Scheme Tests

	[Fact]
	public async Task TransformAsync_WithApiKeyScheme_AddsApiKeySecurityScheme()
	{
		// Arrange
		IAuthenticationSchemeProvider schemeProvider = CreateSchemeProvider("ApiKey");
		SecuritySchemeTransformer transformer = new(schemeProvider);
		OpenApiDocument document = new();

		// Act
		await transformer.TransformAsync(document, null!, CancellationToken.None);

		// Assert
		document.Components.ShouldNotBeNull();
		document.Components.SecuritySchemes.ShouldNotBeNull();
		document.Components.SecuritySchemes.ShouldContainKey("ApiKey");

		IOpenApiSecurityScheme scheme = document.Components.SecuritySchemes["ApiKey"];
		scheme.Type.ShouldBe(SecuritySchemeType.ApiKey);
		scheme.In.ShouldBe(ParameterLocation.Header);
		scheme.Name.ShouldBe("X-API-Key");
	}

	[Fact]
	public async Task TransformAsync_WithApiKeyScheme_UsesCustomHeaderName()
	{
		// Arrange
		IAuthenticationSchemeProvider schemeProvider = CreateSchemeProvider("ApiKey");
		SecuritySchemeTransformerOptions options = new()
		{
			ApiKeyHeaderName = "X-Custom-Api-Key"
		};
		SecuritySchemeTransformer transformer = new(schemeProvider, options);
		OpenApiDocument document = new();

		// Act
		await transformer.TransformAsync(document, null!, CancellationToken.None);

		// Assert
		document.Components.ShouldNotBeNull();
		document.Components.SecuritySchemes.ShouldNotBeNull();
		IOpenApiSecurityScheme scheme = document.Components.SecuritySchemes["ApiKey"];
		scheme.Name.ShouldBe("X-Custom-Api-Key");
	}

	[Fact]
	public async Task TransformAsync_WithApiKeyScheme_CaseInsensitive()
	{
		// Arrange
		IAuthenticationSchemeProvider schemeProvider = CreateSchemeProvider("APIKEY");
		SecuritySchemeTransformer transformer = new(schemeProvider);
		OpenApiDocument document = new();

		// Act
		await transformer.TransformAsync(document, null!, CancellationToken.None);

		// Assert
		document.Components.ShouldNotBeNull();
		document.Components.SecuritySchemes.ShouldNotBeNull();
		IOpenApiSecurityScheme scheme = document.Components.SecuritySchemes["APIKEY"];
		scheme.Type.ShouldBe(SecuritySchemeType.ApiKey);
	}

	#endregion

	#region TransformAsync - OAuth2 Scheme Tests

	[Fact]
	public async Task TransformAsync_WithOAuth2Scheme_AddsOAuth2SecurityScheme()
	{
		// Arrange
		IAuthenticationSchemeProvider schemeProvider = CreateSchemeProvider("OAuth2");
		SecuritySchemeTransformer transformer = new(schemeProvider);
		OpenApiDocument document = new();

		// Act
		await transformer.TransformAsync(document, null!, CancellationToken.None);

		// Assert
		document.Components.ShouldNotBeNull();
		document.Components.SecuritySchemes.ShouldNotBeNull();
		document.Components.SecuritySchemes.ShouldContainKey("OAuth2");

		IOpenApiSecurityScheme scheme = document.Components.SecuritySchemes["OAuth2"];
		scheme.Type.ShouldBe(SecuritySchemeType.OAuth2);
		scheme.Flows.ShouldNotBeNull();
		scheme.Flows.Implicit.ShouldNotBeNull();
		scheme.Flows.Implicit.AuthorizationUrl.ShouldBe(new Uri("https://example.com/oauth/authorize"));
	}

	[Fact]
	public async Task TransformAsync_WithOAuth2Scheme_UsesCustomAuthorizationUrl()
	{
		// Arrange
		IAuthenticationSchemeProvider schemeProvider = CreateSchemeProvider("OAuth2");
		SecuritySchemeTransformerOptions options = new()
		{
			OAuth2AuthorizationUrl = new Uri("https://custom.auth.com/authorize")
		};
		SecuritySchemeTransformer transformer = new(schemeProvider, options);
		OpenApiDocument document = new();

		// Act
		await transformer.TransformAsync(document, null!, CancellationToken.None);

		// Assert
		document.Components.ShouldNotBeNull();
		document.Components.SecuritySchemes.ShouldNotBeNull();
		IOpenApiSecurityScheme scheme = document.Components.SecuritySchemes["OAuth2"];
		scheme.Flows!.Implicit!.AuthorizationUrl.ShouldBe(new Uri("https://custom.auth.com/authorize"));
	}

	[Fact]
	public async Task TransformAsync_WithOAuth2Scheme_UsesCustomScopes()
	{
		// Arrange
		IAuthenticationSchemeProvider schemeProvider = CreateSchemeProvider("OAuth2");
		SecuritySchemeTransformerOptions options = new()
		{
			OAuth2Scopes = new Dictionary<string, string>
			{
				["read"] = "Read access",
				["write"] = "Write access",
				["admin"] = "Admin access"
			}
		};
		SecuritySchemeTransformer transformer = new(schemeProvider, options);
		OpenApiDocument document = new();

		// Act
		await transformer.TransformAsync(document, null!, CancellationToken.None);

		// Assert
		document.Components.ShouldNotBeNull();
		document.Components.SecuritySchemes.ShouldNotBeNull();
		IOpenApiSecurityScheme scheme = document.Components.SecuritySchemes["OAuth2"];
		scheme.Flows.ShouldNotBeNull();
		scheme.Flows.Implicit.ShouldNotBeNull();
		scheme.Flows.Implicit.Scopes.ShouldNotBeNull();
		scheme.Flows.Implicit.Scopes.ShouldContainKeyAndValue("read", "Read access");
		scheme.Flows.Implicit.Scopes.ShouldContainKeyAndValue("write", "Write access");
		scheme.Flows.Implicit.Scopes.ShouldContainKeyAndValue("admin", "Admin access");
	}

	[Fact]
	public async Task TransformAsync_WithOAuth2Scheme_CaseInsensitive()
	{
		// Arrange
		IAuthenticationSchemeProvider schemeProvider = CreateSchemeProvider("OAUTH2");
		SecuritySchemeTransformer transformer = new(schemeProvider);
		OpenApiDocument document = new();

		// Act
		await transformer.TransformAsync(document, null!, CancellationToken.None);

		// Assert
		document.Components.ShouldNotBeNull();
		document.Components.SecuritySchemes.ShouldNotBeNull();
		IOpenApiSecurityScheme scheme = document.Components.SecuritySchemes["OAUTH2"];
		scheme.Type.ShouldBe(SecuritySchemeType.OAuth2);
	}

	#endregion

	#region TransformAsync - OpenIdConnect Scheme Tests

	[Fact]
	public async Task TransformAsync_WithOpenIdConnectScheme_AddsOpenIdConnectSecurityScheme()
	{
		// Arrange
		IAuthenticationSchemeProvider schemeProvider = CreateSchemeProvider("OpenIdConnect");
		SecuritySchemeTransformer transformer = new(schemeProvider);
		OpenApiDocument document = new();

		// Act
		await transformer.TransformAsync(document, null!, CancellationToken.None);

		// Assert
		document.Components.ShouldNotBeNull();
		document.Components.SecuritySchemes.ShouldNotBeNull();
		document.Components.SecuritySchemes.ShouldContainKey("OpenIdConnect");

		IOpenApiSecurityScheme scheme = document.Components.SecuritySchemes["OpenIdConnect"];
		scheme.Type.ShouldBe(SecuritySchemeType.OpenIdConnect);
		scheme.OpenIdConnectUrl.ShouldBe(new Uri("https://example.com/.well-known/openid-configuration"));
	}

	[Fact]
	public async Task TransformAsync_WithOpenIdConnectScheme_UsesCustomUrl()
	{
		// Arrange
		IAuthenticationSchemeProvider schemeProvider = CreateSchemeProvider("OpenIdConnect");
		SecuritySchemeTransformerOptions options = new()
		{
			OpenIdConnectUrl = new Uri("https://custom.identity.com/.well-known/openid-configuration")
		};
		SecuritySchemeTransformer transformer = new(schemeProvider, options);
		OpenApiDocument document = new();

		// Act
		await transformer.TransformAsync(document, null!, CancellationToken.None);

		// Assert
		document.Components.ShouldNotBeNull();
		document.Components.SecuritySchemes.ShouldNotBeNull();
		IOpenApiSecurityScheme scheme = document.Components.SecuritySchemes["OpenIdConnect"];
		scheme.OpenIdConnectUrl.ShouldBe(new Uri("https://custom.identity.com/.well-known/openid-configuration"));
	}

	[Fact]
	public async Task TransformAsync_WithOpenIdConnectScheme_CaseInsensitive()
	{
		// Arrange
		IAuthenticationSchemeProvider schemeProvider = CreateSchemeProvider("OPENIDCONNECT");
		SecuritySchemeTransformer transformer = new(schemeProvider);
		OpenApiDocument document = new();

		// Act
		await transformer.TransformAsync(document, null!, CancellationToken.None);

		// Assert
		document.Components.ShouldNotBeNull();
		document.Components.SecuritySchemes.ShouldNotBeNull();
		IOpenApiSecurityScheme scheme = document.Components.SecuritySchemes["OPENIDCONNECT"];
		scheme.Type.ShouldBe(SecuritySchemeType.OpenIdConnect);
	}

	#endregion

	#region TransformAsync - Unknown/Custom Scheme Tests

	[Fact]
	public async Task TransformAsync_WithUnknownScheme_AddsHttpSecuritySchemeWithSchemeName()
	{
		// Arrange
		IAuthenticationSchemeProvider schemeProvider = CreateSchemeProvider("CustomAuth");
		SecuritySchemeTransformer transformer = new(schemeProvider);
		OpenApiDocument document = new();

		// Act
		await transformer.TransformAsync(document, null!, CancellationToken.None);

		// Assert
		document.Components.ShouldNotBeNull();
		document.Components.SecuritySchemes.ShouldNotBeNull();
		document.Components.SecuritySchemes.ShouldContainKey("CustomAuth");

		IOpenApiSecurityScheme scheme = document.Components.SecuritySchemes["CustomAuth"];
		scheme.Type.ShouldBe(SecuritySchemeType.Http);
		scheme.Scheme.ShouldBe("customauth");
		scheme.In.ShouldBe(ParameterLocation.Header);
	}

	[Fact]
	public async Task TransformAsync_WithUnknownScheme_PreservesOriginalNameAsKey()
	{
		// Arrange
		IAuthenticationSchemeProvider schemeProvider = CreateSchemeProvider("MyCustomScheme");
		SecuritySchemeTransformer transformer = new(schemeProvider);
		OpenApiDocument document = new();

		// Act
		await transformer.TransformAsync(document, null!, CancellationToken.None);

		// Assert
		document.Components.ShouldNotBeNull();
		document.Components.SecuritySchemes.ShouldNotBeNull();
		document.Components.SecuritySchemes.ShouldContainKey("MyCustomScheme");
		IOpenApiSecurityScheme scheme = document.Components.SecuritySchemes["MyCustomScheme"];
		scheme.Scheme.ShouldBe("mycustomscheme");
	}

	#endregion

	#region TransformAsync - Multiple Schemes Tests

	[Fact]
	public async Task TransformAsync_WithMultipleSchemes_AddsAllSecuritySchemes()
	{
		// Arrange
		IAuthenticationSchemeProvider schemeProvider = CreateSchemeProvider("Bearer", "ApiKey", "Basic");
		SecuritySchemeTransformer transformer = new(schemeProvider);
		OpenApiDocument document = new();

		// Act
		await transformer.TransformAsync(document, null!, CancellationToken.None);

		// Assert
		document.Components.ShouldNotBeNull();
		document.Components.SecuritySchemes.ShouldNotBeNull();
		document.Components.SecuritySchemes.Count.ShouldBe(3);
		document.Components.SecuritySchemes.ShouldContainKey("Bearer");
		document.Components.SecuritySchemes.ShouldContainKey("ApiKey");
		document.Components.SecuritySchemes.ShouldContainKey("Basic");
	}

	[Fact]
	public async Task TransformAsync_WithMultipleSchemes_EachSchemeHasCorrectConfiguration()
	{
		// Arrange
		IAuthenticationSchemeProvider schemeProvider = CreateSchemeProvider("Bearer", "ApiKey");
		SecuritySchemeTransformer transformer = new(schemeProvider);
		OpenApiDocument document = new();

		// Act
		await transformer.TransformAsync(document, null!, CancellationToken.None);

		// Assert
		document.Components.ShouldNotBeNull();
		document.Components.SecuritySchemes.ShouldNotBeNull();
		IOpenApiSecurityScheme bearerScheme = document.Components.SecuritySchemes["Bearer"];
		bearerScheme.Type.ShouldBe(SecuritySchemeType.Http);
		bearerScheme.Scheme.ShouldBe("bearer");

		IOpenApiSecurityScheme apiKeyScheme = document.Components.SecuritySchemes["ApiKey"];
		apiKeyScheme.Type.ShouldBe(SecuritySchemeType.ApiKey);
	}

	[Fact]
	public async Task TransformAsync_WithAllKnownSchemes_AddsAllSchemeTypes()
	{
		// Arrange
		IAuthenticationSchemeProvider schemeProvider = CreateSchemeProvider("Bearer", "Basic", "ApiKey", "OAuth2", "OpenIdConnect", "CustomScheme");
		SecuritySchemeTransformer transformer = new(schemeProvider);
		OpenApiDocument document = new();

		// Act
		await transformer.TransformAsync(document, null!, CancellationToken.None);

		// Assert
		document.Components.ShouldNotBeNull();
		document.Components.SecuritySchemes.ShouldNotBeNull();
		document.Components.SecuritySchemes.Count.ShouldBe(6);
		document.Components.SecuritySchemes["Bearer"].Type.ShouldBe(SecuritySchemeType.Http);
		document.Components.SecuritySchemes["Basic"].Type.ShouldBe(SecuritySchemeType.Http);
		document.Components.SecuritySchemes["ApiKey"].Type.ShouldBe(SecuritySchemeType.ApiKey);
		document.Components.SecuritySchemes["OAuth2"].Type.ShouldBe(SecuritySchemeType.OAuth2);
		document.Components.SecuritySchemes["OpenIdConnect"].Type.ShouldBe(SecuritySchemeType.OpenIdConnect);
		document.Components.SecuritySchemes["CustomScheme"].Type.ShouldBe(SecuritySchemeType.Http);
	}

	#endregion

	#region TransformAsync - Document Components Tests

	[Fact]
	public async Task TransformAsync_WhenDocumentComponentsIsNull_CreatesComponents()
	{
		// Arrange
		IAuthenticationSchemeProvider schemeProvider = CreateSchemeProvider("Bearer");
		SecuritySchemeTransformer transformer = new(schemeProvider);
		OpenApiDocument document = new()
		{
			Components = null
		};

		// Act
		await transformer.TransformAsync(document, null!, CancellationToken.None);

		// Assert
		document.Components.ShouldNotBeNull();
		document.Components.SecuritySchemes.ShouldNotBeNull();
		document.Components.SecuritySchemes.ShouldContainKey("Bearer");
	}

	[Fact]
	public async Task TransformAsync_WhenDocumentComponentsExists_ReplacesSecuritySchemes()
	{
		// Arrange
		IAuthenticationSchemeProvider schemeProvider = CreateSchemeProvider("Bearer");
		SecuritySchemeTransformer transformer = new(schemeProvider);
		OpenApiDocument document = new()
		{
			Components = new OpenApiComponents
			{
				SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
				{
					["ExistingScheme"] = new OpenApiSecurityScheme { Type = SecuritySchemeType.Http }
				}
			}
		};

		// Act
		await transformer.TransformAsync(document, null!, CancellationToken.None);

		// Assert
		document.Components.SecuritySchemes.ShouldContainKey("Bearer");
		document.Components.SecuritySchemes.ShouldNotContainKey("ExistingScheme");
	}

	#endregion

	#region SecuritySchemeTransformerOptions Tests

	[Fact]
	public void SecuritySchemeTransformerOptions_DefaultValues_AreCorrect()
	{
		// Arrange & Act
		SecuritySchemeTransformerOptions options = new();

		// Assert
		options.BearerFormat.ShouldBe("JWT");
		options.ApiKeyHeaderName.ShouldBe("X-API-Key");
		options.OAuth2AuthorizationUrl.ShouldBe(new Uri("https://example.com/oauth/authorize"));
		options.OAuth2Scopes.ShouldBeEmpty();
		options.OpenIdConnectUrl.ShouldBe(new Uri("https://example.com/.well-known/openid-configuration"));
	}

	[Fact]
	public void SecuritySchemeTransformerOptions_CanSetAllProperties()
	{
		// Arrange & Act
		SecuritySchemeTransformerOptions options = new()
		{
			BearerFormat = "CustomFormat",
			ApiKeyHeaderName = "X-Custom-Key",
			OAuth2AuthorizationUrl = new Uri("https://custom.com/auth"),
			OAuth2Scopes = new Dictionary<string, string> { ["scope1"] = "Description" },
			OpenIdConnectUrl = new Uri("https://custom.com/.well-known/openid")
		};

		// Assert
		options.BearerFormat.ShouldBe("CustomFormat");
		options.ApiKeyHeaderName.ShouldBe("X-Custom-Key");
		options.OAuth2AuthorizationUrl.ShouldBe(new Uri("https://custom.com/auth"));
		options.OAuth2Scopes.ShouldContainKeyAndValue("scope1", "Description");
		options.OpenIdConnectUrl.ShouldBe(new Uri("https://custom.com/.well-known/openid"));
	}

	#endregion

	#region TransformAsync - Cancellation Tests

	[Fact]
	public async Task TransformAsync_WithCancellationToken_CompletesSuccessfully()
	{
		// Arrange
		IAuthenticationSchemeProvider schemeProvider = CreateSchemeProvider("Bearer");
		SecuritySchemeTransformer transformer = new(schemeProvider);
		OpenApiDocument document = new();
		using CancellationTokenSource cts = new();

		// Act
		await transformer.TransformAsync(document, null!, cts.Token);

		// Assert
		document.Components.ShouldNotBeNull();
		document.Components.SecuritySchemes.ShouldNotBeNull();
		document.Components.SecuritySchemes.ShouldContainKey("Bearer");
	}

	#endregion

	#region Helper Methods

	static IAuthenticationSchemeProvider CreateSchemeProvider(params string[] schemeNames)
	{
		IAuthenticationSchemeProvider schemeProvider = Substitute.For<IAuthenticationSchemeProvider>();

		List<AuthenticationScheme> schemes = [.. schemeNames.Select(name => new AuthenticationScheme(name, name, typeof(TestAuthenticationHandler)))];

		schemeProvider.GetAllSchemesAsync().Returns(schemes);

		return schemeProvider;
	}

	sealed class TestAuthenticationHandler : IAuthenticationHandler
	{
		public Task<AuthenticateResult> AuthenticateAsync() => Task.FromResult(AuthenticateResult.NoResult());
		public Task ChallengeAsync(AuthenticationProperties? properties) => Task.CompletedTask;
		public Task ForbidAsync(AuthenticationProperties? properties) => Task.CompletedTask;
		public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context) => Task.CompletedTask;
	}

	#endregion
}
