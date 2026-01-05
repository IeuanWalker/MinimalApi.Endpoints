using System.Diagnostics.CodeAnalysis;
using ExampleApi.Infrastructure;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.TypeExamples.PostFromForm;

/// <summary>
/// Example endpoint demonstrating all primitive types handled by TypeDocumentTransformer (FromForm with file uploads)
/// </summary>
[ExcludeFromCodeCoverage]
public class PostTypeExamplesFromFormEndpoint : IEndpoint<RequestModel, ResponseModel>
{
	public static void Configure(RouteHandlerBuilder builder)
	{
		builder
			.Group<TypeExamplesEndpointGroup>()
			.Post("/FromForm")
			.Version(1)
			.RequestFromForm()
			.DisableAntiforgery()
			.WithSummary("Type Examples From Form")
			.WithDescription("Demonstrates all primitive types handled by TypeDocumentTransformer as form data: " +
				"string, int, long, short, byte, decimal, double, float, bool, " +
				"DateTime, DateTimeOffset, DateOnly, TimeOnly, Guid, arrays, nested objects, " +
				"and file uploads (IFormFile, IFormFileCollection)");
	}

	public Task<ResponseModel> Handle(RequestModel request, CancellationToken ct)
	{
		// Count non-null properties to demonstrate the endpoint is working
		int count = 0;
		int filesCount = 0;

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

		if (request.DoubleValue != 0)
		{
			count++;
		}

		if (request.NullableDoubleValue.HasValue)
		{
			count++;
		}

		if (request.FloatValue != 0)
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

		// Count file uploads
		if (request.SingleFile != null)
		{
			filesCount++;
		}

		if (request.SingleFileNullable != null)
		{
			filesCount++;
		}

		if (request.ReadOnlyList1?.Count > 0)
		{
			filesCount += request.ReadOnlyList1.Count;
		}

		if (request.ReadOnlyList2?.Count > 0)
		{
			filesCount += request.ReadOnlyList2.Count;
		}

		if (request.ReadOnlyList1Nullable?.Count > 0)
		{
			filesCount += request.ReadOnlyList1Nullable.Count;
		}

		if (request.ReadOnlyList2Nullable?.Count > 0)
		{
			filesCount += request.ReadOnlyList2Nullable.Count;
		}

		if (request.FileCollectionList?.Count > 0)
		{
			filesCount += request.FileCollectionList.Count;
		}

		ResponseModel response = new()
		{
			Message = "Successfully processed form data with all primitive types and file uploads",
			ProcessedPropertiesCount = count,
			UploadedFilesCount = filesCount
		};

		return Task.FromResult(response);
	}
}
