using ExampleApi.Infrastructure;
using ExampleApi;
using ExampleApi.Data;

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
