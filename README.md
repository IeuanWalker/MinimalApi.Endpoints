# MinimalApi.Endpoints

A source generator that brings **clean, class-based endpoints** to ASP.NET Core Minimal APIs. Inspired by FastEndpoints, but **source-generated** and designed to be as minimal as possible.

## Why Use This?

- **🚀 Zero Runtime Overhead**: Source-generated code with no reflection
- **🏗️ Clean Architecture**: Organized, testable endpoint classes  
- **🔧 Full Control**: Complete access to `RouteHandlerBuilder` - it's just Minimal APIs underneath
- **📁 Better Organization**: Clear project structure with endpoint grouping

## Custom Endpoint Configuration

You have complete access to the `RouteHandlerBuilder`, so you can configure endpoints exactly like standard Minimal APIs:

```csharp
public class CreateUserEndpoint : IEndpoint<RequestModel, ResponseModel>
{
    public static void Configure(RouteHandlerBuilder builder)
    {
		builder
            .Post("/users")
            .RequestFromBody()
            .WithName("CreateUser")
            .WithTags("Users")
            .WithSummary("Creates a new user")
            .WithDescription("Creates a new user in the system")
            .Produces<ResponseModel>(201)
            .Produces(400)
            .Produces(409)
            .ProducesValidationProblem()
            .RequireAuthorization()
            .RequireCors("MyPolicy")
            .CacheOutput(TimeSpan.FromMinutes(5));
    }

    public async Task<ResponseModel> HandleAsync(RequestModel request, CancellationToken ct)
    {
        // Your implementation
        return new ResponseModel(newUser.Id);
    }
}
```

## How It Works

1. **Create endpoint classes** implementing one of the [endpoint interfaces](https://github.com/IeuanWalker/MinimalApi.Endpoints/wiki/Endpoints)
2. **Source generator scans** your assembly at compile time
3. **Generates extension methods** for dependency injection and route mapping
4. **Call the extensions** in your `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder();
builder.AddEndpointsFromYourAssembly(); // Registers endpoints as services

var app = builder.Build();
app.MapEndpointsFromYourAssembly();     // Maps all routes
```

That's it! Your endpoints are now mapped with zero runtime reflection.

---

📖 **[Full Documentation & Examples →](https://github.com/IeuanWalker/MinimalApi.Endpoints/wiki)**


## Contributing
Contributions are welcome! Please feel free to open an issue or submit a Pull Request _(start with an issue if its a significant change)_.

## License
This project is licensed under the MIT License.
