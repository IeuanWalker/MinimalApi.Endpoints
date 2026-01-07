using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;

// NOTE: Keep assembly lookup cached to avoid repeated AppDomain scans across transformers.

namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers;

static class SchemaTypeResolver
{
	static readonly ConcurrentDictionary<string, Lazy<Type?>> typeCache = new(StringComparer.Ordinal);
	static readonly Lazy<Assembly[]> assemblies = new(static () =>
	{
		try
		{
			return AppDomain.CurrentDomain
				.GetAssemblies()
				.Where(a => !a.IsDynamic)
				.ToArray();
		}
		catch
		{
			return Array.Empty<Assembly>();
		}
	}, LazyThreadSafetyMode.ExecutionAndPublication);

	public static Type? GetSchemaType(string schemaName)
	{
		return typeCache.GetOrAdd(schemaName, static key => new Lazy<Type?>(() => ResolveType(key), LazyThreadSafetyMode.ExecutionAndPublication)).Value;
	}

	public static Type? GetEnumType(string schemaName)
	{
		Type? foundType = GetSchemaType(schemaName);
		if (foundType is null)
		{
			return null;
		}

		if (foundType.IsEnum)
		{
			return foundType;
		}

		Type? underlyingNullable = Nullable.GetUnderlyingType(foundType);
		return underlyingNullable?.IsEnum is true ? underlyingNullable : null;
	}

	static Type? ResolveType(string schemaName)
	{
		string typeName = schemaName.Replace('+', '.');

		return assemblies.Value
			.Select(assembly => assembly.GetType(typeName))
			.FirstOrDefault(type => type is not null);
	}
}
