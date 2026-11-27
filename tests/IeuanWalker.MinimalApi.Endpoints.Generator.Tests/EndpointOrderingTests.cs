using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Text.RegularExpressions;

namespace IeuanWalker.MinimalApi.Endpoints.Generator.Tests;

public class EndpointOrderingTests
{
	[Fact]
	public void GeneratesIdenticalSourceWhenSyntaxTreeOrderIsReversed()
	{
		SyntaxTree groups = CSharpSyntaxTree.ParseText("""
			using IeuanWalker.MinimalApi.Endpoints;
			using Microsoft.AspNetCore.Builder;
			namespace TestNamespace;

			public class ItemGroup : IEndpointGroup
			{
				public static RouteGroupBuilder Configure(WebApplication app) => app.MapGroup("/items");
			}

			public class ItemMaterialGroup : IEndpointGroup
			{
				public static RouteGroupBuilder Configure(WebApplication app) => app.MapGroup("/materials");
			}
			""", cancellationToken: TestContext.Current.CancellationToken);
		SyntaxTree itemV1 = CreateEndpoint("GetItemV1", "ItemGroup");
		SyntaxTree itemV2 = CreateEndpoint("GetItemV2", "ItemGroup");
		SyntaxTree material = CreateEndpoint("GetMaterial", "ItemMaterialGroup");

		string forward = GenerateSource(groups, itemV1, itemV2, material);
		string reversed = GenerateSource(material, itemV2, itemV1, groups);

		forward.ShouldContain("GetItemV1.Configure");
		forward.ShouldContain("GetItemV2.Configure");
		forward.ShouldContain("GetMaterial.Configure");
		reversed.ShouldBe(forward);

		string[] originalNames = GetOperationNames(forward);
		originalNames.Length.ShouldBe(3);
		originalNames.Distinct(StringComparer.Ordinal).Count().ShouldBe(3);

		SyntaxTree itemV0 = CreateEndpoint("GetItemV0", "ItemGroup");
		string[] expandedNames = GetOperationNames(GenerateSource(groups, itemV0, itemV1, itemV2, material));
		expandedNames.Length.ShouldBe(4);
		expandedNames.Skip(1).ShouldBe(originalNames);
	}

	static string[] GetOperationNames(string source) => Regex.Matches(source, "\\.WithName\\(\"([^\"]+)\"\\)")
		.Select(match => match.Groups[1].Value).ToArray();

	static string GenerateSource(params SyntaxTree[] trees) => TestHelper.RunGenerator(trees)
		.GetRunResult().GeneratedTrees.Single().GetText().ToString();

	static SyntaxTree CreateEndpoint(string typeName, string group) => CSharpSyntaxTree.ParseText($$"""
		using System.Threading;
		using System.Threading.Tasks;
		using IeuanWalker.MinimalApi.Endpoints;
		using Microsoft.AspNetCore.Builder;
		namespace TestNamespace;

		public class {{typeName}} : IEndpoint<string, string>
		{
			public static void Configure(RouteHandlerBuilder builder) => builder.Group<{{group}}>().Get("/lookup");
			public Task<string> Handle(string request, CancellationToken ct) => Task.FromResult(request);
		}
		""");
}
