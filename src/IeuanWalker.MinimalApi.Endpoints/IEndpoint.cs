using Microsoft.AspNetCore.Builder;

namespace IeuanWalker.MinimalApi.Endpoints;

public interface IEndpointBase
{
    static abstract void Configure(RouteHandlerBuilder builder);
}

public interface IEndpoint : IEndpointBase
{
    public Task HandleAsync(CancellationToken ct);
}

public interface IEndpoint<in TRequest, TResponse> : IEndpointBase
{
    public Task<TResponse> HandleAsync(TRequest request, CancellationToken ct);
}

public interface IEndpointWithoutRequest<TResponse> : IEndpointBase
{
    public Task<TResponse> HandleAsync(CancellationToken ct);
}

public interface IEndpointWithoutResponse<in TRequest> : IEndpointBase
{
    public Task HandleAsync(TRequest request, CancellationToken ct);
}
