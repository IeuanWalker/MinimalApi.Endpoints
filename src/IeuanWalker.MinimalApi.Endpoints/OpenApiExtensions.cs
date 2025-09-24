using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace IeuanWalker.MinimalApi.Endpoints;

public static class OpenApiExtensions
{
	public static RouteHandlerBuilder WithResponse(this RouteHandlerBuilder builder, int statusCode, string description, string? contentType = null)
	{
		builder.WithResponse(null, statusCode, description, contentType);

		return builder;
	}

	public static RouteHandlerBuilder WithResponse<T>(this RouteHandlerBuilder builder, int statusCode, string description, string? contentType = null)
	{
		builder.WithResponse(typeof(T), statusCode, description, contentType);

		return builder;
	}

	static RouteHandlerBuilder WithResponse(this RouteHandlerBuilder builder, Type? type, int statusCode, string description, string? contentType = null)
	{
		if (contentType is null)
		{
			builder.WithMetadata(new ProducesResponseTypeMetadata(statusCode, type ?? typeof(void)));
		}
		else
		{
			builder.WithMetadata(new ProducesResponseTypeMetadata(statusCode, type ?? typeof(void), [contentType]));
		}

		builder.AddOpenApiOperationTransformer((operation, _, _) =>
		{
			operation.Responses?[statusCode.ToString()].Description = description;

			return Task.CompletedTask;
		});

		return builder;
	}
}
