using Microsoft.CodeAnalysis;

namespace IeuanWalker.MinimalApi.Endpoints.Generator.Extensions;

static class LocationExtensions
{
	/// <summary>
	/// Converts a Location to a cache-friendly LocationInfo that can be safely stored in incremental generator pipelines.
	/// </summary>
	internal static LocationInfo ToCachableLocation(this Location location)
	{
		return new LocationInfo(location);
	}
}
