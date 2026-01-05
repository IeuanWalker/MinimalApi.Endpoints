using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;

namespace ExampleApi.Endpoints.TypeExamples.GetFromQuery;

/// <summary>
/// Request model demonstrating all primitive types handled by TypeDocumentTransformer (as query parameters)
/// </summary>
[ExcludeFromCodeCoverage]
public class RequestModel
{
	// String types
	[FromQuery]
	public string StringValue { get; set; } = string.Empty;

	[FromQuery]
	public string? NullableStringValue { get; set; }

	// Integer types
	[FromQuery]
	public int IntValue { get; set; }

	[FromQuery]
	public int? NullableIntValue { get; set; }

	[FromQuery]
	public long LongValue { get; set; }

	[FromQuery]
	public long? NullableLongValue { get; set; }

	// Floating point types
	[FromQuery]
	public decimal DecimalValue { get; set; }

	[FromQuery]
	public decimal? NullableDecimalValue { get; set; }

	[FromQuery]
	public double DoubleValue { get; set; }

	[FromQuery]
	public double? NullableDoubleValue { get; set; }

	[FromQuery]
	public float FloatValue { get; set; }

	[FromQuery]
	public float? NullableFloatValue { get; set; }

	// Boolean
	[FromQuery]
	public bool BoolValue { get; set; }

	[FromQuery]
	public bool? NullableBoolValue { get; set; }

	// Date/Time types
	[FromQuery]
	public DateTime DateTimeValue { get; set; }

	[FromQuery]
	public DateTime? NullableDateTimeValue { get; set; }

	[FromQuery]
	public DateOnly DateOnlyValue { get; set; }

	[FromQuery]
	public DateOnly? NullableDateOnlyValue { get; set; }

	[FromQuery]
	public TimeOnly TimeOnlyValue { get; set; }

	[FromQuery]
	public TimeOnly? NullableTimeOnlyValue { get; set; }

	// Guid
	[FromQuery]
	public Guid GuidValue { get; set; }

	[FromQuery]
	public Guid? NullableGuidValue { get; set; }

	// Array types (query parameters work with arrays, not Lists)
	[FromQuery]
	public string[]? StringArray { get; set; }

	[FromQuery]
	public int[]? IntArray { get; set; }

	[FromQuery]
	public double[]? DoubleArray { get; set; }
}
