using System.Collections.Concurrent;
using System.Reflection;

// NOTE: Keep assembly lookup cached to avoid repeated AppDomain scans across transformers.

namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer.Core;

/// <summary>
/// Resolves .NET types from OpenAPI schema names. Uses caching to optimize repeated lookups.
/// </summary>
static class SchemaTypeResolver
{
	static readonly ConcurrentDictionary<string, Lazy<Type?>> typeCache = new(StringComparer.Ordinal);
	static readonly Lazy<Assembly[]> assemblies = new(static () =>
	{
		try
		{
			return [.. AppDomain.CurrentDomain
				.GetAssemblies()
				.Where(a => !a.IsDynamic)];
		}
		catch
		{
			return [];
		}
	}, LazyThreadSafetyMode.ExecutionAndPublication);

	/// <summary>
	/// Gets the .NET type corresponding to an OpenAPI schema name.
	/// </summary>
	/// <param name="schemaName">The schema name (typically a full type name).</param>
	/// <returns>The resolved type, or null if not found.</returns>
	public static Type? GetSchemaType(string schemaName)
	{
		return typeCache.GetOrAdd(schemaName, static key => new Lazy<Type?>(() => ResolveType(key), LazyThreadSafetyMode.ExecutionAndPublication)).Value;
	}

	/// <summary>
	/// Gets the enum type corresponding to an OpenAPI schema name, handling nullable wrappers.
	/// </summary>
	/// <param name="schemaName">The schema name (typically a full type name).</param>
	/// <returns>The resolved enum type, or null if not an enum.</returns>
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

	/// <summary>
	/// Checks if an assembly should be inspected for type discovery.
	/// Excludes system and framework assemblies for performance.
	/// </summary>
	public static bool ShouldInspectAssembly(Assembly assembly)
	{
		if (assembly.IsDynamic)
		{
			return false;
		}

		string? fullName = assembly.FullName;
		if (string.IsNullOrEmpty(fullName))
		{
			return false;
		}

		return !(fullName.StartsWith("System.", StringComparison.Ordinal) ||
				 fullName.StartsWith("Microsoft.", StringComparison.Ordinal) ||
				 fullName.StartsWith("netstandard", StringComparison.Ordinal));
	}

	/// <summary>
	/// Gets all loadable types from an assembly, handling type load exceptions gracefully.
	/// </summary>
	public static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
	{
		try
		{
			return assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException ex) when (ex.Types is not null)
		{
			return ex.Types.Where(type => type is not null).Cast<Type>();
		}
#pragma warning disable CA1031 // Do not catch general exception types
		catch
		{
			return [];
		}
#pragma warning restore CA1031
	}

	static Type? ResolveType(string schemaName)
	{
		string typeName = schemaName.Replace('+', '.');

		return assemblies.Value
			.Select(assembly => assembly.GetType(typeName))
			.FirstOrDefault(type => type is not null);
	}
}
