# MinimalApi.Endpoints [![Nuget](https://img.shields.io/nuget/v/IeuanWalker.MinimalApi.Endpoints?style=for-the-badge)](https://www.nuget.org/packages/IeuanWalker.MinimalApi.Endpoints) [![Nuget](https://img.shields.io/nuget/dt/IeuanWalker.MinimalApi.Endpoints?style=for-the-badge)](https://www.nuget.org/packages/IeuanWalker.MinimalApi.Endpoints) [![License: MIT](https://img.shields.io/badge/License-MIT-green.svg?style=for-the-badge)](https://opensource.org/licenses/MIT)


[![Read the docs](https://img.shields.io/badge/wiki-read%20the%20docs-blue?style=for-the-badge)](https://github.com/IeuanWalker/MinimalApi.Endpoints/wiki)

**MinimalApi.Endpoints** is a source generator that lets you write clean, class-based endpoints for .NET Minimal APIs - no runtime reflection, no boilerplate. Inspired by [FastEndpoints](https://github.com/FastEndpoints/FastEndpoints), but fully **source-generated** and gives you complete control.

## Why Use This?
- **🚀 Zero Runtime Overhead** - Everything is source-generated; no reflection, no hidden costs
- **🏗️ Clean Architecture** - Organise endpoints into testable, maintainable classes, which nudges you towards [REPR Design Pattern (Request-Endpoint-Response)](https://deviq.com/design-patterns/repr-design-pattern) and [Vertical Slice Architecture](https://www.milanjovanovic.tech/blog/vertical-slice-architecture)
- **🔧 Full Control**: Complete access to `RouteHandlerBuilder` and `RouteGroupBuilder` - it's just Minimal APIs underneath
- **📁 Better Organization** - [Group](https://github.com/IeuanWalker/MinimalApi.Endpoints/wiki/Grouping) endpoints clearly; perfect for large projects
- **✅ Built-In Validation** - Support [DataAnnotations](https://github.com/IeuanWalker/MinimalApi.Endpoints/wiki/Data-annotations) and [FluentValidation](https://github.com/IeuanWalker/MinimalApi.Endpoints/wiki/Fluent-validation) out of the box
- **🔓 No Lock-In** - Copy the generated code and remove the library anytime - you own the output

## How It Works _(The [wiki](https://github.com/IeuanWalker/MinimalApi.Endpoints/wiki/How-does-it-work) has the full explanation, but here's a quick overview)_
1. **Create endpoint classes** implementing one of the [endpoint interfaces](https://github.com/IeuanWalker/MinimalApi.Endpoints/wiki/Interfaces)
2. **Source generator scans** your assembly at compile time
3. **Generates extension methods** for dependency injection and route mapping
4. **Call the extensions** in your `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder();
builder.AddEndpoints(); // Registers endpoints as services

var app = builder.Build();
app.MapEndpoints();     // Maps all routes
```

That's it! Your endpoints are now mapped with zero runtime reflection.

## Full access to configure your endpoints
You have complete access to the `RouteHandlerBuilder`, so you can configure endpoints exactly like standard Minimal APIs:

```csharp
public class HelloWorldEndpoint : IEndpoint<RequestModel, ResponseModel>
{
    public static void Configure(RouteHandlerBuilder builder)
    {
        builder
            .Post("/HelloWorld")
            .RequestFromBody()
            .WithName("CreateHelloWorld") // optional, automatically set for you if not set
            .WithTags("HelloWorld") // optional, automatically set for you if not set
            .WithSummary("Creates a hello world")
            .WithDescription("Creates a new world in the system")
            .Produces<ResponseModel>(201)
            .Produces(400)
            .Produces(409)
            .ProducesValidationProblem()
            .RequireAuthorization()
            .RequireCors("MyPolicy")
            .CacheOutput(TimeSpan.FromMinutes(5));
    }

    public async Task<ResponseModel> Handle(RequestModel request, CancellationToken ct)
    {
        // Your implementation
        return new ResponseModel();
    }
}
```



📖 **[Read Full Documentation & Examples →](https://github.com/IeuanWalker/MinimalApi.Endpoints/wiki)**


## Contributing
Contributions are welcome! Please feel free to open an issue or submit a Pull Request _(start with an issue if it's a significant change)_.

## License
This project is licensed under the MIT License.
