namespace ExampleApi.IntegrationTests.Endpoints.WeatherForecast;

/// <summary>
/// Integration tests for WeatherForecast V1 endpoint
/// </summary>
public class WeatherForecastV1Tests : IClassFixture<ExampleApiWebApplicationFactory>
{
	readonly HttpClient _client;

	public WeatherForecastV1Tests(ExampleApiWebApplicationFactory factory)
	{
		_client = factory.CreateClient();
	}

	[Fact]
	public async Task GetWeatherForecast_V1_ReturnsV1Response()
	{
		// Act
		HttpResponseMessage response = await _client.GetAsync("/weatherforecast?api-version=1", TestContext.Current.CancellationToken);

		// Assert
		await Verify(response)
			.IgnoreMembers("Content-Length", "date", "temperatureC", "summary");
	}
}
