using System.Diagnostics.CodeAnalysis;
using ExampleApi.Infrastructure;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.TypeExamples.PostFromBody;

/// <summary>
/// Example endpoint demonstrating all primitive types handled by TypeDocumentTransformer (FromBody)
/// </summary>
[ExcludeFromCodeCoverage]
public class PostTypeExamplesFromBodyEndpoint : IEndpoint<RequestModel, ResponseModel>
{
	public static void Configure(RouteHandlerBuilder builder)
	{
		builder
			.Group<TypeExamplesEndpointGroup>()
			.Post("/FromBody")
			.Version(1)
			.RequestFromBody()
			.WithSummary("Type Examples From Body")
			.WithDescription("Demonstrates all primitive types handled by TypeDocumentTransformer: " +
				"string, int, long, short, byte, decimal, double, float, bool, " +
				"DateTime, DateTimeOffset, DateOnly, TimeOnly, Guid, arrays, and nested objects");
	}

	public Task<ResponseModel> Handle(RequestModel request, CancellationToken ct)
	{
		// Count non-null properties to demonstrate the endpoint is working
		int count = 0;
		if (!string.IsNullOrEmpty(request.StringValue))
		{
			count++;
		}

		if (request.NullableStringValue != null)
		{
			count++;
		}

		if (request.IntValue != 0)
		{
			count++;
		}

		if (request.NullableIntValue.HasValue)
		{
			count++;
		}

		if (request.LongValue != 0)
		{
			count++;
		}

		if (request.NullableLongValue.HasValue)
		{
			count++;
		}

		if (Math.Abs(request.DoubleValue) > double.Epsilon)
		{
			count++;
		}

		if (request.NullableDoubleValue.HasValue)
		{
			count++;
		}

		if (Math.Abs(request.FloatValue) > float.Epsilon)
		{
			count++;
		}

		if (request.NullableFloatValue.HasValue)
		{
			count++;
		}

		if (request.DecimalValue != 0)
		{
			count++;
		}

		if (request.NullableDecimalValue.HasValue)
		{
			count++;
		}

		if (request.BoolValue)
		{
			count++;
		}

		if (request.NullableBoolValue.HasValue)
		{
			count++;
		}

		if (request.DateTimeValue != default)
		{
			count++;
		}

		if (request.NullableDateTimeValue.HasValue)
		{
			count++;
		}

		if (request.GuidValue != default)
		{
			count++;
		}

		if (request.NullableGuidValue.HasValue)
		{
			count++;
		}

		if (request.StringList.Count > 0)
		{
			count++;
		}

		if (request.NullableStringList?.Count > 0)
		{
			count++;
		}

		if (request.IntList.Count > 0)
		{
			count++;
		}

		if (request.NullableIntList?.Count > 0)
		{
			count++;
		}

		if (request.DoubleList.Count > 0)
		{
			count++;
		}

		if (request.NullableDoubleList?.Count > 0)
		{
			count++;
		}

		if (request.IntArray.Length > 0)
		{
			count++;
		}

		if (request.NullableIntArray?.Length > 0)
		{
			count++;
		}

		if (request.ReadOnlyStringList.Count > 0)
		{
			count++;
		}

		if (request.NullableReadOnlyStringList?.Count > 0)
		{
			count++;
		}

		if (request.IntCollection.Count > 0)
		{
			count++;
		}

		if (request.NullableIntCollection?.Count > 0)
		{
			count++;
		}

		if (request.NestedObjectValue != null)
		{
			count++;
		}

		if (request.NestedObjectList?.Count > 0)
		{
			count++;
		}

		ResponseModel response = new()
		{
			Message = "Successfully processed request with all primitive types",
			ProcessedPropertiesCount = count
		};

		return Task.FromResult(response);
	}
}
