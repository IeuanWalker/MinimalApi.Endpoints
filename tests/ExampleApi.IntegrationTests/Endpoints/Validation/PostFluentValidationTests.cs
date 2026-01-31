using System.Net.Http.Json;
using ExampleApi.Endpoints.Validation.PostFluentValidation;

namespace ExampleApi.IntegrationTests.Endpoints.Validation;

public class PostFluentValidationTests : IClassFixture<ExampleApiWebApplicationFactory>
{
	readonly ExampleApiWebApplicationFactory _factory;
	readonly HttpClient _client;

	public PostFluentValidationTests(ExampleApiWebApplicationFactory factory)
	{
		_factory = factory;
		_client = _factory.CreateClient();
	}

	[Fact]
	public async Task PostFluentValidation_WithShortStringMin_ReturnsBadRequest()
	{
		// Arrange
		RequestModel request = new()
		{
			StringMin = "ab", // too short (min length 3)
			NestedObject = new NestedObjectModel
			{
				StringMin = "abc",
				StringMax = "ok",
				StringRange = "range",
				StringPattern = "ABC123",
				IntMin = 1,
				IntMax = 0,
				IntRange = 50,
				DoubleMin = 0.1,
				DoubleMax = 1.0,
				DoubleRange = 1.0,
				ListStringMinCount = ["x"],
				ListStringMaxCount = [],
				ListStringRangeCount = ["x"],
				ListIntMinCount = [1],
				ListIntMaxCount = [],
				ListIntRangeCount = [1]
			},
			// Required members for RequestModel
			AllBuiltInStringValidators = null,
			AllBuiltInNumberValidators = null,
			MaxNumberTest = 0,
			MinNumberTest = 0,
			EnumStringValidator = "Success",
			EnumIntValidator = 0,
			EnumTest = ExampleApi.Endpoints.Validation.PostFluentValidation.StatusEnum.Success
		};

		// Act
		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/validation/FluentValidation", request, TestContext.Current.CancellationToken);

		// Assert
		await Verify(response)
			.IgnoreMembers("Content-Length", "traceId");
	}
}
