using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IeuanWalker.MinimalApi.Endpoints.Generator.Helpers;

static class MapGroupHelper
{
	static readonly DiagnosticDescriptor noMapGroupDescriptor = new(
		id: "MINAPI003",
		title: "No MapGroup configured",
		messageFormat: "Endpoint group '{0}' has no MapGroup configured in the Configure method. Exactly one MapGroup call must be specified.",
		category: "MinimalApiEndpoints",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	static readonly DiagnosticDescriptor multipleMapGroupsDescriptor = new(
		id: "MINAPI004",
		title: "Multiple MapGroup calls configured",
		messageFormat: "Endpoint group '{0}' has multiple MapGroup calls configured in the Configure method. Only one MapGroup call should be specified per endpoint group.",
		category: "MinimalApiEndpoints",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	static readonly DiagnosticDescriptor multipleGroupCallsDescriptor = new(
		id: "MINAPI005",
		title: "Multiple Group calls configured",
		messageFormat: "Type '{0}' has multiple Group calls configured in the Configure method. Only one Group call should be specified per endpoint.",
		category: "MinimalApiEndpoints",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static (INamedTypeSymbol symbol, string pattern)? GetGroup(this TypeDeclarationSyntax typeDeclaration, INamedTypeSymbol endpointGroupSymbol, Compilation compilation, SourceProductionContext context)
	{
		// Find the Configure method
		MethodDeclarationSyntax? configureMethod = typeDeclaration.Members
			.OfType<MethodDeclarationSyntax>()
			.FirstOrDefault(m => m.Identifier.ValueText == "Configure" && m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)));

		if (configureMethod is null)
		{
			return null;
		}

		// Look for Group calls in the method body
		IEnumerable<InvocationExpressionSyntax> groupCalls = configureMethod.DescendantNodes()
			.OfType<InvocationExpressionSyntax>()
			.Where(invocation => invocation.Expression is MemberAccessExpressionSyntax memberAccess && memberAccess.Name.Identifier.ValueText == "Group");

		List<InvocationExpressionSyntax> groupCallsList = groupCalls.ToList();

		// Validate that there's only one Group call
		if (groupCallsList.Count > 1)
		{
			// Multiple Group calls found
			context.ReportDiagnostic(Diagnostic.Create(
				multipleGroupCallsDescriptor,
				configureMethod.Identifier.GetLocation(),
				typeDeclaration.Identifier.ValueText));
			return null;
		}

		InvocationExpressionSyntax? firstGroupCall = groupCallsList.FirstOrDefault();

		if (firstGroupCall?.Expression is MemberAccessExpressionSyntax memberAccessExpr)
		{
			// Check if Group has generic type arguments
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
					string? pattern = GetPatternFromEndpointGroup(namedTypeSymbol, context);

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

	static string? GetPatternFromEndpointGroup(INamedTypeSymbol endpointGroupSymbol, SourceProductionContext context)
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

					List<InvocationExpressionSyntax> mapGroupCallsList = mapGroupCalls.ToList();

					// Validate endpoint group Configure method
					if (mapGroupCallsList.Count == 0)
					{
						// No MapGroup found
						context.ReportDiagnostic(Diagnostic.Create(
							noMapGroupDescriptor,
							configureMethod.Identifier.GetLocation(),
							endpointGroupSymbol.Name));
						return null;
					}

					if (mapGroupCallsList.Count > 1)
					{
						// Multiple MapGroup calls found
						context.ReportDiagnostic(Diagnostic.Create(
							multipleMapGroupsDescriptor,
							configureMethod.Identifier.GetLocation(),
							endpointGroupSymbol.Name));
						return null;
					}

					// Extract pattern from the single MapGroup call
					InvocationExpressionSyntax firstMapGroupCall = mapGroupCallsList.First();
					if (firstMapGroupCall.ArgumentList.Arguments.Count > 0)
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
