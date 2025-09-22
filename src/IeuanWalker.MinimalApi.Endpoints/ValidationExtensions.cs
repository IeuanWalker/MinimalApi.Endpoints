using Microsoft.AspNetCore.Builder;

namespace IeuanWalker.MinimalApi.Endpoints;

public static class ValidationExtensions
{
    public static RouteHandlerBuilder DontValidate(this RouteHandlerBuilder builder)
    {
        return builder;
    }
}
