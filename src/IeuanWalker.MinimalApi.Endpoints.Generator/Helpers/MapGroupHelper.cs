using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IeuanWalker.MinimalApi.Endpoints.Generator.Helpers;

static class MapGroupHelper
{
	public static (INamedTypeSymbol symbol, string pattern)? GetGroup(this TypeDeclarationSyntax typeDeclaration, INamedTypeSymbol endpointGroupSymbol, Compilation compilation)
	{
		// Find the Configure method
		MethodDeclarationSyntax? configureMethod = typeDeclaration.Members
			.OfType<MethodDeclarationSyntax>()
			.FirstOrDefault(m => m.Identifier.ValueText == "Configure" && m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)));

		if (configureMethod is null)
		{
			return null;
		}

		// Look for MapGroup calls in the method body
		IEnumerable<InvocationExpressionSyntax> mapGroupCalls = configureMethod.DescendantNodes()
			.OfType<InvocationExpressionSyntax>()
			.Where(invocation => invocation.Expression is MemberAccessExpressionSyntax memberAccess && memberAccess.Name.Identifier.ValueText == "Group");

		InvocationExpressionSyntax? firstMapGroupCall = mapGroupCalls.FirstOrDefault();

		if (firstMapGroupCall?.Expression is MemberAccessExpressionSyntax memberAccessExpr)
		{
			// Check if MapGroup has generic type arguments
			if (memberAccessExpr.Name is GenericNameSyntax genericName && genericName.TypeArgumentList.Arguments.Count > 0)
			{
				// Extract the first generic type argument
				TypeSyntax endpointGroup = genericName.TypeArgumentList.Arguments[0];

				SemanticModel semanticModel = compilation.GetSemanticModel(endpointGroup.SyntaxTree);

				// Get the type information for the syntax node
				TypeInfo typeInfo = semanticModel.GetTypeInfo(endpointGroup);

				if (typeInfo.Type is not INamedTypeSymbol namedTypeSymbol)
				{
					return null;
				}

				if (SymbolEqualityComparer.Default.Equals(namedTypeSymbol, endpointGroupSymbol) ||
					namedTypeSymbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, endpointGroupSymbol)) ||
					InheritsFrom(namedTypeSymbol, endpointGroupSymbol))
				{
					// Now get the pattern from the endpoint group's Configure method
					string? pattern = GetPatternFromEndpointGroup(namedTypeSymbol);

					if (pattern is null)
					{
						return null;
					}

					return (namedTypeSymbol, pattern);
				}
			}
		}

		return null;
	}

	static string? GetPatternFromEndpointGroup(INamedTypeSymbol endpointGroupSymbol)
	{
		// Get all syntax references for the endpoint group type
		foreach (SyntaxReference syntaxRef in endpointGroupSymbol.DeclaringSyntaxReferences)
		{
			SyntaxNode syntaxNode = syntaxRef.GetSyntax();
			if (syntaxNode is TypeDeclarationSyntax typeDeclaration)
			{
				// Find the Configure method in the endpoint group
				MethodDeclarationSyntax? configureMethod = typeDeclaration.Members
					.OfType<MethodDeclarationSyntax>()
					.FirstOrDefault(m => m.Identifier.ValueText == "Configure" && m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)));

				if (configureMethod is not null)
				{
					// Look for MapGroup calls in the Configure method
					IEnumerable<InvocationExpressionSyntax> mapGroupCalls = configureMethod.DescendantNodes()
						.OfType<InvocationExpressionSyntax>()
						.Where(invocation => invocation.Expression is MemberAccessExpressionSyntax memberAccess && memberAccess.Name.Identifier.ValueText == "MapGroup");

					InvocationExpressionSyntax? firstMapGroupCall = mapGroupCalls.FirstOrDefault();

					if (firstMapGroupCall is not null && firstMapGroupCall.ArgumentList.Arguments.Count > 0)
					{
						ArgumentSyntax firstArgument = firstMapGroupCall.ArgumentList.Arguments[0];
						if (firstArgument.Expression is LiteralExpressionSyntax literalExpr && literalExpr.Token.IsKind(SyntaxKind.StringLiteralToken))
						{
							return literalExpr.Token.ValueText;
						}
					}
				}
			}
		}

		return null;
	}

	static bool InheritsFrom(INamedTypeSymbol typeSymbol, INamedTypeSymbol baseTypeSymbol)
	{
		INamedTypeSymbol? current = typeSymbol.BaseType;

		while (current is not null)
		{
			if (SymbolEqualityComparer.Default.Equals(current, baseTypeSymbol))
			{
				return true;
			}
			current = current.BaseType;
		}

		return false;
	}
}
