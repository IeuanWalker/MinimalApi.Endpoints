using System.Net;
using System.Net.Http.Json;

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
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		ExampleApi.Endpoints.WeatherForecast.Get.V1.ResponseModel[]? forecast = await response.Content.ReadFromJsonAsync<ExampleApi.Endpoints.WeatherForecast.Get.V1.ResponseModel[]>(TestContext.Current.CancellationToken);
		forecast.ShouldNotBeNull();
		forecast.Length.ShouldBe(5);

		foreach (ExampleApi.Endpoints.WeatherForecast.Get.V1.ResponseModel item in forecast)
		{
			item.Date.ShouldNotBe(default);
			item.Summary.ShouldNotBeNullOrWhiteSpace();
			item.TemperatureC.ShouldBeGreaterThanOrEqualTo(-20);
			item.TemperatureC.ShouldBeLessThanOrEqualTo(55);
		}
	}

	[Fact]
	public async Task GetWeatherForecast_WithVersionHeader_V1_ReturnsV1Response()
	{
		// Arrange
		using HttpRequestMessage request = new(HttpMethod.Get, "/weatherforecast");
		request.Headers.Add("X-Version", "1");

		// Act
		HttpResponseMessage response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		ExampleApi.Endpoints.WeatherForecast.Get.V1.ResponseModel[]? forecast = await response.Content.ReadFromJsonAsync<ExampleApi.Endpoints.WeatherForecast.Get.V1.ResponseModel[]>(TestContext.Current.CancellationToken);
		forecast.ShouldNotBeNull();
		forecast.Length.ShouldBe(5);
	}

	[Fact]
	public async Task GetWeatherForecast_WithQueryParameter_V1_ReturnsV1Response()
	{
		// Act
		HttpResponseMessage response = await _client.GetAsync("/weatherforecast?api-version=1", TestContext.Current.CancellationToken);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		ExampleApi.Endpoints.WeatherForecast.Get.V1.ResponseModel[]? forecast = await response.Content.ReadFromJsonAsync<ExampleApi.Endpoints.WeatherForecast.Get.V1.ResponseModel[]>(TestContext.Current.CancellationToken);
		forecast.ShouldNotBeNull();
	}

	[Fact]
	public async Task GetWeatherForecast_WithInvalidVersion_ReturnsBadRequest()
	{
		// Act
		HttpResponseMessage response = await _client.GetAsync("/weatherforecast?api-version=3", TestContext.Current.CancellationToken);

		// Assert
		response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
	}
}
