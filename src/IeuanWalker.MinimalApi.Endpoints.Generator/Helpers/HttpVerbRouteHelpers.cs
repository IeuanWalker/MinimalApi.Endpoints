using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IeuanWalker.MinimalApi.Endpoints.Generator.Helpers;

static class HttpVerbRouteHelpers
{
	static readonly DiagnosticDescriptor noHttpVerbDescriptor = new(
		id: "MINAPI001",
		title: "No HTTP verb configured",
		messageFormat: "Type '{0}' has no HTTP verb configured in the Configure method. At least one HTTP verb (Get, Post, Put, Patch, Delete) must be specified.",
		category: "MinimalApiEndpoints",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	static readonly DiagnosticDescriptor multipleHttpVerbsDescriptor = new(
		id: "MINAPI002",
		title: "Multiple HTTP verbs configured",
		messageFormat: "Multiple HTTP verbs are configured in the Configure method. Only one HTTP verb should be specified per endpoint. Remove this '{0}' call or the other conflicting HTTP verb calls.",
		category: "MinimalApiEndpoints",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static (HttpVerb verb, string pattern)? GetVerbAndPattern(this TypeDeclarationSyntax typeDeclaration, SourceProductionContext context)
	{
		// Find the Configure method
		MethodDeclarationSyntax? configureMethod = typeDeclaration.Members
			.OfType<MethodDeclarationSyntax>()
			.FirstOrDefault(m => m.Identifier.ValueText == "Configure" && m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)));

		if (configureMethod is null)
		{
			return null;
		}

		HttpVerb? verb = null;
		string? pattern = null;

		// Look for HTTP verb extension method calls (Get, Post, Put, Patch, Delete)
		string[] httpVerbMethods = ["Get", "Post", "Put", "Patch", "Delete"];
		IEnumerable<InvocationExpressionSyntax> httpVerbCalls = configureMethod.DescendantNodes()
			.OfType<InvocationExpressionSyntax>()
			.Where(invocation => invocation.Expression is MemberAccessExpressionSyntax memberAccess && httpVerbMethods.Contains(memberAccess.Name.Identifier.ValueText));

		List<InvocationExpressionSyntax> httpVerbCallsList = [.. httpVerbCalls];

		// Validate HTTP verb usage
		if (httpVerbCallsList.Count == 0)
		{
			// No HTTP verb found - report on Configure method
			context.ReportDiagnostic(Diagnostic.Create(
				noHttpVerbDescriptor,
				configureMethod.Identifier.GetLocation(),
				typeDeclaration.Identifier.ValueText));
			return null;
		}

		if (httpVerbCallsList.Count > 1)
		{
			// Report error on each HTTP verb method call
			foreach (InvocationExpressionSyntax httpVerbCall in httpVerbCallsList)
			{
				if (httpVerbCall.Expression is MemberAccessExpressionSyntax memberAccess)
				{
					context.ReportDiagnostic(Diagnostic.Create(
						multipleHttpVerbsDescriptor,
						httpVerbCall.GetLocation(),
						memberAccess.Name.Identifier.ValueText));
				}
			}
			return null;
		}

		InvocationExpressionSyntax firstHttpVerbCall = httpVerbCallsList[0];
		if (firstHttpVerbCall.Expression is MemberAccessExpressionSyntax verbMemberAccess)
		{
			verb = ConvertToHttpVerb(verbMemberAccess.Name.Identifier.ValueText);

			// Try to extract the route pattern argument
			if (verb is not null && firstHttpVerbCall.ArgumentList.Arguments.Count > 0)
			{
				ArgumentSyntax argument = firstHttpVerbCall.ArgumentList.Arguments[0];

				if (argument.Expression is LiteralExpressionSyntax literal && literal.Token.IsKind(SyntaxKind.StringLiteralToken))
				{
					pattern = literal.Token.ValueText;
				}
			}
		}

		return verb is null || pattern is null ? null : (verb.Value, pattern);
	}

	public static string ToMap(this HttpVerb verb)
	{
		return verb switch
		{
			HttpVerb.Get => "MapGet",
			HttpVerb.Post => "MapPost",
			HttpVerb.Put => "MapPut",
			HttpVerb.Patch => "MapPatch",
			HttpVerb.Delete => "MapDelete",
			_ => throw new ArgumentOutOfRangeException(nameof(verb), verb, null)
		};
	}

	static HttpVerb? ConvertToHttpVerb(string verb) => verb.ToLower() switch
	{
		"get" => HttpVerb.Get,
		"post" => HttpVerb.Post,
		"put" => HttpVerb.Put,
		"patch" => HttpVerb.Patch,
		"delete" => HttpVerb.Delete,
		_ => null
	};

	/// <summary>
	/// Extracts HTTP verb and pattern from a type declaration for use in incremental generator transform step.
	/// Collects diagnostics instead of reporting them immediately.
	/// </summary>
	public static (HttpVerb verb, string pattern)? ExtractVerbAndPattern(TypeDeclarationSyntax typeDeclaration, string typeName, List<DiagnosticInfo> diagnostics)
	{
		MethodDeclarationSyntax? configureMethod = typeDeclaration.Members
			.OfType<MethodDeclarationSyntax>()
			.FirstOrDefault(m => m.Identifier.ValueText == "Configure" && m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)));

		if (configureMethod is null)
		{
			return null;
		}

		string[] httpVerbMethods = ["Get", "Post", "Put", "Patch", "Delete"];
		IEnumerable<InvocationExpressionSyntax> httpVerbCalls = configureMethod.DescendantNodes()
			.OfType<InvocationExpressionSyntax>()
			.Where(invocation => invocation.Expression is MemberAccessExpressionSyntax memberAccess && httpVerbMethods.Contains(memberAccess.Name.Identifier.ValueText));

		List<InvocationExpressionSyntax> httpVerbCallsList = httpVerbCalls.ToList();

		// Validate HTTP verb usage
		if (httpVerbCallsList.Count == 0)
		{
			// No HTTP verb found - report on Configure method
			diagnostics.Add(new DiagnosticInfo(
				noHttpVerbDescriptor.Id,
				noHttpVerbDescriptor.Title.ToString(),
				noHttpVerbDescriptor.MessageFormat.ToString(),
				noHttpVerbDescriptor.Category,
				noHttpVerbDescriptor.DefaultSeverity,
				configureMethod.Identifier.GetLocation(),
				typeName));
			return null;
		}

		if (httpVerbCallsList.Count > 1)
		{
			// Report error on each HTTP verb method call
			foreach (InvocationExpressionSyntax httpVerbCall in httpVerbCallsList)
			{
				if (httpVerbCall.Expression is MemberAccessExpressionSyntax memberAccess)
				{
					diagnostics.Add(new DiagnosticInfo(
						multipleHttpVerbsDescriptor.Id,
						multipleHttpVerbsDescriptor.Title.ToString(),
						multipleHttpVerbsDescriptor.MessageFormat.ToString(),
						multipleHttpVerbsDescriptor.Category,
						multipleHttpVerbsDescriptor.DefaultSeverity,
						httpVerbCall.GetLocation(),
						memberAccess.Name.Identifier.ValueText));
				}
			}
			return null;
		}

		InvocationExpressionSyntax firstHttpVerbCall = httpVerbCallsList[0];
		if (firstHttpVerbCall.Expression is MemberAccessExpressionSyntax verbMemberAccess)
		{
			HttpVerb? verb = ConvertToHttpVerb(verbMemberAccess.Name.Identifier.ValueText);

			if (verb.HasValue && firstHttpVerbCall.ArgumentList.Arguments.Count > 0)
			{
				ArgumentSyntax argument = firstHttpVerbCall.ArgumentList.Arguments[0];
				if (argument.Expression is LiteralExpressionSyntax literal && literal.Token.IsKind(SyntaxKind.StringLiteralToken))
				{
					return (verb.Value, literal.Token.ValueText);
				}
			}
		}

		return null;
	}
}
