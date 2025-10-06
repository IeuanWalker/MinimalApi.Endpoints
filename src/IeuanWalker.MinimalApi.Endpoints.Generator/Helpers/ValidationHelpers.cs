using IeuanWalker.MinimalApi.Endpoints.Generator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IeuanWalker.MinimalApi.Endpoints.Generator.Helpers;

static class ValidationHelpers
{
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
			if (current.IsGenericType && SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, validatorBaseSymbol))
			{
				// Return the first type argument (the T in Validator<T>)
				return current.TypeArguments.Length > 0 ? current.TypeArguments[0] : null;
			}
			current = current.BaseType;
		}

		return null;
	}
}
