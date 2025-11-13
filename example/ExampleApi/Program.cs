using ExampleApi;
using ExampleApi.Data;
using ExampleApi.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.AddApiVersioning();
builder.AddEndpoints();
builder.Services.AddSingleton<ITodoStore, InMemoryTodoStore>();
builder.AddScalar();

WebApplication app = builder.Build();
app.UseHttpsRedirection();
app.UseExceptionHandler();
app.UseApiVersioning();
app.MapEndpoints();
app.UseScalar();

await app.RunAsync();
