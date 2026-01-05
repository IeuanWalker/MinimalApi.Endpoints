using System.Diagnostics.CodeAnalysis;

namespace ExampleApi.Endpoints.TypeExamples.PostFromForm;

/// <summary>
/// Request model demonstrating all primitive types handled by TypeDocumentTransformer (FromForm)
/// </summary>
[ExcludeFromCodeCoverage]
public class RequestModel
{
	// String types
	public string StringValue { get; set; } = string.Empty;
	public string? NullableStringValue { get; set; }

	// Integer types
	public int IntValue { get; set; }
	public int? NullableIntValue { get; set; }
	public long LongValue { get; set; }
	public long? NullableLongValue { get; set; }
	public short ShortValue { get; set; }
	public short? NullableShortValue { get; set; }
	public byte ByteValue { get; set; }
	public byte? NullableByteValue { get; set; }

	// Floating point types
	public decimal DecimalValue { get; set; }
	public decimal? NullableDecimalValue { get; set; }
	public double DoubleValue { get; set; }
	public double? NullableDoubleValue { get; set; }
	public float FloatValue { get; set; }
	public float? NullableFloatValue { get; set; }

	// Boolean
	public bool BoolValue { get; set; }
	public bool? NullableBoolValue { get; set; }

	// Date/Time types
	public DateTime DateTimeValue { get; set; }
	public DateTime? NullableDateTimeValue { get; set; }
	public DateTimeOffset DateTimeOffsetValue { get; set; }
	public DateTimeOffset? NullableDateTimeOffsetValue { get; set; }
	public DateOnly DateOnlyValue { get; set; }
	public DateOnly? NullableDateOnlyValue { get; set; }
	public TimeOnly TimeOnlyValue { get; set; }
	public TimeOnly? NullableTimeOnlyValue { get; set; }

	// Guid
	public Guid GuidValue { get; set; }
	public Guid? NullableGuidValue { get; set; }

	// Array types
	public List<string> StringList { get; set; } = [];
	public List<string>? NullableStringList { get; set; }
	public List<int> IntList { get; set; } = [];
	public List<int>? NullableIntList { get; set; }
	public List<double> DoubleList { get; set; } = [];
	public List<double>? NullableDoubleList { get; set; }
	public int[] IntArray { get; set; } = [];
	public int[]? NullableIntArray { get; set; }
	public IReadOnlyList<string> ReadOnlyStringList { get; set; } = [];
	public IReadOnlyList<string>? NullableReadOnlyStringList { get; set; }
	public ICollection<int> IntCollection { get; set; } = [];
	public ICollection<int>? NullableIntCollection { get; set; }

	// IFormFile types (proper use case for FromForm)
	public IFormFile SingleFile { get; set; } = null!;
	public IReadOnlyList<IFormFile> ReadOnlyList1 { get; set; } = [];
	public IReadOnlyList<IFormFile> ReadOnlyList2 { get; set; } = [];
	public IFormFile? SingleFileNullable { get; set; }
	public IReadOnlyList<IFormFile>? ReadOnlyList1Nullable { get; set; }
	public IReadOnlyList<IFormFile>? ReadOnlyList2Nullable { get; set; }
	/// <summary>
	/// Important: IFormFileCollection doesnt respect property names and binds all files in the request
	/// IFormFile and IReadOnlyList respect property names and only bind the files relevant to their property names
	/// https://github.com/dotnet/aspnetcore/issues/54999
	/// </summary>
	public IFormFileCollection FileCollectionList { get; set; } = null!;

	// Nested object
	public NestedObject? NestedObjectValue { get; set; }
	public List<NestedObject>? NestedObjectList { get; set; }
}

/// <summary>
/// Nested object to demonstrate complex type handling
/// </summary>
[ExcludeFromCodeCoverage]
public class NestedObject
{
	public string Name { get; set; } = string.Empty;
	public int Value { get; set; }
	public DateTime CreatedAt { get; set; }
}
