using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IeuanWalker.MinimalApi.Endpoints.Generator.Helpers;

internal static class ValidationHelpers
{
    public static bool Validate(this TypeDeclarationSyntax typeDeclaration)
    {
        // Find the Configure method
        MethodDeclarationSyntax? configureMethod = typeDeclaration.Members
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => m.Identifier.ValueText == "Configure" && m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)));

        if(configureMethod is null)
        {
            return true;
        }

        // Look for WithName calls in the method body
        IEnumerable<InvocationExpressionSyntax> withNameCalls = configureMethod.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(invocation => invocation.Expression is MemberAccessExpressionSyntax memberAccess && memberAccess.Name.Identifier.ValueText == "DontValidate");

        return !withNameCalls.Any();
    }
}
