using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace IeuanWalker.MinimalApi.Endpoints.Generator.Tests;

public class DiagnosticTests
{
	[Fact]
	public void ReportsDiagnostic_WhenNoHttpVerbConfigured()
	{
		// Arrange
		string source = """
			using IeuanWalker.MinimalApi.Endpoints;
			using Microsoft.AspNetCore.Builder;

			namespace TestApp.Endpoints;

			public class NoVerbEndpoint : IEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					// No HTTP verb configured
				}

				public Task Handle(CancellationToken ct)
				{
					return Task.CompletedTask;
				}
			}
			""";

		// Act
		GeneratorDriverRunResult result = RunGenerator(source);

		// Assert
		result.Diagnostics.Should().Contain(d => d.Id == "MINAPI001");
	}

	[Fact]
	public void ReportsDiagnostic_WhenMultipleHttpVerbsConfigured()
	{
		// Arrange
		string source = """
			using IeuanWalker.MinimalApi.Endpoints;
			using Microsoft.AspNetCore.Builder;

			namespace TestApp.Endpoints;

			public class MultipleVerbsEndpoint : IEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Get("/test").Post("/test");
				}

				public Task Handle(CancellationToken ct)
				{
					return Task.CompletedTask;
				}
			}
			""";

		// Act
		GeneratorDriverRunResult result = RunGenerator(source);

		// Assert
		result.Diagnostics.Should().Contain(d => d.Id == "MINAPI002");
	}

	[Fact]
	public void ReportsDiagnostic_WhenMultipleGroupsConfigured()
	{
		// Arrange
		string source = """
			using IeuanWalker.MinimalApi.Endpoints;
			using Microsoft.AspNetCore.Builder;

			namespace TestApp.Endpoints;

			public class Group1 : IEndpointGroup
			{
				public static RouteGroupBuilder Configure(WebApplication app)
				{
					return app.MapGroup("/group1");
				}
			}

			public class Group2 : IEndpointGroup
			{
				public static RouteGroupBuilder Configure(WebApplication app)
				{
					return app.MapGroup("/group2");
				}
			}

			public class MultipleGroupsEndpoint : IEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Group<Group1>().Group<Group2>().Get("/test");
				}

				public Task Handle(CancellationToken ct)
				{
					return Task.CompletedTask;
				}
			}
			""";

		// Act
		GeneratorDriverRunResult result = RunGenerator(source);

		// Assert
		result.Diagnostics.Should().Contain(d => d.Id == "MINAPI005");
	}

	[Fact]
	public void ReportsWarning_WhenEndpointGroupIsUnused()
	{
		// Arrange
		string source = """
			using IeuanWalker.MinimalApi.Endpoints;
			using Microsoft.AspNetCore.Builder;

			namespace TestApp.Endpoints;

			public class UnusedGroup : IEndpointGroup
			{
				public static RouteGroupBuilder Configure(WebApplication app)
				{
					return app.MapGroup("/unused");
				}
			}

			public class TestEndpoint : IEndpoint
			{
				public static void Configure(RouteHandlerBuilder builder)
				{
					builder.Get("/test");
				}

				public Task Handle(CancellationToken ct)
				{
					return Task.CompletedTask;
				}
			}
			""";

		// Act
		GeneratorDriverRunResult result = RunGenerator(source);

		// Assert
		result.Diagnostics.Should().Contain(d => d.Id == "MINAPI006" && d.Severity == DiagnosticSeverity.Warning);
	}

	private static GeneratorDriverRunResult RunGenerator(string source)
	{
		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

		IEnumerable<MetadataReference> references = [
			MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(IEndpoint).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(Microsoft.AspNetCore.Builder.EndpointRouteBuilderExtensions).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(Microsoft.AspNetCore.Http.HttpContext).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(FluentValidation.IValidator<>).Assembly.Location),
			MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "netstandard.dll")),
			MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "System.Runtime.dll"))
		];

		CSharpCompilation compilation = CSharpCompilation.Create(
			assemblyName: "TestApp",
			syntaxTrees: [syntaxTree],
			references: references,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		EndpointGenerator generator = new();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
		driver = driver.RunGenerators(compilation);

		return driver.GetRunResult();
	}
}
