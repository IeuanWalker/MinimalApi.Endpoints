using System.Net;
using System.Net.Http.Json;

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
		HttpResponseMessage response = await _client.GetAsync("/api/v2/weatherforecast");

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		ExampleApi.Endpoints.WeatherForecast.Get.V2.ResponseModel[]? forecast = await response.Content.ReadFromJsonAsync<ExampleApi.Endpoints.WeatherForecast.Get.V2.ResponseModel[]>();
		forecast.ShouldNotBeNull();
		forecast.Length.ShouldBe(5);

		foreach (ExampleApi.Endpoints.WeatherForecast.Get.V2.ResponseModel item in forecast)
		{
			item.Date.ShouldNotBe(default);
			item.Summary.ShouldNotBeNullOrWhiteSpace();
			item.TemperatureC.ShouldBeGreaterThanOrEqualTo(-20);
			item.TemperatureC.ShouldBeLessThanOrEqualTo(55);
			item.TemperatureF.ShouldBeGreaterThanOrEqualTo(-4);
			item.TemperatureF.ShouldBeLessThanOrEqualTo(131);

			// Verify F = C * 9/5 + 32
			int expectedF = 32 + (int)(item.TemperatureC / 0.5556);
			Math.Abs(item.TemperatureF - expectedF).ShouldBeLessThanOrEqualTo(1); // Allow for rounding
		}
	}

	[Fact]
	public async Task GetWeatherForecast_WithVersionHeader_V2_ReturnsV2Response()
	{
		// Arrange
		using HttpRequestMessage request = new(HttpMethod.Get, "/api/v2/weatherforecast");
		request.Headers.Add("X-Version", "2");

		// Act
		HttpResponseMessage response = await _client.SendAsync(request);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		ExampleApi.Endpoints.WeatherForecast.Get.V2.ResponseModel[]? forecast = await response.Content.ReadFromJsonAsync<ExampleApi.Endpoints.WeatherForecast.Get.V2.ResponseModel[]>();
		forecast.ShouldNotBeNull();
		// V2 should have TemperatureF property
		forecast[0].TemperatureF.ShouldNotBe(default);
	}

	[Fact]
	public async Task GetWeatherForecast_WithQueryParameter_V2_ReturnsV2Response()
	{
		// Act
		HttpResponseMessage response = await _client.GetAsync("/api/v2/weatherforecast?api-version=2");

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		ExampleApi.Endpoints.WeatherForecast.Get.V2.ResponseModel[]? forecast = await response.Content.ReadFromJsonAsync<ExampleApi.Endpoints.WeatherForecast.Get.V2.ResponseModel[]>();
		forecast.ShouldNotBeNull();
		forecast[0].TemperatureF.ShouldNotBe(default);
	}
}
