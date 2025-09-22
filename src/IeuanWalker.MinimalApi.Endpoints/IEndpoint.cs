using Microsoft.AspNetCore.Builder;

namespace IeuanWalker.MinimalApi.Endpoints;

public interface IEndpointBase
{
    static abstract void Configure(RouteHandlerBuilder builder);
}

public interface IEndpoint : IEndpointBase
{
    Task HandleAsync(CancellationToken ct);
}

public interface IEndpoint<in TRequest, TResponse> : IEndpointBase
{
    Task<TResponse> HandleAsync(TRequest request, CancellationToken ct);
}

public interface IEndpointWithoutRequest<TResponse> : IEndpointBase
{
    Task<TResponse> HandleAsync(CancellationToken ct);
}

public interface IEndpointWithoutResponse<in TRequest> : IEndpointBase
{
    Task HandleAsync(TRequest request, CancellationToken ct);
}
