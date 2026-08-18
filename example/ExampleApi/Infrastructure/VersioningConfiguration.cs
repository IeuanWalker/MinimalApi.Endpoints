using System.Diagnostics.CodeAnalysis;
using Asp.Versioning;
using Asp.Versioning.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;

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
			.AddOpenApi();

		ServiceDescriptor? versioningOpenApiPostConfigure = builder.Services.FirstOrDefault(descriptor =>
			descriptor.ServiceType == typeof(IPostConfigureOptions<OpenApiOptions>) &&
			descriptor.ImplementationType?.FullName == "Asp.Versioning.OpenApi.Configuration.ConfigureOpenApiOptions");

		if (versioningOpenApiPostConfigure is not null)
		{
			builder.Services.Remove(versioningOpenApiPostConfigure);
		}

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
