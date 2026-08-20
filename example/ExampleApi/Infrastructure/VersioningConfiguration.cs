using System.Diagnostics.CodeAnalysis;
using Asp.Versioning;
using Asp.Versioning.Builder;
using IeuanWalker.MinimalApi.Endpoints;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ExampleApi.Infrastructure;

[ExcludeFromCodeCoverage]
public static class VersioningConfiguration
{
	public static IHostApplicationBuilder AddApiVersioning(this IHostApplicationBuilder builder)
	{
		builder.Services
			.AddApiVersioning(options =>
			{
				options.ReportApiVersions = true;
				options.AssumeDefaultVersionWhenUnspecified = true;
			})
			.AddApiExplorer(config =>
			{
				config.GroupNameFormat = "'v'VVV";
				config.SubstituteApiVersionInUrl = true;
			})
			.AddOpenApi(options =>
			{
				options.Document.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
				options.Document.CreateSchemaReferenceId = jsonTypeInfo =>
					OpenApiOptions.CreateDefaultSchemaReferenceId(jsonTypeInfo) is null
						? null
						: jsonTypeInfo.Type.FullName?.Replace('+', '.');
				options.Document.AddDocumentTransformer((document, _, _) =>
				{
					document.Info = new OpenApiInfo
					{
						Title = "Test API",
						Version = options.Description.ApiVersion.ToString(),
						Description = "Example API demonstrating MinimalApi.Endpoints."
					};

					return Task.CompletedTask;
				});
				options.Document.EnhancePropertiesAndValidation();
			});

		return builder;
	}

	public static ApiVersionSet ApiVersionSet { get; set; } = null!;

	public static WebApplication UseApiVersioning(this WebApplication app)
	{
		ApiVersionSet = app.NewApiVersionSet()
			.HasApiVersion(new ApiVersion(1))
			.HasApiVersion(new ApiVersion(2))
			.ReportApiVersions()
			.Build();

		return app;
	}

	public static RouteHandlerBuilder Version(this RouteHandlerBuilder builder, double version)
	{
		return builder
			.WithApiVersionSet(ApiVersionSet)
			.MapToApiVersion(version);
	}
}
