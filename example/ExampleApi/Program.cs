using ExampleApi.Infrastructure;
using ExampleApi.Services;
using ExampleApi;
using IeuanWalker.MinimalApi.Endpoints;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.AddApiVersioning();
builder.AddEndpointsFromExampleApi();
builder.Services.AddSingleton<ITodoStore, InMemoryTodoStore>();
builder.AddScalar();

WebApplication app = builder.Build();
app.UseHttpsRedirection();
app.UseApiVersioning();
app.MapEndpointsFromExampleApi();
app.UseScalar();
app.UseDefaultGlobalExceptionHandler();

await app.RunAsync();
