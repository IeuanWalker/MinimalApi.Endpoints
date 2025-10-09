using IeuanWalker.MinimalApi.Endpoints.Generator.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IeuanWalker.MinimalApi.Endpoints.Generator.Tests.Helpers;

public class WithNameHelpersTests
{
	#region GetWithName Tests

	[Fact]
	public void GetWithName_WithNoConfigureMethod_ReturnsNull()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public void SomeOtherMethod()
				{
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = GetTypeDeclaration(sourceCode);

		// Act
		string? result = typeDeclaration.GetWithName();

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void GetWithName_WithConfigureMethodButNoWithName_ReturnsNull()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Get("/test");
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = GetTypeDeclaration(sourceCode);

		// Act
		string? result = typeDeclaration.GetWithName();

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void GetWithName_WithSimpleWithNameCall_ReturnsName()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Get("/test").WithName("GetTest");
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = GetTypeDeclaration(sourceCode);

		// Act
		string? result = typeDeclaration.GetWithName();

		// Assert
		result.ShouldBe("GetTest");
	}

	[Fact]
	public void GetWithName_WithChainedMethodCalls_ReturnsName()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder
						.Get("/test")
						.WithName("GetTestChained")
						.WithTags("Test");
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = GetTypeDeclaration(sourceCode);

		// Act
		string? result = typeDeclaration.GetWithName();

		// Assert
		result.ShouldBe("GetTestChained");
	}

	[Fact]
	public void GetWithName_WithMultipleWithNameCalls_ReturnsFirstName()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Get("/test").WithName("FirstName");
					builder.Post("/test").WithName("SecondName");
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = GetTypeDeclaration(sourceCode);

		// Act
		string? result = typeDeclaration.GetWithName();

		// Assert
		result.ShouldBe("FirstName");
	}

	[Fact]
	public void GetWithName_WithWithNameAtBeginningOfChain_ReturnsName()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.WithName("BeginningName").Get("/test").WithTags("Test");
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = GetTypeDeclaration(sourceCode);

		// Act
		string? result = typeDeclaration.GetWithName();

		// Assert
		result.ShouldBe("BeginningName");
	}

	[Fact]
	public void GetWithName_WithNonStaticConfigureMethod_ReturnsNull()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public void Configure(RouteHandlerBuilder builder)
				{
					builder.Get("/test").WithName("InstanceMethod");
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = GetTypeDeclaration(sourceCode);

		// Act
		string? result = typeDeclaration.GetWithName();

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void GetWithName_WithWrongParameterType_ReturnsNull()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(string builder)
				{
					builder.WithName("WrongParam");
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = GetTypeDeclaration(sourceCode);

		// Act
		string? result = typeDeclaration.GetWithName();

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void GetWithName_WithEmptyStringArgument_ReturnsEmptyString()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Get("/test").WithName("");
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = GetTypeDeclaration(sourceCode);

		// Act
		string? result = typeDeclaration.GetWithName();

		// Assert
		result.ShouldBe("");
	}

	[Fact]
	public void GetWithName_WithVariableArgument_ReturnsNull()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					var name = "DynamicName";
					builder.Get("/test").WithName(name);
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = GetTypeDeclaration(sourceCode);

		// Act
		string? result = typeDeclaration.GetWithName();

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void GetWithName_WithNoArguments_ReturnsNull()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Get("/test").WithName();
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = GetTypeDeclaration(sourceCode);

		// Act
		string? result = typeDeclaration.GetWithName();

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void GetWithName_WithComplexMethodChaining_ReturnsName()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder
						.MapPost("/users")
						.WithName("CreateUser")
						.WithSummary("Create a new user")
						.Produces<User>(201)
						.ProducesValidationProblem();
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = GetTypeDeclaration(sourceCode);

		// Act
		string? result = typeDeclaration.GetWithName();

		// Assert
		result.ShouldBe("CreateUser");
	}

	#endregion

	#region GenerateWithName Tests

	[Theory]
	[InlineData(HttpVerb.Get, "/api/users", 1, "Get_Users_1")]
	[InlineData(HttpVerb.Post, "/api/users", 1, "Post_Users_1")]
	[InlineData(HttpVerb.Put, "/api/users/{id}", 1, "Put_Users_1")]
	[InlineData(HttpVerb.Patch, "/api/users/{id}", 1, "Patch_Users_1")]
	[InlineData(HttpVerb.Delete, "/api/users/{id}", 1, "Delete_Users_1")]
	public void GenerateWithName_WithBasicRoutes_GeneratesCorrectName(HttpVerb verb, string pattern, int routeNumber, string expected)
	{
		// Act
		string result = WithNameHelpers.GenerateWithName(verb, pattern, routeNumber);

		// Assert
		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData("/api/users/{id}", "Users")]
	[InlineData("/api/products/{productId}/reviews/{reviewId}", "ProductsReviews")]
	[InlineData("/users/{userId}/posts", "UsersPosts")]
	[InlineData("/api/users", "Users")]
	[InlineData("/API/USERS", "USERS")]
	[InlineData("/Api/Users", "Users")]
	[InlineData("/api/v1/users", "Users")]
	[InlineData("/api/V1/products", "Products")]
	[InlineData("/v2/orders", "Orders")]
	[InlineData("/V3/items", "Items")]
	[InlineData("/api/v/users", "Users")] // standalone 'v' is removed
	[InlineData("/v/orders", "Orders")] // standalone 'v' is removed
	[InlineData("/api/V/products", "Products")] // standalone 'V' is removed
	[InlineData("/api/v10/users", "Users")] // multi-digit versions like v10 are removed
	[InlineData("/v123/orders", "Orders")] // even larger version numbers are removed
	[InlineData("/api/v1.5/users", "Users")] // decimal versions like v1.5 are removed
	[InlineData("/v2.0/products", "Products")] // decimal versions like v2.0 are removed
	[InlineData("/V3.14/items", "Items")] // decimal versions with more digits are removed
	[InlineData("/api/v1.2.3/users", "Users")] // multiple decimal parts like v1.2.3 are removed
	[InlineData("/users-profiles", "UsersProfiles")]
	[InlineData("/users_settings", "Users_settings")] // Underscore creates word boundary, case preserved
	[InlineData("/users.config", "UsersConfig")]
	[InlineData("/users@domain", "UsersDomain")]
	[InlineData("/users#section", "UsersSection")]
	[InlineData("/users   profiles", "UsersProfiles")]
	[InlineData("/users\t\tprofiles", "UsersProfiles")]
	[InlineData("/users\n\nprofiles", "UsersProfiles")]
	[InlineData("/   users   profiles   ", "UsersProfiles")]
	[InlineData("/user-profile", "UserProfile")]
	[InlineData("/product-category", "ProductCategory")]
	[InlineData("/order-item", "OrderItem")]
	[InlineData("/single", "Single")]
	[InlineData("/a", "A")]
	[InlineData("/A", "A")]
	[InlineData("/api/v1/api/users/v2", "Users")]
	[InlineData("/application/users", "ApplicationUsers")]
	[InlineData("/api/v1.5/users/v2.0/profiles", "UsersProfiles")]
	public void GenerateWithName_WithRouteParameters_RemovesParameters(string pattern, string expectedMiddle)
	{
		// Act
		string result = WithNameHelpers.GenerateWithName(HttpVerb.Get, pattern, 1);

		// Assert
		result.ShouldBe($"Get_{expectedMiddle}_1");
	}

	[Fact]
	public void GenerateWithName_WithEmptyPattern_GeneratesWithVerbAndRouteNumber()
	{
		// Act
		string result = WithNameHelpers.GenerateWithName(HttpVerb.Get, "", 1);

		// Assert
		result.ShouldBe("Get__1");
	}

	[Fact]
	public void GenerateWithName_WithSlashOnlyPattern_GeneratesWithVerbAndRouteNumber()
	{
		// Act
		string result = WithNameHelpers.GenerateWithName(HttpVerb.Post, "/", 5);

		// Assert
		result.ShouldBe("Post__5");
	}

	[Fact]
	public void GenerateWithName_WithOnlySpecialCharacters_GeneratesWithVerbAndRouteNumber()
	{
		// Act
		string result = WithNameHelpers.GenerateWithName(HttpVerb.Put, "/!@#$%^&*()", 3);

		// Assert
		result.ShouldBe("Put__3");
	}

	[Fact]
	public void GenerateWithName_WithComplexRoute_GeneratesCorrectName()
	{
		// Arrange
		const string complexRoute = "/api/v1/organizations/{orgId}/projects/{projectId}/issues/{issueId}/comments";

		// Act
		string result = WithNameHelpers.GenerateWithName(HttpVerb.Post, complexRoute, 1);

		// Assert - "api" and "v1" are removed, leaving just the resource names
		result.ShouldBe("Post_OrganizationsProjectsIssuesComments_1");
	}

	[Theory]
	[InlineData(1)]
	[InlineData(5)]
	[InlineData(99)]
	[InlineData(1000)]
	public void GenerateWithName_WithDifferentRouteNumbers_IncludesNumber(int routeNumber)
	{
		// Act
		string result = WithNameHelpers.GenerateWithName(HttpVerb.Get, "/users", routeNumber);

		// Assert
		result.ShouldBe($"Get_Users_{routeNumber}");
	}

	#endregion

	#region Helper Methods

	static TypeDeclarationSyntax GetTypeDeclaration(string sourceCode)
	{
		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
		CompilationUnitSyntax root = syntaxTree.GetCompilationUnitRoot();
		return root.DescendantNodes().OfType<TypeDeclarationSyntax>().First();
	}

	#endregion
}
