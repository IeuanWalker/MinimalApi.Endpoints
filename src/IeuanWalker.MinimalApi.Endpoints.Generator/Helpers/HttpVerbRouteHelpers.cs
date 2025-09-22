using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IeuanWalker.MinimalApi.Endpoints.Generator.Helpers;

static class HttpVerbRouteHelpers
{
    public static (HttpVerb verb, string pattern)? GetVerbAndPattern(this TypeDeclarationSyntax typeDeclaration)
    {
        // Find the Configure method
        MethodDeclarationSyntax? configureMethod = typeDeclaration.Members
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => m.Identifier.ValueText == "Configure" && m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)));

        if(configureMethod is null)
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

        InvocationExpressionSyntax firstHttpVerbCall = httpVerbCalls.FirstOrDefault();
        if(firstHttpVerbCall?.Expression is MemberAccessExpressionSyntax verbMemberAccess)
        {
            verb = ConvertToHttpVerb(verbMemberAccess.Name.Identifier.ValueText);

            // Try to extract the route pattern argument
            if(verb is not null && firstHttpVerbCall.ArgumentList.Arguments.Count > 0)
            {
                ArgumentSyntax argument = firstHttpVerbCall.ArgumentList.Arguments[0];

                if(argument.Expression is LiteralExpressionSyntax literal && literal.Token.IsKind(SyntaxKind.StringLiteralToken))
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
}
