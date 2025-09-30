using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace IeuanWalker.MinimalApi.Endpoints;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public class GlobalExceptionHandler : IExceptionHandler
{
	readonly IProblemDetailsService _problemDetailsService;
	readonly IHostEnvironment _hostEnvironment;
	readonly ILogger<GlobalExceptionHandler> _logger;

	public GlobalExceptionHandler(
		IProblemDetailsService problemDetailsService,
		IHostEnvironment hostEnvironment,
		ILogger<GlobalExceptionHandler> logger)
	{
		_problemDetailsService = problemDetailsService;
		_hostEnvironment = hostEnvironment;
		_logger = logger;
	}

	public async ValueTask<bool> TryHandleAsync(
		HttpContext httpContext,
		Exception exception,
		CancellationToken cancellationToken)
	{
		string? route = httpContext.GetEndpoint()?.DisplayName?.Split(" => ")[0];
		string exceptionType = exception.GetType().Name;
		string reason = exception.Message;

		_logger.LogStructuredException(exception, exceptionType, route, reason);

		ProblemDetails problemDetails = new()
		{
			Status = exception switch
			{
				ArgumentException => StatusCodes.Status400BadRequest,
				_ => StatusCodes.Status500InternalServerError
			},
			Title = "Internal Server Error!",
			Type = _hostEnvironment.IsProduction() ? string.Empty : exception.GetType().Name,
			Detail = _hostEnvironment.IsProduction() ? "An unexpected error has occurred." : exception.Message
		};

		return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
		{
			Exception = exception,
			HttpContext = httpContext,
			ProblemDetails = problemDetails
		});
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
