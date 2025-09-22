using ExampleApi.Infrastructure;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.TestV2;

public class PostTestEndpoint : IEndpoint<RequestModel, ResponseModel[]>
{
    readonly ILogger<PostTestEndpoint> _logger;
    public PostTestEndpoint(ILogger<PostTestEndpoint> logger)
    {
        _logger = logger;
    }

    public static void Configure(RouteHandlerBuilder builder)
    {
        builder
            .Get("api/v{version:apiVersion}/weatherforecast")
            .WithName("GetWeatherForecastV2")
            .WithTags("WeatherForecast")
            .Version(2.0);
    }

    static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    public async Task<ResponseModel[]> HandleAsync(RequestModel request, CancellationToken ct)
    {
        ResponseModel[] forecast = Enumerable.Range(1, 5).Select(index =>
            new ResponseModel
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();

        return await Task.FromResult(forecast);
    }
}
