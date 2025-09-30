using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;

namespace IeuanWalker.MinimalApi.Endpoints.Filters;

public class FluentValidationFilter<T> : IEndpointFilter where T : class
{
	readonly IValidator<T> _validator;

	public FluentValidationFilter(IValidator<T> validator)
	{
		_validator = validator ?? throw new ArgumentNullException(nameof(validator));
	}

	public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
	{
		T? obj = null;
		for (int i = 0; i < context.Arguments.Count; i++)
		{
			if (context.Arguments[i] is T targetObj)
			{
				obj = targetObj;
				break;
			}
		}

		if (obj is not null)
		{
			ValidationResult validationResult = await _validator.ValidateAsync(obj, context.HttpContext.RequestAborted);

			if (!validationResult.IsValid)
			{
				return Results.ValidationProblem(validationResult.ToDictionary());
			}
		}

		return await next.Invoke(context);
	}
}
