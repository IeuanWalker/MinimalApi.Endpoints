using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace IeuanWalker.MinimalApi.Endpoints.Generator.Helpers;

static class ValidationHelpers
{
	const string fluentValidationInterface = "FluentValidation.IValidator`1";

	/// <summary>
	/// Validates whether the specified type declaration requires validation.
	/// </summary>
	/// <param name="typeDeclaration">The type declaration to validate</param>
	/// <param name="compilation">The compilation context</param>
	/// <param name="requestClass">The request class symbol</param>
	/// <returns>Validation information if validation is required, null otherwise</returns>
	public static INamedTypeSymbol? Validate(this TypeDeclarationSyntax typeDeclaration, Compilation compilation, ITypeSymbol requestClass)
	{
		if (typeDeclaration.DontValidate())
		{
			return null;
		}

		return GetFluentValidationClass(compilation, requestClass);
	}

	/// <summary>
	/// Determines if validation should be skipped for the given type declaration.
	/// </summary>
	/// <param name="typeDeclaration">The type declaration to check</param>
	/// <returns>True if validation should be skipped, false otherwise</returns>
	public static bool DontValidate(this TypeDeclarationSyntax typeDeclaration)
	{
		if (typeDeclaration is null)
		{
			return false;
		}

		MethodDeclarationSyntax? configureMethod = typeDeclaration.Members
			.OfType<MethodDeclarationSyntax>()
			.FirstOrDefault(m => m.Identifier.ValueText == "Configure" &&
								m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)));

		if (configureMethod is null)
		{
			return false;
		}

		return configureMethod.DescendantNodes()
			.OfType<InvocationExpressionSyntax>()
			.Any(invocation =>
				invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
				memberAccess.Name.Identifier.ValueText == "DisableValidation");
	}

	/// <summary>
	/// Finds the FluentValidation validator class for the specified request type.
	/// </summary>
	/// <param name="compilation">The compilation context</param>
	/// <param name="requestClass">The request class to find validator for</param>
	/// <returns>The validator class name if found, null otherwise</returns>
	static INamedTypeSymbol? GetFluentValidationClass(Compilation compilation, ITypeSymbol requestClass)
	{
		if (compilation is null || requestClass is null)
		{
			return null;
		}

		// Get the IValidator`1 interface symbol to check against
		INamedTypeSymbol? iValidatorBase = compilation.GetTypeByMetadataName(fluentValidationInterface);

		if (iValidatorBase is null)
		{
			return null;
		}

		// Use the requestClass ITypeSymbol directly
		if (requestClass is not INamedTypeSymbol requestTypeSymbol)
		{
			return null;
		}

		// Construct the specific IValidator<TRequest> type
		INamedTypeSymbol iValidatorOfRequest = iValidatorBase.Construct(requestTypeSymbol);

		return FindValidatorInSourceTrees(compilation, iValidatorOfRequest);
	}

	/// <summary>
	/// Searches for validator implementations in the source trees.
	/// </summary>
	/// <param name="compilation">The compilation context</param>
	/// <param name="iValidatorOfRequest">The specific validator interface to search for</param>
	/// <returns>The validator class name if found, null otherwise</returns>
	static INamedTypeSymbol? FindValidatorInSourceTrees(Compilation compilation, INamedTypeSymbol iValidatorOfRequest)
	{
		// Only search in source trees (user's code), not all referenced assemblies
		foreach (SyntaxTree syntaxTree in compilation.SyntaxTrees)
		{
			SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);
			SyntaxNode root = syntaxTree.GetRoot();

			foreach (TypeDeclarationSyntax typeDeclaration in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
			{
				INamedTypeSymbol? typeSymbol = semanticModel.GetDeclaredSymbol(typeDeclaration) as INamedTypeSymbol;
				if (typeSymbol is not null && ImplementsValidatorInterface(typeSymbol, iValidatorOfRequest))
				{

					return typeSymbol;
				}
			}
		}

		return null;
	}

	/// <summary>
	/// Checks if the type symbol implements the specified validator interface.
	/// </summary>
	/// <param name="typeSymbol">The type symbol to check</param>
	/// <param name="validatorInterface">The validator interface to check for</param>
	/// <returns>True if the type implements the interface, false otherwise</returns>
	static bool ImplementsValidatorInterface(INamedTypeSymbol typeSymbol, INamedTypeSymbol validatorInterface)
	{
		return typeSymbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, validatorInterface));
	}
}
