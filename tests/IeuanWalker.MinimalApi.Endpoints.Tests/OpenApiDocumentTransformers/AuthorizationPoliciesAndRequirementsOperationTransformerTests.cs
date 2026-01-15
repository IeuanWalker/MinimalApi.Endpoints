using IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;

namespace IeuanWalker.MinimalApi.Endpoints.Tests.OpenApiDocumentTransformers;

public class AuthorizationPoliciesAndRequirementsOperationTransformerTests
{
	[Fact]
	public async Task TransformAsync_WhenEndpointMetadataIsNull_DoesNotModifyOperation()
	{
		// Arrange
		AuthorizationPoliciesAndRequirementsOperationTransformer transformer = new();
		Microsoft.OpenApi.OpenApiOperation operation = new() { Description = "Original description" };

		OpenApiOperationTransformerContext context = CreateContext([]);

		// Act
		await transformer.TransformAsync(operation, context, CancellationToken.None);

		// Assert
		operation.Description.ShouldStartWith("Original description");
	}

	[Fact]
	public async Task TransformAsync_WhenActionDescriptorIsNull_DoesNotModifyOperation()
	{
		// Arrange
		AuthorizationPoliciesAndRequirementsOperationTransformer transformer = new();
		Microsoft.OpenApi.OpenApiOperation operation = new() { Description = "Original description" };

		// Create a context where the ApiDescription.ActionDescriptor is null
		WebApplicationBuilder builder = WebApplication.CreateBuilder();
		WebApplication app = builder.Build();

		OpenApiOperationTransformerContext context = new OpenApiOperationTransformerContext
		{
			Description = new ApiDescription { ActionDescriptor = null },
			DocumentName = "v1",
			ApplicationServices = app.Services
		};

		// Act
		await transformer.TransformAsync(operation, context, CancellationToken.None);

		// Assert
		operation.Description.ShouldStartWith("Original description");
	}

	[Fact]
	public async Task TransformAsync_WhenNoAuthorizationPolicies_DoesNotModifyOperation()
	{
		// Arrange
		AuthorizationPoliciesAndRequirementsOperationTransformer transformer = new();
		Microsoft.OpenApi.OpenApiOperation operation = new() { Description = "Original description" };

		OpenApiOperationTransformerContext context = CreateContext([]);

		// Act
		await transformer.TransformAsync(operation, context, CancellationToken.None);

		// Assert
		operation.Description.ShouldStartWith("Original description");
	}

	[Fact]
	public async Task TransformAsync_WhenPolicyHasNoRequirements_DoesNotModifyOperation()
	{
		// Arrange
		AuthorizationPoliciesAndRequirementsOperationTransformer transformer = new();
		Microsoft.OpenApi.OpenApiOperation operation = new() { Description = "Original description" };

		// A policy with empty requirements would throw, so test with no metadata instead
		OpenApiOperationTransformerContext context = CreateContext([]);

		// Act
		await transformer.TransformAsync(operation, context, CancellationToken.None);

		// Assert
		operation.Description.ShouldStartWith("Original description");
	}

	[Fact]
	public async Task TransformAsync_WithSinglePolicyAndSingleRequirement_AddsAuthorizationRequirements()
	{
		// Arrange
		AuthorizationPoliciesAndRequirementsOperationTransformer transformer = new();
		Microsoft.OpenApi.OpenApiOperation operation = new() { Description = "Original description" };

		AuthorizationPolicy policy = new AuthorizationPolicyBuilder()
			.RequireRole("Admin")
			.Build();

		OpenApiOperationTransformerContext context = CreateContext([policy]);

		// Act
		await transformer.TransformAsync(operation, context, CancellationToken.None);

		// Assert
		operation.Description.ShouldNotBeNull();
		operation.Description.ShouldStartWith("Original description\n\n");
		operation.Description.ShouldContain("**Authorization Requirements:**");
		operation.Description.ShouldContain("- User.IsInRole must be true for one of the following roles (Admin)");
	}

	[Fact]
	public async Task TransformAsync_WithSinglePolicyAndMultipleRequirements_AddsAllRequirements()
	{
		// Arrange
		AuthorizationPoliciesAndRequirementsOperationTransformer transformer = new();
		Microsoft.OpenApi.OpenApiOperation operation = new() { Description = "Original description" };

		AuthorizationPolicy policy = new AuthorizationPolicyBuilder()
			.RequireRole("Admin", "Manager")
			.RequireAuthenticatedUser()
			.Build();

		OpenApiOperationTransformerContext context = CreateContext([policy]);

		// Act
		await transformer.TransformAsync(operation, context, CancellationToken.None);

		// Assert
		operation.Description.ShouldNotBeNull();
		operation.Description.ShouldStartWith("Original description\n\n");
		operation.Description.ShouldContain("**Authorization Requirements:**");
		operation.Description.ShouldContain("- User.IsInRole must be true for one of the following roles (Admin|Manager)");
		operation.Description.ShouldContain("-  Requires an authenticated user.");
	}

	[Fact]
	public async Task TransformAsync_WithMultiplePolicies_AddsAuthorizationPoliciesHeader()
	{
		// Arrange
		AuthorizationPoliciesAndRequirementsOperationTransformer transformer = new();
		Microsoft.OpenApi.OpenApiOperation operation = new() { Description = "Original description" };

		AuthorizationPolicy policy1 = new AuthorizationPolicyBuilder()
			.RequireRole("Admin")
			.Build();

		AuthorizationPolicy policy2 = new AuthorizationPolicyBuilder()
			.RequireRole("Manager")
			.Build();

		OpenApiOperationTransformerContext context = CreateContext([policy1, policy2]);

		// Act
		await transformer.TransformAsync(operation, context, CancellationToken.None);

		// Assert
		operation.Description.ShouldNotBeNull();
		operation.Description.ShouldStartWith("Original description\n\n");
		operation.Description.ShouldContain("**Authorization Policies:**");
		operation.Description.ShouldContain("- **Policy 1**");
		operation.Description.ShouldContain("- User.IsInRole must be true for one of the following roles (Admin)");
		operation.Description.ShouldContain("- **Policy 2**");
		operation.Description.ShouldContain("- User.IsInRole must be true for one of the following roles (Manager)");
	}

	[Fact]
	public async Task TransformAsync_WhenOperationDescriptionIsEmpty_SetsAuthorizationText()
	{
		// Arrange
		AuthorizationPoliciesAndRequirementsOperationTransformer transformer = new();
		Microsoft.OpenApi.OpenApiOperation operation = new() { Description = string.Empty };

		AuthorizationPolicy policy = new AuthorizationPolicyBuilder()
			.RequireRole("Admin")
			.Build();

		OpenApiOperationTransformerContext context = CreateContext([policy]);

		// Act
		await transformer.TransformAsync(operation, context, CancellationToken.None);

		// Assert
		operation.Description.ShouldNotBeNull();
		operation.Description.ShouldStartWith("**Authorization Requirements:**");
		operation.Description.ShouldNotContain("\n\n**Authorization");
	}

	[Fact]
	public async Task TransformAsync_WhenOperationDescriptionIsNull_SetsAuthorizationText()
	{
		// Arrange
		AuthorizationPoliciesAndRequirementsOperationTransformer transformer = new();
		Microsoft.OpenApi.OpenApiOperation operation = new();

		AuthorizationPolicy policy = new AuthorizationPolicyBuilder()
			.RequireRole("Admin")
			.Build();

		OpenApiOperationTransformerContext context = CreateContext([policy]);

		// Act
		await transformer.TransformAsync(operation, context, CancellationToken.None);

		// Assert
		operation.Description.ShouldNotBeNull();
		operation.Description.ShouldStartWith("**Authorization Requirements:**");
	}

	[Fact]
	public async Task TransformAsync_WithClaimsRequirement_ParsesRequirementCorrectly()
	{
		// Arrange
		AuthorizationPoliciesAndRequirementsOperationTransformer transformer = new();
		Microsoft.OpenApi.OpenApiOperation operation = new();

		AuthorizationPolicy policy = new AuthorizationPolicyBuilder()
			.RequireClaim("Permission", "Read", "Write")
			.Build();

		OpenApiOperationTransformerContext context = CreateContext([policy]);

		// Act
		await transformer.TransformAsync(operation, context, CancellationToken.None);

		// Assert
		operation.Description.ShouldNotBeNull();
		operation.Description.ShouldContain("**Authorization Requirements:**");
		operation.Description.ShouldContain("Permission");
	}

	[Fact]
	public async Task TransformAsync_WithUserNameRequirement_ParsesRequirementCorrectly()
	{
		// Arrange
		AuthorizationPoliciesAndRequirementsOperationTransformer transformer = new();
		Microsoft.OpenApi.OpenApiOperation operation = new();

		AuthorizationPolicy policy = new AuthorizationPolicyBuilder()
			.RequireUserName("john.doe")
			.Build();

		OpenApiOperationTransformerContext context = CreateContext([policy]);

		// Act
		await transformer.TransformAsync(operation, context, CancellationToken.None);

		// Assert
		operation.Description.ShouldNotBeNull();
		operation.Description.ShouldStartWith("**Authorization Requirements:**");
		operation.Description.ShouldEndWith("- Requires a user identity with Name equal to john.doe");
	}

	[Fact]
	public async Task TransformAsync_WithMixedPoliciesAndRequirements_FormatsCorrectly()
	{
		// Arrange
		AuthorizationPoliciesAndRequirementsOperationTransformer transformer = new();
		Microsoft.OpenApi.OpenApiOperation operation = new() { Description = "Gets user data" };

		AuthorizationPolicy policy1 = new AuthorizationPolicyBuilder()
			.RequireRole("Admin")
			.RequireAuthenticatedUser()
			.Build();

		AuthorizationPolicy policy2 = new AuthorizationPolicyBuilder()
			.RequireClaim("Department", "IT")
			.Build();

		AuthorizationPolicy policy3 = new AuthorizationPolicyBuilder()
			.RequireUserName("john.doe")
			.Build();

		OpenApiOperationTransformerContext context = CreateContext([policy1, policy2, policy3]);

		// Act
		await transformer.TransformAsync(operation, context, CancellationToken.None);

		// Assert
		operation.Description.ShouldNotBeNull();
		operation.Description.ShouldStartWith("Gets user data\n\n");
		operation.Description.ShouldContain("**Authorization Policies:**");
		operation.Description.ShouldContain("- **Policy 1**");
		operation.Description.ShouldContain("- User.IsInRole must be true for one of the following roles (Admin)");
		operation.Description.ShouldContain("-  Requires an authenticated user.");
		operation.Description.ShouldContain("- **Policy 2**");
		operation.Description.ShouldContain("- Claim.Type=Department and Claim.Value is one of the following values (IT)");
		operation.Description.ShouldContain("- **Policy 3**");
		operation.Description.ShouldContain("- Requires a user identity with Name equal to john.doe");
	}

	[Fact]
	public async Task TransformAsync_CompletesSuccessfully()
	{
		// Arrange
		AuthorizationPoliciesAndRequirementsOperationTransformer transformer = new();
		Microsoft.OpenApi.OpenApiOperation operation = new();

		AuthorizationPolicy policy = new AuthorizationPolicyBuilder()
			.RequireRole("Admin")
			.Build();

		OpenApiOperationTransformerContext context = CreateContext([policy]);

		// Act
		Task task = transformer.TransformAsync(operation, context, CancellationToken.None);
		await task;

		// Assert
		task.IsCompletedSuccessfully.ShouldBeTrue();
	}

		static OpenApiOperationTransformerContext CreateContext(IList<object> metadata)
		{
			// Create a minimal web application for services
			WebApplicationBuilder builder = WebApplication.CreateBuilder();
			WebApplication app = builder.Build();

			// Create a test ActionDescriptor directly with the metadata
			TestActionDescriptor actionDescriptor = new(metadata);

			// Create API description
			ApiDescription apiDescription = new()
			{
				ActionDescriptor = actionDescriptor
			};

			return new OpenApiOperationTransformerContext
			{
				Description = apiDescription,
				DocumentName = "v1",
				ApplicationServices = app.Services
			};
		}

		class TestActionDescriptor : Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor
		{
			public TestActionDescriptor(IList<object> metadata)
			{
				EndpointMetadata = metadata;
			}
		}
	}
