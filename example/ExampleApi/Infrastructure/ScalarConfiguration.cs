using System.Diagnostics.CodeAnalysis;
using Asp.Versioning.ApiExplorer;
using Scalar.AspNetCore;

namespace ExampleApi.Infrastructure;

[ExcludeFromCodeCoverage]
static class ScalarConfiguration
{
	internal static IApplicationBuilder UseScalar(this WebApplication app)
	{
		IReadOnlyList<ApiVersionDescription> descriptions = app.DescribeApiVersions();

		app.MapOpenApi().WithDocumentPerVersion();
		app.MapScalarApiReference((options, _) =>
		{
			for (int i = 0; i < descriptions.Count; i++)
			{
				ApiVersionDescription description = descriptions[i];
				bool isDefault = i == descriptions.Count - 1;

				options.AddDocument(description.GroupName, description.GroupName, isDefault: isDefault);
			}

			options
				.WithTheme(ScalarTheme.Default)
				.WithFavicon("https://scalar.com/logo-light.svg")
				.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
		});

		return app;
	}
}
