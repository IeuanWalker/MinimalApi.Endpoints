using Microsoft.AspNetCore.Builder;
using System.Diagnostics.CodeAnalysis;

namespace IeuanWalker.MinimalApi.Endpoints;

[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
[SuppressMessage("Roslynator", "RCS1163:Unused parameter", Justification = "<Pending>")]
public static class MapEndpointExtensions
{
    public static RouteHandlerBuilder Get(this RouteHandlerBuilder builder, [StringSyntax("Route")] string pattern)
    {
        return builder;
    }
    public static RouteHandlerBuilder Post(this RouteHandlerBuilder builder, [StringSyntax("Route")] string pattern)
    {
        return builder;
    }
    public static RouteHandlerBuilder Put(this RouteHandlerBuilder builder, [StringSyntax("Route")] string pattern)
    {
        return builder;
    }
    public static RouteHandlerBuilder Patch(this RouteHandlerBuilder builder, [StringSyntax("Route")] string pattern)
    {
        return builder;
    }
    public static RouteHandlerBuilder Delete(this RouteHandlerBuilder builder, [StringSyntax("Route")] string pattern)
    {
        return builder;
    }
}
