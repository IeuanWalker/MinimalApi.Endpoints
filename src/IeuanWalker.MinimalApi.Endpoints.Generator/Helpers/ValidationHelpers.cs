using IeuanWalker.MinimalApi.Endpoints.Generator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IeuanWalker.MinimalApi.Endpoints.Generator.Helpers;

static class ValidationHelpers
{
	const string validator = "IeuanWalker.MinimalApi.Endpoints.Validator`1";

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

		MethodDeclarationSyntax? configureMethod = typeDeclaration.Members.GetConfigureMethod();

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

		// Get the Validator`1 base class symbol to check against
		INamedTypeSymbol? validatorBase = compilation.GetTypeByMetadataName(validator);

		if (validatorBase is null)
		{
			return null;
		}

		// Use the requestClass ITypeSymbol directly
		if (requestClass is not INamedTypeSymbol requestTypeSymbol)
		{
			return null;
		}

		// Construct the specific Validator<TRequest> type
		INamedTypeSymbol validatorOfRequest = validatorBase.Construct(requestTypeSymbol);

		return FindValidatorInSourceTrees(compilation, validatorOfRequest);
	}

	/// <summary>
	/// Searches for validator implementations in the source trees.
	/// </summary>
	/// <param name="compilation">The compilation context</param>
	/// <param name="validatorOfRequest">The specific validator base class to search for</param>
	/// <returns>The validator class name if found, null otherwise</returns>
	static INamedTypeSymbol? FindValidatorInSourceTrees(Compilation compilation, INamedTypeSymbol validatorOfRequest)
	{
		// Only search in source trees (user's code), not all referenced assemblies
		foreach (SyntaxTree syntaxTree in compilation.SyntaxTrees)
		{
			SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);
			SyntaxNode root = syntaxTree.GetRoot();

			foreach (TypeDeclarationSyntax typeDeclaration in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
			{
				INamedTypeSymbol? typeSymbol = semanticModel.GetDeclaredSymbol(typeDeclaration) as INamedTypeSymbol;
				if (typeSymbol is not null && typeSymbol.InheritsFromValidatorBase(validatorOfRequest))
				{
					return typeSymbol;
				}
			}
		}

		return null;
	}

	/// <summary>
	/// Checks if the type symbol inherits from the specified validator base class.
	/// </summary>
	/// <param name="typeSymbol">The type symbol to check</param>
	/// <param name="validatorBaseClass">The validator base class to check for</param>
	/// <returns>True if the type inherits from the base class, false otherwise</returns>
	public static bool InheritsFromValidatorBase(this INamedTypeSymbol typeSymbol, INamedTypeSymbol validatorBaseClass)
	{
		// Check the inheritance chain
		INamedTypeSymbol? current = typeSymbol.BaseType;
		while (current is not null)
		{
			// Check if the current type is a generic type and its original definition matches the validator base class
			if (current.IsGenericType && SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, validatorBaseClass))
			{
				return true;
			}
			// Also check for exact match (non-generic case)
			if (SymbolEqualityComparer.Default.Equals(current, validatorBaseClass))
			{
				return true;
			}
			current = current.BaseType;
		}

		return false;
	}

	public static ITypeSymbol? GetValidatedTypeFromValidator(this INamedTypeSymbol validatorTypeSymbol, INamedTypeSymbol validatorBaseSymbol)
	{
		// Walk up the inheritance chain to find the Validator<T> base class
		INamedTypeSymbol? current = validatorTypeSymbol.BaseType;
		while (current != null)
		{
			// Check if this is the Validator<T> base class
			if (current.IsGenericType &&
				SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, validatorBaseSymbol))
			{
				// Return the first type argument (the T in Validator<T>)
				return current.TypeArguments.Length > 0 ? current.TypeArguments[0] : null;
			}
			current = current.BaseType;
		}

		return null;
	}
}
