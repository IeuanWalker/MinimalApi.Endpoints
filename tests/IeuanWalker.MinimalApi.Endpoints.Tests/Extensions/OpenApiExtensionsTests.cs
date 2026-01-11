using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.OpenApi;
using Shouldly;

namespace IeuanWalker.MinimalApi.Endpoints.Tests.Extensions;

public class OpenApiExtensionsTests
{
	[Fact]
	public void EnhanceRequestProperties_ReturnsSameOptionsAndDoesNotThrow()
	{
		// Arrange
		OpenApiOptions options = new();

		// Act
		OpenApiOptions result = options.EnhanceRequestProperties();

		// Assert
		result.ShouldBeSameAs(options);
	}

	[Fact]
	public void AddAuthenticationSchemes_WithoutConfigure_ReturnsSameOptionsAndDoesNotThrow()
	{
		// Arrange
		OpenApiOptions options = new();

		// Act
		OpenApiOptions result = options.AddAuthenticationSchemes();

		// Assert
		result.ShouldBeSameAs(options);
	}

	[Fact]
	public void AddAuthenticationSchemes_WithConfigure_ReturnsSameOptions()
	{
		// Arrange
		OpenApiOptions options = new();

		// Act
		OpenApiOptions result = options.AddAuthenticationSchemes(o =>
        {
            o.BearerFormat = "Custom";
            o.ApiKeyHeaderName = "X-Custom";
            o.OAuth2AuthorizationUrl = new Uri("https://auth.example.com/authorize");
            o.OAuth2Scopes = new System.Collections.Generic.Dictionary<string, string> { { "read", "Read" } };
            o.OpenIdConnectUrl = new Uri("https://auth.example.com/.well-known/openid-configuration");
        });

        // Assert
        result.ShouldBeSameAs(options);
    }

    [Fact]
    public void AddAuthorizationPoliciesAndRequirements_ReturnsSameOptions()
    {
        // Arrange
        OpenApiOptions options = new();

        // Act
        OpenApiOptions result = options.AddAuthorizationPoliciesAndRequirements();

        // Assert
        result.ShouldBeSameAs(options);
    }
}
