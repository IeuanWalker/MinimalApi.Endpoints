using System.Text.RegularExpressions;
using IeuanWalker.MinimalApi.Endpoints.Generator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IeuanWalker.MinimalApi.Endpoints.Generator.Helpers;

static class WithNameHelpers
{
	public static string? GetWithName(this TypeDeclarationSyntax typeDeclaration)
	{
		MethodDeclarationSyntax? configureMethod = typeDeclaration.Members.GetConfigureMethod();

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
		pattern = Regex.Replace(pattern, @"\{[^}]+\}", ""); // Remove route parameters
		pattern = Regex.Replace(pattern, @"\bapi\b", "", RegexOptions.IgnoreCase); // Remove "api"
		pattern = Regex.Replace(pattern, @"\bv\d*(?:\.\d+)*\b", "", RegexOptions.IgnoreCase); // Remove version numbers
		pattern = Regex.Replace(pattern, @"[^\w]", " "); // Convert non-word chars to spaces
		pattern = Regex.Replace(pattern, @"\s+", " ").Trim(); // Normalize whitespace
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
