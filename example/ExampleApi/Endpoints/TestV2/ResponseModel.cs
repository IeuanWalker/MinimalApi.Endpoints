namespace ExampleApi.Endpoints.TestV2;

public class ResponseModel
{
    public DateOnly Date { get; set; }
    public int TemperatureC { get; set; }
    public string? Summary { get; set; }
}
