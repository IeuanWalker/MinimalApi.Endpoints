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
		id: "MINAPI007",
		title: "Duplicate Validator",
		messageFormat: "Multiple validators found for the model type '{0}'. Only one validator per model type is allowed. Remove this validator or the other conflicting validators for the same model type.",
		category: "Validation",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	static readonly DiagnosticDescriptor unusedGroupDescriptor = new(
		id: "MINAPI006",
		title: "Unused Endpoint Group",
		messageFormat: "The endpoint group '{0}' is defined but not used by any endpoints. Remove this group or assign it to an endpoint.",
		category: "Map group",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	const string fullValidator = "IeuanWalker.MinimalApi.Endpoints.Validator`1";
	const string fullIEndpointGroup = "IeuanWalker.MinimalApi.Endpoints.IEndpointGroup";
	const string fullIEndpointBase = "IeuanWalker.MinimalApi.Endpoints.IEndpointBase";
	const string fullIEndpointWithRequestAndResponse = "IeuanWalker.MinimalApi.Endpoints.IEndpoint`2";
	const string fullIEndpointWithoutRequest = "IeuanWalker.MinimalApi.Endpoints.IEndpointWithoutRequest`1";
	const string fullIEndpointWithoutResponse = "IeuanWalker.MinimalApi.Endpoints.IEndpointWithoutResponse`1";

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		// Extract type information during syntax analysis
		IncrementalValuesProvider<TypeInfo?> typeInfos = context.SyntaxProvider
			.CreateSyntaxProvider(
				predicate: static (s, _) => s is TypeDeclarationSyntax tds && tds.BaseList != null,
				transform: static (ctx, ct) => ExtractTypeInfo(ctx, ct))
			.Where(static typeInfo => typeInfo != null);

		// Collect all type information
		IncrementalValueProvider<ImmutableArray<TypeInfo?>> collectedTypeInfos = typeInfos.Collect();

		// Get assembly name from compilation
		IncrementalValueProvider<string> assemblyNameProvider = context.CompilationProvider
			.Select(static (compilation, _) => compilation.Assembly.Name.Trim());

		// Combine collected type infos with assembly name
		IncrementalValueProvider<(ImmutableArray<TypeInfo?>, string)> combined =
			collectedTypeInfos.Combine(assemblyNameProvider);

		// Generate source output
		context.RegisterSourceOutput(combined,
			static (spc, source) => Execute(source.Item1, source.Item2, spc));
	}

	static TypeInfo? ExtractTypeInfo(GeneratorSyntaxContext context, CancellationToken cancellationToken)
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

		Compilation compilation = semanticModel.Compilation;

		List<DiagnosticInfo> diagnostics = [];
		Location location = typeDeclaration.GetLocation();
		string typeName = typeSymbol.ToDisplayString();

		// Check if this is a validator
		INamedTypeSymbol validatorSymbol = compilation.GetTypeByMetadataName(fullValidator)!;
		bool isValidator = typeSymbol.InheritsFromValidatorBase(validatorSymbol);
		if (isValidator)
		{
			ITypeSymbol? validatedType = typeSymbol.GetValidatedTypeFromValidator(validatorSymbol);
			string? validatedTypeName = validatedType?.ToDisplayString();
			if (validatedTypeName != null)
			{
				return new ValidatorInfo(typeName, validatedTypeName, location, [.. diagnostics]);
			}
		}

		// Check if this is an endpoint group
		INamedTypeSymbol endpointGroupSymbol = compilation.GetTypeByMetadataName(fullIEndpointGroup)!;
		bool isEndpointGroup = typeSymbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, endpointGroupSymbol));
		if (isEndpointGroup)
		{
			string? pattern = semanticModel.GetDeclaredSymbol(typeDeclaration)?.GetPatternFromEndpointGroup(diagnostics);

			if (pattern is null)
			{
				return null;
			}

			return new EndpointGroupInfo(typeName, pattern, typeDeclaration.GetWithName(), typeDeclaration.GetTags(), location, [.. diagnostics]);
		}

		// Check if this is an endpoint
		INamedTypeSymbol endpointBaseSymbol = compilation.GetTypeByMetadataName(fullIEndpointBase)!;
		bool isEndpoint = typeSymbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, endpointBaseSymbol));

		if (!isEndpoint)
		{
			return null;
		}

		(HttpVerb verb, string pattern)? verbAndPattern = typeDeclaration.GetVerbAndPattern(typeSymbol.Name, diagnostics);

		if (verbAndPattern is null)
		{
			if (diagnostics.Any())
			{
				return new TypeInfo(typeName, location, diagnostics);
			}

			return null;
		}

		INamedTypeSymbol endpointWithRequestAndResponseSymbol = compilation.GetTypeByMetadataName(fullIEndpointWithRequestAndResponse)!;
		INamedTypeSymbol endpointWithoutRequestSymbol = compilation.GetTypeByMetadataName(fullIEndpointWithoutRequest)!;
		INamedTypeSymbol endpointWithoutResponseSymbol = compilation.GetTypeByMetadataName(fullIEndpointWithoutResponse)!;

		string? requestTypeName = null;
		string? responseTypeName = null;
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

		return new EndpointInfo(
			typeName,
			verbAndPattern.Value.verb,
			verbAndPattern.Value.pattern,
			typeDeclaration.GetWithName(),
			typeDeclaration.GetTags(),
			typeDeclaration.GetGroup(endpointGroupSymbol, semanticModel, diagnostics),
			requestTypeName,
			typeDeclaration.GetRequestTypeAndName(diagnostics),
			typeDeclaration.DontValidate(),
			responseTypeName,
			location,
			diagnostics);
	}

	static void Execute(ImmutableArray<TypeInfo?> typeInfos, string assemblyName, SourceProductionContext context)
	{
		List<EndpointInfo> allEndpoints = [.. typeInfos.OfType<EndpointInfo>()];
		List<EndpointGroupInfo> allEndpointGroups = [.. typeInfos.OfType<EndpointGroupInfo>()];
		List<ValidatorInfo> allValidators = [.. typeInfos.OfType<ValidatorInfo>()];

		if (allEndpoints.Count == 0)
		{
			return;
		}

		// Report diagnostics
		foreach (DiagnosticInfo diagnosticInfo in typeInfos.Where(x => x is not null).SelectMany(x => x?.Diagnostics))
		{
			DiagnosticDescriptor descriptor = new(
				diagnosticInfo.Id,
				diagnosticInfo.Title,
				diagnosticInfo.MessageFormat,
				diagnosticInfo.Category,
				diagnosticInfo.Severity,
				isEnabledByDefault: true);
			context.ReportDiagnostic(Diagnostic.Create(descriptor, diagnosticInfo.Location, diagnosticInfo.MessageArgs));
		}

		// Check for unused groups
		IEnumerable<string?> usedGroups = allEndpoints
			.Select(e => e.Group)
			.Where(g => g is not null)
			.Distinct();

		foreach (EndpointGroupInfo group in allEndpointGroups.Where(g => !usedGroups.Contains(g.TypeName)))
		{
			context.ReportDiagnostic(Diagnostic.Create(
					unusedGroupDescriptor,
					group.Location,
					group.TypeName));
		}


		// Handle duplicate validators
		IEnumerable<IGrouping<string, ValidatorInfo>> validatorGroups = allValidators.GroupBy(v => v.ValidatedTypeName);

		foreach (IGrouping<string, ValidatorInfo> group in validatorGroups.Where(x => x.Count() > 1))
		{
			// Report error on each duplicate validator
			foreach (ValidatorInfo validator in group)
			{
				context.ReportDiagnostic(Diagnostic.Create(
					duplicateValidatorsDescriptor,
					validator.Location,
					validator.ValidatedTypeName));
			}
		}

		string source = GenerateEndpointExtensions(allEndpoints, allEndpointGroups, allValidators, assemblyName);

		context.AddSource("EndpointExtensions.g.cs", SourceText.From(source, Encoding.UTF8));
	}

	static string GenerateEndpointExtensions(List<EndpointInfo> endpointClasses, List<EndpointGroupInfo> endpointGroups, List<ValidatorInfo> validators, string assemblyName)
	{
		using IndentedTextBuilder builder = new();
		string sanitisedAssemblyName = assemblyName?.Sanitize(string.Empty) ?? "Assembly";

		IEnumerable<IGrouping<string?, EndpointInfo>> groupedEndpoints = endpointClasses.GroupBy(x => x.Group);

		builder.AppendLine("""
			  // <auto-generated>
			  //   Generated by the IeuanWalker.MinimalApi.Endpoints
			  //   https://github.com/IeuanWalker/IeuanWalker.MinimalApi.Endpoints
			  // </auto-generated>

			  using Microsoft.AspNetCore.Mvc;
			  """);

		if (validators.Any())
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
				foreach (IGrouping<string?, EndpointInfo> group in groupedEndpoints)
				{
					foreach (EndpointInfo? endpoint in group.OrderBy(x => x.HttpVerb).ThenBy(x => x.RoutePattern))
					{
						if (endpoint.RequestType is not null)
						{
							ValidatorInfo? requestValidator = validators.FirstOrDefault(x => x.ValidatedTypeName.Equals(endpoint.RequestType));

							if (requestValidator is not null)
							{
								builder.AppendLine($"builder.Services.AddSingleton<IValidator<global::{endpoint.RequestType}>, global::{requestValidator.TypeName}>();");
							}
						}

						builder.AppendLine($"builder.Services.AddScoped<global::{endpoint.TypeName}>();");
					}
				}

				builder.AppendLine();

				List<ValidatorInfo> nonRequestModelValidators = [.. validators.Where(v => !endpointClasses.Any(e => e.RequestType == v.ValidatedTypeName))];
				if (nonRequestModelValidators.Any())
				{
					builder.AppendLine("// Validators not directly related to an endpoints request model");
					foreach (ValidatorInfo validator in nonRequestModelValidators)
					{
						builder.AppendLine($"builder.Services.AddSingleton<IValidator<global::{validator.ValidatedTypeName}>, global::{validator.TypeName}>();");
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
				int groupIndex = 0;
				int endpointIndex = 0;

				foreach (IGrouping<string?, EndpointInfo> endpointGroup in groupedEndpoints)
				{
					if (endpointGroup.Key is not null)
					{
						EndpointGroupInfo group = endpointGroups.First(x => x.TypeName == endpointGroup.Key);

						int lastDotIndex = group.TypeName.LastIndexOf('.');
						string lastSegment = lastDotIndex >= 0 ? group.TypeName.Substring(lastDotIndex + 1) : group.TypeName;
						string groupName = $"group_{lastSegment.Sanitize().ToLowerFirstLetter()}_{groupIndex}";

						builder.AppendLine($"// GROUP: {group.TypeName}");
						builder.AppendLine($"RouteGroupBuilder {groupName} = {group.TypeName}.Configure(app);");
						builder.AppendLine();

						foreach (EndpointInfo endpoint in endpointGroup.OrderBy(x => x.HttpVerb).ThenBy(x => x.RoutePattern))
						{
							builder.ToEndpoint(endpoint, endpointIndex, validators, (group, groupName));
							builder.AppendLine();

							endpointIndex++;
						}
					}
					else
					{
						// Endpoints without a group
						foreach (EndpointInfo endpoint in endpointGroup.OrderBy(x => x.HttpVerb).ThenBy(x => x.RoutePattern))
						{
							builder.ToEndpoint(endpoint, endpointIndex, validators, null);
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
