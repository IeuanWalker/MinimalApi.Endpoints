using Microsoft.AspNetCore.Builder;

namespace IeuanWalker.MinimalApi.Endpoints;

public interface IEndpointBase
{
    static abstract void Configure(RouteHandlerBuilder builder);
}

public interface IEndpointBaseWithRequest<in TRequest> : IEndpointBase where TRequest : notnull
{
    /// <summary>
    /// override this method if you'd like to do something to the request dto before it gets validated.
    /// </summary>
    /// <param name="request">the request dto</param>
    public virtual void OnBeforeValidate(TRequest request) { }

    /// <summary>
    /// override this method if you'd like to do something to the request dto before it gets validated.
    /// </summary>
    /// <param name="request">the request dto</param>
    /// <param name="ct">a cancellation token</param>
    public virtual Task OnBeforeValidateAsync(TRequest request, CancellationToken ct)
        => Task.CompletedTask;

    /// <summary>
    /// override this method if you'd like to do something to the request dto after it gets validated.
    /// </summary>
    /// <param name="request">the request dto</param>
    public virtual void OnAfterValidate(TRequest request) { }

    /// <summary>
    /// override this method if you'd like to do something to the request dto after it gets validated.
    /// </summary>
    /// <param name="request">the request dto</param>
    /// <param name="ct">a cancellation token</param>
    public virtual Task OnAfterValidateAsync(TRequest request, CancellationToken ct)
        => Task.CompletedTask;
}
