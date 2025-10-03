using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IeuanWalker.MinimalApi.Endpoints.Generator.Helpers;

static class RequestBindingTypeHelpers
{
	static readonly DiagnosticDescriptor multipleRequestTypeMethodsDescriptor = new(
		id: "MINAPI008",
		title: "Multiple request type methods configured",
		messageFormat: "Multiple request type methods are configured in the Configure method. Only one request type method should be specified per endpoint. Remove this '{0}' call or the other conflicting request type method calls.",
		category: "Request type",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static (RequestBindingTypeEnum requestType, string? name)? GetRequestTypeAndName(this TypeDeclarationSyntax typeDeclaration, List<DiagnosticInfo> diagnostics)
	{
		// Find the Configure method
		MethodDeclarationSyntax? configureMethod = typeDeclaration.Members
			.OfType<MethodDeclarationSyntax>()
			.FirstOrDefault(m => m.Identifier.ValueText == "Configure" && m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)));

		if (configureMethod is null)
		{
			return null;
		}

		string[] requestTypeMethods = ["RequestFromBody", "RequestFromQuery", "RequestFromRoute", "RequestFromHeader", "RequestFromForm", "RequestAsParameters"];
		IEnumerable<InvocationExpressionSyntax> requestTypeCalls = configureMethod.DescendantNodes()
			.OfType<InvocationExpressionSyntax>()
			.Where(invocation => invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
				   requestTypeMethods.Contains(memberAccess.Name.Identifier.ValueText));

		// Validate that there's only one request type method call
		if (requestTypeCalls.Count() > 1)
		{
			// Report error on each request type method call
			foreach (InvocationExpressionSyntax requestTypeCall in requestTypeCalls)
			{
				if (requestTypeCall.Expression is MemberAccessExpressionSyntax memberAccess)
				{
					diagnostics.Add(new DiagnosticInfo(
						multipleRequestTypeMethodsDescriptor.Id,
						multipleRequestTypeMethodsDescriptor.Title.ToString(),
						multipleRequestTypeMethodsDescriptor.MessageFormat.ToString(),
						multipleRequestTypeMethodsDescriptor.Category,
						multipleRequestTypeMethodsDescriptor.DefaultSeverity,
						requestTypeCall.GetLocation(),
						memberAccess.Name.Identifier.ValueText));
				}
			}
			return null;
		}

		InvocationExpressionSyntax? firstRequestTypeCall = requestTypeCalls.FirstOrDefault();
		if (firstRequestTypeCall?.Expression is MemberAccessExpressionSyntax requestTypeMemberAccess)
		{
			RequestBindingTypeEnum? requestType = ConvertToRequestBindingType(requestTypeMemberAccess.Name.Identifier.ValueText);

			if (requestType is null)
			{
				return null;
			}

			string? name = null;

			// Try to extract the optional name argument
			if (firstRequestTypeCall.ArgumentList.Arguments.Count > 0)
			{
				ArgumentSyntax argument = firstRequestTypeCall.ArgumentList.Arguments[0];

				if (argument.Expression is LiteralExpressionSyntax literal && literal.Token.IsKind(SyntaxKind.StringLiteralToken))
				{
					name = literal.Token.ValueText;
				}
			}

			return (requestType.Value, name);
		}

		return null;
	}

	static RequestBindingTypeEnum? ConvertToRequestBindingType(string requestType) => requestType switch
	{
		"RequestFromBody" => RequestBindingTypeEnum.FromBody,
		"RequestFromQuery" => RequestBindingTypeEnum.FromQuery,
		"RequestFromRoute" => RequestBindingTypeEnum.FromRoute,
		"RequestFromHeader" => RequestBindingTypeEnum.FromHeader,
		"RequestFromForm" => RequestBindingTypeEnum.FromForm,
		"RequestAsParameters" => RequestBindingTypeEnum.AsParameters,
		_ => throw new NotImplementedException($"Unknown request type: {requestType}")
	};

	internal static string ConvertFromRequestBindingType(this RequestBindingTypeEnum requestType) => requestType switch
	{
		RequestBindingTypeEnum.FromBody => "FromBody",
		RequestBindingTypeEnum.FromQuery => "FromQuery",
		RequestBindingTypeEnum.FromRoute => "FromRoute",
		RequestBindingTypeEnum.FromHeader => "FromHeader",
		RequestBindingTypeEnum.FromForm => "FromForm",
		RequestBindingTypeEnum.AsParameters => "AsParameters",
		_ => throw new NotImplementedException()
	};
}

public enum RequestBindingTypeEnum
{
	FromBody,
	FromHeader,
	FromRoute,
	FromQuery,
	FromForm,
	AsParameters
}
