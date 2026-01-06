using System.Diagnostics.CodeAnalysis;
using ExampleApi.Infrastructure;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.TypeExamples.GetFromQuery;

/// <summary>
/// Example endpoint demonstrating all primitive types handled by TypeDocumentTransformer (FromQuery)
/// </summary>
[ExcludeFromCodeCoverage]
public class GetTypeExamplesFromQueryEndpoint : IEndpoint<RequestModel, ResponseModel>
{
	public static void Configure(RouteHandlerBuilder builder)
	{
		builder
			.Group<TypeExamplesEndpointGroup>()
			.Get("/FromQuery")
			.Version(1)
			.RequestAsParameters()
			.WithSummary("Type Examples From Query")
			.WithDescription("Demonstrates all primitive types handled by TypeDocumentTransformer as query parameters: " +
				"string, int, long, decimal, double, float, bool, " +
				"DateTime, DateOnly, TimeOnly, Guid, and arrays");
	}

	public Task<ResponseModel> Handle(RequestModel request, CancellationToken ct)
	{
		Dictionary<string, string> receivedValues = [];
		int count = 0;

		// Track which parameters were provided
		if (!string.IsNullOrEmpty(request.StringValue))
		{
			receivedValues["stringValue"] = request.StringValue;
			count++;
		}

		if (request.NullableStringValue != null)
		{
			receivedValues["nullableStringValue"] = request.NullableStringValue;
			count++;
		}

		if (request.IntValue != 0)
		{
			receivedValues["intValue"] = request.IntValue.ToString();
			count++;
		}

		if (request.NullableIntValue.HasValue)
		{
			receivedValues["nullableIntValue"] = request.NullableIntValue.Value.ToString();
			count++;
		}

		if (request.LongValue != 0)
		{
			receivedValues["longValue"] = request.LongValue.ToString();
			count++;
		}

		if (Math.Abs(request.DoubleValue) > double.Epsilon)
		{
			receivedValues["doubleValue"] = request.DoubleValue.ToString();
			count++;
		}

		if (Math.Abs(request.FloatValue) > float.Epsilon)
		{
			receivedValues["floatValue"] = request.FloatValue.ToString();
			count++;
		}

		if (request.DecimalValue != 0)
		{
			receivedValues["decimalValue"] = request.DecimalValue.ToString();
			count++;
		}

		if (request.BoolValue)
		{
			receivedValues["boolValue"] = request.BoolValue.ToString();
			count++;
		}

		if (request.DateTimeValue != default)
		{
			receivedValues["dateTimeValue"] = request.DateTimeValue.ToString("O");
			count++;
		}

		if (request.GuidValue != default)
		{
			receivedValues["guidValue"] = request.GuidValue.ToString();
			count++;
		}

		if (request.StringArray?.Length > 0)
		{
			receivedValues["stringArray"] = string.Join(", ", request.StringArray);
			count++;
		}

		if (request.DoubleArray?.Length > 0)
		{
			receivedValues["doubleArray"] = string.Join(", ", request.DoubleArray);
			count++;
		}

		if (request.IntArray?.Length > 0)
		{
			receivedValues["intArray"] = string.Join(", ", request.IntArray);
			count++;
		}

		ResponseModel response = new()
		{
			Message = "Successfully processed query parameters with all primitive types",
			ProvidedParametersCount = count,
			ReceivedValues = receivedValues
		};

		return Task.FromResult(response);
	}
}
