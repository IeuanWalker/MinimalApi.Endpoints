# MinimalApi.Endpoints

A source generator that provides a structured way to create ASP.NET Core Minimal API endpoints using classes. FastEndpoints inspired this library, but it is source-generated and designed to be as minimal as possible.

## Features

- **Source Generated**: Zero runtime reflection, all endpoint mapping is generated at compile time
- **Type-Safe**: Strongly typed request/response models with compile-time validation
- **Minimal Boilerplate**: Clean class-based endpoint definitions
- **Flexible**: Support for various endpoint patterns (with/without request/response)
- **Performance**: Optimised generated code with minimal overhead
- **Request Binding Control**: Fine-grained control over how request parameters are bound
- **Automatic Validation**: Built-in support for DataAnnotations and FluentValidation

## Quick Start

### Installation

Add the NuGet package to your project:

```xml
<PackageReference Include="IeuanWalker.MinimalApi.Endpoints" Version="1.0.0" />
```

### Basic Setup

1. **Configure your Program.cs**:

```csharp
using MyApi;

var builder = WebApplication.CreateBuilder(args);

// Register all endpoints from the current assembly - extension method is generated after you create your first endpoint
builder.AddEndpointsFromMyApi();

var app = builder.Build();

// Map all endpoints from the current assembly - extension method is generated after you create your first endpoint
app.MapEndpointsFromMyApi();

app.Run();
```

## Creating Endpoints

### Endpoint with Request and Response

```csharp
using IeuanWalker.MinimalApi.Endpoints;

public class GetUserEndpoint : IEndpoint<RequestModel, ResponseModel>
{
    public static void Configure(RouteHandlerBuilder builder)
    {
        builder
            .Get("/users/{id}")
            .RequestAsParameters();
    }

    public async Task<GetUserResponseModel> HandleAsync(RequestModel request, CancellationToken ct)
    {
        // Your endpoint logic here
        return new ResponseModel { Name = "John Doe", Id = request.Id };
    }
}
```

### Endpoint without Request

```csharp
public class GetAllUsersEndpoint : IEndpointWithoutRequest<List<User>>
{
    public static void Configure(RouteHandlerBuilder builder)
    {
        builder.Get("/users");
    }

    public async Task<List<User>> HandleAsync(CancellationToken ct)
    {
        // Return list of users
        return await GetUsersAsync();
    }
}
```

### Endpoint without Response

```csharp
public class DeleteUserEndpoint : IEndpointWithoutResponse<RequestModel>
{
    public static void Configure(RouteHandlerBuilder builder)
    {
        builder
            .Delete("/users/{id}")
            .RequestAsParameters();
    }

    public async Task HandleAsync(RequestModel request, CancellationToken ct)
    {
        // Delete user logic
        await DeleteUserAsync(request.Id);
    }
}
```

### Endpoint without Request or Response

```csharp
public class TriggerJobEndpoint : IEndpoint
{
    public static void Configure(RouteHandlerBuilder builder)
    {
        builder.Get("/trigger");
    }

    public async Task HandleAsync(CancellationToken ct)
    {
        await ExecuteJob();
    }
}
```

## Request Validation

MinimalApi.Endpoints provides automatic validation support for both **DataAnnotations** and **FluentValidation**. Validation is applied automatically based on the attributes or validators you define.

In your startup, ensure you registered the AddProblemDetails service - 
```csharp
builder.Services.AddProblemDetails();
```

Both validation methods return the `400 Bad Request` status code with a `ValidationProblem` response containing detailed error information if validation fails.

Both validation methods can be used independently or together. If both are present for the same type, FluentValidation takes precedence.

> Its recommeded to use FluentValidation over DataAnnoations as the requried attribute to enable dataAnnoation is _`is for evaluation purposes only and is subject to change or removal in future updates`_

### DataAnnotations Validation

Use standard DataAnnotations attributes on your request models. You must also add the `[ValidatableType]` attribute to enable validation. This is what .net uses to source generate the validation logic.

>_If you were using minimal api's directly you wouldn't need to add this as the source generator automatically runs when it finds minimal api endpoints in your code, the limitation here is source generator cant run on top/ chain other source generators. As this library source generates the minimal api endpoints the automatic generator .net built doesnt see them._

>_So this attribute tells the .NET source generator that this type needs the logic generated for it, even if it cant find the endpoint that uses it_

```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Validation;

#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
[ValidatableType]// Required for DataAnnotations validation
#pragma warning restore ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
public class CreateUserRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Range(18, 120)]
    public int Age { get; set; }

    [Phone]
    public string? PhoneNumber { get; set; }
}
```

### FluentValidation Support

Create validators by implementing `AbstractValidator<T>` for your request models. The source generator will automatically detect and wire up the validators.

```csharp
using FluentValidation;

public class CreateUserRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
    public string? PhoneNumber { get; set; }
}

// Validator must be in the same assembly as your endpoints
public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Age)
            .InclusiveBetween(18, 120)
            .WithMessage("Age must be between 18 and 120");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?[1-9]\d{1,14}$")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber))
            .WithMessage("Please provide a valid phone number");
    }
}

public class CreateUserEndpoint : IEndpoint<CreateUserRequest, UserResponse>
{
    public static void Configure(RouteHandlerBuilder builder)
    {
        builder
            .Post("/users")
            .RequestFromBody()
            .WithSummary("Create a new user")
            .ProducesValidationProblem(); // Documents validation responses
    }

    public async Task<UserResponse> HandleAsync(CreateUserRequest request, CancellationToken ct)
    {
        // Request is automatically validated using FluentValidation before reaching this method
        // If validation fails, a 400 Bad Request with detailed validation errors is returned
        return await CreateUserAsync(request, ct);
    }
}
```

### Disabling Validation

You can disable automatic validation for specific endpoints using the `DisableValidation()` extension method:

```csharp
public class CreateUserEndpoint : IEndpoint<CreateUserRequest, UserResponse>
{
    public static void Configure(RouteHandlerBuilder builder)
    {
        builder
            .Post("/users/unsafe")
            .RequestFromBody()
            .DisableValidation();
    }

    public async Task<UserResponse> HandleAsync(CreateUserRequest request, CancellationToken ct)
    {
        // Manual validation can be performed here if needed
        // No automatic validation is applied
        return await CreateUserAsync(request, ct);
    }
}
```

## Advanced Configuration

### HTTP Verbs

The source generator supports all major HTTP verbs through the `Configure` method:

- `builder.Get(pattern)`
- `builder.Post(pattern)`
- `builder.Put(pattern)`
- `builder.Patch(pattern)`
- `builder.Delete(pattern)`

### Request Binding Control

The library provides extension methods to control how request parameters are bound, giving you fine-grained control over the source generator's behavior:

```csharp
public class UpdateUserEndpoint : IEndpoint<RequestModel, ResponseModel>
{
    public static void Configure(RouteHandlerBuilder builder)
    {
        builder
            .Put("/users/{id}")
            .RequestAsParameters(); // Treats RequestModel as [AsParameters]
    }

    public async Task<ResponseModel> HandleAsync(RequestModel request, CancellationToken ct)
    {
        // Implementation
        return new ResponseModel(request.Id);
    }
}
```

#### Available Request Binding Methods

| Method | Description |
|--------|-------------|
| `RequestFromBody()` | Treats the request model as `[FromBody]` |
| `RequestFromRoute(string? name = null)` | Treats the request model as `[FromRoute]` |
| `RequestFromQuery(string? name = null)` | Treats the request model as `[FromQuery]` |
| `RequestFromHeader(string? name = null)` | Treats the request model as `[FromHeader]` |
| `RequestFromForm(string? name = null)` | Treats the request model as `[FromForm]` |
| `RequestAsParameters()` | Treats the request model as `[AsParameters]` |
| `DisableValidation()` | Disables automatic validation for this endpoint |

*Note: `RequestAsParameters()` is for mixed parameter sources (route + query + headers).*
### Custom Endpoint Configuration

The `Configure` method gives you full access to the `RouteHandlerBuilder`, allowing you to configure the endpoint exactly as you would with standard ASP.NET Core minimal APIs. This means you can use any configuration method available on `RouteHandlerBuilder`:

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
            .CacheOutput(TimeSpan.FromMinutes(5))
            .AddEndpointFilter<ValidationFilter>()
            .WithMetadata(new SwaggerOperationAttribute("Create User", "Creates a new user"));
    }

    public async Task<ResponseModel> HandleAsync(RequestModel request, CancellationToken ct)
    {
        // Implementation
        return new ResponseModel(newUser.Id);
    }
}
```

Since you have complete access to the `RouteHandlerBuilder`, you can configure:
- **OpenAPI/Swagger documentation** with summaries, descriptions, and response types
- **Authentication and authorization** requirements
- **CORS policies** and caching strategies
- **Endpoint filters** for cross-cutting concerns
- **Rate limiting** and other middleware
- **Custom metadata** for documentation or tooling
- **Response compression** and content negotiation
- Any other configuration available in minimal APIs

### Multiple Response Types

You can return different response types based on the operation result using ASP.NET Core's `Results<T1, T2, ...>` type:

```csharp
using Microsoft.AspNetCore.Http.HttpResults;

public class CreateTodoEndpoint : IEndpoint<RequestModel, Results<Ok<ResponseModel>, Conflict>>
{
    private readonly ITodoService _todoService;

    public CreateTodoEndpoint(ITodoService todoService)
    {
        _todoService = todoService;
    }

    public static void Configure(RouteHandlerBuilder builder)
    {
        builder
            .Post("/api/todos")
            .WithSummary("Create a new todo")
            .WithDescription("Creates a new todo item")
            .Produces<ResponseModel>(201)
            .Produces(409); // Conflict
    }

    public async Task<Results<Ok<ResponseModel>, Conflict>> HandleAsync(RequestModel request, CancellationToken ct)
    {
        // Check if todo with same title already exists
        if (await _todoService.ExistsAsync(request.Title, ct))
        {
            return TypedResults.Conflict();
        }

        var createdTodo = await _todoService.CreateAsync(request, ct);
        return TypedResults.Ok(ResponseModel.FromTodo(createdTodo));
    }
}
```

This approach provides:
- **Type Safety**: All possible response types are known at compile time
- **OpenAPI Documentation**: Automatically generates correct API documentation with all possible responses
- **Clear Intent**: Makes it explicit what responses the endpoint can return

## Project Structure

Helps organise your endpoints in a clean folder structure where each endpoint is contained within its own folder:

```
MyApi/
├── Endpoints/
│   ├── Users/
│   │   ├── Get/
│   │   │   ├── GetUsersEndpoint.cs
│   │   │   ├── RequestModel.cs
│   │   │   └── ResponseModel.cs
│   │   ├── Post/
│   │   │   ├── PostUserEndpoint.cs
│   │   │   ├── RequestModel.cs
│   │   │   └── ResponseModel.cs
│   │   ├── Put/
│   │   │   ├── PutUserEndpoint.cs
│   │   │   ├── RequestModel.cs
│   │   │   └── ResponseModel.cs
│   │   └── Delete/
│   │       ├── DeleteUserEndpoint.cs
│   │       ├── RequestModel.cs
│   │       └── ResponseModel.cs
│   └── Products/
│       ├── Get/
│       │   ├── GetProductEndpoint.cs
│       │   ├── RequestModel.cs
│       │   └── ResponseModel.cs
│       └── Post/
│           ├── PostProductEndpoint.cs
│           ├── RequestModel.cs
│           └── ResponseModel.cs
└── Program.cs
```

## How It Works

1. **Source Generation**: The source generator scans your assembly for classes implementing `IEndpointBase`
2. **Code Generation**: It generates extension methods (`AddEndpointsFromYourAssembly` and `MapEndpointsFromYourAssembly`)
3. **Dependency Injection**: Endpoints are automatically registered as scoped services
4. **Route Mapping**: HTTP verbs and patterns are extracted from the `Configure` method and mapped to your handlers
5. **Request Binding**: Extension methods control how request parameters are bound in the generated code
6. **Validation Integration**: Automatically detects and integrates DataAnnotations and FluentValidation

### Generated Code Example

For each assembly containing endpoints, the generator creates:

```csharp
public static class EndpointExtensions
{
    public static IHostApplicationBuilder AddEndpointsFromMyApi(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<GetUserEndpoint>();
        builder.Services.AddScoped<CreateUserEndpoint>();
        // ... other endpoints
        return builder;
    }

    public static WebApplication MapEndpointsFromMyApi(this WebApplication app)
    {
        // GET: /users/{id} - with RequestAsParameters()
        RouteHandlerBuilder getUserEndpoint = app
            .MapGet("/users/{id}", async ([AsParameters] RequestModel request, [FromServices] GetUserEndpoint endpoint, CancellationToken ct) =>
            {
                return await endpoint.HandleAsync(request, ct);
            })
            .WithName("GetUserEndpoint");
        
        GetUserEndpoint.Configure(getUserEndpoint);
        
        // POST: /users - with RequestFromBody() and FluentValidation
        RouteHandlerBuilder createUserEndpoint = app
            .MapPost("/users", async ([FromBody] CreateUserRequest request, [FromServices] CreateUserEndpoint endpoint, CancellationToken ct) =>
            {
                return await endpoint.HandleAsync(request, ct);
            })
            .WithName("CreateUserEndpoint")
            .AddEndpointFilter<FluentValidationFilter<CreateUserRequest>>();
        
        CreateUserEndpoint.Configure(createUserEndpoint);
        
        // ... other endpoint mappings
        return app;
    }
}
```

## Why Use This?

- **Compile-Time Safety**: Catch routing errors at build time, not runtime
- **Clean Architecture**: Separate your endpoint logic into focused classes
- **Performance**: No reflection overhead - everything is source-generated
- **Maintainable**: Easy to find, test, and modify specific endpoints
- **Familiar**: If you've used FastEndpoints, you'll feel right at home
- **Minimal**: Less boilerplate than controller-based approaches
- **Flexible Binding**: Fine-grained control over parameter binding behavior
- **Automatic Validation**: Built-in support for popular validation libraries
- **Type-Safe Validation**: Compile-time validation integration with zero runtime overhead

## Interface Reference

| Interface | Use Case | Request | Response |
|-----------|----------|---------|----------|
| `IEndpoint` | Simple endpoints with no request or response | ❌ | ❌ |
| `IEndpoint<TRequest, TResponse>` | Standard endpoint with both request and response | ✅ | ✅ |
| `IEndpointWithoutRequest<TResponse>` | Query endpoints that return data without input | ❌ | ✅ |
| `IEndpointWithoutResponse<TRequest>` | Command endpoints that don't return data | ✅ | ❌ |

## Contributing

Contributions are welcome! Please feel free to open an issue or submit a Pull Request (start with an issue if its a significant change).

## License

This project is licensed under the MIT License.
