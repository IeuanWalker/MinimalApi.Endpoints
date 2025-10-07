using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Verify.SourceGenerators;

namespace IeuanWalker.MinimalApi.Endpoints.Generator.Tests;

public static class TestHelper
{
	public static Task Verify(string source, [CallerFilePath] string sourceFile = "")
	{
		// Parse the provided source code
		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

		// Create references for all the assemblies we need
		IEnumerable<PortableExecutableReference> references = GetReferences();

		// Create a compilation with the source code
		CSharpCompilation compilation = CSharpCompilation.Create(
			assemblyName: "Tests",
			syntaxTrees: [syntaxTree],
			references: references);

		// Create an instance of the source generator
		EndpointGenerator generator = new();

		// Use the Roslyn driver to run the generator
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

		// Run the generator
		driver = driver.RunGenerators(compilation);

		// Use Verify to snapshot test the results
		return Verifier.Verify(driver, sourceFile: sourceFile)
			.UseDirectory("Snapshots");
	}

	static IEnumerable<PortableExecutableReference> GetReferences()
	{
		// Get references to core .NET assemblies
		Assembly[] assemblies =
		[
			typeof(object).Assembly,                          // System.Private.CoreLib
			typeof(Console).Assembly,                         // System.Console
			typeof(Enumerable).Assembly,                      // System.Linq
			typeof(ImmutableArray).Assembly,                  // System.Collections.Immutable
			Assembly.Load("System.Runtime"),
			Assembly.Load("System.Collections"),
			Assembly.Load("netstandard"),
		];

		List<PortableExecutableReference> references = assemblies
			.Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
			.ToList();

		// Add ASP.NET Core references
		try
		{
			references.Add(MetadataReference.CreateFromFile(Assembly.Load("Microsoft.AspNetCore.Http").Location));
			references.Add(MetadataReference.CreateFromFile(Assembly.Load("Microsoft.AspNetCore.Http.Abstractions").Location));
			references.Add(MetadataReference.CreateFromFile(Assembly.Load("Microsoft.AspNetCore.Routing").Location));
			references.Add(MetadataReference.CreateFromFile(Assembly.Load("Microsoft.AspNetCore.Http.Results").Location));
		}
		catch
		{
			// If we can't load ASP.NET Core assemblies, that's okay for basic tests
		}

		// Add reference to the library project containing the interfaces
		try
		{
			Assembly endpointsAssembly = Assembly.Load("IeuanWalker.MinimalApi.Endpoints");
			references.Add(MetadataReference.CreateFromFile(endpointsAssembly.Location));
		}
		catch
		{
			// Try alternative loading method
			string? assemblyPath = Assembly.GetExecutingAssembly().Location;
			if (!string.IsNullOrEmpty(assemblyPath))
			{
				string? directory = Path.GetDirectoryName(assemblyPath);
				if (!string.IsNullOrEmpty(directory))
				{
					string endpointsPath = Path.Combine(directory, "IeuanWalker.MinimalApi.Endpoints.dll");
					if (File.Exists(endpointsPath))
					{
						references.Add(MetadataReference.CreateFromFile(endpointsPath));
					}
				}
			}
		}

		// Add FluentValidation reference
		try
		{
			Assembly fluentValidationAssembly = Assembly.Load("FluentValidation");
			references.Add(MetadataReference.CreateFromFile(fluentValidationAssembly.Location));
		}
		catch
		{
			// FluentValidation is optional
		}

		return references;
	}
}
