using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace IeuanWalker.MinimalApi.Endpoints;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Extension methods for generating ETags for output cache revalidation.
/// </summary>
public static class ETagExtensions
{
	/// <summary>
	/// Generates a deterministic ETag based on the serialized object content using SHA256.
	/// Useful for output caching with cache revalidation (If-None-Match → 304 Not Modified).
	/// </summary>
	public static string GenerateEtag<T>(this T obj)
	{
		string jsonContent = JsonSerializer.Serialize(obj);
		byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(jsonContent));
		return $"\"{Convert.ToHexString(hash)}\"";
	}
}
