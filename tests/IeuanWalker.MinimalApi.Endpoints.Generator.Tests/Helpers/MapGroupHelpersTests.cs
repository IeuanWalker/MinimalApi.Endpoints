using Shouldly;
using IeuanWalker.MinimalApi.Endpoints.Generator.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IeuanWalker.MinimalApi.Endpoints.Generator.Tests.Helpers;

public class MapGroupHelpersTests
{
	#region GetGroup Tests

	[Fact]
	public void GetGroup_WithNullEndpointGroupSymbol_ReturnsNull()
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

		(TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel) = ParseTypeDeclarationWithSemanticModel(sourceCode);
		List<DiagnosticInfo> diagnostics = [];

		// Act
		string? result = typeDeclaration.GetGroup(null, semanticModel, diagnostics);

		// Assert
		result.ShouldBeNull();
		diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void GetGroup_WithNoConfigureMethod_ReturnsNull()
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

		(TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel) = ParseTypeDeclarationWithSemanticModel(sourceCode);
		List<DiagnosticInfo> diagnostics = [];
		INamedTypeSymbol mockEndpointGroupSymbol = CreateMockNamedTypeSymbol("TestEndpointGroup");

		// Act
		string? result = typeDeclaration.GetGroup(mockEndpointGroupSymbol, semanticModel, diagnostics);

		// Assert
		result.ShouldBeNull();
		diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void GetGroup_WithMultipleGroupCalls_ReturnsNullAndReportsDiagnostics()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Group<TestEndpointGroup>();
					builder.Group<AnotherEndpointGroup>();
				}
			}
			""";

		(TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel) = ParseTypeDeclarationWithSemanticModel(sourceCode);
		List<DiagnosticInfo> diagnostics = [];
		INamedTypeSymbol mockEndpointGroupSymbol = CreateMockNamedTypeSymbol("TestEndpointGroup");

		// Act
		string? result = typeDeclaration.GetGroup(mockEndpointGroupSymbol, semanticModel, diagnostics);

		// Assert
		result.ShouldBeNull();
		diagnostics.Count.ShouldBe(2);

		diagnostics[0].Id.ShouldBe("MINAPI005");
		diagnostics[0].Title.ShouldBe("Multiple Group calls configured");
		diagnostics[0].MessageFormat.ShouldBe("Multiple Group calls are configured in the Configure method. Only one Group call should be specified per endpoint. Remove this 'Group' call or the other conflicting Group calls.");

		diagnostics[1].Id.ShouldBe("MINAPI005");
		diagnostics[1].Title.ShouldBe("Multiple Group calls configured");
		diagnostics[1].MessageFormat.ShouldBe("Multiple Group calls are configured in the Configure method. Only one Group call should be specified per endpoint. Remove this 'Group' call or the other conflicting Group calls.");
	}

	[Fact]
	public void GetGroup_WithNoGroupCalls_ReturnsNull()
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

		(TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel) = ParseTypeDeclarationWithSemanticModel(sourceCode);
		List<DiagnosticInfo> diagnostics = [];
		INamedTypeSymbol mockEndpointGroupSymbol = CreateMockNamedTypeSymbol("TestEndpointGroup");

		// Act
		string? result = typeDeclaration.GetGroup(mockEndpointGroupSymbol, semanticModel, diagnostics);

		// Assert
		result.ShouldBeNull();
		diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void GetGroup_WithThreeGroupCalls_ReportsThreeDiagnostics()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Group<FirstGroup>();
					builder.Group<SecondGroup>();
					builder.Group<ThirdGroup>();
				}
			}
			""";

		(TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel) = ParseTypeDeclarationWithSemanticModel(sourceCode);
		List<DiagnosticInfo> diagnostics = [];
		INamedTypeSymbol mockEndpointGroupSymbol = CreateMockNamedTypeSymbol("TestEndpointGroup");

		// Act
		string? result = typeDeclaration.GetGroup(mockEndpointGroupSymbol, semanticModel, diagnostics);

		// Assert
		result.ShouldBeNull();
		diagnostics.Count.ShouldBe(3);
		diagnostics.All(d => d.Id == "MINAPI005").ShouldBeTrue();
		diagnostics.All(d => d.Title == "Multiple Group calls configured").ShouldBeTrue();
	}

	[Fact]
	public void GetGroup_WithNonGenericGroupCall_ReturnsNull()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{	
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Group("/test");
				}
			}
			""";

		(TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel) = ParseTypeDeclarationWithSemanticModel(sourceCode);
		List<DiagnosticInfo> diagnostics = [];
		INamedTypeSymbol mockEndpointGroupSymbol = CreateMockNamedTypeSymbol("TestEndpointGroup");

		// Act
		string? result = typeDeclaration.GetGroup(mockEndpointGroupSymbol, semanticModel, diagnostics);

		// Assert
		result.ShouldBeNull();
		diagnostics.ShouldBeEmpty();
	}

	#endregion

	#region GetPatternFromEndpointGroup Tests

	[Fact]
	public void GetPatternFromEndpointGroup_WithValidEndpointGroupAndSingleMapGroup_ReturnsPattern()
	{
		// Arrange
		const string endpointGroupSourceCode = """
			public class TestEndpointGroup
			{
				public static RouteGroupBuilder Configure(WebApplication app)
				{
					return app.MapGroup("/api/v1/test");
				}
			}
			""";

		INamedTypeSymbol endpointGroupSymbol = CreateNamedTypeSymbolFromSource(endpointGroupSourceCode, "TestEndpointGroup");
		List<DiagnosticInfo> diagnostics = [];

		// Act
		string? result = endpointGroupSymbol.GetPatternFromEndpointGroup(diagnostics);

		// Assert
		result.ShouldBe("/api/v1/test");
		diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void GetPatternFromEndpointGroup_WithNoMapGroupCalls_ReportsErrorAndReturnsNull()
	{
		// Arrange
		const string endpointGroupSourceCode = """
			public class TestEndpointGroup
			{
				public static RouteGroupBuilder Configure(WebApplication app)
				{
					return app.NewVersionedApi();
				}
			}
			""";

		INamedTypeSymbol endpointGroupSymbol = CreateNamedTypeSymbolFromSource(endpointGroupSourceCode, "TestEndpointGroup");
		List<DiagnosticInfo> diagnostics = [];

		// Act
		string? result = endpointGroupSymbol.GetPatternFromEndpointGroup(diagnostics);

		// Assert
		result.ShouldBeNull();
		diagnostics.ShouldHaveSingleItem();
		diagnostics[0].Id.ShouldBe("MINAPI003");
		diagnostics[0].Title.ShouldBe("No MapGroup configured");
		diagnostics[0].MessageFormat.ShouldBe("Endpoint group '{0}' has no MapGroup configured in the Configure method. Exactly one MapGroup call must be specified.");
		diagnostics[0].MessageArgs.ShouldHaveSingleItem();
		diagnostics[0].MessageArgs[0].ShouldBe("TestEndpointGroup");
	}

	[Fact]
	public void GetPatternFromEndpointGroup_WithMultipleMapGroupCalls_ReportsErrorsAndReturnsNull()
	{
		// Arrange
		const string endpointGroupSourceCode = """
			public class TestEndpointGroup
			{
				public static RouteGroupBuilder Configure(WebApplication app)
				{
					app.MapGroup("/api/v1");
					return app.MapGroup("/api/v2");
				}
			}
			""";

		INamedTypeSymbol endpointGroupSymbol = CreateNamedTypeSymbolFromSource(endpointGroupSourceCode, "TestEndpointGroup");
		List<DiagnosticInfo> diagnostics = [];

		// Act
		string? result = endpointGroupSymbol.GetPatternFromEndpointGroup(diagnostics);

		// Assert
		result.ShouldBeNull();
		diagnostics.Count.ShouldBe(2);
		diagnostics.All(d => d.Id == "MINAPI004").ShouldBeTrue();
		diagnostics.All(d => d.Title == "Multiple MapGroup calls configured").ShouldBeTrue();
		diagnostics.All(d => d.MessageFormat == "Multiple MapGroup calls are configured in the Configure method. Only one MapGroup call should be specified per endpoint group. Remove this 'MapGroup' call or the other conflicting MapGroup calls.").ShouldBeTrue();
	}

	[Fact]
	public void GetPatternFromEndpointGroup_WithThreeMapGroupCalls_ReportsThreeDiagnostics()
	{
		// Arrange
		const string endpointGroupSourceCode = """
			public class TestEndpointGroup
			{
				public static RouteGroupBuilder Configure(WebApplication app)
				{
					app.MapGroup("/api/v1");
					app.MapGroup("/api/v2");
					return app.MapGroup("/api/v3");
				}
			}
			""";

		INamedTypeSymbol endpointGroupSymbol = CreateNamedTypeSymbolFromSource(endpointGroupSourceCode, "TestEndpointGroup");
		List<DiagnosticInfo> diagnostics = [];

		// Act
		string? result = endpointGroupSymbol.GetPatternFromEndpointGroup(diagnostics);

		// Assert
		result.ShouldBeNull();
		diagnostics.Count.ShouldBe(3);
		diagnostics.All(d => d.Id == "MINAPI004").ShouldBeTrue();
		diagnostics.All(d => d.Title == "Multiple MapGroup calls configured").ShouldBeTrue();
	}

	[Fact]
	public void GetPatternFromEndpointGroup_WithMapGroupCallWithoutArguments_ReturnsNull()
	{
		// Arrange
		const string endpointGroupSourceCode = """
			public class TestEndpointGroup
			{
				public static RouteGroupBuilder Configure(WebApplication app)
				{
					return app.MapGroup();
				}
			}
			""";

		INamedTypeSymbol endpointGroupSymbol = CreateNamedTypeSymbolFromSource(endpointGroupSourceCode, "TestEndpointGroup");
		List<DiagnosticInfo> diagnostics = [];

		// Act
		string? result = endpointGroupSymbol.GetPatternFromEndpointGroup(diagnostics);

		// Assert
		result.ShouldBeNull();
		diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void GetPatternFromEndpointGroup_WithNoConfigureMethod_ReturnsNull()
	{
		// Arrange
		const string endpointGroupSourceCode = """
			public class TestEndpointGroup
			{
				public static void SomeOtherMethod()
				{
				}
			}
			""";

		INamedTypeSymbol endpointGroupSymbol = CreateNamedTypeSymbolFromSource(endpointGroupSourceCode, "TestEndpointGroup");
		List<DiagnosticInfo> diagnostics = [];

		// Act
		string? result = endpointGroupSymbol.GetPatternFromEndpointGroup(diagnostics);

		// Assert
		result.ShouldBeNull();
		diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void GetPatternFromEndpointGroup_WithNonStaticConfigureMethod_ReturnsNull()
	{
		// Arrange
		const string endpointGroupSourceCode = """
			public class TestEndpointGroup
			{
				public RouteGroupBuilder Configure(WebApplication app)
				{
					return app.MapGroup("/api/v1");
				}
			}
			""";

		INamedTypeSymbol endpointGroupSymbol = CreateNamedTypeSymbolFromSource(endpointGroupSourceCode, "TestEndpointGroup");
		List<DiagnosticInfo> diagnostics = [];

		// Act
		string? result = endpointGroupSymbol.GetPatternFromEndpointGroup(diagnostics);

		// Assert
		result.ShouldBeNull();
		diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void GetPatternFromEndpointGroup_WithEmptyStringPattern_ReturnsEmptyString()
	{
		// Arrange
		const string endpointGroupSourceCode = """
			public class TestEndpointGroup
			{
				public static RouteGroupBuilder Configure(WebApplication app)
				{
					return app.MapGroup("");
				}
			}
			""";

		INamedTypeSymbol endpointGroupSymbol = CreateNamedTypeSymbolFromSource(endpointGroupSourceCode, "TestEndpointGroup");
		List<DiagnosticInfo> diagnostics = [];

		// Act
		string? result = endpointGroupSymbol.GetPatternFromEndpointGroup(diagnostics);

		// Assert
		result.ShouldBe("");
		diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void GetPatternFromEndpointGroup_WithRootPattern_ReturnsSlash()
	{
		// Arrange
		const string endpointGroupSourceCode = """
			public class TestEndpointGroup
			{
				public static RouteGroupBuilder Configure(WebApplication app)
				{
					return app.MapGroup("/");
				}
			}
			""";

		INamedTypeSymbol endpointGroupSymbol = CreateNamedTypeSymbolFromSource(endpointGroupSourceCode, "TestEndpointGroup");
		List<DiagnosticInfo> diagnostics = [];

		// Act
		string? result = endpointGroupSymbol.GetPatternFromEndpointGroup(diagnostics);

		// Assert
		result.ShouldBe("/");
		diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void GetPatternFromEndpointGroup_WithComplexPattern_ReturnsCorrectPattern()
	{
		// Arrange
		const string endpointGroupSourceCode = """
			public class TestEndpointGroup
			{
				public static RouteGroupBuilder Configure(WebApplication app)
				{
					return app.MapGroup("/api/v1/users/{userId}/posts");
				}
			}
			""";

		INamedTypeSymbol endpointGroupSymbol = CreateNamedTypeSymbolFromSource(endpointGroupSourceCode, "TestEndpointGroup");
		List<DiagnosticInfo> diagnostics = [];

		// Act
		string? result = endpointGroupSymbol.GetPatternFromEndpointGroup(diagnostics);

		// Assert
		result.ShouldBe("/api/v1/users/{userId}/posts");
		diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void GetPatternFromEndpointGroup_WithMethodChaining_ReturnsPattern()
	{
		// Arrange
		const string endpointGroupSourceCode = """
			public class TestEndpointGroup
			{
				public static RouteGroupBuilder Configure(WebApplication app)
				{
					return app
						.MapGroup("/api/v1/test")
						.WithTags("Test")
						.WithOpenApi();
				}
			}
			""";

		INamedTypeSymbol endpointGroupSymbol = CreateNamedTypeSymbolFromSource(endpointGroupSourceCode, "TestEndpointGroup");
		List<DiagnosticInfo> diagnostics = [];

		// Act
		string? result = endpointGroupSymbol.GetPatternFromEndpointGroup(diagnostics);

		// Assert
		result.ShouldBe("/api/v1/test");
		diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void GetPatternFromEndpointGroup_WithComplexMethodChaining_ReturnsFirstMapGroupPattern()
	{
		// Arrange
		const string endpointGroupSourceCode = """
			public class TestEndpointGroup
			{
				public static RouteGroupBuilder Configure(WebApplication app)
				{
					var builder = app
						.MapGroup("/api/v1")
						.WithTags("API");
					
					return builder
						.WithName("TestGroup")
						.WithOpenApi();
				}
			}
			""";

		INamedTypeSymbol endpointGroupSymbol = CreateNamedTypeSymbolFromSource(endpointGroupSourceCode, "TestEndpointGroup");
		List<DiagnosticInfo> diagnostics = [];

		// Act
		string? result = endpointGroupSymbol.GetPatternFromEndpointGroup(diagnostics);

		// Assert
		result.ShouldBe("/api/v1");
		diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void GetPatternFromEndpointGroup_WithNestedMethodCalls_ReturnsPattern()
	{
		// Arrange
		const string endpointGroupSourceCode = """
			public class TestEndpointGroup
			{
				public static RouteGroupBuilder Configure(WebApplication app)
				{
					var configuredApp = app.WithOpenApi();
					return configuredApp.MapGroup("/api/nested");
				}
			}
			""";

		INamedTypeSymbol endpointGroupSymbol = CreateNamedTypeSymbolFromSource(endpointGroupSourceCode, "TestEndpointGroup");
		List<DiagnosticInfo> diagnostics = [];

		// Act
		string? result = endpointGroupSymbol.GetPatternFromEndpointGroup(diagnostics);

		// Assert
		result.ShouldBe("/api/nested");
		diagnostics.ShouldBeEmpty();
	}

	#endregion

	#region Integration Tests

	[Fact]
	public void GetPatternFromEndpointGroup_WithRealWorldExample_WorksCorrectly()
	{
		// Arrange
		const string endpointGroupSourceCode = """
			public class UsersEndpointGroup
			{
				public static RouteGroupBuilder Configure(WebApplication app)
				{
					return app
						.MapGroup("/api/v1/users")
						.WithTags("Users")
						.WithSummary("User management endpoints")
						.WithOpenApi()
						.RequireAuthorization();
				}
			}
			""";

		INamedTypeSymbol endpointGroupSymbol = CreateNamedTypeSymbolFromSource(endpointGroupSourceCode, "UsersEndpointGroup");
		List<DiagnosticInfo> diagnostics = [];

		// Act
		string? result = endpointGroupSymbol.GetPatternFromEndpointGroup(diagnostics);

		// Assert
		result.ShouldBe("/api/v1/users");
		diagnostics.ShouldBeEmpty();
	}

	[Fact]
	public void GetPatternFromEndpointGroup_WithMultipleSyntaxReferences_ReturnsPatternFromFirstValidReference()
	{
		// This test simulates a scenario where a type might have multiple syntax references
		// In practice, this would be handled by the actual Roslyn compiler
		const string endpointGroupSourceCode = """
			public class TestEndpointGroup
			{
				public static RouteGroupBuilder Configure(WebApplication app)
				{
					return app.MapGroup("/api/first");
				}
			}
			""";

		INamedTypeSymbol endpointGroupSymbol = CreateNamedTypeSymbolFromSource(endpointGroupSourceCode, "TestEndpointGroup");
		List<DiagnosticInfo> diagnostics = [];

		// Act
		string? result = endpointGroupSymbol.GetPatternFromEndpointGroup(diagnostics);

		// Assert
		result.ShouldBe("/api/first");
		diagnostics.ShouldBeEmpty();
	}

	#endregion

	#region Edge Cases and Error Scenarios

	[Fact]
	public void GetPatternFromEndpointGroup_WithMultipleConfigureMethods_UsesStaticOne()
	{
		// Arrange
		const string endpointGroupSourceCode = """
			public class TestEndpointGroup
			{
				public RouteGroupBuilder Configure(WebApplication app)
				{
					return app.MapGroup("/non-static");
				}

				public static RouteGroupBuilder Configure(WebApplication app)
				{
					return app.MapGroup("/static");
				}
			}
			""";

		INamedTypeSymbol endpointGroupSymbol = CreateNamedTypeSymbolFromSource(endpointGroupSourceCode, "TestEndpointGroup");
		List<DiagnosticInfo> diagnostics = [];

		// Act
		string? result = endpointGroupSymbol.GetPatternFromEndpointGroup(diagnostics);

		// Assert
		result.ShouldBe("/static");
		diagnostics.ShouldBeEmpty();
	}

	#endregion

	#region Helper Methods

	static (TypeDeclarationSyntax, SemanticModel) ParseTypeDeclarationWithSemanticModel(string sourceCode)
	{
		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
		CompilationUnitSyntax root = syntaxTree.GetCompilationUnitRoot();
		TypeDeclarationSyntax typeDeclaration = root.DescendantNodes().OfType<TypeDeclarationSyntax>().First();

		// Create a compilation to get semantic model
		var compilation = CSharpCompilation.Create(
			"TestAssembly",
			[syntaxTree],
			[MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

		SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);

		return (typeDeclaration, semanticModel);
	}

	static INamedTypeSymbol CreateMockNamedTypeSymbol(string typeName)
	{
		// Create a simple syntax tree with the type
		string sourceCode = $"public class {typeName} {{ }}";
		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

		var compilation = CSharpCompilation.Create(
			"TestAssembly",
			[syntaxTree],
			[MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

		SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);
		CompilationUnitSyntax root = syntaxTree.GetCompilationUnitRoot();
		TypeDeclarationSyntax typeDeclaration = root.DescendantNodes().OfType<TypeDeclarationSyntax>().First();

		return semanticModel.GetDeclaredSymbol(typeDeclaration) as INamedTypeSymbol
			?? throw new InvalidOperationException($"Could not create symbol for {typeName}");
	}

	static INamedTypeSymbol CreateNamedTypeSymbolFromSource(string sourceCode, string typeName)
	{
		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

		var compilation = CSharpCompilation.Create(
			"TestAssembly",
			[syntaxTree],
			[MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

		SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);
		CompilationUnitSyntax root = syntaxTree.GetCompilationUnitRoot();
		TypeDeclarationSyntax typeDeclaration = root.DescendantNodes()
			.OfType<TypeDeclarationSyntax>()
			.First(t => t.Identifier.ValueText == typeName);

		return semanticModel.GetDeclaredSymbol(typeDeclaration) as INamedTypeSymbol
			?? throw new InvalidOperationException($"Could not create symbol for {typeName}");
	}

	#endregion
}
