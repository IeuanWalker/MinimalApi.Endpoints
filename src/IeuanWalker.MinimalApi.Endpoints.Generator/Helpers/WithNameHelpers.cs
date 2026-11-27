using System.Security.Cryptography;
using System.Text;
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

	public static string GenerateNamePrefix(HttpVerb verb, string pattern)
	{
		pattern = Regex.Replace(pattern, @"\{[^}]+\}", ""); // Remove route parameters
		pattern = Regex.Replace(pattern, @"\bapi\b", "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant); // Remove "api"
		pattern = Regex.Replace(pattern, @"\bv\d*(?:\.\d+)*\b", "", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant); // Remove version numbers
		pattern = Regex.Replace(pattern, @"[^\w]", " "); // Convert non-word chars to spaces
		pattern = Regex.Replace(pattern, @"\s+", " ").Trim(); // Normalize whitespace
		if (pattern.Length > 0)
		{
			pattern = string.Concat(pattern.Split(' ').Select(word => char.ToUpperInvariant(word[0]) + word.Substring(1)));
		}

		return $"{verb}_{pattern}";
	}

	public static string GenerateWithName(string prefix, string typeName)
	{
		using SHA256 sha256 = SHA256.Create();
		byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(typeName));
		string suffix = BitConverter.ToString(hash, 0, 8).Replace("-", string.Empty).ToLowerInvariant();
		return $"{prefix}_{suffix}";
	}
}
