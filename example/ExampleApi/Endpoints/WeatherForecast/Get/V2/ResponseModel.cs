namespace ExampleApi.Endpoints.WeatherForecast.Get.V2;

public class ResponseModel
{
	public DateOnly Date { get; set; }
	public int TemperatureC { get; set; }
	public int TemperatureF { get; set; }
	public string? Summary { get; set; }
}
