using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;

namespace ExampleApi.Infrastructure;

static class ScalarConfiguration
{
	internal static IHostApplicationBuilder AddScalar(this IHostApplicationBuilder builder)
	{
		builder.Services.AddEndpointsApiExplorer();
		builder.Services.AddOpenApi();

		List<ApiVersion> versions =
		[
			new ApiVersion(1),
			new ApiVersion(2)
		];

		foreach(int majorVersion in versions.Where(x => x.MajorVersion is not null).Select(versions => versions.MinorVersion!.Value))
		{
			builder.Services.Configure<ScalarOptions>(options => options.AddDocument($"v{majorVersion}", $"v{majorVersion}"));
			builder.Services.AddOpenApi($"v{majorVersion}", options =>
			{
				options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0;
				options.AddDocumentTransformer((document, context, _) =>
				{
					IApiVersionDescriptionProvider provider = context.ApplicationServices.GetRequiredService<IApiVersionDescriptionProvider>();
					ApiVersionDescription? description = provider.ApiVersionDescriptions.FirstOrDefault(d => d.GroupName == context.DocumentName);

					document.Info = new OpenApiInfo
					{
						Title = "Test API",
						Version = description?.ApiVersion.ToString() ?? context.DocumentName,
						Description = description?.IsDeprecated == true ? "This API version is deprecated." : null
					};

					return Task.CompletedTask;
				});
			});
		}

		return builder;
	}


	internal static IApplicationBuilder UseScalar(this WebApplication app)
	{
		app.MapOpenApi();
		app.MapScalarApiReference((options, _) =>
		{
			options
				.WithTheme(ScalarTheme.Default)
				.WithLayout(ScalarLayout.Modern)
				.WithFavicon("https://scalar.com/logo-light.svg")
				.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
		});

		return app;
	}
}