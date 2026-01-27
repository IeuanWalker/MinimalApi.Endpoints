using Microsoft.AspNetCore.Builder;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace IeuanWalker.MinimalApi.Endpoints;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class RouteHandlerBuilderExtensions
{
	extension(RouteHandlerBuilder source)
	{
		public RouteHandlerBuilder RequireRoles(params string[] roles)
		{
			return source.RequireAuthorization(p =>
			{
				p.RequireRole(roles);
			});
		}
	}
}
