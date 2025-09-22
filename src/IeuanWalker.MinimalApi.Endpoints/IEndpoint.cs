namespace IeuanWalker.MinimalApi.Endpoints;

public interface IEndpointWithoutRequest<TResponse> : IEndpointBase
{
    public Task<TResponse> HandleAsync(CancellationToken ct);
}

public interface IEndpoint : IEndpointBase
{
    public Task HandleAsync(CancellationToken ct);
}

public interface IEndpoint<in TRequest, TResponse> : IEndpointBaseWithRequest<TRequest> where TRequest : notnull
{
    public Task<TResponse> HandleAsync(TRequest request, CancellationToken ct);
}

public interface IEndpointWithoutResponse<in TRequest> : IEndpointBaseWithRequest<TRequest> where TRequest : notnull
{
    public Task HandleAsync(TRequest request, CancellationToken ct);
}
