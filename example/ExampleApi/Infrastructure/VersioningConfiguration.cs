using System.Diagnostics.CodeAnalysis;
using Asp.Versioning;
using Asp.Versioning.Builder;
using ExampleApi.Infrastructure;

namespace ExampleApi.Infrastructure;

[ExcludeFromCodeCoverage]
public static class VersioningConfiguration
{
	public static IHostApplicationBuilder AddApiVersioning(this IHostApplicationBuilder builder)
	{
		builder.Services
			.AddApiVersioning(options =>
			{
				options.DefaultApiVersion = new ApiVersion(1, 0);
				options.ReportApiVersions = true;
				options.AssumeDefaultVersionWhenUnspecified = true;
			})
			.AddApiExplorer(config =>
			{
				config.GroupNameFormat = "'v'VVV";
				config.SubstituteApiVersionInUrl = true;
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
