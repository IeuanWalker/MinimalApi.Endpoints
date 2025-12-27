namespace IeuanWalker.MinimalApi.Endpoints.Extensions;

public static class StringExtensions
{
	extension(string value)
	{
		internal string ToCamelCase()
		{
			if (string.IsNullOrEmpty(value))
			{
				return value;
			}

			return char.ToLowerInvariant(value[0]) + value[1..];
		}
	}
}
