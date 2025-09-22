using ExampleApi.Infrastructure;
using IeuanWalker.MinimalApi.Endpoints;

namespace ExampleApi.Endpoints.TestV1;

public class PostTestEndpoint : IEndpoint<RequestModel, ResponseModel[]>
{
    public static void Configure(RouteHandlerBuilder builder)
    {
        builder
            .Get("/weatherforecast")
            .Version(1.0);
    }

    public async Task<ResponseModel[]> HandleAsync(RequestModel request, CancellationToken ct)
    {
        string[] summaries =
        [
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        ];

        ResponseModel[] forecast = [.. Enumerable.Range(1, 5).Select(index =>
            new ResponseModel
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = summaries[Random.Shared.Next(summaries.Length)]
            })];

        return await Task.FromResult(forecast);
    }
}
