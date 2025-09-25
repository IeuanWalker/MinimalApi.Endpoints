using System.Collections.Immutable;
using System.Text;
using IeuanWalker.MinimalApi.Endpoints.Generator.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace IeuanWalker.MinimalApi.Endpoints.Generator;

[Generator]
public class EndpointGenerator : IIncrementalGenerator
{
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
		INamedTypeSymbol endpointBase = compilation.GetTypeByMetadataName(fullIEndpointBase)!;
		INamedTypeSymbol endpointWithRequestAndResponse = compilation.GetTypeByMetadataName(fullIEndpointWithRequestAndResponse)!;
		INamedTypeSymbol endpointWithoutRequest = compilation.GetTypeByMetadataName(fullIEndpointWithoutRequest)!;
		INamedTypeSymbol endpointWithoutResponse = compilation.GetTypeByMetadataName(fullIEndpointWithoutResponse)!;
		INamedTypeSymbol endpointWithoutRequestOrResponse = compilation.GetTypeByMetadataName(fullIEndpointWithoutRequestOrResponse)!;

		if (endpointBase is null)
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
			bool implementsEndpointBase = typeSymbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, endpointBase));

			if (!implementsEndpointBase)
			{
				continue;
			}

			// Analyze the Configure method for WithName usage, HTTP verb, and route pattern
			(HttpVerb verb, string pattern)? verbAndPattern = typeDeclaration.GetVerbAndPattern();
			if (verbAndPattern is null)
			{
				continue;
			}

			string? withName = typeDeclaration.GetWithName();
			string? withTags = typeDeclaration.GetTags();

			// Determine which specific endpoint interface it implements
			foreach (INamedTypeSymbol interfaceType in typeSymbol.AllInterfaces)
			{
				// Check for IEndpoint<TRequest, TResponse>
				if (SymbolEqualityComparer.Default.Equals(interfaceType.OriginalDefinition, endpointWithRequestAndResponse) && interfaceType.TypeArguments.Length == 2)
				{
					endpointClasses.Add(new EndpointInfo(
						typeSymbol.ToDisplayString(),
						EndpointType.WithRequestAndResponse,
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
				else if (SymbolEqualityComparer.Default.Equals(interfaceType.OriginalDefinition, endpointWithoutRequest) && interfaceType.TypeArguments.Length == 1)
				{
					endpointClasses.Add(new EndpointInfo(
						typeSymbol.ToDisplayString(),
						EndpointType.WithoutRequest,
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
				else if (SymbolEqualityComparer.Default.Equals(interfaceType.OriginalDefinition, endpointWithoutResponse) && interfaceType.TypeArguments.Length == 1)
				{
					endpointClasses.Add(new EndpointInfo(
						typeSymbol.ToDisplayString(),
						EndpointType.WithoutResponse,
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
				else if (SymbolEqualityComparer.Default.Equals(interfaceType, endpointWithoutRequestOrResponse))
				{
					endpointClasses.Add(new EndpointInfo(
						typeSymbol.ToDisplayString(),
						EndpointType.WithoutRequestOrResponse,
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
			builder.AppendLine("using IeuanWalker.MinimalApi.Endpoints;");
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
				builder.AppendLine("builder.Services.AddProblemDetails();");
				foreach (EndpointInfo endpoint in endpointClasses)
				{
					if (endpoint.FluentValidationClass is not null)
					{
						builder.AppendLine($"builder.Services.AddScoped<IValidator<global::{endpoint.RequestType}>, global::{endpoint.FluentValidationClass}>();");
					}
					builder.AppendLine($"builder.Services.AddScoped<global::{endpoint.ClassName}>();");
				}
				builder.AppendLine();
				builder.AppendLine("return builder;");
			}

			builder.AppendLine();

			// Generate MapEndpoints method
			builder.AppendLine($"public static WebApplication MapEndpointsFrom{sanitisedAssemblyName}(this WebApplication app)");
			using (builder.AppendBlock())
			{
				for (int i = 0; i < endpointClasses.Count; i++)
				{
					EndpointInfo? endpoint = endpointClasses[i];

					builder.ToEndpoint(endpoint, i);

					builder.AppendLine();
				}

				builder.AppendLine("return app;");
			}
		}

		return builder.ToString();
	}
}
