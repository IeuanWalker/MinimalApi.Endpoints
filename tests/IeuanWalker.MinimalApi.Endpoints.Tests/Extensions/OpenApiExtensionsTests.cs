using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;

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
	public void EnhanceRequestProperties_WithCustomParameters_ReturnsSameOptions()
	{
		// Arrange
		OpenApiOptions options = new();

		// Act
		OpenApiOptions result = options.EnhanceRequestProperties(autoDocumentFluentValidation: false, appendRulesToPropertyDescription: false);

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
			o.OAuth2Scopes = new Dictionary<string, string>
			{
				{
					"read", "Read"
				}
			};
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

	[Fact]
	public void WithResponse_NoContentType_ReturnsSameBuilder()
	{
		// Arrange
		WebApplication app = WebApplication.CreateBuilder().Build();
		RouteHandlerBuilder route = app.MapGet("/no-content", () => Results.Ok());

		// Act
		RouteHandlerBuilder result = route.WithResponse(204, "No Content");

		// Assert
		result.ShouldBeSameAs(route);
	}

	[Fact]
	public void WithResponse_Generic_ReturnsSameBuilder()
	{
		// Arrange
		WebApplication app = WebApplication.CreateBuilder().Build();
		RouteHandlerBuilder route = app.MapGet("/generic", () => Results.Ok<string>("x"));

		// Act
		RouteHandlerBuilder result = route.WithResponse<string>(200, "OK");

		// Assert
		result.ShouldBeSameAs(route);
	}

	[Fact]
	public void WithResponse_WithContentTypes_ReturnsSameBuilder()
	{
		// Arrange
		WebApplication app = WebApplication.CreateBuilder().Build();
		RouteHandlerBuilder route = app.MapPost("/created", (HttpContext ctx) => Results.Created("/created", null));

		// Act
		RouteHandlerBuilder result = route.WithResponse<string>(201, "Created", "application/json", ["application/xml"]);

		// Assert
		result.ShouldBeSameAs(route);
	}

	[Fact]
	public void WithResponse_NonGeneric_WithExplicitContentType_ReturnsSameBuilder()
	{
		// Arrange
		WebApplication app = WebApplication.CreateBuilder().Build();
		RouteHandlerBuilder route = app.MapDelete("/plain", () => Results.NoContent());

		// Act
		RouteHandlerBuilder result = route.WithResponse(202, "Accepted", "text/plain");

		// Assert
		result.ShouldBeSameAs(route);
	}

	[Fact]
	public void WithResponse_MultipleAdditionalContentTypes_ReturnsSameBuilder()
	{
		// Arrange
		WebApplication app = WebApplication.CreateBuilder().Build();
		RouteHandlerBuilder route = app.MapGet("/multi", () => Results.Ok());

		// Act
		RouteHandlerBuilder result = route.WithResponse<string>(200, "OK", "application/json", ["application/xml", "text/plain"]);

		// Assert
		result.ShouldBeSameAs(route);
	}

	[Fact]
	public void WithValidationRules_ReturnsSameBuilder()
	{
		// Arrange
		WebApplication app = WebApplication.CreateBuilder().Build();
		RouteHandlerBuilder route = app.MapPost("/validate", (SampleRequest req) => Results.Ok());

		// Act
		RouteHandlerBuilder result = route.WithValidationRules<SampleRequest>(config =>
		{
			config.Property(x => x.Title).Required();
		});

		// Assert
		result.ShouldBeSameAs(route);
	}

	[Fact]
	public void WithValidationRules_WithMultipleRules_ReturnsSameBuilder()
	{
		// Arrange
		WebApplication app = WebApplication.CreateBuilder().Build();
		RouteHandlerBuilder route = app.MapPost("/validate-multi", (SampleRequest req) => Results.Ok());

		// Act
		RouteHandlerBuilder result = route.WithValidationRules<SampleRequest>(config =>
		{
			config.Property(x => x.Title)
				.Required()
				.MinLength(5)
				.MaxLength(100);

			config.Property(x => x.Description)
				.MaxLength(500);
		});

		// Assert
		result.ShouldBeSameAs(route);
	}

	[Fact]
	public void WithValidationRules_NullConfigure_ThrowsArgumentNullException()
	{
		// Arrange
		WebApplication app = WebApplication.CreateBuilder().Build();
		RouteHandlerBuilder route = app.MapPost("/validate2", (SampleRequest req) => Results.Ok());

		// Act & Assert
		Should.Throw<ArgumentNullException>(() => route.WithValidationRules<SampleRequest>(null!));
	}

	[Fact]
	public void WithValidationRules_WithAllRuleTypes_ReturnsSameBuilder()
	{
		// Arrange
		WebApplication app = WebApplication.CreateBuilder().Build();
		RouteHandlerBuilder route = app.MapPost("/validate-all", (SampleRequest req) => Results.Ok());

		// Act
		RouteHandlerBuilder result = route.WithValidationRules<SampleRequest>(config =>
		{
			config.Property(x => x.Title)
				.Required()
				.MinLength(1)
				.MaxLength(100)
				.Length(1, 100)
				.Pattern(@"^[a-zA-Z]+$")
				.Email()
				.Url()
				.Custom("Custom validation")
				.GreaterThan(10)
				.GreaterThanOrEqual(10)
				.LessThan(100)
				.LessThanOrEqual(100)
				.Between(10, 100);
		});

		// Assert
		result.ShouldBeSameAs(route);
	}

	class SampleRequest
	{
		public string? Title { get; set; }
		public string? Description { get; set; }
	}
}
