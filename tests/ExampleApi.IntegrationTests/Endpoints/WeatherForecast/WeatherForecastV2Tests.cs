namespace ExampleApi.IntegrationTests.Endpoints.WeatherForecast;

/// <summary>
/// Integration tests for WeatherForecast V2 endpoint
/// </summary>
public class WeatherForecastV2Tests : IClassFixture<ExampleApiWebApplicationFactory>
{
	readonly HttpClient _client;

	public WeatherForecastV2Tests(ExampleApiWebApplicationFactory factory)
	{
		_client = factory.CreateClient();
	}

	[Fact]
	public async Task GetWeatherForecast_V2_ReturnsV2Response()
	{
		// Act
		HttpResponseMessage response = await _client.GetAsync("/api/v2/weatherforecast", TestContext.Current.CancellationToken);

		// Assert
		await Verify(response)
			.IgnoreMembers("Content-Length", "date", "temperatureC", "temperatureF", "summary");
	}
}
