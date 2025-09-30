using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace IeuanWalker.MinimalApi.Endpoints;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public interface IEndpointGroup
{
	static abstract RouteGroupBuilder Configure(WebApplication app);
}
