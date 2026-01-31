namespace ExampleApi.IntegrationTests.Infrastructure;

public partial class OpenApiTests : IClassFixture<ExampleApiWebApplicationFactory>
{
	readonly HttpClient _client;

	public OpenApiTests(ExampleApiWebApplicationFactory factory)
	{
		_client = factory.CreateClient();
	}

	[Fact]
	public async Task OpenApiJson_ReturnsValidResponse()
	{
		// Act
		HttpResponseMessage response = await _client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

		// Assert
		await Verify(response)
			.IgnoreMember("Content-Length");
	}
}
