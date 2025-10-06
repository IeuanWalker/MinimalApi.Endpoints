using IeuanWalker.MinimalApi.Endpoints.Generator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IeuanWalker.MinimalApi.Endpoints.Generator.Helpers;

static class WithTagsHelpers
{
	public static string? GetTags(this TypeDeclarationSyntax typeDeclaration)
	{
		MethodDeclarationSyntax? configureMethod = typeDeclaration.Members.GetConfigureMethod();

		if (configureMethod is null)
		{
			return null;
		}

		IEnumerable<InvocationExpressionSyntax> withTagsCalls = configureMethod.DescendantNodes()
			.OfType<InvocationExpressionSyntax>()
			.Where(invocation => invocation.Expression is MemberAccessExpressionSyntax memberAccess && memberAccess.Name.Identifier.ValueText == "WithTags");

		InvocationExpressionSyntax? firstWithNameCall = withTagsCalls.FirstOrDefault();

		if (firstWithNameCall?.ArgumentList.Arguments.Count > 0)
		{
			ArgumentSyntax argument = firstWithNameCall.ArgumentList.Arguments[0];

			if (argument.Expression is LiteralExpressionSyntax literal && literal.Token.IsKind(SyntaxKind.StringLiteralToken))
			{
				return literal.Token.ValueText;
			}
		}

		return null;
	}

	public static void GenerateAndAddTags(this IndentedTextBuilder builder, string pattern)
	{
		string? apiName = ExtractApiNameFromPattern(pattern);

		if (string.IsNullOrEmpty(apiName))
		{
			return;
		}

		builder.AppendLine();
		builder.Append($".WithTags(\"{apiName!.ToUpperFirstLetter()}\")");
	}

	static string? ExtractApiNameFromPattern(string pattern)
	{
		// Handle null or empty pattern
		if (string.IsNullOrEmpty(pattern))
		{
			return null;
		}

		// Remove query string if present
		int queryIndex = pattern.IndexOf('?');
		if (queryIndex >= 0)
		{
			pattern = pattern.Substring(0, queryIndex);
		}

		// Remove leading slash if present
		if (pattern.StartsWith("/"))
		{
			pattern = pattern.Substring(1);
		}

		// Split the pattern by slashes and remove empty entries
		string[] segments = [.. pattern.Split('/').Where(s => !string.IsNullOrEmpty(s))];

		if (segments.Length == 0)
		{
			return null;
		}

		// Determine starting index based on API segment presence
		int startIndex = 0;
		int apiIndex = Array.FindIndex(segments, s => s.Equals("api", StringComparison.OrdinalIgnoreCase));

		if (apiIndex != -1)
		{
			// API segment found - start after it and skip version segments
			startIndex = apiIndex + 1;
			while (startIndex < segments.Length && IsVersionSegment(segments[startIndex]))
			{
				startIndex++;
			}
		}

		// Find the first valid segment starting from determined index
		for (int i = startIndex; i < segments.Length; i++)
		{
			if (!IsRouteParameter(segments[i]) && !IsVersionSegment(segments[i]))
			{
				return segments[i];
			}
		}

		// If no good segments found, return null
		return null;
	}

	static bool IsRouteParameter(string segment)
	{
		if (string.IsNullOrEmpty(segment))
		{
			return false;
		}

		// Check for route parameter patterns like {id}, {userId}, {postId}, etc.
		return segment.StartsWith("{") && segment.EndsWith("}");
	}

	static bool IsVersionSegment(string segment)
	{
		if (string.IsNullOrEmpty(segment))
		{
			return false;
		}

		// Check for patterns like v1, v2, v10, V1, V2, etc. (case insensitive)
		if (segment.Length >= 2 &&
		   char.ToLowerInvariant(segment[0]) == 'v' &&
		   segment.Substring(1).All(char.IsDigit))
		{
			return true;
		}

		// Check for decimal versions like v1.5, V2.0, v3.14, V1.2.3, etc. (case insensitive)
		if (segment.Length >= 4 && char.ToLowerInvariant(segment[0]) == 'v')
		{
			string versionPart = segment.Substring(1);

			if (versionPart.All(c => char.IsDigit(c) || c == '.') &&
				!versionPart.StartsWith(".") &&
				!versionPart.EndsWith(".") &&
				versionPart.Any(char.IsDigit))
			{
				return true;
			}
		}

		// Check for versioning parameter patterns like v{version:apiVersion} or V{version:apiVersion}
		return segment.StartsWith("v{", StringComparison.OrdinalIgnoreCase) && segment.EndsWith("}");
	}
}
