using Microsoft.AspNetCore.Builder;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace IeuanWalker.MinimalApi.Endpoints;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public interface IEndpointBase
{
	static abstract void Configure(RouteHandlerBuilder builder);
}

public interface IEndpoint : IEndpointBase
{
	Task Handle(CancellationToken ct);
}

public interface IEndpoint<in TRequest, TResponse> : IEndpointBase
{
	Task<TResponse> Handle(TRequest request, CancellationToken ct);
}

public interface IEndpointWithoutRequest<TResponse> : IEndpointBase
{
	Task<TResponse> Handle(CancellationToken ct);
}

public interface IEndpointWithoutResponse<in TRequest> : IEndpointBase
{
	Task Handle(TRequest request, CancellationToken ct);
}
