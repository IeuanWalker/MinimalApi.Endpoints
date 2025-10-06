using Shouldly;
using IeuanWalker.MinimalApi.Endpoints.Generator.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IeuanWalker.MinimalApi.Endpoints.Generator.Tests.Helpers;

public class WithTagsHelpersTests
{
	#region GetTags Tests

	[Fact]
	public void GetTags_WithNoConfigureMethod_ReturnsNull()
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
		string? result = typeDeclaration.GetTags();

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void GetTags_WithConfigureMethodButNoWithTags_ReturnsNull()
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
		string? result = typeDeclaration.GetTags();

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void GetTags_WithSimpleWithTagsCall_ReturnsTag()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Get("/test").WithTags("TestTag");
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = GetTypeDeclaration(sourceCode);

		// Act
		string? result = typeDeclaration.GetTags();

		// Assert
		result.ShouldBe("TestTag");
	}

	[Fact]
	public void GetTags_WithChainedMethodCalls_ReturnsTag()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder
						.Get("/test")
						.WithTags("ChainedTag")
						.WithName("TestName");
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = GetTypeDeclaration(sourceCode);

		// Act
		string? result = typeDeclaration.GetTags();

		// Assert
		result.ShouldBe("ChainedTag");
	}

	[Fact]
	public void GetTags_WithMultipleWithTagsCalls_ReturnsFirstTag()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Get("/test").WithTags("FirstTag");
					builder.Post("/test").WithTags("SecondTag");
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = GetTypeDeclaration(sourceCode);

		// Act
		string? result = typeDeclaration.GetTags();

		// Assert
		result.ShouldBe("FirstTag");
	}

	[Fact]
	public void GetTags_WithWithTagsAtBeginningOfChain_ReturnsTag()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.WithTags("BeginningTag").Get("/test").WithName("TestName");
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = GetTypeDeclaration(sourceCode);

		// Act
		string? result = typeDeclaration.GetTags();

		// Assert
		result.ShouldBe("BeginningTag");
	}

	[Fact]
	public void GetTags_WithNonStaticConfigureMethod_ReturnsNull()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public void Configure(RouteHandlerBuilder builder)
				{
					builder.Get("/test").WithTags("InstanceMethod");
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = GetTypeDeclaration(sourceCode);

		// Act
		string? result = typeDeclaration.GetTags();

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void GetTags_WithWrongParameterType_ReturnsNull()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(string builder)
				{
					builder.WithTags("WrongParam");
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = GetTypeDeclaration(sourceCode);

		// Act
		string? result = typeDeclaration.GetTags();

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void GetTags_WithEmptyStringArgument_ReturnsEmptyString()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Get("/test").WithTags("");
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = GetTypeDeclaration(sourceCode);

		// Act
		string? result = typeDeclaration.GetTags();

		// Assert
		result.ShouldBe("");
	}

	[Fact]
	public void GetTags_WithComplexMethodChaining_ReturnsTag()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder
						.MapPost("/users")
						.WithTags("Users")
						.WithName("CreateUser")
						.WithSummary("Create a new user")
						.Produces<User>(201)
						.ProducesValidationProblem();
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = GetTypeDeclaration(sourceCode);

		// Act
		string? result = typeDeclaration.GetTags();

		// Assert
		result.ShouldBe("Users");
	}

	#endregion

	#region GenerateAndAddTags Tests

	[Theory]
	[InlineData("/api/users", "Users")]
	[InlineData("/api/products", "Products")]
	[InlineData("/api/orders", "Orders")]
	[InlineData("/API/USERS", "USERS")]
	[InlineData("/Api/Products", "Products")]
	[InlineData("/users", "Users")]
	[InlineData("/api/users/{id}", "Users")]
	[InlineData("/api/products/{productId}", "Products")]
	[InlineData("/api/users/{userId}/posts/{postId}", "Users")]
	[InlineData("/users/{userId}/settings", "Users")]
	[InlineData("/api/organizations/{orgId}/projects/{projectId}", "Organizations")]
	[InlineData("/api/v1/users", "Users")]
	[InlineData("/api/v2/products", "Products")]
	[InlineData("/api/v10/orders", "Orders")]
	[InlineData("/api/v{version:apiVersion}/users", "Users")]
	[InlineData("/v1/products", "Products")]
	[InlineData("/V2/orders", "Orders")]
	[InlineData("/rootName/api/v1/users", "Users")]
	[InlineData("/rootName/api/products", "Products")]
	[InlineData("/company/api/v2/orders", "Orders")]
	[InlineData("/app/api/users/{id}", "Users")]
	[InlineData("/root/users/{id}", "Root")]
	[InlineData("/app/products", "App")]
	[InlineData("/company/orders/{orderId}", "Company")]
	[InlineData("/api/users?filter=active", "Users")]
	[InlineData("/api/products?page=1&size=10", "Products")]
	[InlineData("/users?sort=name", "Users")]
	[InlineData("/api/v1/organizations/{orgId}/projects/{projectId}/issues/{issueId}/comments", "Organizations")]
	[InlineData("/rootName/api/ApiName", "ApiName")]
	[InlineData("/rootName/api/v1/ApiName", "ApiName")]
	[InlineData("/api/v{version:apiVersion}/Users", "Users")]
	[InlineData("/api/v1/Users/{userId}/Posts/{postId}", "Users")]
	[InlineData("/root/ApiName/{Id}", "Root")]
	[InlineData("/api/v1.5/users", "Users")]
	[InlineData("/api/v2.0/products", "Products")]
	[InlineData("/api/v3.14/items", "Items")]
	[InlineData("/api/v1.2.3/services", "Services")]
	[InlineData("/{param1}/{param2}/users", "Users")]
	[InlineData("/api/{param}/users", "Users")]
	[InlineData("/root/{id}/products/{productId}/details", "Root")]
	[InlineData("/root/v1", "Root")]
	[InlineData("/company/v2", "Company")]
	[InlineData("/app/v{version}", "App")]
	public void GenerateAndAddTags_WithBasicPatterns_GeneratesCorrectTags(string pattern, string expectedTag)
	{
		// Arrange
		using IndentedTextBuilder builder = new();

		// Act
		builder.GenerateAndAddTags(pattern);
		string result = builder.ToString();

		// Assert
		result.ShouldContain($".WithTags(\"{expectedTag}\")");
	}

	[Fact]
	public void GenerateAndAddTags_WithEmptyPattern_DoesNotGenerateTags()
	{
		// Arrange
		using IndentedTextBuilder builder = new();

		// Act
		builder.GenerateAndAddTags("");
		string result = builder.ToString();

		// Assert
		result.ShouldBeEmpty();
	}

	[Fact]
	public void GenerateAndAddTags_WithNullPattern_DoesNotGenerateTags()
	{
		// Arrange
		using IndentedTextBuilder builder = new();

		// Act
		builder.GenerateAndAddTags(null!);
		string result = builder.ToString();

		// Assert
		result.ShouldBeEmpty();
	}

	[Fact]
	public void GenerateAndAddTags_WithSlashOnlyPattern_DoesNotGenerateTags()
	{
		// Arrange
		using IndentedTextBuilder builder = new();

		// Act
		builder.GenerateAndAddTags("/");
		string result = builder.ToString();

		// Assert
		result.ShouldBeEmpty();
	}

	[Theory]
	[InlineData("/{id}")]
	[InlineData("/{userId}/{postId}")]
	[InlineData("/api/{id}")]
	[InlineData("/api/v1/{userId}")]
	public void GenerateAndAddTags_WithOnlyParametersAndVersions_DoesNotGenerateTags(string pattern)
	{
		// Arrange
		using IndentedTextBuilder builder = new();

		// Act
		builder.GenerateAndAddTags(pattern);
		string result = builder.ToString();

		// Assert
		result.ShouldBeEmpty();
	}

	#endregion

	#region ExtractApiNameFromPattern Integration Tests (via GenerateAndAddTags)

	[Theory]
	[InlineData("/v1")]
	[InlineData("/api")]
	[InlineData("/api/v1")]
	[InlineData("/api/v{version:apiVersion}")]
	public void GenerateAndAddTags_WithOnlyVersionOrApiSegments_DoesNotGenerateTags(string pattern)
	{
		// Arrange
		using IndentedTextBuilder builder = new();

		// Act
		builder.GenerateAndAddTags(pattern);
		string result = builder.ToString();

		// Assert
		result.ShouldBeEmpty();
	}

	[Fact]
	public void GenerateAndAddTags_WithEdgeCaseAllParameters_DoesNotGenerateTags()
	{
		// Arrange
		using IndentedTextBuilder builder = new();

		// Act
		builder.GenerateAndAddTags("/{param1}/{param2}/{param3}");
		string result = builder.ToString();

		// Assert
		result.ShouldBeEmpty();
	}

	[Fact]
	public void GenerateAndAddTags_WithEdgeCaseAllVersions_DoesNotGenerateTags()
	{
		// Arrange
		using IndentedTextBuilder builder = new();

		// Act
		builder.GenerateAndAddTags("/v1/v2/v3");
		string result = builder.ToString();

		// Assert
		result.ShouldBeEmpty();
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
