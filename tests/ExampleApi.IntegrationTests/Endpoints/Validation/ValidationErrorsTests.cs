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
		await Verify(response)
			.IgnoreMembers("Content-Length", "traceId");
	}
}
