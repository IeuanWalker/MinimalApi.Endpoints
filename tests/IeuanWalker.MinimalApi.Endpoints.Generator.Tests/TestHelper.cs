using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

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
			assemblyName: "TestAssembly",
			syntaxTrees: [syntaxTree],
			references: references,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

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

	static List<PortableExecutableReference> GetReferences()
	{
		// Get references to core .NET assemblies
		List<PortableExecutableReference> references = [];

		// Add core .NET references
		references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
		references.Add(MetadataReference.CreateFromFile(typeof(Console).Assembly.Location));
		references.Add(MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location));
		references.Add(MetadataReference.CreateFromFile(typeof(ImmutableArray).Assembly.Location));

		// Add System references
		string[] systemAssemblies = [
			"System.Runtime",
			"System.Collections",
			"System.Threading.Tasks",
			"System.ComponentModel.Annotations",
			"netstandard"
		];

		foreach (string assemblyName in systemAssemblies)
		{
			references.Add(MetadataReference.CreateFromFile(Assembly.Load(assemblyName).Location));
		}

		// Add ASP.NET Core references
		string[] aspNetAssemblies = [
			"Microsoft.AspNetCore.Http",
			"Microsoft.AspNetCore.Http.Abstractions",
			"Microsoft.AspNetCore.Routing",
			"Microsoft.AspNetCore.Http.Results",
			"Microsoft.AspNetCore.Authorization",
			"Microsoft.Extensions.DependencyInjection.Abstractions"
		];

		foreach (string assemblyName in aspNetAssemblies)
		{
			references.Add(MetadataReference.CreateFromFile(Assembly.Load(assemblyName).Location));
		}

		// Add reference to the library project containing the interfaces
		try
		{
			Assembly endpointsAssembly = Assembly.Load("IeuanWalker.MinimalApi.Endpoints");
			references.Add(MetadataReference.CreateFromFile(endpointsAssembly.Location));
		}
#pragma warning disable CA1031 // Do not catch general exception types
		catch
		{
			// Try alternative loading method via file system
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
#pragma warning restore CA1031 // Do not catch general exception types

		// Add FluentValidation reference
		Assembly fluentValidationAssembly = Assembly.Load("FluentValidation");
		references.Add(MetadataReference.CreateFromFile(fluentValidationAssembly.Location));

		return references;
	}
}
