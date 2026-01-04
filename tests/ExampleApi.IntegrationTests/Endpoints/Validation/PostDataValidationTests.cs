using System.Net;
using System.Net.Http.Json;
using ExampleApi.Endpoints.Validation.PostDataAnnotationsFromBody;

namespace ExampleApi.IntegrationTests.Endpoints.Validation;

public class PostDataValidationTests : IClassFixture<ExampleApiWebApplicationFactory>
{
	readonly ExampleApiWebApplicationFactory _factory;
	readonly HttpClient _client;

	public PostDataValidationTests(ExampleApiWebApplicationFactory factory)
	{
		_factory = factory;
		_client = _factory.CreateClient();
	}

	[Fact]
	public async Task PostDataValidation_WithValidData_ReturnsOk()
	{
		RequestModel request = new()
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
			}
		};

		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/validation/DataValidation", request);

		response.StatusCode.ShouldBe(HttpStatusCode.OK);
	}

	[Fact]
	public async Task PostDataValidation_WithMissingNestedObject_ReturnsBadRequest()
	{
		RequestModel request = new()
		{
			StringMin = "abc",
			NestedObject = null! // missing required nested object
		};

		HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/validation/DataValidation", request);

		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

		string content = await response.Content.ReadAsStringAsync();
		content.ShouldContain("NestedObject", Case.Insensitive);
	}
}
