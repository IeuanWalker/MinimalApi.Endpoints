using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;

namespace ExampleApi.Infrastructure;

internal static class ScalarConfiguration
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

        foreach(ApiVersion version in versions)
        {
            builder.Services.Configure<ScalarOptions>(options => options.AddDocument($"v{version.MajorVersion}", $"v{version.MajorVersion}"));
            builder.Services.AddOpenApi($"v{version.MajorVersion}", options =>
            {
                options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0;
                options.AddDocumentTransformer((document, context, ct) =>
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
        app.MapScalarApiReference((options, context) =>
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