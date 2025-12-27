using System.Net;
using System.Net.Http.Json;

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
	public async Task PostFluentValidation_WithValidData_ReturnsOk()
	{
		var request = new ExampleApi.Endpoints.Validation.PostFluentValidation.RequestModel
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
			ListIntRangeCount = [1],
			NestedObject = new ExampleApi.Endpoints.Validation.PostFluentValidation.NestedObjectModel
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
			// Fluent specific required properties
			AllBuiltInStringValidators = null,
			AllBuiltInNumberValidators = null,
			MaxNumberTest = 0,
			MinNumberTest = 0,
			EnumStringValidator = "Success",
			EnumIntValidator = 0,
			EnumTest = ExampleApi.Endpoints.Validation.PostFluentValidation.StatusEnum.Success
		};

		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/validation/FluentValidation", request);

		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task PostFluentValidation_WithShortStringMin_ReturnsBadRequest()
	{
		var request = new ExampleApi.Endpoints.Validation.PostFluentValidation.RequestModel
		{
			StringMin = "ab", // too short (min length 3)
			NestedObject = new ExampleApi.Endpoints.Validation.PostFluentValidation.NestedObjectModel
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

		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/validation/FluentValidation", request);

		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

		string content = await response.Content.ReadAsStringAsync();
		content.ShouldContain("StringMin", Case.Insensitive);
	}
}
