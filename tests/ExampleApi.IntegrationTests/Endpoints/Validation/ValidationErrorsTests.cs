using System.Net;
using System.Text.Json;

namespace ExampleApi.IntegrationTests.Endpoints.Validation;

public class ValidationErrorsTests : IClassFixture<ExampleApiWebApplicationFactory>
{
	readonly HttpClient _client;

	public ValidationErrorsTests(ExampleApiWebApplicationFactory factory)
	{
		_client = factory.CreateClient();
	}

	[Fact]
	public async Task GetValidationErrors_ReturnsProblemDetailsWithErrors()
	{
		// Act
		HttpResponseMessage response = await _client.GetAsync("/api/v1/validation/ValidationErrors", TestContext.Current.CancellationToken);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
		response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");

		string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
		using JsonDocument json = JsonDocument.Parse(content);

		JsonElement errors = json.RootElement.GetProperty("errors");
		errors.TryGetProperty("Name", out JsonElement nameErrors).ShouldBeTrue();
		nameErrors[0].GetString().ShouldBe("Name is required");

		errors.TryGetProperty("Nested.Description", out JsonElement nestedErrors).ShouldBeTrue();
		nestedErrors[0].GetString().ShouldBe("Description is required");
	}
}
