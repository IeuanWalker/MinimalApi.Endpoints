using System.Collections.Immutable;
using System.Text;
using IeuanWalker.MinimalApi.Endpoints.Generator.Extensions;
using IeuanWalker.MinimalApi.Endpoints.Generator.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace IeuanWalker.MinimalApi.Endpoints.Generator;

[Generator]
public class EndpointGenerator : IIncrementalGenerator
{
	static readonly DiagnosticDescriptor duplicateValidatorsDescriptor = new(
		id: "MINAPI006",
		title: "Duplicate Validator",
		messageFormat: "Multiple validators found for the model type '{0}'. Only one validator per model type is allowed. Remove this validator or the other conflicting validators for the same model type.",
		category: "Validation",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

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

	static readonly DiagnosticDescriptor multipleRequestTypeMethodsDescriptor = new(
		id: "MINAPI007",
		title: "Multiple request type methods configured",
		messageFormat: "Multiple request type methods are configured in the Configure method. Only one request type method should be specified per endpoint. Remove this '{0}' call or the other conflicting request type method calls.",
		category: "Request type",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	const string fullValidator = "IeuanWalker.MinimalApi.Endpoints.Validator`1";
	const string fullIEndpointGroup = "IeuanWalker.MinimalApi.Endpoints.IEndpointGroup";
	const string fullIEndpointBase = "IeuanWalker.MinimalApi.Endpoints.IEndpointBase";
	const string fullIEndpointWithRequestAndResponse = "IeuanWalker.MinimalApi.Endpoints.IEndpoint`2";
	const string fullIEndpointWithoutRequest = "IeuanWalker.MinimalApi.Endpoints.IEndpointWithoutRequest`1";
	const string fullIEndpointWithoutResponse = "IeuanWalker.MinimalApi.Endpoints.IEndpointWithoutResponse`1";
	const string fullIEndpointWithoutRequestOrResponse = "IeuanWalker.MinimalApi.Endpoints.IEndpoint";

	static EndpointTypeInfo? ExtractTypeInfo(GeneratorSyntaxContext context, CancellationToken cancellationToken)
	{
		if (context.Node is not TypeDeclarationSyntax typeDeclaration)
		{
			return null;
		}

		SemanticModel semanticModel = context.SemanticModel;
		INamedTypeSymbol? typeSymbol = semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken);

		if (typeSymbol is null || typeSymbol.IsAbstract)
		{
			return null;
		}

		// Get required symbols from compilation
		Compilation compilation = semanticModel.Compilation;
		INamedTypeSymbol? validatorSymbol = compilation.GetTypeByMetadataName(fullValidator);
		INamedTypeSymbol? endpointGroupSymbol = compilation.GetTypeByMetadataName(fullIEndpointGroup);
		INamedTypeSymbol? endpointBaseSymbol = compilation.GetTypeByMetadataName(fullIEndpointBase);
		INamedTypeSymbol? endpointWithRequestAndResponseSymbol = compilation.GetTypeByMetadataName(fullIEndpointWithRequestAndResponse);
		INamedTypeSymbol? endpointWithoutRequestSymbol = compilation.GetTypeByMetadataName(fullIEndpointWithoutRequest);
		INamedTypeSymbol? endpointWithoutResponseSymbol = compilation.GetTypeByMetadataName(fullIEndpointWithoutResponse);
		INamedTypeSymbol? endpointWithoutRequestOrResponseSymbol = compilation.GetTypeByMetadataName(fullIEndpointWithoutRequestOrResponse);

		if (validatorSymbol is null || endpointBaseSymbol is null)
		{
			return null;
		}

		// Check if this is a validator
		bool isValidator = typeSymbol.InheritsFromValidatorBase(validatorSymbol);
		string? validatedTypeName = null;
		if (isValidator)
		{
			ITypeSymbol? validatedType = typeSymbol.GetValidatedTypeFromValidator(validatorSymbol);
			validatedTypeName = validatedType?.ToDisplayString();
		}

		// Check if this is an endpoint
		bool isEndpoint = typeSymbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, endpointBaseSymbol));

		// Extract interface names for endpoint type determination
		string[] interfaceNames = typeSymbol.AllInterfaces
			.Select(i => i.OriginalDefinition.ToDisplayString())
			.ToArray();

		// Extract syntax-based information
		string? httpVerb = null;
		string? routePattern = null;
		string? withName = null;
		string? withTags = null;
		string? groupTypeName = null;
		string? groupPattern = null;
		string? requestTypeName = null;
		string? requestBindingType = null;
		string? requestBindingName = null;
		bool disableValidation = false;
		string? responseTypeName = null;
		List<DiagnosticInfo> diagnostics = new();

		if (isEndpoint)
		{
			// Extract HTTP verb and route pattern with diagnostics
			(HttpVerb verb, string pattern)? verbAndPattern = ExtractVerbAndPattern(typeDeclaration, typeSymbol.Name, diagnostics);
			if (verbAndPattern.HasValue)
			{
				httpVerb = verbAndPattern.Value.verb.ToString();
				routePattern = verbAndPattern.Value.pattern;
			}

			// Extract WithName
			withName = ExtractWithName(typeDeclaration);

			// Extract WithTags
			withTags = ExtractTags(typeDeclaration);

			// Extract Group information with diagnostics
			(string? groupType, string? pattern)? groupInfo = ExtractGroup(typeDeclaration, endpointGroupSymbol, semanticModel, diagnostics);
			if (groupInfo.HasValue)
			{
				groupTypeName = groupInfo.Value.groupType;
				groupPattern = groupInfo.Value.pattern;
			}

			// Extract request binding type with diagnostics
			(RequestBindingTypeEnum requestType, string? name)? requestBindingInfo = ExtractRequestBindingType(typeDeclaration, diagnostics);
			if (requestBindingInfo.HasValue)
			{
				requestBindingType = requestBindingInfo.Value.requestType.ToString();
				requestBindingName = requestBindingInfo.Value.name;
			}

			// Check if validation is disabled
			disableValidation = CheckDisableValidation(typeDeclaration);

			// Extract request and response types from interfaces
			foreach (INamedTypeSymbol interfaceType in typeSymbol.AllInterfaces)
			{
				if (SymbolEqualityComparer.Default.Equals(interfaceType.OriginalDefinition, endpointWithRequestAndResponseSymbol) && interfaceType.TypeArguments.Length == 2)
				{
					requestTypeName = interfaceType.TypeArguments[0].ToDisplayString();
					responseTypeName = interfaceType.TypeArguments[1].ToDisplayString();
				}
				else if (SymbolEqualityComparer.Default.Equals(interfaceType.OriginalDefinition, endpointWithoutRequestSymbol) && interfaceType.TypeArguments.Length == 1)
				{
					responseTypeName = interfaceType.TypeArguments[0].ToDisplayString();
				}
				else if (SymbolEqualityComparer.Default.Equals(interfaceType.OriginalDefinition, endpointWithoutResponseSymbol) && interfaceType.TypeArguments.Length == 1)
				{
					requestTypeName = interfaceType.TypeArguments[0].ToDisplayString();
				}
			}
		}

		return new EndpointTypeInfo(
			typeName: typeSymbol.ToDisplayString(),
			isAbstract: typeSymbol.IsAbstract,
			isValidator: isValidator,
			isEndpoint: isEndpoint,
			validatedTypeName: validatedTypeName,
			httpVerb: httpVerb,
			routePattern: routePattern,
			withName: withName,
			withTags: withTags,
			groupTypeName: groupTypeName,
			groupPattern: groupPattern,
			requestTypeName: requestTypeName,
			requestBindingType: requestBindingType,
			requestBindingName: requestBindingName,
			disableValidation: disableValidation,
			responseTypeName: responseTypeName,
			interfaceNames: interfaceNames,
			location: typeDeclaration.GetLocation(),
			diagnostics: diagnostics.ToArray()
		);
	}

	static (HttpVerb verb, string pattern)? ExtractVerbAndPattern(TypeDeclarationSyntax typeDeclaration, string typeName, List<DiagnosticInfo> diagnostics)
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

	static HttpVerb? ConvertToHttpVerb(string verb) => verb.ToLower() switch
	{
		"get" => HttpVerb.Get,
		"post" => HttpVerb.Post,
		"put" => HttpVerb.Put,
		"patch" => HttpVerb.Patch,
		"delete" => HttpVerb.Delete,
		_ => null
	};

	static string? ExtractWithName(TypeDeclarationSyntax typeDeclaration)
	{
		MethodDeclarationSyntax? configureMethod = typeDeclaration.Members
			.OfType<MethodDeclarationSyntax>()
			.FirstOrDefault(m => m.Identifier.ValueText == "Configure" && m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)));

		if (configureMethod is null)
		{
			return null;
		}

		InvocationExpressionSyntax? withNameCall = configureMethod.DescendantNodes()
			.OfType<InvocationExpressionSyntax>()
			.FirstOrDefault(invocation => invocation.Expression is MemberAccessExpressionSyntax memberAccess && memberAccess.Name.Identifier.ValueText == "WithName");

		if (withNameCall?.ArgumentList.Arguments.Count > 0)
		{
			ArgumentSyntax argument = withNameCall.ArgumentList.Arguments[0];
			if (argument.Expression is LiteralExpressionSyntax literal && literal.Token.IsKind(SyntaxKind.StringLiteralToken))
			{
				return literal.Token.ValueText;
			}
		}

		return null;
	}

	static string? ExtractTags(TypeDeclarationSyntax typeDeclaration)
	{
		MethodDeclarationSyntax? configureMethod = typeDeclaration.Members
			.OfType<MethodDeclarationSyntax>()
			.FirstOrDefault(m => m.Identifier.ValueText == "Configure" && m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)));

		if (configureMethod is null)
		{
			return null;
		}

		InvocationExpressionSyntax? withTagsCall = configureMethod.DescendantNodes()
			.OfType<InvocationExpressionSyntax>()
			.FirstOrDefault(invocation => invocation.Expression is MemberAccessExpressionSyntax memberAccess && memberAccess.Name.Identifier.ValueText == "WithTags");

		if (withTagsCall?.ArgumentList.Arguments.Count > 0)
		{
			ArgumentSyntax argument = withTagsCall.ArgumentList.Arguments[0];
			if (argument.Expression is LiteralExpressionSyntax literal && literal.Token.IsKind(SyntaxKind.StringLiteralToken))
			{
				return literal.Token.ValueText;
			}
		}

		return null;
	}

	static (string? groupTypeName, string? pattern)? ExtractGroup(TypeDeclarationSyntax typeDeclaration, INamedTypeSymbol? endpointGroupSymbol, SemanticModel semanticModel, List<DiagnosticInfo> diagnostics)
	{
		if (endpointGroupSymbol is null)
		{
			return null;
		}

		MethodDeclarationSyntax? configureMethod = typeDeclaration.Members
			.OfType<MethodDeclarationSyntax>()
			.FirstOrDefault(m => m.Identifier.ValueText == "Configure" && m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)));

		if (configureMethod is null)
		{
			return null;
		}

		IEnumerable<InvocationExpressionSyntax> groupCalls = configureMethod.DescendantNodes()
			.OfType<InvocationExpressionSyntax>()
			.Where(invocation => invocation.Expression is MemberAccessExpressionSyntax memberAccess && memberAccess.Name.Identifier.ValueText == "Group");

		List<InvocationExpressionSyntax> groupCallsList = groupCalls.ToList();

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
					groupCall.GetLocation()));
			}
			return null;
		}

		InvocationExpressionSyntax? firstGroupCall = groupCallsList.FirstOrDefault();

		if (firstGroupCall?.Expression is MemberAccessExpressionSyntax memberAccessExpr &&
			memberAccessExpr.Name is GenericNameSyntax genericName &&
			genericName.TypeArgumentList.Arguments.Count > 0)
		{
			TypeSyntax endpointGroup = genericName.TypeArgumentList.Arguments[0];
			Microsoft.CodeAnalysis.TypeInfo symbolTypeInfo = semanticModel.GetTypeInfo(endpointGroup);

			if (symbolTypeInfo.Type is INamedTypeSymbol namedTypeSymbol)
			{
				if (SymbolEqualityComparer.Default.Equals(namedTypeSymbol, endpointGroupSymbol) ||
					namedTypeSymbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, endpointGroupSymbol)) ||
					InheritsFrom(namedTypeSymbol, endpointGroupSymbol))
				{
					string? pattern = ExtractPatternFromEndpointGroup(namedTypeSymbol, diagnostics);
					if (pattern is not null)
					{
						return (namedTypeSymbol.ToDisplayString(), pattern);
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

	static string? ExtractPatternFromEndpointGroup(INamedTypeSymbol endpointGroupSymbol, List<DiagnosticInfo> diagnostics)
	{
		foreach (SyntaxReference syntaxRef in endpointGroupSymbol.DeclaringSyntaxReferences)
		{
			SyntaxNode syntaxNode = syntaxRef.GetSyntax();
			if (syntaxNode is TypeDeclarationSyntax typeDeclaration)
			{
				MethodDeclarationSyntax? configureMethod = typeDeclaration.Members
					.OfType<MethodDeclarationSyntax>()
					.FirstOrDefault(m => m.Identifier.ValueText == "Configure" && m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)));

				if (configureMethod is not null)
				{
					IEnumerable<InvocationExpressionSyntax> mapGroupCalls = configureMethod.DescendantNodes()
						.OfType<InvocationExpressionSyntax>()
						.Where(invocation => invocation.Expression is MemberAccessExpressionSyntax memberAccess && memberAccess.Name.Identifier.ValueText == "MapGroup");

					List<InvocationExpressionSyntax> mapGroupCallsList = mapGroupCalls.ToList();

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
							configureMethod.Identifier.GetLocation(),
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
								mapGroupCall.GetLocation()));
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

	static (RequestBindingTypeEnum requestType, string? name)? ExtractRequestBindingType(TypeDeclarationSyntax typeDeclaration, List<DiagnosticInfo> diagnostics)
	{
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
			.Where(invocation => invocation.Expression is MemberAccessExpressionSyntax memberAccess && requestTypeMethods.Contains(memberAccess.Name.Identifier.ValueText));

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
			if (requestType.HasValue)
			{
				string? name = null;
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
		_ => null
	};

	static bool CheckDisableValidation(TypeDeclarationSyntax typeDeclaration)
	{
		MethodDeclarationSyntax? configureMethod = typeDeclaration.Members
			.OfType<MethodDeclarationSyntax>()
			.FirstOrDefault(m => m.Identifier.ValueText == "Configure" && m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)));

		if (configureMethod is null)
		{
			return false;
		}

		return configureMethod.DescendantNodes()
			.OfType<InvocationExpressionSyntax>()
			.Any(invocation => invocation.Expression is MemberAccessExpressionSyntax memberAccess && memberAccess.Name.Identifier.ValueText == "DisableValidation");
	}

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		// Extract type information during syntax analysis
		IncrementalValuesProvider<EndpointTypeInfo?> typeInfos = context.SyntaxProvider
			.CreateSyntaxProvider(
				predicate: static (s, _) => s is TypeDeclarationSyntax tds && tds.BaseList != null,
				transform: static (ctx, ct) => ExtractTypeInfo(ctx, ct))
			.Where(static typeInfo => typeInfo != null);

		// Collect all type information
		IncrementalValueProvider<ImmutableArray<EndpointTypeInfo?>> collectedTypeInfos = typeInfos.Collect();

		// Get assembly name from compilation
		IncrementalValueProvider<string> assemblyNameProvider = context.CompilationProvider
			.Select(static (compilation, _) => compilation.Assembly.Name.Trim());

		// Combine collected type infos with assembly name
		IncrementalValueProvider<(ImmutableArray<EndpointTypeInfo?>, string)> combined = 
			collectedTypeInfos.Combine(assemblyNameProvider);

		// Generate source output
		context.RegisterSourceOutput(combined,
			static (spc, source) => Execute(source.Item1, source.Item2, spc));
	}

	static void Execute(ImmutableArray<EndpointTypeInfo?> typeInfos, string assemblyName, SourceProductionContext context)
	{
		List<EndpointInfo> endpointClasses = [];
		List<(string validator, string model, Location location)> allValidators = [];
		Dictionary<string, EndpointTypeInfo> typeInfoMap = new();

		foreach (EndpointTypeInfo? typeInfo in typeInfos)
		{
			if (typeInfo is null)
			{
				continue;
			}

			// Report diagnostics for this type
			foreach (DiagnosticInfo diagInfo in typeInfo.Diagnostics)
			{
				DiagnosticDescriptor descriptor = new(
					diagInfo.Id,
					diagInfo.Title,
					diagInfo.MessageFormat,
					diagInfo.Category,
					diagInfo.Severity,
					isEnabledByDefault: true);
				context.ReportDiagnostic(Diagnostic.Create(descriptor, diagInfo.Location, diagInfo.MessageArgs));
			}

			// Build a map for group lookups
			typeInfoMap[typeInfo.TypeName] = typeInfo;

			// Collect validators
			if (typeInfo.IsValidator && typeInfo.ValidatedTypeName is not null)
			{
				allValidators.Add((typeInfo.TypeName, typeInfo.ValidatedTypeName, typeInfo.Location));
				continue;
			}

			// Process endpoints
			if (!typeInfo.IsEndpoint || typeInfo.HttpVerb is null || typeInfo.RoutePattern is null)
			{
				continue;
			}

			// Parse the HTTP verb
			if (!Enum.TryParse<HttpVerb>(typeInfo.HttpVerb, out HttpVerb verb))
			{
				continue;
			}

			// Handle group inheritance for WithName and WithTags
			string? withName = typeInfo.WithName;
			string? withTags = typeInfo.WithTags;
			
			if (typeInfo.GroupTypeName is not null && typeInfoMap.TryGetValue(typeInfo.GroupTypeName, out EndpointTypeInfo? groupTypeInfo))
			{
				withName ??= groupTypeInfo.WithName;
				withTags ??= groupTypeInfo.WithTags;
			}

			// Prepare group info
			(string symbol, string pattern)? mapGroup = null;
			if (typeInfo.GroupTypeName is not null && typeInfo.GroupPattern is not null)
			{
				mapGroup = (typeInfo.GroupTypeName, typeInfo.GroupPattern);
			}

			// Find validator for request type
			string? fluentValidationClass = null;
			if (!typeInfo.DisableValidation && typeInfo.RequestTypeName is not null)
			{
				fluentValidationClass = allValidators
					.Where(v => v.model == typeInfo.RequestTypeName)
					.Select(v => v.validator)
					.FirstOrDefault();
			}

			// Prepare request binding info
			(RequestBindingTypeEnum RequestBindingType, string? Name)? requestBindingInfo = null;
			if (typeInfo.RequestBindingType is not null && Enum.TryParse<RequestBindingTypeEnum>(typeInfo.RequestBindingType, out RequestBindingTypeEnum bindingType))
			{
				requestBindingInfo = (bindingType, typeInfo.RequestBindingName);
			}

			// Determine endpoint type based on interfaces
			EndpointType? endpointType = null;
			if (typeInfo.InterfaceNames.Contains(fullIEndpointWithRequestAndResponse) && typeInfo.RequestTypeName is not null && typeInfo.ResponseTypeName is not null)
			{
				endpointType = EndpointType.WithRequestAndResponse;
			}
			else if (typeInfo.InterfaceNames.Contains(fullIEndpointWithoutRequest) && typeInfo.ResponseTypeName is not null)
			{
				endpointType = EndpointType.WithoutRequest;
			}
			else if (typeInfo.InterfaceNames.Contains(fullIEndpointWithoutResponse) && typeInfo.RequestTypeName is not null)
			{
				endpointType = EndpointType.WithoutResponse;
			}
			else if (typeInfo.InterfaceNames.Contains(fullIEndpointWithoutRequestOrResponse))
			{
				endpointType = EndpointType.WithoutRequestOrResponse;
			}

			if (endpointType.HasValue)
			{
				endpointClasses.Add(new EndpointInfo(
					typeInfo.TypeName,
					endpointType.Value,
					mapGroup,
					verb,
					typeInfo.RoutePattern,
					withName,
					withTags,
					typeInfo.RequestTypeName,
					requestBindingInfo,
					fluentValidationClass,
					typeInfo.ResponseTypeName));
			}
		}

		// Handle duplicate validators
		IEnumerable<IGrouping<string, (string validator, string model, Location location)>> validatorGroups = allValidators
			.GroupBy(v => v.model);

		List<(string validator, string model)> validators = [];

		foreach (IGrouping<string, (string validator, string model, Location location)> group in validatorGroups)
		{
			if (group.Count() > 1)
			{
				// Report error on each duplicate validator
				foreach ((string _, string model, Location location) in group)
				{
					context.ReportDiagnostic(Diagnostic.Create(
						duplicateValidatorsDescriptor,
						location,
						model));
				}
			}
			else
			{
				// Only one validator for this model type, add it to the valid validators list
				(string validator, string model, Location _) = group.First();
				validators.Add((validator, model));
			}
		}

		if (endpointClasses.Count == 0)
		{
			return;
		}

		string source = GenerateEndpointExtensions(endpointClasses, validators, assemblyName);
		context.AddSource("EndpointExtensions.g.cs", SourceText.From(source, Encoding.UTF8));
	}

	static string GenerateEndpointExtensions(List<EndpointInfo> endpointClasses, List<(string validator, string model)> validators, string assemblyName)
	{
		endpointClasses = [.. endpointClasses.OrderBy(x => x.ClassName)];

		using IndentedTextBuilder builder = new();
		string sanitisedAssemblyName = assemblyName?.Sanitize(string.Empty) ?? "Assembly";

		builder.AppendLine("""
			  // <auto-generated>
			  //   Generated by the IeuanWalker.MinimalApi.Endpoints
			  //   https://github.com/IeuanWalker/IeuanWalker.MinimalApi.Endpoints
			  // </auto-generated>

			  using Microsoft.AspNetCore.Mvc;
			  """);

		if (endpointClasses.Any(x => x.FluentValidationClass is not null))
		{
			builder.AppendLine("using IeuanWalker.MinimalApi.Endpoints.Filters;");
			builder.AppendLine("using FluentValidation;");
		}

		builder.AppendLine();
		builder.AppendLine($"namespace {assemblyName};");
		builder.AppendLine();

		builder.AppendLine("public static class EndpointExtensions");
		using (builder.AppendBlock())
		{
			// Generate AddEndpoints method
			builder.AppendLine($"public static IHostApplicationBuilder AddEndpointsFrom{sanitisedAssemblyName}(this IHostApplicationBuilder builder)");
			using (builder.AppendBlock())
			{
				foreach (EndpointInfo endpoint in endpointClasses)
				{
					if (endpoint.FluentValidationClass is not null && endpoint.RequestType is not null)
					{
						builder.AppendLine($"builder.Services.AddSingleton<IValidator<global::{endpoint.RequestType}>, global::{endpoint.FluentValidationClass}>();");

						validators.RemoveAll(x => x.model.Equals(endpoint.RequestType));
					}
					builder.AppendLine($"builder.Services.AddScoped<global::{endpoint.ClassName}>();");
				}
				builder.AppendLine();

				if (validators.Any())
				{
					builder.AppendLine("// Validators not directly related to an endpoints request model");
					foreach ((string validator, string model) in validators)
					{
						builder.AppendLine($"builder.Services.AddSingleton<IValidator<global::{model}>, global::{validator}>();");
					}
					builder.AppendLine();
				}

				builder.AppendLine("return builder;");
			}

			builder.AppendLine();

			// Generate MapEndpoints method with grouping
			builder.AppendLine($"public static WebApplication MapEndpointsFrom{sanitisedAssemblyName}(this WebApplication app)");
			using (builder.AppendBlock())
			{
				// Group endpoints by their Group property (both symbol and pattern)
				IOrderedEnumerable<IGrouping<(string? symbol, string? pattern), EndpointInfo>> groupedEndpoints = endpointClasses
					.GroupBy(x => (x.Group?.symbol, x.Group?.pattern))
					.OrderBy(g => g.Key.Item1).ThenBy(g => g.Key.pattern);

				int groupIndex = 0;
				int endpointIndex = 0;

				foreach (IGrouping<(string? symbol, string? pattern), EndpointInfo> group in groupedEndpoints)
				{
					if (group.Key.symbol is not null)
					{
						int lastDotIndex = group.Key.symbol.LastIndexOf('.');
						string lastSegment = lastDotIndex >= 0 ? group.Key.symbol.Substring(lastDotIndex + 1) : group.Key.symbol;
						string groupName = $"group_{lastSegment.Sanitize().ToLowerFirstLetter()}_{groupIndex}";

						builder.AppendLine($"// GROUP: {group.Key.symbol}");
						builder.AppendLine($"RouteGroupBuilder {groupName} = {group.Key.symbol}.Configure(app);");
						builder.AppendLine();

						foreach (EndpointInfo endpoint in group.OrderBy(x => x.Verb).ThenBy(x => x.Pattern))
						{
							builder.ToEndpoint(endpoint, endpointIndex, (groupName, group.Key.pattern ?? string.Empty));
							builder.AppendLine();

							endpointIndex++;
						}
					}
					else
					{
						// Endpoints without a group
						foreach (EndpointInfo endpoint in group.OrderBy(x => x.Verb).ThenBy(x => x.Pattern))
						{
							builder.ToEndpoint(endpoint, endpointIndex, null);
							builder.AppendLine();

							endpointIndex++;
						}
					}

					groupIndex++;
				}

				builder.AppendLine("return app;");
			}
		}

		return builder.ToString();
	}
}
