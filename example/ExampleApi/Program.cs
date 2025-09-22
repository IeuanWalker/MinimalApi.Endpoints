using ExampleApi;
using ExampleApi.Infrastructure;
using ExampleApi.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddApiVersioning();
builder.AddScalar();
builder.AddEndpointsFromExampleApi();
builder.Services.AddSingleton<ITodoStore, InMemoryTodoStore>();

WebApplication app = builder.Build();

app.UseHttpsRedirection();
app.UseApiVersioning();
app.MapEndpointsFromExampleApi();
app.UseScalar();

await app.RunAsync();