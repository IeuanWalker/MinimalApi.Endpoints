using System.Diagnostics.CodeAnalysis;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using IeuanWalker.MinimalApi.Endpoints;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace ExampleApi.Infrastructure;

[ExcludeFromCodeCoverage]
static class ScalarConfiguration
{
	internal static IHostApplicationBuilder AddScalar(this IHostApplicationBuilder builder)
	{
		builder.Services.AddOpenApi(config =>
		{
			config.CreateSchemaReferenceId = jsonTypeInfo => jsonTypeInfo.Type.FullName?.Replace('+', '.');
			config.EnhancePropertiesAndValidation();
		});

		List<ApiVersion> versions =
		[
			new ApiVersion(1),
			new ApiVersion(2)
		];

		foreach (int majorVersion in versions.Where(version => version.MajorVersion is not null).Select(version => version.MajorVersion!.Value))
		{
			builder.Services.AddOpenApi($"v{majorVersion}", options =>
			{
				options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0;
				options.CreateSchemaReferenceId = jsonTypeInfo => jsonTypeInfo.Type.FullName?.Replace('+', '.');
				options.EnhancePropertiesAndValidation();
				options.AddDocumentTransformer((document, _, _) =>
				{
					document.Info = new OpenApiInfo
					{
						Title = "Test API",
						Version = majorVersion.ToString(),
						Description = "Example API demonstrating MinimalApi.Endpoints."
					};

					return Task.CompletedTask;
				});
			});
		}

		return builder;
	}

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
