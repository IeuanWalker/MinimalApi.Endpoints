using ExampleApi.Infrastructure;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.WeatherForecast.Get.V1;

public class GetWeatherForecastEndpoint : IEndpoint<RequestModel, ResponseModel[]>
{
    public static void Configure(RouteHandlerBuilder builder)
    {
        builder
            .Get("/weatherforecast")
            .Version(1.0);
    }

    static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    public async Task<ResponseModel[]> HandleAsync(RequestModel request, CancellationToken ct)
    {
        ResponseModel[] forecast = [.. Enumerable.Range(1, 5).Select(index =>
            new ResponseModel
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })];

        return await Task.FromResult(forecast);
    }
}
