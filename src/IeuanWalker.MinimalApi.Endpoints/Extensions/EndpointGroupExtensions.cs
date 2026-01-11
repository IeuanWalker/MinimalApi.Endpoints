using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace IeuanWalker.MinimalApi.Endpoints;
#pragma warning restore IDE0130 // Namespace does not match folder structure

[ExcludeFromCodeCoverage]
public static class EndpointGroupExtensions
{
	extension(RouteHandlerBuilder source)
	{
		public RouteHandlerBuilder Group<TEndpointGroup>() where TEndpointGroup : IEndpointGroup
		{
			return source;
		}
	}
}
