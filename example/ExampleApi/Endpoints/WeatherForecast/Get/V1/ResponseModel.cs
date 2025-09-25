namespace ExampleApi.Endpoints.WeatherForecast.Get.V1;

public class ResponseModel
{
	public DateOnly Date { get; set; }
	public int TemperatureC { get; set; }
	public string? Summary { get; set; }
}
