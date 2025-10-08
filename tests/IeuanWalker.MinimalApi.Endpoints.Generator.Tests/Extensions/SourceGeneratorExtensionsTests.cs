using System.Collections.Immutable;
using IeuanWalker.MinimalApi.Endpoints.Generator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shouldly;

namespace IeuanWalker.MinimalApi.Endpoints.Generator.Tests.Extensions;

public class SourceGeneratorExtensionsTests
{
	#region ToTypeDeclarationSyntax Tests

	[Fact]
	public void ToTypeDeclarationSyntax_WithMatchingSymbol_ReturnsCorrectTypeDeclaration()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public void Handle() { }
			}
			""";

		(INamedTypeSymbol symbol, ImmutableArray<TypeDeclarationSyntax?> typeDeclarations, Compilation compilation) = CreateTestData(sourceCode);

		// Act
		TypeDeclarationSyntax? result = symbol.ToTypeDeclarationSyntax(typeDeclarations, compilation);

		// Assert
		result.ShouldNotBeNull();
		result.Identifier.ValueText.ShouldBe("TestEndpoint");
	}

	[Fact]
	public void ToTypeDeclarationSyntax_WithNoMatchingSymbol_ReturnsNull()
	{
		// Arrange
		const string sourceCode1 = """
			public class TestEndpoint
			{
				public void Handle() { }
			}
			""";

		const string sourceCode2 = """
			public class DifferentEndpoint
			{
				public void Handle() { }
			}
			""";

		(INamedTypeSymbol _, ImmutableArray<TypeDeclarationSyntax?> typeDeclarations1, Compilation compilation1) = CreateTestData(sourceCode1);
		(INamedTypeSymbol symbol2, _, _) = CreateTestData(sourceCode2);

		// Act - try to find symbol2 in typeDeclarations1
		TypeDeclarationSyntax? result = symbol2.ToTypeDeclarationSyntax(typeDeclarations1, compilation1);

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void ToTypeDeclarationSyntax_WithNullTypeDeclarationInArray_SkipsNullEntries()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public void Handle() { }
			}
			""";

		(INamedTypeSymbol symbol, ImmutableArray<TypeDeclarationSyntax?> typeDeclarations, Compilation compilation) = CreateTestData(sourceCode);

		// Add null entries to the array
		TypeDeclarationSyntax?[] arrayWithNulls = [null, typeDeclarations[0], null];
		ImmutableArray<TypeDeclarationSyntax?> typeDeclarationsWithNulls = [.. arrayWithNulls];

		// Act
		TypeDeclarationSyntax? result = symbol.ToTypeDeclarationSyntax(typeDeclarationsWithNulls, compilation);

		// Assert
		result.ShouldNotBeNull();
		result.Identifier.ValueText.ShouldBe("TestEndpoint");
	}

	[Fact]
	public void ToTypeDeclarationSyntax_WithEmptyArray_ReturnsNull()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public void Handle() { }
			}
			""";

		(INamedTypeSymbol symbol, _, Compilation compilation) = CreateTestData(sourceCode);
		ImmutableArray<TypeDeclarationSyntax?> emptyArray = [];

		// Act
		TypeDeclarationSyntax? result = symbol.ToTypeDeclarationSyntax(emptyArray, compilation);

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void ToTypeDeclarationSyntax_WithMultipleTypeDeclarations_ReturnsMatchingOne()
	{
		// Arrange
		const string sourceCode = """
			public class FirstClass
			{
				public void Method1() { }
			}
			
			public class SecondClass
			{
				public void Method2() { }
			}
			
			public class ThirdClass
			{
				public void Method3() { }
			}
			""";

		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
		CompilationUnitSyntax root = syntaxTree.GetCompilationUnitRoot();

		// Get all type declarations
		TypeDeclarationSyntax[] allTypeDeclarations = [.. root.DescendantNodes().OfType<TypeDeclarationSyntax>()];

		ImmutableArray<TypeDeclarationSyntax?> typeDeclarations = [.. allTypeDeclarations.Cast<TypeDeclarationSyntax?>()];

		// Create compilation and get symbol for SecondClass
		Compilation compilation = CSharpCompilation.Create("TestCompilation")
			.AddSyntaxTrees(syntaxTree)
			.AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

		SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);
		TypeDeclarationSyntax secondClassDeclaration = allTypeDeclarations.First(t => t.Identifier.ValueText == "SecondClass");
		INamedTypeSymbol? symbol = semanticModel.GetDeclaredSymbol(secondClassDeclaration);

		// Act
		TypeDeclarationSyntax? result = symbol!.ToTypeDeclarationSyntax(typeDeclarations, compilation);

		// Assert
		result.ShouldNotBeNull();
		result.Identifier.ValueText.ShouldBe("SecondClass");
	}

	[Fact]
	public void ToTypeDeclarationSyntax_WithRecordDeclaration_WorksCorrectly()
	{
		// Arrange
		const string sourceCode = """
			public record TestRecord(string Name, int Age);
			""";

		(INamedTypeSymbol symbol, ImmutableArray<TypeDeclarationSyntax?> typeDeclarations, Compilation compilation) = CreateTestData(sourceCode);

		// Act
		TypeDeclarationSyntax? result = symbol.ToTypeDeclarationSyntax(typeDeclarations, compilation);

		// Assert
		result.ShouldNotBeNull();
		result.Identifier.ValueText.ShouldBe("TestRecord");
		result.ShouldBeOfType<RecordDeclarationSyntax>();
	}

	[Fact]
	public void ToTypeDeclarationSyntax_WithInterfaceDeclaration_WorksCorrectly()
	{
		// Arrange
		const string sourceCode = """
			public interface ITestInterface
			{
				void TestMethod();
			}
			""";

		(INamedTypeSymbol symbol, ImmutableArray<TypeDeclarationSyntax?> typeDeclarations, Compilation compilation) = CreateTestData(sourceCode);

		// Act
		TypeDeclarationSyntax? result = symbol.ToTypeDeclarationSyntax(typeDeclarations, compilation);

		// Assert
		result.ShouldNotBeNull();
		result.Identifier.ValueText.ShouldBe("ITestInterface");
		result.ShouldBeOfType<InterfaceDeclarationSyntax>();
	}

	[Fact]
	public void ToTypeDeclarationSyntax_WithStructDeclaration_WorksCorrectly()
	{
		// Arrange
		const string sourceCode = """
			public struct TestStruct
			{
				public int Value { get; set; }
			}
			""";

		(INamedTypeSymbol symbol, ImmutableArray<TypeDeclarationSyntax?> typeDeclarations, Compilation compilation) = CreateTestData(sourceCode);

		// Act
		TypeDeclarationSyntax? result = symbol.ToTypeDeclarationSyntax(typeDeclarations, compilation);

		// Assert
		result.ShouldNotBeNull();
		result.Identifier.ValueText.ShouldBe("TestStruct");
		result.ShouldBeOfType<StructDeclarationSyntax>();
	}

	#endregion

	#region GetConfigureMethod Tests

	[Fact]
	public void GetConfigureMethod_WithValidStaticConfigureMethod_ReturnsMethod()
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

		SyntaxList<MemberDeclarationSyntax> members = ParseMembersFromClass(sourceCode);

		// Act
		MethodDeclarationSyntax? result = members.GetConfigureMethod();

		// Assert
		result.ShouldNotBeNull();
		result.Identifier.ValueText.ShouldBe("Configure");
		result.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)).ShouldBeTrue();
	}

	[Fact]
	public void GetConfigureMethod_WithValidStaticConfigureMethodWithWebApplication_ReturnsMethod()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(WebApplication app)
				{
					app.MapGet("/test", () => "Hello");
				}
			}
			""";

		SyntaxList<MemberDeclarationSyntax> members = ParseMembersFromClass(sourceCode);

		// Act
		MethodDeclarationSyntax? result = members.GetConfigureMethod();

		// Assert
		result.ShouldNotBeNull();
		result.Identifier.ValueText.ShouldBe("Configure");
		result.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)).ShouldBeTrue();
	}

	[Fact]
	public void GetConfigureMethod_WithNonStaticConfigureMethod_ReturnsNull()
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

		SyntaxList<MemberDeclarationSyntax> members = ParseMembersFromClass(sourceCode);

		// Act
		MethodDeclarationSyntax? result = members.GetConfigureMethod();

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void GetConfigureMethod_WithNoConfigureMethod_ReturnsNull()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void SomeOtherMethod(RouteHandlerBuilder builder)
				{
					builder.Get("/test");
				}
			}
			""";

		SyntaxList<MemberDeclarationSyntax> members = ParseMembersFromClass(sourceCode);

		// Act
		MethodDeclarationSyntax? result = members.GetConfigureMethod();

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void GetConfigureMethod_WithConfigureMethodWithWrongParameterCount_ReturnsNull()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure()
				{
					// No parameters
				}
			}
			""";

		SyntaxList<MemberDeclarationSyntax> members = ParseMembersFromClass(sourceCode);

		// Act
		MethodDeclarationSyntax? result = members.GetConfigureMethod();

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void GetConfigureMethod_WithConfigureMethodWithTooManyParameters_ReturnsNull()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder, string extra)
				{
					// Too many parameters
				}
			}
			""";

		SyntaxList<MemberDeclarationSyntax> members = ParseMembersFromClass(sourceCode);

		// Act
		MethodDeclarationSyntax? result = members.GetConfigureMethod();

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void GetConfigureMethod_WithConfigureMethodWithWrongParameterType_ReturnsNull()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(string wrongType)
				{
					// Wrong parameter type
				}
			}
			""";

		SyntaxList<MemberDeclarationSyntax> members = ParseMembersFromClass(sourceCode);

		// Act
		MethodDeclarationSyntax? result = members.GetConfigureMethod();

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void GetConfigureMethod_WithQualifiedRouteHandlerBuilder_ReturnsMethod()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(Microsoft.AspNetCore.Builder.RouteHandlerBuilder builder)
				{
					builder.Get("/test");
				}
			}
			""";

		SyntaxList<MemberDeclarationSyntax> members = ParseMembersFromClass(sourceCode);

		// Act
		MethodDeclarationSyntax? result = members.GetConfigureMethod();

		// Assert
		result.ShouldNotBeNull();
		result.Identifier.ValueText.ShouldBe("Configure");
	}

	[Fact]
	public void GetConfigureMethod_WithQualifiedWebApplication_ReturnsMethod()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(Microsoft.AspNetCore.Builder.WebApplication app)
				{
					app.MapGet("/test", () => "Hello");
				}
			}
			""";

		SyntaxList<MemberDeclarationSyntax> members = ParseMembersFromClass(sourceCode);

		// Act
		MethodDeclarationSyntax? result = members.GetConfigureMethod();

		// Assert
		result.ShouldNotBeNull();
		result.Identifier.ValueText.ShouldBe("Configure");
	}

	[Fact]
	public void GetConfigureMethod_WithMultipleConfigureMethods_ReturnsFirstValid()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public void Configure(string wrongType)
				{
					// Wrong parameter type - should be ignored
				}

				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Get("/test");
				}

				public static void Configure(int wrongType)
				{
					// Wrong parameter type - should be ignored
				}
			}
			""";

		SyntaxList<MemberDeclarationSyntax> members = ParseMembersFromClass(sourceCode);

		// Act
		MethodDeclarationSyntax? result = members.GetConfigureMethod();

		// Assert
		result.ShouldNotBeNull();
		result.Identifier.ValueText.ShouldBe("Configure");
		result.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)).ShouldBeTrue();
		result.ParameterList.Parameters.Count.ShouldBe(1);
		result.ParameterList.Parameters[0].Type.ShouldBeOfType<IdentifierNameSyntax>();
		((IdentifierNameSyntax)result.ParameterList.Parameters[0].Type!).Identifier.ValueText.ShouldBe("RouteHandlerBuilder");
	}

	[Fact]
	public void GetConfigureMethod_WithPrivateStaticConfigureMethod_ReturnsMethod()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				private static void Configure(RouteHandlerBuilder builder)
				{
					builder.Get("/test");
				}
			}
			""";

		SyntaxList<MemberDeclarationSyntax> members = ParseMembersFromClass(sourceCode);

		// Act
		MethodDeclarationSyntax? result = members.GetConfigureMethod();

		// Assert
		result.ShouldNotBeNull();
		result.Identifier.ValueText.ShouldBe("Configure");
	}

	[Fact]
	public void GetConfigureMethod_WithEmptyMembersList_ReturnsNull()
	{
		// Arrange
		SyntaxList<MemberDeclarationSyntax> emptyMembers = [];

		// Act
		MethodDeclarationSyntax? result = emptyMembers.GetConfigureMethod();

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void GetConfigureMethod_WithOnlyNonMethodMembers_ReturnsNull()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public string Property { get; set; }
				
				public int Field;
				
				public event EventHandler SomeEvent;
			}
			""";

		SyntaxList<MemberDeclarationSyntax> members = ParseMembersFromClass(sourceCode);

		// Act
		MethodDeclarationSyntax? result = members.GetConfigureMethod();

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void GetConfigureMethod_WithAsyncConfigureMethod_ReturnsMethod()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static async Task Configure(RouteHandlerBuilder builder)
				{
					builder.Get("/test");
				}
			}
			""";

		SyntaxList<MemberDeclarationSyntax> members = ParseMembersFromClass(sourceCode);

		// Act
		MethodDeclarationSyntax? result = members.GetConfigureMethod();

		// Assert
		result.ShouldNotBeNull();
		result.Identifier.ValueText.ShouldBe("Configure");
		result.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)).ShouldBeTrue();
		result.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword)).ShouldBeTrue();
	}

	[Fact]
	public void GetConfigureMethod_WithGenericParameter_ReturnsNull()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure<T>(T builder)
				{
					// Generic parameters not supported
				}
			}
			""";

		SyntaxList<MemberDeclarationSyntax> members = ParseMembersFromClass(sourceCode);

		// Act
		MethodDeclarationSyntax? result = members.GetConfigureMethod();

		// Assert
		result.ShouldBeNull();
	}

	#endregion

	#region Integration Tests

	[Fact]
	public void ToTypeDeclarationSyntax_AndGetConfigureMethod_WorkTogether()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Get("/test");
				}
				
				public void Handle() { }
			}
			""";

		(INamedTypeSymbol symbol, ImmutableArray<TypeDeclarationSyntax?> typeDeclarations, Compilation compilation) = CreateTestData(sourceCode);

		// Act
		TypeDeclarationSyntax? typeDeclaration = symbol.ToTypeDeclarationSyntax(typeDeclarations, compilation);
		MethodDeclarationSyntax? configureMethod = typeDeclaration?.Members.GetConfigureMethod();

		// Assert
		typeDeclaration.ShouldNotBeNull();
		configureMethod.ShouldNotBeNull();
		configureMethod.Identifier.ValueText.ShouldBe("Configure");
	}

	[Fact]
	public void GetConfigureMethod_WithComplexRealWorldExample_WorksCorrectly()
	{
		// Arrange
		const string sourceCode = """
			public class GetUserByIdEndpoint
			{
				readonly IUserService _userService;
				
				public GetUserByIdEndpoint(IUserService userService)
				{
					_userService = userService;
				}
				
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder
						.Get("/api/v1/users/{id}")
						.WithName("GetUserById")
						.WithSummary("Gets a user by their ID")
						.Produces<UserResponse>(200)
						.ProducesProblem(404);
				}
				
				public async Task<Results<Ok<UserResponse>, NotFound>> Handle(GetUserRequest request, CancellationToken ct)
				{
					var user = await _userService.GetByIdAsync(request.Id, ct);
					return user is null 
						? TypedResults.NotFound() 
						: TypedResults.Ok(UserResponse.FromUser(user));
				}
			}
			""";

		SyntaxList<MemberDeclarationSyntax> members = ParseMembersFromClass(sourceCode);

		// Act
		MethodDeclarationSyntax? result = members.GetConfigureMethod();

		// Assert
		result.ShouldNotBeNull();
		result.Identifier.ValueText.ShouldBe("Configure");
		result.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)).ShouldBeTrue();
		result.ParameterList.Parameters.Count.ShouldBe(1);
		result.ParameterList.Parameters[0].Type.ShouldBeOfType<IdentifierNameSyntax>();
		((IdentifierNameSyntax)result.ParameterList.Parameters[0].Type!).Identifier.ValueText.ShouldBe("RouteHandlerBuilder");
	}

	#endregion

	#region Edge Cases

	[Fact]
	public void GetConfigureMethod_WithNullableRouteHandlerBuilder_ReturnsNull()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder? builder)
				{
					builder?.Get("/test");
				}
			}
			""";

		SyntaxList<MemberDeclarationSyntax> members = ParseMembersFromClass(sourceCode);

		// Act
		MethodDeclarationSyntax? result = members.GetConfigureMethod();

		// Assert
		// The current implementation doesn't handle nullable types, so this returns null
		result.ShouldBeNull();
	}

	[Fact]
	public void GetConfigureMethod_WithNullableWebApplication_ReturnsNull()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(WebApplication? app)
				{
					app?.MapGet("/test", () => "Hello");
				}
			}
			""";

		SyntaxList<MemberDeclarationSyntax> members = ParseMembersFromClass(sourceCode);

		// Act
		MethodDeclarationSyntax? result = members.GetConfigureMethod();

		// Assert
		// The current implementation doesn't handle nullable types, so this returns null
		result.ShouldBeNull();
	}

	[Fact]
	public void GetConfigureMethod_WithNullableQualifiedType_ReturnsNull()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(Microsoft.AspNetCore.Builder.RouteHandlerBuilder? builder)
				{
					builder?.Get("/test");
				}
			}
			""";

		SyntaxList<MemberDeclarationSyntax> members = ParseMembersFromClass(sourceCode);

		// Act
		MethodDeclarationSyntax? result = members.GetConfigureMethod();

		// Assert
		// The current implementation doesn't handle nullable types, so this returns null
		result.ShouldBeNull();
	}

	[Fact]
	public void GetConfigureMethod_WithPointerType_ReturnsNull()
	{
		// Arrange - unsafe code with pointer type
		const string sourceCode = """
			public class TestEndpoint
			{
				public static unsafe void Configure(RouteHandlerBuilder* builder)
				{
					// Pointer type not supported
				}
			}
			""";

		SyntaxList<MemberDeclarationSyntax> members = ParseMembersFromClass(sourceCode);

		// Act
		MethodDeclarationSyntax? result = members.GetConfigureMethod();

		// Assert
		result.ShouldBeNull();
	}

	[Fact]
	public void ToTypeDeclarationSyntax_WithAllNullTypeDeclarations_ReturnsNull()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public void Handle() { }
			}
			""";

		(INamedTypeSymbol symbol, _, Compilation compilation) = CreateTestData(sourceCode);
		ImmutableArray<TypeDeclarationSyntax?> allNullArray = [null, null, null];

		// Act
		TypeDeclarationSyntax? result = symbol.ToTypeDeclarationSyntax(allNullArray, compilation);

		// Assert
		result.ShouldBeNull();
	}

	#endregion

	#region Helper Methods

	static (INamedTypeSymbol symbol, ImmutableArray<TypeDeclarationSyntax?> typeDeclarations, Compilation compilation) CreateTestData(string sourceCode)
	{
		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
		CompilationUnitSyntax root = syntaxTree.GetCompilationUnitRoot();
		TypeDeclarationSyntax typeDeclaration = root.DescendantNodes().OfType<TypeDeclarationSyntax>().First();

		ImmutableArray<TypeDeclarationSyntax?> typeDeclarations = [typeDeclaration];

		Compilation compilation = CSharpCompilation.Create("TestCompilation")
			.AddSyntaxTrees(syntaxTree)
			.AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

		SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);
		INamedTypeSymbol? symbol = semanticModel.GetDeclaredSymbol(typeDeclaration);

		return (symbol!, typeDeclarations, compilation);
	}

	static SyntaxList<MemberDeclarationSyntax> ParseMembersFromClass(string sourceCode)
	{
		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
		CompilationUnitSyntax root = syntaxTree.GetCompilationUnitRoot();
		TypeDeclarationSyntax typeDeclaration = root.DescendantNodes().OfType<TypeDeclarationSyntax>().First();

		return typeDeclaration.Members;
	}

	#endregion
}
