using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IeuanWalker.MinimalApi.Endpoints.Generator.Helpers;

static class WithTagsHelper
{
	public static string? GetTags(this TypeDeclarationSyntax typeDeclaration)
	{
		MethodDeclarationSyntax? configureMethod = typeDeclaration.Members
			.OfType<MethodDeclarationSyntax>()
			.FirstOrDefault(m => m.Identifier.ValueText == "Configure" && m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)));

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
		builder.Append($".WithTags(\"{apiName}\")");
	}

	static string? ExtractApiNameFromPattern(string pattern)
	{
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

		// Handle different pattern formats:
		// /rootName/api/v1/ApiName -> ApiName
		// /rootName/api/ApiName -> ApiName  
		// /api/v1/ApiName -> ApiName
		// /api/v{version:apiVersion}/ApiName -> ApiName
		// /api/ApiName -> ApiName
		// /root/ApiName/{Id} -> ApiName
		// /api/v1/Users/{userId}/Posts/{postId} -> Users

		if (segments.Length == 0)
		{
			return null;
		}

		// Find the "api" segment
		int apiIndex = -1;
		for (int i = 0; i < segments.Length; i++)
		{
			if (segments[i].Equals("api", StringComparison.OrdinalIgnoreCase))
			{
				apiIndex = i;
				break;
			}
		}

		if (apiIndex == -1)
		{
			// No "api" segment found, find the first non-route-parameter segment
			return GetFirstNonParameterSegment(segments);
		}

		// Look for the API name after the "api" segment
		int apiNameIndex = apiIndex + 1;

		// Skip version segments (v1, v2, etc.)
		while (apiNameIndex < segments.Length && IsVersionSegment(segments[apiNameIndex]))
		{
			apiNameIndex++;
		}

		// Find the first non-route-parameter segment after api/version
		for (int i = apiNameIndex; i < segments.Length; i++)
		{
			if (!IsRouteParameter(segments[i]))
			{
				return segments[i];
			}
		}

		// Fallback: find any non-parameter segment
		return GetFirstNonParameterSegment(segments);
	}

	static string? GetFirstNonParameterSegment(string[] segments)
	{
		// Find the first segment that isn't a route parameter
		for (int i = segments.Length - 1; i >= 0; i--)
		{
			if (!IsRouteParameter(segments[i]) && !IsVersionSegment(segments[i]))
			{
				return segments[i];
			}
		}

		// If all segments are parameters or versions, return the last non-parameter one
		for (int i = segments.Length - 1; i >= 0; i--)
		{
			if (!IsRouteParameter(segments[i]))
			{
				return segments[i];
			}
		}

		return segments.Length > 0 ? segments[segments.Length - 1] : null;
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

		// Check for patterns like v1, v2, v10, etc.
		if (segment.Length >= 2 &&
		   segment[0] == 'v' &&
		   segment.Substring(1).All(char.IsDigit))
		{
			return true;
		}

		// Check for versioning parameter patterns like v{version:apiVersion}
		if (segment.StartsWith("v{") && segment.EndsWith("}"))
		{
			return true;
		}

		return false;
	}
}
