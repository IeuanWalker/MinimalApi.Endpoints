using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace IeuanWalker.MinimalApi.Endpoints.Generator.Helpers;

static class SourceGeneratorHelpers
{
	internal static TypeDeclarationSyntax? ToTypeDeclarationSyntax(this INamedTypeSymbol symbol, ImmutableArray<TypeDeclarationSyntax?> allTypeDeclarations, Compilation compilation)
	{
		foreach (TypeDeclarationSyntax? typeDeclaration in allTypeDeclarations)
		{
			if (typeDeclaration is null)
			{
				continue;
			}

			SemanticModel semanticModel = compilation.GetSemanticModel(typeDeclaration.SyntaxTree);
			INamedTypeSymbol? declaredSymbol = semanticModel.GetDeclaredSymbol(typeDeclaration);

			if (declaredSymbol is not null && SymbolEqualityComparer.Default.Equals(declaredSymbol, symbol))
			{
				return typeDeclaration;
			}
		}

		return null;
	}
}
