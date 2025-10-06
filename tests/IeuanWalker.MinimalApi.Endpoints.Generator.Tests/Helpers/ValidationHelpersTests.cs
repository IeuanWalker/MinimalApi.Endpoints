using Shouldly;
using IeuanWalker.MinimalApi.Endpoints.Generator.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IeuanWalker.MinimalApi.Endpoints.Generator.Tests.Helpers;

public class ValidationHelpersTests
{
	#region DontValidate Tests

	[Fact]
	public void DontValidate_WithNullTypeDeclaration_ReturnsFalse()
	{
		// Arrange
		TypeDeclarationSyntax? typeDeclaration = null;

		// Act
		bool result = ValidationHelpers.DontValidate(typeDeclaration!);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void DontValidate_WithNoConfigureMethod_ReturnsFalse()
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
		bool result = typeDeclaration.DontValidate();

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void DontValidate_WithConfigureMethodButNoDisableValidation_ReturnsFalse()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Get("/test").WithName("Test");
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = GetTypeDeclaration(sourceCode);

		// Act
		bool result = typeDeclaration.DontValidate();

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void DontValidate_WithDisableValidationInConfigureMethod_ReturnsTrue()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Get("/test").DisableValidation();
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = GetTypeDeclaration(sourceCode);

		// Act
		bool result = typeDeclaration.DontValidate();

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public void DontValidate_WithMultipleMethodCallsIncludingDisableValidation_ReturnsTrue()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder
						.Get("/test")
						.WithName("Test")
						.DisableValidation()
						.WithTags("TestTag");
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = GetTypeDeclaration(sourceCode);

		// Act
		bool result = typeDeclaration.DontValidate();

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public void DontValidate_WithNestedMethodCallsButNoDisableValidation_ReturnsFalse()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					var result = SomeOtherMethod();
					builder.Get("/test").WithName("Test");
				}

				private static string SomeOtherMethod()
				{
					return "test";
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = GetTypeDeclaration(sourceCode);

		// Act
		bool result = typeDeclaration.DontValidate();

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void DontValidate_WithDisableValidationInDifferentMethod_ReturnsFalse()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Get("/test");
				}

				public static void SomeOtherMethod()
				{
					var builder = new object();
					DisableValidation();
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = GetTypeDeclaration(sourceCode);

		// Act
		bool result = typeDeclaration.DontValidate();

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void DontValidate_WithConfigureMethodHavingWrongSignature_ReturnsFalse()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(string parameter)
				{
					DisableValidation();
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = GetTypeDeclaration(sourceCode);

		// Act
		bool result = typeDeclaration.DontValidate();

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void DontValidate_WithNonStaticConfigureMethod_ReturnsFalse()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public void Configure(RouteHandlerBuilder builder)
				{
					builder.DisableValidation();
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = GetTypeDeclaration(sourceCode);

		// Act
		bool result = typeDeclaration.DontValidate();

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void DontValidate_WithDisableValidationAtBeginningOfChain_ReturnsTrue()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.DisableValidation().Get("/test").WithName("Test");
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = GetTypeDeclaration(sourceCode);

		// Act
		bool result = typeDeclaration.DontValidate();

		// Assert
		result.ShouldBeTrue();
	}

	#endregion

	#region Integration Tests with Real Compilation

	[Fact]
	public void InheritsFromValidatorBase_WithRealCompilation_DirectInheritance_ReturnsTrue()
	{
		// Arrange
		(CSharpCompilation compilation, INamedTypeSymbol requestType) = CreateCompilationWithValidator("TestRequest", hasValidator: true);
		INamedTypeSymbol? validatorBase = compilation.GetTypeByMetadataName("IeuanWalker.MinimalApi.Endpoints.Validator`1");
		INamedTypeSymbol? validator = compilation.GetTypeByMetadataName("TestApp.TestRequestValidator");

		// Act
		bool result = validator!.InheritsFromValidatorBase(validatorBase!);

		// Assert
		result.ShouldBeTrue();
	}

	[Fact]
	public void InheritsFromValidatorBase_WithRealCompilation_NoInheritance_ReturnsFalse()
	{
		// Arrange
		(CSharpCompilation compilation, INamedTypeSymbol requestType) = CreateCompilationWithValidator("TestRequest", hasValidator: false);
		INamedTypeSymbol? validatorBase = compilation.GetTypeByMetadataName("IeuanWalker.MinimalApi.Endpoints.Validator`1");

		// Act
		bool result = requestType.InheritsFromValidatorBase(validatorBase!);

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void GetValidatedTypeFromValidator_WithRealCompilation_ReturnsCorrectType()
	{
		// Arrange
		(CSharpCompilation compilation, INamedTypeSymbol _) = CreateCompilationWithValidator("TestRequest", hasValidator: true);
		INamedTypeSymbol? validatorBase = compilation.GetTypeByMetadataName("IeuanWalker.MinimalApi.Endpoints.Validator`1");
		INamedTypeSymbol? validator = compilation.GetTypeByMetadataName("TestApp.TestRequestValidator");

		// Act
		ITypeSymbol? result = validator!.GetValidatedTypeFromValidator(validatorBase!);

		// Assert
		result.ShouldNotBeNull();
		result.Name.ShouldBe("TestRequest");
		result.ToDisplayString().ShouldBe("TestApp.TestRequest");
	}

	[Fact]
	public void GetValidatedTypeFromValidator_WithNonValidator_ReturnsNull()
	{
		// Arrange
		(CSharpCompilation compilation, INamedTypeSymbol requestType) = CreateCompilationWithValidator("TestRequest", hasValidator: false);
		INamedTypeSymbol? validatorBase = compilation.GetTypeByMetadataName("IeuanWalker.MinimalApi.Endpoints.Validator`1");

		// Act
		ITypeSymbol? result = requestType.GetValidatedTypeFromValidator(validatorBase!);

		// Assert
		result.ShouldBeNull();
	}

	#endregion

	#region Edge Case Tests

	[Fact]
	public void DontValidate_WithEmptyConfigureMethodBody_ReturnsFalse()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = GetTypeDeclaration(sourceCode);

		// Act
		bool result = typeDeclaration.DontValidate();

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void DontValidate_WithCommentedOutDisableValidation_ReturnsFalse()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Get("/test");
					// builder.DisableValidation();
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = GetTypeDeclaration(sourceCode);

		// Act
		bool result = typeDeclaration.DontValidate();

		// Assert
		result.ShouldBeFalse();
	}

	[Fact]
	public void DontValidate_WithDisableValidationInStringLiteral_ReturnsFalse()
	{
		// Arrange
		const string sourceCode = """
			public class TestEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Get("/test");
					var message = "DisableValidation";
				}
			}
			""";

		TypeDeclarationSyntax typeDeclaration = GetTypeDeclaration(sourceCode);

		// Act
		bool result = typeDeclaration.DontValidate();

		// Assert
		result.ShouldBeFalse();
	}

	#endregion

	#region Helper Methods

	static TypeDeclarationSyntax GetTypeDeclaration(string sourceCode)
	{
		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
		CompilationUnitSyntax root = syntaxTree.GetCompilationUnitRoot();
		return root.DescendantNodes().OfType<TypeDeclarationSyntax>().First();
	}

	static (CSharpCompilation compilation, INamedTypeSymbol requestType) CreateCompilationWithValidator(string requestTypeName, bool hasValidator)
	{
		List<string> sources = [];

		// Add request type
		sources.Add($@"
			namespace TestApp 
			{{
				public class {requestTypeName}
				{{
					public string Name {{ get; set; }} = string.Empty;
				}}
			}}
			");

		// Add validator if requested
		if (hasValidator)
		{
			sources.Add($@"
				using IeuanWalker.MinimalApi.Endpoints;

				namespace TestApp 
				{{
					public class {requestTypeName}Validator : Validator<{requestTypeName}>
					{{
						public {requestTypeName}Validator()
						{{
						}}
					}}
				}}
				");
		}

		// Add validator base class
		sources.Add(@"
			namespace IeuanWalker.MinimalApi.Endpoints
			{
				public abstract class Validator<T>
				{
				}
			}
			");

		List<SyntaxTree> syntaxTrees = sources.Select(source => CSharpSyntaxTree.ParseText(source)).ToList();

		CSharpCompilation compilation = CSharpCompilation.Create(
			assemblyName: "TestAssembly",
			syntaxTrees: syntaxTrees,
			references: []);

		INamedTypeSymbol? requestType = compilation.GetTypeByMetadataName($"TestApp.{requestTypeName}");
		return (compilation, requestType!);
	}

	#endregion
}
