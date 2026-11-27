using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace IeuanWalker.MinimalApi.Endpoints.Generator.Tests;

public class GroupedEndpointTagTests
{
	[Theory]
	[InlineData("WasteManagementBulky/api/Booking", "", "")]
	[InlineData("WasteManagementBulky/api/Booking", "/", "/")]
	[InlineData("WasteManagementBulky/api/Booking/", "", "")]
	[InlineData("WasteManagementBulky/api/Booking/", "/", "/")]
	public void GeneratesSameTagForBookingGroupEndpoints(string groupPattern, string childPrefix, string rootPattern)
	{
		SyntaxTree group = CSharpSyntaxTree.ParseText($$"""
			using IeuanWalker.MinimalApi.Endpoints;
			using Microsoft.AspNetCore.Builder;
			namespace TestNamespace;

			public class BookingGroup : IEndpointGroup
			{
				public static RouteGroupBuilder Configure(WebApplication app) => app.MapGroup("{{groupPattern}}");
			}
			""", cancellationToken: TestContext.Current.CancellationToken);

		string[] children = ["GetDetails", "GetBookingCountByDate", "ChangeDate", "ChangeItems"];
		SyntaxTree[] endpoints = [
			.. children.Select(child => CreateEndpoint(child + "Endpoint", childPrefix + child)),
			CreateEndpoint("RootBookingEndpoint", rootPattern)
		];

		string source = TestHelper.RunGenerator([group, .. endpoints])
			.GetRunResult().GeneratedTrees.Single().GetText(TestContext.Current.CancellationToken).ToString();

		string[] tags = Regex.Matches(source, """\.WithTags\("([^"]+)"\)""")
			.Select(match => match.Groups[1].Value).ToArray();
		tags.ShouldBe(new[] { "Booking", "Booking", "Booking", "Booking", "Booking" });

		foreach (string child in children)
		{
			source.ShouldContain($"// GET: WasteManagementBulky/api/Booking/{child}");
			source.ShouldContain($".MapGet(\"{childPrefix}{child}\",");
		}
		source.ShouldContain($".MapGet(\"{rootPattern}\",");
	}

	static SyntaxTree CreateEndpoint(string typeName, string pattern) => CSharpSyntaxTree.ParseText($$"""
		using System.Threading;
		using System.Threading.Tasks;
		using IeuanWalker.MinimalApi.Endpoints;
		using Microsoft.AspNetCore.Builder;
		namespace TestNamespace;

		public class {{typeName}} : IEndpoint<string, string>
		{
			public static void Configure(RouteHandlerBuilder builder) => builder.Group<BookingGroup>().Get("{{pattern}}");
			public Task<string> Handle(string request, CancellationToken ct) => Task.FromResult(request);
		}
		""", cancellationToken: TestContext.Current.CancellationToken);
}
