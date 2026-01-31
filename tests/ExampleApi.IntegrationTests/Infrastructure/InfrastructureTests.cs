using System.Net;

namespace ExampleApi.IntegrationTests.Infrastructure;

/// <summary>
/// Integration tests for infrastructure concerns like API documentation, health checks, etc.
/// </summary>
public partial class InfrastructureTests : IClassFixture<ExampleApiWebApplicationFactory>
{
	readonly HttpClient _client;

	public InfrastructureTests(ExampleApiWebApplicationFactory factory)
	{
		_client = factory.CreateClient();
	}

	[Fact]
	public async Task NonExistentEndpoint_Returns404()
	{
		// Act
		HttpResponseMessage response = await _client.GetAsync("/api/nonexistent", TestContext.Current.CancellationToken);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task ApiVersioning_IsConfigured()
	{
		// Test that API versioning middleware is working by trying different version formats

		// URL path versioning
		HttpResponseMessage v1Response = await _client.GetAsync("/weatherforecast", TestContext.Current.CancellationToken);
		HttpResponseMessage v2Response = await _client.GetAsync("/api/v2/weatherforecast", TestContext.Current.CancellationToken);

		v1Response.StatusCode.ShouldBe(HttpStatusCode.OK);
		v2Response.StatusCode.ShouldBe(HttpStatusCode.OK);

		// The responses should be different (V2 has TemperatureF, V1 doesn't)
		string v1Content = await v1Response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
		string v2Content = await v2Response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

		v1Content.ShouldNotContain("temperatureF");
		v2Content.ShouldContain("temperatureF");
	}
}
