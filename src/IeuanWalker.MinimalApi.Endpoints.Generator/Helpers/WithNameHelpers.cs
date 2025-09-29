using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IeuanWalker.MinimalApi.Endpoints.Generator.Helpers;

static class WithNameHelpers
{
	public static string? GetWithName(this TypeDeclarationSyntax typeDeclaration)
	{
		// Find the Configure method
		MethodDeclarationSyntax? configureMethod = typeDeclaration.Members
			.OfType<MethodDeclarationSyntax>()
			.FirstOrDefault(m => m.Identifier.ValueText == "Configure" && m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)));

		if (configureMethod is null)
		{
			return null;
		}

		// Look for WithName calls in the method body
		IEnumerable<InvocationExpressionSyntax> withNameCalls = configureMethod.DescendantNodes()
			.OfType<InvocationExpressionSyntax>()
			.Where(invocation => invocation.Expression is MemberAccessExpressionSyntax memberAccess && memberAccess.Name.Identifier.ValueText == "WithName");

		InvocationExpressionSyntax? firstWithNameCall = withNameCalls.FirstOrDefault();

		if (firstWithNameCall?.ArgumentList.Arguments.Count > 0)
		{
			// Try to extract the name argument
			ArgumentSyntax argument = firstWithNameCall.ArgumentList.Arguments[0];

			if (argument.Expression is LiteralExpressionSyntax literal && literal.Token.IsKind(SyntaxKind.StringLiteralToken))
			{
				return literal.Token.ValueText;
			}
		}

		return null;
	}

	public static string GenerateWithName(HttpVerb verb, string pattern, int routeNumber)
	{
		pattern = Regex.Replace(pattern, @"\{[^}]+\}", "");
		pattern = Regex.Replace(pattern, @"[^\w]", " ");
		pattern = Regex.Replace(pattern, @"\s+", " ").Trim();
		pattern = Regex.Replace(pattern, @"\bapi\b", "", RegexOptions.IgnoreCase).Trim();
		pattern = Regex.Replace(pattern, @"\bv\b", "", RegexOptions.IgnoreCase).Trim();
		try
		{
			pattern = string.Concat(pattern.Split(' ').Select(word => char.ToUpperInvariant(word[0]) + word.Substring(1)));
		}
		catch (Exception)
		{
			// Intentionally ignored - fallback to using just verb and route number
		}

		return $"{verb}_{pattern}_{routeNumber}";
	}

}
