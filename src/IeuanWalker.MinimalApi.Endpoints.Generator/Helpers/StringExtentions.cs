using System.Text.RegularExpressions;

namespace IeuanWalker.MinimalApi.Endpoints.Generator.Helpers;

static class Extensions
{
	static readonly Regex regex = new("[^a-zA-Z0-9]+", RegexOptions.Compiled);

	internal static string Sanitize(this string input, string replacement = "_") => regex.Replace(input, replacement);
}