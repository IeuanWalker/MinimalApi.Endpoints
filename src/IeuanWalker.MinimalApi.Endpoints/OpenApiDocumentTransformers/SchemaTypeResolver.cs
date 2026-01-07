using System.Collections.Concurrent;
using System.Threading;

namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers;

static class SchemaTypeResolver
{
	static readonly ConcurrentDictionary<string, Lazy<Type?>> typeCache = new(StringComparer.Ordinal);

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

		return AppDomain.CurrentDomain.GetAssemblies()
			.Select(assembly => assembly.GetType(typeName))
			.FirstOrDefault(type => type is not null);
	}
}
