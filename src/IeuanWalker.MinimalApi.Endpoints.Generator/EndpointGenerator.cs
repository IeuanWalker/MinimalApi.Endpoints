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
	const string fullIEndpointGroup = "IeuanWalker.MinimalApi.Endpoints.IEndpointGroup";
	const string fullIEndpointBase = "IeuanWalker.MinimalApi.Endpoints.IEndpointBase";
	const string fullIEndpointWithRequestAndResponse = "IeuanWalker.MinimalApi.Endpoints.IEndpoint`2";
	const string fullIEndpointWithoutRequest = "IeuanWalker.MinimalApi.Endpoints.IEndpointWithoutRequest`1";
	const string fullIEndpointWithoutResponse = "IeuanWalker.MinimalApi.Endpoints.IEndpointWithoutResponse`1";
	const string fullIEndpointWithoutRequestOrResponse = "IeuanWalker.MinimalApi.Endpoints.IEndpoint";

	static string? assemblyName;

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		// Find any type declarations with base lists (potential interface implementers)
		IncrementalValuesProvider<TypeDeclarationSyntax?> typeDeclarations = context.SyntaxProvider
			.CreateSyntaxProvider(
				predicate: static (s, _) => s is TypeDeclarationSyntax tds && tds.BaseList != null,
				transform: static (ctx, _) => ctx.Node as TypeDeclarationSyntax)
			.Where(type => type != null);

		// Combine with compilation for semantic analysis
		IncrementalValueProvider<(Compilation, ImmutableArray<TypeDeclarationSyntax?>)> compilationAndTypes
			= context.CompilationProvider.Combine(typeDeclarations.Collect());

		// Generate source output
		context.RegisterSourceOutput(compilationAndTypes,
			(spc, source) => Execute(source.Item1, source.Item2, spc));
	}

	static void Execute(Compilation compilation, ImmutableArray<TypeDeclarationSyntax?> types, SourceProductionContext context)
	{
		// Get the assembly name from the compilation
		assemblyName = compilation.Assembly.Name.Trim();

		List<EndpointInfo> endpointClasses = [];

		// Get the endpoint interface symbols to check against
		INamedTypeSymbol endpointGroupSymbol = compilation.GetTypeByMetadataName(fullIEndpointGroup)!;
		INamedTypeSymbol endpointBaseSymbol = compilation.GetTypeByMetadataName(fullIEndpointBase)!;
		INamedTypeSymbol endpointWithRequestAndResponseSymbol = compilation.GetTypeByMetadataName(fullIEndpointWithRequestAndResponse)!;
		INamedTypeSymbol endpointWithoutRequestSymbol = compilation.GetTypeByMetadataName(fullIEndpointWithoutRequest)!;
		INamedTypeSymbol endpointWithoutResponseSymbol = compilation.GetTypeByMetadataName(fullIEndpointWithoutResponse)!;
		INamedTypeSymbol endpointWithoutRequestOrResponseSymbol = compilation.GetTypeByMetadataName(fullIEndpointWithoutRequestOrResponse)!;

		if (endpointBaseSymbol is null)
		{
			return;
		}

		foreach (TypeDeclarationSyntax? typeDeclaration in types)
		{
			if (typeDeclaration is null)
			{
				continue;
			}

			SemanticModel semanticModel = compilation.GetSemanticModel(typeDeclaration.SyntaxTree);
			INamedTypeSymbol? typeSymbol = semanticModel.GetDeclaredSymbol(typeDeclaration);

			if (typeSymbol is null || typeSymbol.IsAbstract)
			{
				continue;
			}

			// Check if the type implements IEndpointBase
			bool implementsEndpointBase = typeSymbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, endpointBaseSymbol));

			if (!implementsEndpointBase)
			{
				continue;
			}

			// Analyze the Configure method for WithName usage, HTTP verb, and route pattern
			(HttpVerb verb, string pattern)? verbAndPattern = typeDeclaration.GetVerbAndPattern(context);
			if (verbAndPattern is null)
			{
				continue;
			}

			string? withName = typeDeclaration.GetWithName();
			string? withTags = typeDeclaration.GetTags();
			(INamedTypeSymbol symbol, string pattern)? mapGroup = typeDeclaration.GetGroup(endpointGroupSymbol, compilation, context);

			// If withName is null and there's a mapGroup, try to inherit from the group
			if (withName is null && mapGroup is not null)
			{
				TypeDeclarationSyntax? groupTypeDeclaration = mapGroup.Value.symbol.ToTypeDeclarationSyntax(types, compilation);
				if (groupTypeDeclaration is not null)
				{
					withName = groupTypeDeclaration.GetWithName();
				}
			}

			// If withTags is null and there's a mapGroup, try to inherit from the group
			if (withTags is null && mapGroup is not null)
			{
				TypeDeclarationSyntax? groupTypeDeclaration = mapGroup.Value.symbol.ToTypeDeclarationSyntax(types, compilation);
				if (groupTypeDeclaration is not null)
				{
					withTags = groupTypeDeclaration.GetTags();
				}
			}

			// Determine which specific endpoint interface it implements
			foreach (INamedTypeSymbol interfaceType in typeSymbol.AllInterfaces)
			{
				// Check for IEndpoint<TRequest, TResponse>
				if (SymbolEqualityComparer.Default.Equals(interfaceType.OriginalDefinition, endpointWithRequestAndResponseSymbol) && interfaceType.TypeArguments.Length == 2)
				{
					endpointClasses.Add(new EndpointInfo(
						typeSymbol.ToDisplayString(),
						EndpointType.WithRequestAndResponse,
						mapGroup,
						verbAndPattern.Value.verb,
						verbAndPattern.Value.pattern,
						withName,
						withTags,
						interfaceType.TypeArguments[0],
						typeDeclaration.GetRequestTypeAndName(),
						typeDeclaration.Validate(compilation, interfaceType.TypeArguments[0]),
						interfaceType.TypeArguments[1]));
				}
				// Check for IEndpointWithoutRequest<TResponse>
				else if (SymbolEqualityComparer.Default.Equals(interfaceType.OriginalDefinition, endpointWithoutRequestSymbol) && interfaceType.TypeArguments.Length == 1)
				{
					endpointClasses.Add(new EndpointInfo(
						typeSymbol.ToDisplayString(),
						EndpointType.WithoutRequest,
						mapGroup,
						verbAndPattern.Value.verb,
						verbAndPattern.Value.pattern,
						withName,
						withTags,
						null,
						null,
						null,
						interfaceType.TypeArguments[0]));
				}
				// Check for IEndpointWithRequestWithoutResponse<TRequest>
				else if (SymbolEqualityComparer.Default.Equals(interfaceType.OriginalDefinition, endpointWithoutResponseSymbol) && interfaceType.TypeArguments.Length == 1)
				{
					endpointClasses.Add(new EndpointInfo(
						typeSymbol.ToDisplayString(),
						EndpointType.WithoutResponse,
						mapGroup,
						verbAndPattern.Value.verb,
						verbAndPattern.Value.pattern,
						withName,
						withTags,
						interfaceType.TypeArguments[0],
						typeDeclaration.GetRequestTypeAndName(),
						typeDeclaration.Validate(compilation, interfaceType.TypeArguments[0]),
						null));
				}
				// Check for IEndpointWithoutRequest (no generics)
				else if (SymbolEqualityComparer.Default.Equals(interfaceType, endpointWithoutRequestOrResponseSymbol))
				{
					endpointClasses.Add(new EndpointInfo(
						typeSymbol.ToDisplayString(),
						EndpointType.WithoutRequestOrResponse,
						mapGroup,
						verbAndPattern.Value.verb,
						verbAndPattern.Value.pattern,
						withName,
						withTags,
						null,
						null,
						null,
						null));
				}
			}
		}

		if (endpointClasses.Count == 0)
		{
			return;
		}

		string source = GenerateEndpointExtensions(endpointClasses);
		context.AddSource("EndpointExtensions.g.cs", SourceText.From(source, Encoding.UTF8));
	}

	static string GenerateEndpointExtensions(List<EndpointInfo> endpointClasses)
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
					if (endpoint.FluentValidationClass is not null)
					{
						builder.AppendLine($"builder.Services.AddSingleton<IValidator<global::{endpoint.RequestType}>, global::{endpoint.FluentValidationClass}>();");
					}
					builder.AppendLine($"builder.Services.AddScoped<global::{endpoint.ClassName}>();");
				}
				builder.AppendLine();
				builder.AppendLine("return builder;");
			}

			builder.AppendLine();

			// Generate MapEndpoints method with grouping
			builder.AppendLine($"public static WebApplication MapEndpointsFrom{sanitisedAssemblyName}(this WebApplication app)");
			using (builder.AppendBlock())
			{
				// Group endpoints by their Group property (both symbol and pattern)
				IOrderedEnumerable<IGrouping<(string? symbol, string? pattern), EndpointInfo>> groupedEndpoints = endpointClasses
					.GroupBy(x => (x.Group?.symbol?.ToDisplayString(), x.Group?.pattern))
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
