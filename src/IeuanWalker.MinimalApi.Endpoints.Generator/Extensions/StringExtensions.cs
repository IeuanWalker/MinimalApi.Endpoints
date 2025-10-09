using System.Text.RegularExpressions;

namespace IeuanWalker.MinimalApi.Endpoints.Generator.Extensions;

static class StringExtensions
{
	static readonly Regex regex = new("[^a-zA-Z0-9]+", RegexOptions.Compiled);

	internal static string Sanitize(this string input, string replacement = "_") => regex.Replace(input, replacement);

	internal static string ToLowerFirstLetter(this string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return input;
		}

		if (input.Length == 1)
		{
			return input.ToLower();
		}

		return char.ToLower(input[0]) + input.Substring(1);
	}

	internal static string ToUpperFirstLetter(this string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return input;
		}

		if (input.Length == 1)
		{
			return input.ToUpper();
		}

		return char.ToUpper(input[0]) + input.Substring(1);
	}
}
