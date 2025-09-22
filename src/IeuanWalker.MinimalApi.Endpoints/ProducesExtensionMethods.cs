using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Net;

namespace IeuanWalker.MinimalApi.Endpoints;

public static class ProducesExtensionMethods
{
    /// <summary>
    /// Adds an <see cref="IProducesResponseTypeMetadata"/> to <see cref="EndpointBuilder.Metadata"/> for all endpoints
    /// produced by <paramref name="builder"/>.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="builder">The <see cref="RouteHandlerBuilder"/>.</param>
    /// <param name="statusCode">The response status code. Defaults to <see cref="HttpStatusCode.OK"/>.</param>
    /// <param name="contentType">The response content type. Defaults to "application/json".</param>
    /// <param name="additionalContentTypes">Additional response content types the endpoint produces for the supplied status code.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> that can be used to further customize the endpoint.</returns>
    public static RouteHandlerBuilder Produces<TResponse>(
        this RouteHandlerBuilder builder,
        HttpStatusCode statusCode = HttpStatusCode.OK,
        string? contentType = null,
        params string[] additionalContentTypes)
    {
        return builder.Produces<TResponse>((int)statusCode, contentType, additionalContentTypes);
    }

    /// <summary>
    /// Adds an <see cref="IProducesResponseTypeMetadata"/> to <see cref="EndpointBuilder.Metadata"/> for all endpoints
    /// produced by <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="RouteHandlerBuilder"/>.</param>
    /// <param name="statusCode">The response status code.</param>
    /// <param name="responseType">The type of the response. Defaults to null.</param>
    /// <param name="contentType">The response content type. Defaults to "application/json" if responseType is not null, otherwise defaults to null.</param>
    /// <param name="additionalContentTypes">Additional response content types the endpoint produces for the supplied status code.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> that can be used to further customize the endpoint.</returns>
    public static RouteHandlerBuilder Produces(
        this RouteHandlerBuilder builder,
        HttpStatusCode statusCode,
        Type? responseType = null,
        string? contentType = null,
        params string[] additionalContentTypes)
    {
        return builder.Produces((int)statusCode, responseType, contentType, additionalContentTypes);
    }
}
