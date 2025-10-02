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

		if (isEndpoint)
		{
			// Extract HTTP verb and route pattern
			(HttpVerb verb, string pattern)? verbAndPattern = ExtractVerbAndPattern(typeDeclaration);
			if (verbAndPattern.HasValue)
			{
				httpVerb = verbAndPattern.Value.verb.ToString();
				routePattern = verbAndPattern.Value.pattern;
			}

			// Extract WithName
			withName = ExtractWithName(typeDeclaration);

			// Extract WithTags
			withTags = ExtractTags(typeDeclaration);

			// Extract Group information
			(string? groupType, string? pattern)? groupInfo = ExtractGroup(typeDeclaration, endpointGroupSymbol, semanticModel);
			if (groupInfo.HasValue)
			{
				groupTypeName = groupInfo.Value.groupType;
				groupPattern = groupInfo.Value.pattern;
			}

			// Extract request binding type
			(RequestBindingTypeEnum requestType, string? name)? requestBindingInfo = ExtractRequestBindingType(typeDeclaration);
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
			location: typeDeclaration.GetLocation()
		);
	}

	static (HttpVerb verb, string pattern)? ExtractVerbAndPattern(TypeDeclarationSyntax typeDeclaration)
	{
		MethodDeclarationSyntax? configureMethod = typeDeclaration.Members
			.OfType<MethodDeclarationSyntax>()
			.FirstOrDefault(m => m.Identifier.ValueText == "Configure" && m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)));

		if (configureMethod is null)
		{
			return null;
		}

		string[] httpVerbMethods = ["Get", "Post", "Put", "Patch", "Delete"];
		InvocationExpressionSyntax? httpVerbCall = configureMethod.DescendantNodes()
			.OfType<InvocationExpressionSyntax>()
			.FirstOrDefault(invocation => invocation.Expression is MemberAccessExpressionSyntax memberAccess && httpVerbMethods.Contains(memberAccess.Name.Identifier.ValueText));

		if (httpVerbCall?.Expression is MemberAccessExpressionSyntax verbMemberAccess)
		{
			HttpVerb? verb = ConvertToHttpVerb(verbMemberAccess.Name.Identifier.ValueText);

			if (verb.HasValue && httpVerbCall.ArgumentList.Arguments.Count > 0)
			{
				ArgumentSyntax argument = httpVerbCall.ArgumentList.Arguments[0];
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

	static (string? groupTypeName, string? pattern)? ExtractGroup(TypeDeclarationSyntax typeDeclaration, INamedTypeSymbol? endpointGroupSymbol, SemanticModel semanticModel)
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

		InvocationExpressionSyntax? groupCall = configureMethod.DescendantNodes()
			.OfType<InvocationExpressionSyntax>()
			.FirstOrDefault(invocation => invocation.Expression is MemberAccessExpressionSyntax memberAccess && memberAccess.Name.Identifier.ValueText == "Group");

		if (groupCall?.Expression is MemberAccessExpressionSyntax memberAccessExpr &&
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
					string? pattern = ExtractPatternFromEndpointGroup(namedTypeSymbol);
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

	static string? ExtractPatternFromEndpointGroup(INamedTypeSymbol endpointGroupSymbol)
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
					InvocationExpressionSyntax? mapGroupCall = configureMethod.DescendantNodes()
						.OfType<InvocationExpressionSyntax>()
						.FirstOrDefault(invocation => invocation.Expression is MemberAccessExpressionSyntax memberAccess && memberAccess.Name.Identifier.ValueText == "MapGroup");

					if (mapGroupCall?.ArgumentList.Arguments.Count > 0)
					{
						ArgumentSyntax firstArgument = mapGroupCall.ArgumentList.Arguments[0];
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

	static (RequestBindingTypeEnum requestType, string? name)? ExtractRequestBindingType(TypeDeclarationSyntax typeDeclaration)
	{
		MethodDeclarationSyntax? configureMethod = typeDeclaration.Members
			.OfType<MethodDeclarationSyntax>()
			.FirstOrDefault(m => m.Identifier.ValueText == "Configure" && m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)));

		if (configureMethod is null)
		{
			return null;
		}

		string[] requestTypeMethods = ["RequestFromBody", "RequestFromQuery", "RequestFromRoute", "RequestFromHeader", "RequestFromForm", "RequestAsParameters"];
		InvocationExpressionSyntax? requestTypeCall = configureMethod.DescendantNodes()
			.OfType<InvocationExpressionSyntax>()
			.FirstOrDefault(invocation => invocation.Expression is MemberAccessExpressionSyntax memberAccess && requestTypeMethods.Contains(memberAccess.Name.Identifier.ValueText));

		if (requestTypeCall?.Expression is MemberAccessExpressionSyntax requestTypeMemberAccess)
		{
			RequestBindingTypeEnum? requestType = ConvertToRequestBindingType(requestTypeMemberAccess.Name.Identifier.ValueText);
			if (requestType.HasValue)
			{
				string? name = null;
				if (requestTypeCall.ArgumentList.Arguments.Count > 0)
				{
					ArgumentSyntax argument = requestTypeCall.ArgumentList.Arguments[0];
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
