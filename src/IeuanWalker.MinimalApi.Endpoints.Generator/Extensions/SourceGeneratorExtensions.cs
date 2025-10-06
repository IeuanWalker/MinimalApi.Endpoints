using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace IeuanWalker.MinimalApi.Endpoints.Generator.Extensions;

static class SourceGeneratorExtensions
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

	internal static MethodDeclarationSyntax? GetConfigureMethod(this SyntaxList<MemberDeclarationSyntax> members)
	{
		return members
			.OfType<MethodDeclarationSyntax>()
			.FirstOrDefault(m => m.Identifier.ValueText == "Configure"
				&& m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword))
				&& HasRouteHandlerBuilderParameter(m));

		static bool HasRouteHandlerBuilderParameter(MethodDeclarationSyntax method)
		{
			// Check if method has exactly one parameter of type RouteHandlerBuilder
			if (method.ParameterList.Parameters.Count != 1)
			{
				return false;
			}

			ParameterSyntax parameter = method.ParameterList.Parameters[0];

			// Check if parameter type is RouteHandlerBuilder (handle both simple and qualified names)
			return parameter.Type switch
			{
				IdentifierNameSyntax identifierName => (identifierName.Identifier.ValueText == "RouteHandlerBuilder" || identifierName.Identifier.ValueText == "WebApplication"),
				QualifiedNameSyntax qualifiedName => (qualifiedName.Right.Identifier.ValueText == "RouteHandlerBuilder" || qualifiedName.Right.Identifier.ValueText == "WebApplication"),
				_ => false
			};
		}
	}


}
