using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace IeuanWalker.MinimalApi.Endpoints;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class OpenApiExtensions
{
	[ExcludeFromCodeCoverage]
	public static RouteHandlerBuilder WithResponse(this RouteHandlerBuilder builder, int statusCode, string description, string? contentType = null, params string[] additionalContentTypes)
	{
		builder.WithResponse(null, statusCode, description, contentType, additionalContentTypes);

		return builder;
	}

	[ExcludeFromCodeCoverage]
	public static RouteHandlerBuilder WithResponse<T>(this RouteHandlerBuilder builder, int statusCode, string description, string? contentType = null, params string[] additionalContentTypes)
	{
		builder.WithResponse(typeof(T), statusCode, description, contentType, additionalContentTypes);

		return builder;
	}

	[ExcludeFromCodeCoverage]
	static RouteHandlerBuilder WithResponse(this RouteHandlerBuilder builder, Type? responseType, int statusCode, string description, string? contentType = null, params string[] additionalContentTypes)
	{
		if (responseType is not null && string.IsNullOrEmpty(contentType))
		{
			contentType = "application/json";
		}


		if (contentType is null)
		{
			builder.WithMetadata(new ProducesResponseTypeMetadata(statusCode, responseType ?? typeof(void)));
		}
		else
		{
			string[] contentTypes = new string[additionalContentTypes.Length + 1];
			contentTypes[0] = contentType;
			additionalContentTypes.CopyTo(contentTypes, 1);

			builder.WithMetadata(new ProducesResponseTypeMetadata(statusCode, responseType ?? typeof(void), contentTypes));
		}

		builder.AddOpenApiOperationTransformer((operation, _, _) =>
		{
			if (operation.Responses is not null)
			{
				IOpenApiResponse? response = operation.Responses[statusCode.ToString()];
				response?.Description = description;
			}

			return Task.CompletedTask;
		});

		return builder;
	}
}
