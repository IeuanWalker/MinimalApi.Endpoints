using System.Diagnostics.CodeAnalysis;
using ExampleApi.Infrastructure;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.WeatherForecast.Get.V2;

public class GetWeatherForecastEndpoint : IEndpointWithoutRequest<ResponseModel[]>
{
	[ExcludeFromCodeCoverage]
	public static void Configure(RouteHandlerBuilder builder)
	{
		builder
			.Get("api/v{version:apiVersion}/weatherforecast")
			.WithName("GetWeatherForecastV2")
			.WithTags("WeatherForecast")
			.Version(2.0);
	}

	static readonly string[] summaries =
	[
		"Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
	];

	public async Task<ResponseModel[]> Handle(CancellationToken ct)
	{
		ResponseModel[] forecast = [.. Enumerable.Range(1, 5).Select(index =>
		{
			var tempC = Random.Shared.Next(-20, 55);
			return new ResponseModel
			{
				Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
				TemperatureC = tempC,
				TemperatureF = 32 + (int)(tempC / 0.5556),
				Summary = summaries[Random.Shared.Next(summaries.Length)]
			};
		})];

		return await Task.FromResult(forecast);
	}
}
