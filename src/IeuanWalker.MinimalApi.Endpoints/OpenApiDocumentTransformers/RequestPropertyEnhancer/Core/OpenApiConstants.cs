namespace IeuanWalker.MinimalApi.Endpoints.OpenApiDocumentTransformers.RequestPropertyEnhancer.Core;

/// <summary>
/// Constants used across OpenAPI document transformers for consistent naming and type identification.
/// </summary>
static class SchemaConstants
{
	// OpenAPI extension names
	public const string NullableExtension = "nullable";
	public const string EnumExtension = "enum";
	public const string EnumVarNamesExtension = "x-enum-varnames";
	public const string EnumDescriptionsExtension = "x-enum-descriptions";

	// System type prefixes
	public const string SystemPrefix = "System.";
	public const string NullableTypePrefix = "System.Nullable`1";
	public const string AspNetCoreHttpPrefix = "Microsoft.AspNetCore.Http.";

	// Common type names
	public const string SystemString = "System.String";
	public const string SystemInt32 = "System.Int32";
	public const string SystemInt64 = "System.Int64";
	public const string SystemInt16 = "System.Int16";
	public const string SystemByte = "System.Byte";
	public const string SystemDecimal = "System.Decimal";
	public const string SystemDouble = "System.Double";
	public const string SystemSingle = "System.Single";
	public const string SystemBoolean = "System.Boolean";
	public const string SystemDateTime = "System.DateTime";
	public const string SystemDateTimeOffset = "System.DateTimeOffset";
	public const string SystemDateOnly = "System.DateOnly";
	public const string SystemTimeOnly = "System.TimeOnly";
	public const string SystemGuid = "System.Guid";
	public const string SystemUri = "System.Uri";

	// Collection type names
	public const string ListGenericType = "System.Collections.Generic.List`1";
	public const string IEnumerableGenericType = "System.Collections.Generic.IEnumerable`1";
	public const string ICollectionGenericType = "System.Collections.Generic.ICollection`1";
	public const string IReadOnlyListGenericType = "System.Collections.Generic.IReadOnlyList`1";
	public const string IReadOnlyCollectionGenericType = "System.Collections.Generic.IReadOnlyCollection`1";

	// Dictionary type names
	public const string IDictionaryGenericType = "System.Collections.Generic.IDictionary`2";
	public const string DictionaryGenericType = "System.Collections.Generic.Dictionary`2";
	public const string IReadOnlyDictionaryGenericType = "System.Collections.Generic.IReadOnlyDictionary`2";
	public const string ConcurrentDictionaryGenericType = "System.Collections.Concurrent.ConcurrentDictionary`2";

	// ASP.NET Core types
	public const string IFormFile = "Microsoft.AspNetCore.Http.IFormFile";
	public const string IFormFileCollection = "Microsoft.AspNetCore.Http.IFormFileCollection";

	// OpenAPI format strings
	public const string FormatInt32 = "int32";
	public const string FormatInt64 = "int64";
	public const string FormatFloat = "float";
	public const string FormatDouble = "double";
	public const string FormatDateTime = "date-time";
	public const string FormatDate = "date";
	public const string FormatTime = "time";
	public const string FormatUuid = "uuid";
	public const string FormatEmail = "email";
	public const string FormatUri = "uri";
	public const string FormatBinary = "binary";

	// Array marker
	public const string ArraySuffix = "[]";

	/// <summary>
	/// Checks if a type reference ID represents a system primitive type.
	/// </summary>
	public static bool IsSystemType(string refId) =>
		refId.StartsWith(SystemPrefix, StringComparison.Ordinal);

	/// <summary>
	/// Checks if a type reference ID represents a nullable type wrapper.
	/// </summary>
	public static bool IsNullableType(string refId) =>
		refId.StartsWith(NullableTypePrefix, StringComparison.Ordinal);

	/// <summary>
	/// Checks if a type reference ID represents a collection type.
	/// </summary>
	public static bool IsCollectionType(string refId) =>
		refId.EndsWith(ArraySuffix, StringComparison.Ordinal) ||
		refId.Contains(ListGenericType, StringComparison.Ordinal) ||
		refId.Contains(IEnumerableGenericType, StringComparison.Ordinal) ||
		refId.Contains(ICollectionGenericType, StringComparison.Ordinal) ||
		refId.Contains(IReadOnlyListGenericType, StringComparison.Ordinal) ||
		refId.Contains(IReadOnlyCollectionGenericType, StringComparison.Ordinal);

	/// <summary>
	/// Checks if a type reference ID represents a dictionary type.
	/// </summary>
	public static bool IsDictionaryType(string refId) =>
		refId.Contains(IDictionaryGenericType, StringComparison.Ordinal) ||
		refId.Contains(DictionaryGenericType, StringComparison.Ordinal) ||
		refId.Contains(IReadOnlyDictionaryGenericType, StringComparison.Ordinal) ||
		refId.Contains(ConcurrentDictionaryGenericType, StringComparison.Ordinal);
}
