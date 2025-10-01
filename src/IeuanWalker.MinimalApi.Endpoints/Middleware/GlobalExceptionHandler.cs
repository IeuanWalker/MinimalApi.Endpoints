using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace IeuanWalker.MinimalApi.Endpoints;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class GlobalExceptionHandlerExtensions
{
	public static IApplicationBuilder UseDefaultExceptionHandler(this IApplicationBuilder app)
	{
		app.UseExceptionHandler(exceptionHandlerApp =>
		{
			exceptionHandlerApp.Run(async httpContext =>
			{
				IExceptionHandlerFeature? exHandlerFeature = httpContext.Features.Get<IExceptionHandlerFeature>();
				IProblemDetailsService? problemDetailsService = httpContext.RequestServices.GetService<IProblemDetailsService>();
				IHostEnvironment? hostEnvironment = httpContext.RequestServices.GetService<IHostEnvironment>();

				if (exHandlerFeature is not null && problemDetailsService is not null)
				{
					ProblemDetails problemDetails = new()
					{
						Status = StatusCodes.Status500InternalServerError,
						Title = "Internal Server Error!",
						Type = hostEnvironment?.IsProduction() ?? true ? string.Empty : exHandlerFeature.Error.GetType().Name,
						Detail = hostEnvironment?.IsProduction() ?? true ? "An unexpected error has occurred." : exHandlerFeature.Error.Message
					};

					await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
					{
						Exception = exHandlerFeature.Error,
						HttpContext = httpContext,
						ProblemDetails = problemDetails
					});
				}
			});
		});

		return app;
	}
}
