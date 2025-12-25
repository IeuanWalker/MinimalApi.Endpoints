using Microsoft.AspNetCore.Http;

namespace IeuanWalker.MinimalApi.Endpoints.Validation;

/// <summary>
/// Endpoint filter that validates requests using configured validation rules
/// </summary>
public sealed class ValidationEndpointFilter<TRequest> : IEndpointFilter
{
	readonly ValidationConfiguration<TRequest> _configuration;

	public ValidationEndpointFilter(ValidationConfiguration<TRequest> configuration)
	{
		_configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
	}

	public async ValueTask<object?> InvokeAsync(
		EndpointFilterInvocationContext context,
		EndpointFilterDelegate next)
	{
		// Extract request from context
		TRequest? request = default;
		for (int i = 0; i < context.Arguments.Count; i++)
		{
			if (context.Arguments[i] is TRequest targetObj)
			{
				request = targetObj;
				break;
			}
		}

		if (request == null)
		{
			return Results.Problem(
				title: "Request body required",
				statusCode: 400);
		}

		// Validate
		Dictionary<string, string[]> errors = [];

		// Property-level validation
		foreach (ValidationRule rule in _configuration.Rules)
		{
			object? propertyValue = GetPropertyValue(request, rule.PropertyName);
			if (!rule.IsValid(propertyValue))
			{
				if (!errors.ContainsKey(rule.PropertyName))
				{
					errors[rule.PropertyName] = [];
				}

				errors[rule.PropertyName] = [.. errors[rule.PropertyName], rule.ErrorMessage];
			}
		}

		// Cross-field validation
		foreach (Func<TRequest, Dictionary<string, string[]>> crossFieldValidator in _configuration.CrossFieldValidators)
		{
			Dictionary<string, string[]> crossFieldErrors = crossFieldValidator(request);
			foreach (KeyValuePair<string, string[]> kvp in crossFieldErrors)
			{
				if (!errors.ContainsKey(kvp.Key))
				{
					errors[kvp.Key] = [];
				}

				errors[kvp.Key] = [.. errors[kvp.Key], .. kvp.Value];
			}
		}

		// Return validation errors if any
		if (errors.Count > 0)
		{
			return Results.ValidationProblem(errors, title: "Validation failed");
		}

		// Continue to endpoint handler
		return await next(context);
	}

	static object? GetPropertyValue(TRequest request, string propertyName)
	{
		System.Reflection.PropertyInfo? property = typeof(TRequest).GetProperty(propertyName);
		return property?.GetValue(request);
	}
}
