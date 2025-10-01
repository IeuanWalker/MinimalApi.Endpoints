using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace IeuanWalker.MinimalApi.Endpoints;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public class GlobalExceptionHandler : IExceptionHandler
{
	readonly IProblemDetailsService _problemDetailsService;
	readonly IHostEnvironment _hostEnvironment;

	public GlobalExceptionHandler(IProblemDetailsService problemDetailsService, IHostEnvironment hostEnvironment)
	{
		_problemDetailsService = problemDetailsService;
		_hostEnvironment = hostEnvironment;
	}

	public async ValueTask<bool> TryHandleAsync(
		HttpContext httpContext,
		Exception exception,
		CancellationToken cancellationToken)
	{
		ProblemDetails problemDetails = new()
		{
			Status = StatusCodes.Status500InternalServerError,
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
