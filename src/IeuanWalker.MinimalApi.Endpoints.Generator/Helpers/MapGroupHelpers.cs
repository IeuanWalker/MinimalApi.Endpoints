using IeuanWalker.MinimalApi.Endpoints.Generator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IeuanWalker.MinimalApi.Endpoints.Generator.Helpers;

static class MapGroupHelpers
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
		messageFormat: "Multiple MapGroup calls are configured in the Configure method. Only one MapGroup call should be specified per endpoint group. Remove this 'MapGroup' call or the other conflicting MapGroup calls.",
		category: "MinimalApiEndpoints",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	static readonly DiagnosticDescriptor multipleGroupCallsDescriptor = new(
		id: "MINAPI005",
		title: "Multiple Group calls configured",
		messageFormat: "Multiple Group calls are configured in the Configure method. Only one Group call should be specified per endpoint. Remove this 'Group' call or the other conflicting Group calls.",
		category: "MinimalApiEndpoints",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static string? GetGroup(this TypeDeclarationSyntax typeDeclaration, INamedTypeSymbol? endpointGroupSymbol, SemanticModel semanticModel, List<DiagnosticInfo> diagnostics)
	{
		if (endpointGroupSymbol is null)
		{
			return null;
		}

		MethodDeclarationSyntax? configureMethod = typeDeclaration.Members.GetConfigureMethod();

		if (configureMethod is null)
		{
			return null;
		}

		// Look for Group calls in the method body
		IEnumerable<InvocationExpressionSyntax> groupCalls = configureMethod.DescendantNodes()
			.OfType<InvocationExpressionSyntax>()
			.Where(invocation => invocation.Expression is MemberAccessExpressionSyntax memberAccess && memberAccess.Name.Identifier.ValueText == "Group");

		List<InvocationExpressionSyntax> groupCallsList = [.. groupCalls];

		// Validate that there's only one Group call
		if (groupCallsList.Count > 1)
		{
			// Report error on each Group method call
			foreach (InvocationExpressionSyntax groupCall in groupCallsList)
			{
				diagnostics.Add(new DiagnosticInfo(
					multipleGroupCallsDescriptor.Id,
					multipleGroupCallsDescriptor.Title.ToString(),
					multipleGroupCallsDescriptor.MessageFormat.ToString(),
					multipleGroupCallsDescriptor.Category,
					multipleGroupCallsDescriptor.DefaultSeverity,
					new LocationInfo(groupCall.GetLocation())));
			}
			return null;
		}

		InvocationExpressionSyntax? firstGroupCall = groupCallsList.FirstOrDefault();

		if (
			firstGroupCall?.Expression is MemberAccessExpressionSyntax memberAccessExpr &&
			memberAccessExpr.Name is GenericNameSyntax genericName &&
			genericName.TypeArgumentList.Arguments.Count > 0)
		{
			// Extract the first generic type argument
			TypeSyntax endpointGroup = genericName.TypeArgumentList.Arguments[0];

			// Get the type information for the syntax node
			Microsoft.CodeAnalysis.TypeInfo typeInfo = semanticModel.GetTypeInfo(endpointGroup);

			if (typeInfo.Type is not INamedTypeSymbol namedTypeSymbol)
			{
				return null;
			}

			if (SymbolEqualityComparer.Default.Equals(namedTypeSymbol, endpointGroupSymbol) ||
				namedTypeSymbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, endpointGroupSymbol)) ||
				InheritsFrom(namedTypeSymbol, endpointGroupSymbol))
			{
				// Now get the pattern from the endpoint group's Configure method
				string? pattern = GetPatternFromEndpointGroup(namedTypeSymbol, diagnostics);

				if (pattern is null)
				{
					return null;
				}

				return namedTypeSymbol.ToDisplayString();
			}
		}

		return null;
	}

	public static string? GetPatternFromEndpointGroup(this INamedTypeSymbol endpointGroupSymbol, List<DiagnosticInfo> diagnostics)
	{
		// Get all syntax references for the endpoint group type
		foreach (SyntaxReference syntaxRef in endpointGroupSymbol.DeclaringSyntaxReferences)
		{
			SyntaxNode syntaxNode = syntaxRef.GetSyntax();
			if (syntaxNode is TypeDeclarationSyntax typeDeclaration)
			{
				MethodDeclarationSyntax? configureMethod = typeDeclaration.Members.GetConfigureMethod();

				if (configureMethod is not null)
				{
					// Look for MapGroup calls in the Configure method
					IEnumerable<InvocationExpressionSyntax> mapGroupCalls = configureMethod.DescendantNodes()
						.OfType<InvocationExpressionSyntax>()
						.Where(invocation => invocation.Expression is MemberAccessExpressionSyntax memberAccess && memberAccess.Name.Identifier.ValueText == "MapGroup");

					List<InvocationExpressionSyntax> mapGroupCallsList = [.. mapGroupCalls];

					// Validate endpoint group Configure method
					if (mapGroupCallsList.Count == 0)
					{
						// No MapGroup found
						diagnostics.Add(new DiagnosticInfo(
							noMapGroupDescriptor.Id,
							noMapGroupDescriptor.Title.ToString(),
							noMapGroupDescriptor.MessageFormat.ToString(),
							noMapGroupDescriptor.Category,
							noMapGroupDescriptor.DefaultSeverity,
							new LocationInfo(configureMethod.Identifier.GetLocation()),
							endpointGroupSymbol.Name));
						return null;
					}

					if (mapGroupCallsList.Count > 1)
					{
						// Report error on each MapGroup method call
						foreach (InvocationExpressionSyntax mapGroupCall in mapGroupCallsList)
						{
							diagnostics.Add(new DiagnosticInfo(
								multipleMapGroupsDescriptor.Id,
								multipleMapGroupsDescriptor.Title.ToString(),
								multipleMapGroupsDescriptor.MessageFormat.ToString(),
								multipleMapGroupsDescriptor.Category,
								multipleMapGroupsDescriptor.DefaultSeverity,
								new LocationInfo(mapGroupCall.GetLocation())));
						}
						return null;
					}

					// Extract pattern from the single MapGroup call
					InvocationExpressionSyntax firstMapGroupCall = mapGroupCallsList[0];
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
