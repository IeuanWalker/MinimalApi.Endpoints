using Shouldly;
using ExampleApi.Endpoints.WeatherForecast.Get.V2;

namespace ExampleApi.Tests.Endpoints.WeatherForecast;

public class GetWeatherForecastV2EndpointTests
{
	[Fact]
	public async Task Handle_ReturnsFiveForecasts()
	{
		// Arrange
		GetWeatherForecastEndpoint endpoint = new();

		// Act
		ResponseModel[] result = await endpoint.Handle(CancellationToken.None);

		// Assert
		result.ShouldNotBeNull();
		result.Length.ShouldBe(5);
		foreach (ResponseModel item in result)
		{
			item.Date.ShouldNotBe(default);
			item.Summary.ShouldNotBeNullOrWhiteSpace();
		}
	}
}
