using Shouldly;
using IeuanWalker.MinimalApi.Endpoints.Generator.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IeuanWalker.MinimalApi.Endpoints.Generator.Tests;

public class HttpVerbRouteHelpersTests
{
	#region GetVerbAndPattern Tests

	[Theory]
	[InlineData(HttpVerb.Get, "/test")]
	[InlineData(HttpVerb.Post, "/users")]
	[InlineData(HttpVerb.Put, "/users/{id}")]
	[InlineData(HttpVerb.Patch, "/users/{id}")]
	[InlineData(HttpVerb.Delete, "/users/{id}")]
	public void GetVerbAndPattern_WithValidVerb_ReturnsExpectedVerbAndPattern(HttpVerb expectedVerb, string expectedPattern)
	{
		// Arrange
		string sourceCode = $$"""
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.{{expectedVerb}}("{{expectedPattern}}");
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = ParseTypeDeclaration(sourceCode);
		List<DiagnosticInfo> diagnostics = [];

		// Act
		(HttpVerb verb, string pattern)? result = typeDeclaration.GetVerbAndPattern("TestEndpoint", diagnostics);

		// Assert
		result.ShouldNotBeNull();
		result.Value.verb.ShouldBe(expectedVerb);
		result.Value.pattern.ShouldBe(expectedPattern);
		diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void GetVerbAndPattern_WithNoConfigureMethod_ReturnsNull()
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

		TypeDeclarationSyntax typeDeclaration = ParseTypeDeclaration(sourceCode);
		List<DiagnosticInfo> diagnostics = [];

		// Act
		(HttpVerb verb, string pattern)? result = typeDeclaration.GetVerbAndPattern("TestEndpoint", diagnostics);

		// Assert
		result.ShouldBeNull();
		diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void GetVerbAndPattern_WithNonStaticConfigureMethod_ReturnsNull()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public void Configure(RouteHandlerBuilder builder)
				{
					builder.Get("/test");
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = ParseTypeDeclaration(sourceCode);
		List<DiagnosticInfo> diagnostics = [];

		// Act
		(HttpVerb verb, string pattern)? result = typeDeclaration.GetVerbAndPattern("TestEndpoint", diagnostics);

		// Assert
		result.ShouldBeNull();
		diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void GetVerbAndPattern_WithNoHttpVerbCalls_ReturnsNullAndReportsDiagnostic()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.WithName("Test");
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = ParseTypeDeclaration(sourceCode);
		List<DiagnosticInfo> diagnostics = [];

		// Act
		(HttpVerb verb, string pattern)? result = typeDeclaration.GetVerbAndPattern("TestEndpoint", diagnostics);

		// Assert
		result.ShouldBeNull();
		diagnostics.ShouldHaveSingleItem();
		diagnostics[0].Id.ShouldBe("MINAPI001");
		diagnostics[0].Title.ShouldBe("No HTTP verb configured");
		diagnostics[0].MessageArgs.ShouldHaveSingleItem();
		diagnostics[0].MessageArgs[0].ShouldBe("TestEndpoint");
	}

	[Fact]
	public void GetVerbAndPattern_WithMultipleHttpVerbCalls_ReturnsNullAndReportsDiagnostics()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Get("/test");
					builder.Post("/test");
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = ParseTypeDeclaration(sourceCode);
		List<DiagnosticInfo> diagnostics = [];

		// Act
		(HttpVerb verb, string pattern)? result = typeDeclaration.GetVerbAndPattern("TestEndpoint", diagnostics);

		// Assert
		result.ShouldBeNull();
		diagnostics.Count.ShouldBe(2);

		diagnostics[0].Id.ShouldBe("MINAPI002");
		diagnostics[0].Title.ShouldBe("Multiple HTTP verbs configured");
		diagnostics[0].MessageArgs.ShouldHaveSingleItem();
		diagnostics[0].MessageArgs[0].ShouldBe("Get");

		diagnostics[1].Id.ShouldBe("MINAPI002");
		diagnostics[1].Title.ShouldBe("Multiple HTTP verbs configured");
		diagnostics[1].MessageArgs.ShouldHaveSingleItem();
		diagnostics[1].MessageArgs[0].ShouldBe("Post");
	}

	[Fact]
	public void GetVerbAndPattern_WithHttpVerbButNoRoutePattern_ReturnsNull()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Get();
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = ParseTypeDeclaration(sourceCode);
		List<DiagnosticInfo> diagnostics = [];

		// Act
		(HttpVerb verb, string pattern)? result = typeDeclaration.GetVerbAndPattern("TestEndpoint", diagnostics);

		// Assert
		result.ShouldBeNull();
		diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void GetVerbAndPattern_WithHttpVerbAndNonStringArgument_ReturnsNull()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					var pattern = "/test";
					builder.Get(pattern);
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = ParseTypeDeclaration(sourceCode);
		List<DiagnosticInfo> diagnostics = [];

		// Act
		(HttpVerb verb, string pattern)? result = typeDeclaration.GetVerbAndPattern("TestEndpoint", diagnostics);

		// Assert
		result.ShouldBeNull();
		diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void GetVerbAndPattern_WithComplexRoutePattern_ReturnsCorrectPattern()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Get("/api/v1/users/{id}/posts/{postId}");
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = ParseTypeDeclaration(sourceCode);
		List<DiagnosticInfo> diagnostics = [];

		// Act
		(HttpVerb verb, string pattern)? result = typeDeclaration.GetVerbAndPattern("TestEndpoint", diagnostics);

		// Assert
		result.ShouldNotBeNull();
		result.Value.verb.ShouldBe(HttpVerb.Get);
		result.Value.pattern.ShouldBe("/api/v1/users/{id}/posts/{postId}");
		diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void GetVerbAndPattern_WithMethodChaining_ReturnsFirstHttpVerb()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder
						.Get("/test")
						.WithName("TestEndpoint")
						.WithSummary("Test endpoint");
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = ParseTypeDeclaration(sourceCode);
		List<DiagnosticInfo> diagnostics = [];

		// Act
		(HttpVerb verb, string pattern)? result = typeDeclaration.GetVerbAndPattern("TestEndpoint", diagnostics);

		// Assert
		result.ShouldNotBeNull();
		result.Value.verb.ShouldBe(HttpVerb.Get);
		result.Value.pattern.ShouldBe("/test");
		diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void GetVerbAndPattern_WithThreeHttpVerbCalls_ReturnsNullAndReportsAllDiagnostics()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Get("/test");
					builder.Post("/test");
					builder.Put("/test");
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = ParseTypeDeclaration(sourceCode);
		List<DiagnosticInfo> diagnostics = [];

		// Act
		(HttpVerb verb, string pattern)? result = typeDeclaration.GetVerbAndPattern("TestEndpoint", diagnostics);

		// Assert
		result.ShouldBeNull();
		diagnostics.Count.ShouldBe(3);

		diagnostics.All(d => d.Id == "MINAPI002").ShouldBeTrue();
		diagnostics.All(d => d.Title == "Multiple HTTP verbs configured").ShouldBeTrue();

		diagnostics[0].MessageArgs[0].ShouldBe("Get");
		diagnostics[1].MessageArgs[0].ShouldBe("Post");
		diagnostics[2].MessageArgs[0].ShouldBe("Put");
	}

	#endregion

	#region ToMap Tests

	[Theory]
	[InlineData(HttpVerb.Get, "MapGet")]
	[InlineData(HttpVerb.Post, "MapPost")]
	[InlineData(HttpVerb.Put, "MapPut")]
	[InlineData(HttpVerb.Patch, "MapPatch")]
	[InlineData(HttpVerb.Delete, "MapDelete")]
	public void ToMap_WithValidHttpVerb_ReturnsCorrectMapMethod(HttpVerb verb, string expectedMapMethod)
	{
		// Act
		string result = verb.ToMap();

		// Assert
		result.ShouldBe(expectedMapMethod);
	}

	[Fact]
	public void ToMap_WithInvalidHttpVerb_ThrowsArgumentOutOfRangeException()
	{
		// Arrange
		const HttpVerb invalidVerb = (HttpVerb)999;

		// Act & Assert
		Should.Throw<ArgumentOutOfRangeException>(() => invalidVerb.ToMap())
			.ParamName.ShouldBe("verb");
	}

	#endregion

	#region Integration Tests

	[Fact]
	public void GetVerbAndPattern_WithRealWorldExample_WorksCorrectly()
	{
		// Arrange
		const string sourceCode = """
			public class GetUserByIdEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder
						.Get("/api/v1/users/{id}")
						.WithName("GetUserById")
						.WithSummary("Gets a user by their ID")
						.Produces<UserResponse>(200)
						.ProducesProblem(404);
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = ParseTypeDeclaration(sourceCode);
		List<DiagnosticInfo> diagnostics = [];

		// Act
		(HttpVerb verb, string pattern)? result = typeDeclaration.GetVerbAndPattern("GetUserByIdEndpoint", diagnostics);

		// Assert
		result.ShouldNotBeNull();
		result.Value.verb.ShouldBe(HttpVerb.Get);
		result.Value.pattern.ShouldBe("/api/v1/users/{id}");
		diagnostics.ShouldBeEmpty();

		// Verify ToMap works correctly with the result
		result.Value.verb.ToMap().ShouldBe("MapGet");
	}

	[Fact]
	public void GetVerbAndPattern_WithNestedMethodCalls_WorksCorrectly()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					var configuredBuilder = builder
						.WithName("Test")
						.WithSummary("Test endpoint");
					
					configuredBuilder.Post("/test");
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = ParseTypeDeclaration(sourceCode);
		List<DiagnosticInfo> diagnostics = [];

		// Act
		(HttpVerb verb, string pattern)? result = typeDeclaration.GetVerbAndPattern("TestEndpoint", diagnostics);

		// Assert
		result.ShouldNotBeNull();
		result.Value.verb.ShouldBe(HttpVerb.Post);
		result.Value.pattern.ShouldBe("/test");
		diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void GetVerbAndPattern_WithEmptyRoutePattern_ReturnsEmptyString()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Get("");
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = ParseTypeDeclaration(sourceCode);
		List<DiagnosticInfo> diagnostics = [];

		// Act
		(HttpVerb verb, string pattern)? result = typeDeclaration.GetVerbAndPattern("TestEndpoint", diagnostics);

		// Assert
		result.ShouldNotBeNull();
		result.Value.verb.ShouldBe(HttpVerb.Get);
		result.Value.pattern.ShouldBe("");
		diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void GetVerbAndPattern_WithRootPattern_ReturnsSlash()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Get("/");
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = ParseTypeDeclaration(sourceCode);
		List<DiagnosticInfo> diagnostics = [];

		// Act
		(HttpVerb verb, string pattern)? result = typeDeclaration.GetVerbAndPattern("TestEndpoint", diagnostics);

		// Assert
		result.ShouldNotBeNull();
		result.Value.verb.ShouldBe(HttpVerb.Get);
		result.Value.pattern.ShouldBe("/");
		diagnostics.ShouldBeEmpty();
	}

	#endregion

	#region Edge Cases

	// TODO: Fix - Should only find methods with RouteHandlerBuilder parameter
	[Fact]
	public void GetVerbAndPattern_WithConfigureMethodWithDifferentParameters_ReturnsNullAndReportsDiagnostic()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure()
				{
					// No parameters - this is still a valid Configure method but with no HTTP verbs
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = ParseTypeDeclaration(sourceCode);
		List<DiagnosticInfo> diagnostics = [];

		// Act
		(HttpVerb verb, string pattern)? result = typeDeclaration.GetVerbAndPattern("TestEndpoint", diagnostics);

		// Assert
		result.ShouldBeNull();
		diagnostics.ShouldHaveSingleItem();
		diagnostics[0].Id.ShouldBe("MINAPI001");
		diagnostics[0].Title.ShouldBe("No HTTP verb configured");
		diagnostics[0].MessageArgs.ShouldHaveSingleItem();
		diagnostics[0].MessageArgs[0].ShouldBe("TestEndpoint");
	}

	#endregion

	#region Helper Methods

	static TypeDeclarationSyntax ParseTypeDeclaration(string sourceCode)
	{
		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
		CompilationUnitSyntax root = syntaxTree.GetCompilationUnitRoot();
		return root.DescendantNodes().OfType<TypeDeclarationSyntax>().First();
	}

	#endregion
}
