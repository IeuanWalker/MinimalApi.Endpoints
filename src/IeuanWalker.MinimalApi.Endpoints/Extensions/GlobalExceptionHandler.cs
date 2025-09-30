using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace IeuanWalker.MinimalApi.Endpoints;
#pragma warning restore IDE0130 // Namespace does not match folder structure


[SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty", Justification = "<Pending>")]
class ExceptionHandler { }

public static class ExceptionHandlerExtensions
{
	/// <summary>
	/// registers the default global exception handler which will log the exceptions on the server and return a user-friendly json response to the client
	/// when unhandled exceptions occur.
	/// TIP: when using this exception handler, you may want to turn off the asp.net core exception middleware logging to avoid duplication like so:
	/// <code>
	/// "Logging": { "LogLevel": { "Default": "Warning", "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware": "None" } }
	/// </code>
	/// </summary>
	/// <param name="logStructuredException">set to true if you'd like to log the error in a structured manner</param>
	/// <param name="useGenericReason">set to true if you don't want to expose the actual exception reason in the json response sent to the client</param>
	public static IApplicationBuilder UseDefaultExceptionHandler(this IApplicationBuilder app,
																 bool logStructuredException = true,
																 bool? useGenericReason = null)
	{
		app.UseExceptionHandler(
			errApp =>
			{
				errApp.Run(
					async httpContext =>
					{
						IExceptionHandlerFeature? exHandlerFeature = httpContext.Features.Get<IExceptionHandlerFeature>();

						if (exHandlerFeature is not null)
						{
							ILogger? logger = httpContext.RequestServices.GetService<ILogger<ExceptionHandler>>();

							string? route = exHandlerFeature.Endpoint?.DisplayName?.Split(" => ")[0];
							string exceptionType = exHandlerFeature.Error.GetType().Name;
							string reason = exHandlerFeature.Error.Message;

							if (logStructuredException)
							{
								logger?.LogStructuredException(exHandlerFeature.Error, exceptionType, route, reason);
							}
							else
							{
								//this branch is only meant for unstructured textual logging
								logger?.LogUnStructuredException(exceptionType, route, reason, exHandlerFeature.Error.StackTrace);
							}

							if (useGenericReason is null)
							{
								IHostEnvironment? hostEnvironment = httpContext.RequestServices.GetService<IHostEnvironment>();

								useGenericReason = hostEnvironment?.IsProduction();
							}

							ProblemDetails problemDetails = new()
							{
								Status = StatusCodes.Status500InternalServerError,
								Title = "Internal Server Error!",
								Detail = useGenericReason ?? true ? "An unexpected error has occurred." : exHandlerFeature.Error.Message
							};

							httpContext.Response.StatusCode = problemDetails.Status.Value;

							await httpContext.Response.WriteAsJsonAsync(problemDetails, httpContext.RequestAborted);
						}
					});
			});

		return app;
	}
}

static partial class LoggingExtensions
{
	[LoggerMessage(3, LogLevel.Error, "[{@exceptionType}] at [{@route}] due to [{@reason}]")]
	public static partial void LogStructuredException(this ILogger l, Exception ex, string? exceptionType, string? route, string? reason);

	[LoggerMessage(4, LogLevel.Error, """
                                     =================================
                                     {route}
                                     TYPE: {exceptionType}
                                     REASON: {reason}
                                     ---------------------------------
                                     {stackTrace}
                                     """)]
	public static partial void LogUnStructuredException(this ILogger l, string? exceptionType, string? route, string? reason, string? stackTrace);
}
