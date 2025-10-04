using ExampleApi.Infrastructure;
using ExampleApi.Services;
using ExampleApi;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.AddApiVersioning();
builder.AddEndpointsFromExampleApi();
builder.Services.AddSingleton<ITodoStore, InMemoryTodoStore>();
builder.AddScalar();

WebApplication app = builder.Build();
app.UseHttpsRedirection();
app.UseExceptionHandler();
app.UseApiVersioning();
app.MapEndpointsFromExampleApi();
app.UseScalar();

await app.RunAsync();

// Make the implicit Program class accessible for testing
public partial class Program { }
