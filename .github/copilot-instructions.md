# GitHub Copilot Instructions for MinimalApi.Endpoints

## Project Overview

This is a **C# source generator library** for ASP.NET Core that brings clean, class-based endpoints to Minimal APIs. The project targets **.NET 10.0** and uses modern C# features including file-scoped namespaces, collection expressions, and nullable reference types.

### Repository Architecture

The repository is organized into three main categories:

#### **Core Library Projects**

1. **`src/IeuanWalker.MinimalApi.Endpoints/`** - Main library project
   - Interface definitions (`IEndpoint`, `IEndpointGroup`, `Validator`)
   - Extension methods and filters for consumers
   - Runtime utilities and base functionality
   - **No source generation logic** (handled separately in Generator project)

2. **`src/IeuanWalker.MinimalApi.Endpoints.Generator/`** - Source generator project
   - `EndpointGenerator.cs` - Main incremental source generator
   - Compile-time analysis of endpoint implementations
   - Generates registration extension methods (`AddEndpointsFrom{AssemblyName}()`, `MapEndpointsFrom{AssemblyName}()`)
   - Diagnostic reporting for configuration validation
   - Uses Roslyn's `IIncrementalGenerator` API for optimal performance

#### **Example & Demonstration**

3. **`example/ExampleApi/`** - Full-featured example application
   - Demonstrates all endpoint patterns and configurations
   - API versioning implementation
   - Scalar OpenAPI documentation integration
   - FluentValidation and DataAnnotations examples
   - Feature-based organization under `/Endpoints` folder

#### **Test Projects**

4. **`tests/IeuanWalker.MinimalApi.Endpoints.Tests/`** - Core library unit tests
5. **`tests/IeuanWalker.MinimalApi.Endpoints.Generator.Tests/`** - Source generator unit tests with snapshot testing
6. **`tests/ExampleApi.Tests/`** - Example API unit tests
7. **`tests/ExampleApi.IntegrationTests/`** - Comprehensive integration tests

**Testing Stack:**
- **xUnit** - Test framework
- **Shouldly** - Fluent assertions
- **Verify** (Verify.Xunit / Verify.SourceGenerators) - Snapshot testing for generated code
- **Microsoft.AspNetCore.Mvc.Testing** - Integration test hosting

## Core Architecture & Design Principles

### Zero-Runtime-Reflection Approach
- **Compile-time code generation** - All endpoint registration happens at build time
- **Performance optimized** - No reflection overhead during application startup or runtime
- **Incremental generation** - Only regenerates when relevant source code changes
- **Diagnostic validation** - Build-time validation of endpoint configurations with clear error messages

### Endpoint Interface System

The library provides four endpoint interfaces to cover all common scenarios:

```csharp
// Standard request/response endpoint
IEndpoint<TRequest, TResponse>

// Response-only endpoint (no request body)
IEndpointWithoutRequest<TResponse>

// Request-only endpoint (void return)
IEndpointWithoutResponse<TRequest>

// Simple endpoint (no request or response)
IEndpoint
```

**Required Implementation Pattern:**
```csharp
public class MyEndpoint : IEndpoint<RequestModel, ResponseModel>
{
    // 1. Static Configure method for route configuration
    public static void Configure(RouteHandlerBuilder builder)
    {
        builder.Get("/path").WithName("EndpointName");
    }
    
    // 2. Handle method with appropriate signature
    public async Task<ResponseModel> Handle(RequestModel request, CancellationToken ct)
    {
        // Implementation
    }
}
```

### Endpoint Groups

Groups provide shared configuration for related endpoints:

```csharp
public class UserEndpointGroup : IEndpointGroup
{
    public static RouteGroupBuilder Configure(WebApplication app)
    {
        return app.MapGroup("/api/v1/users")
                  .WithTags("Users")
                  .RequireAuthorization();
    }
}

// Usage in endpoints
public static void Configure(RouteHandlerBuilder builder)
{
    builder.Group<UserEndpointGroup>()
           .Get("/{id}");
}
```

### Generated Extension Methods

The source generator creates two main extension methods:

1. **`AddEndpointsFrom{AssemblyName}()`** - Registers endpoint services with DI container
2. **`MapEndpointsFrom{AssemblyName}()`** - Maps all endpoints to the application's route table

## Integration Testing Architecture

### Test Infrastructure Components

#### **ExampleApiWebApplicationFactory**
Custom `WebApplicationFactory<Program>` that:
- Overrides `ConfigureWebHost()` for test-specific service registration
- Replaces production services with test doubles (e.g., `ITodoStore` → `TestTodoStore`)
- Configures "Testing" environment
- Ensures isolated test execution

#### **TestTodoStore** 
In-memory implementation providing:
- Full CRUD operations via `Dictionary<int, Todo>`
- Test lifecycle methods: `Clear()`, `SeedData(params Todo[] todos)`
- Deterministic ID generation and timestamp handling
- Predictable state management between tests

#### **TestHelpers**
Static utility class with:
- `CreateJsonContent<T>()` - JSON serialization with custom options
- `GenerateRandomTitle()` - Test data generation
- `CreateTestTodo()` - Factory methods for test entities
- `WaitForConditionAsync()` - Async condition polling
- `AssertJsonContainsAsync()` - JSON assertion helpers

### Test Organization Strategy

Tests mirror the endpoint organization structure:

```
tests/ExampleApi.IntegrationTests/
├── Endpoints/
│   ├── Todos/               # Feature-based grouping
│   │   ├── GetAllTodosTests.cs
│   │   ├── GetTodoByIdTests.cs
│   │   ├── PostTodoTests.cs
│   │   ├── PostTodoDataAnnotationTests.cs
│   │   ├── PostTodoFluentValidationTests.cs
│   │   ├── PutTodoTests.cs
│   │   ├── PatchTodoTests.cs
│   │   ├── DeleteTodoTests.cs
│   │   └── GetExportTests.cs
│   └── WeatherForecast/
│       ├── WeatherForecastV1Tests.cs
│       └── WeatherForecastV2Tests.cs
├── Infrastructure/
│   └── InfrastructureTests.cs
└── Helpers/
    └── TestHelpers.cs
```

### Integration Test Patterns

#### **Standard Test Class Structure**
```csharp
public class EndpointTests : IClassFixture<ExampleApiWebApplicationFactory>
{
    readonly ExampleApiWebApplicationFactory _factory;
    readonly HttpClient _client;
    
    public EndpointTests(ExampleApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }
}
```

#### **Test Method Pattern**
```csharp
[Fact]
public async Task Method_Scenario_ExpectedResult()
{
    // Arrange - Setup test data
    TestTodoStore? todoStore = _factory.Services.GetRequiredService<ITodoStore>() as TestTodoStore;
    todoStore!.Clear();
    todoStore.SeedData(/* seed data */);
    
    // Act - Execute HTTP request
    HttpResponseMessage response = await _client.GetAsync("/endpoint");
    
    // Assert - Verify behavior
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    ResponseModel? result = await response.Content.ReadFromJsonAsync<ResponseModel>();
    result.ShouldNotBeNull();
    // Additional assertions...
}
```

#### **Comprehensive Test Coverage Areas**

1. **HTTP Protocol Compliance**
   - Status codes (200, 201, 400, 404, etc.)
   - Content-Type headers
   - Request/response serialization

2. **Validation Testing**
   - **DataAnnotations**: Required fields, string length, format validation
   - **FluentValidation**: Complex rules, nested objects, custom validators
   - Edge cases: null, empty, whitespace, malformed JSON

3. **CRUD Operations**
   - **GET**: Empty results, populated data, filtering, not found scenarios
   - **POST**: Valid creation, validation failures, conflict handling
   - **PUT**: Full updates, partial updates, not found scenarios
   - **PATCH**: JSON Patch format validation, partial modifications
   - **DELETE**: Successful deletion, not found scenarios, cascade effects

4. **API Features**
   - Versioning strategies (URL path, headers, query parameters)
   - OpenAPI documentation generation
   - Authentication/authorization integration
   - Error response consistency

#### **Integration Test Guidelines**

**✅ Best Practices:**
- Use `IClassFixture<ExampleApiWebApplicationFactory>` pattern
- Clear test state with `todoStore.Clear()` before each test
- Use `SeedData()` for predictable test scenarios
- Test both success and failure paths comprehensively
- Use `Shouldly` for readable, fluent assertions
- Verify HTTP status codes explicitly
- Test edge cases and error conditions
- Use `[Theory]` for parameterized testing scenarios

**❌ Anti-Patterns:**
- Sharing state between tests without cleanup
- Hardcoding volatile data (IDs, timestamps, GUIDs)
- Skipping error scenario testing
- Using production services in tests
- Ignoring HTTP protocol details
- Testing implementation details instead of behavior

## Source Generator Implementation

### Incremental Generator Architecture

The `EndpointGenerator` uses Roslyn's incremental generator pattern:

```csharp
[Generator]
public class EndpointGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Incremental pipeline setup
        IncrementalValuesProvider<TypeInfo> endpointProvider = context.SyntaxProvider
            .CreateSyntaxProvider(/* syntax predicate */, /* semantic transform */)
            .Where(/* filtering */);
            
        // Generate code when endpoints change
        context.RegisterSourceOutput(endpointProvider.Collect(), GenerateEndpointExtensions);
    }
}
```

### Generated Code Structure

The generator produces `EndpointExtensions.g.cs` containing:

```csharp
#nullable enable
// <auto-generated/>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.Extensions.DependencyInjection;

public static class EndpointExtensions
{
    public static IServiceCollection AddEndpointsFrom{AssemblyName}(this IServiceCollection services)
    {
        // Generated service registrations
        return services;
    }
}

namespace Microsoft.AspNetCore.Builder;

public static class EndpointExtensions  
{
    public static WebApplication MapEndpointsFrom{AssemblyName}(this WebApplication app)
    {
        // Generated endpoint mappings
        return app;
    }
}
```

### Diagnostic System

The generator reports validation errors using these diagnostic IDs:

| ID | Severity | Description |
|---|---|---|
| `MINAPI001` | Error | Missing HTTP verb in Configure method |
| `MINAPI002` | Error | Multiple HTTP verbs configured |
| `MINAPI003` | Error | No MapGroup configured when required |
| `MINAPI004` | Error | Multiple MapGroup calls |
| `MINAPI005` | Error | Multiple Group calls |
| `MINAPI006` | Warning | Unused endpoint group |
| `MINAPI007` | Error | Multiple validators for same type |
| `MINAPI008` | Error | Multiple request types configured |

## Snapshot Testing with Verify

### Overview
This project uses the `Verify` library for snapshot testing generated source code, ensuring output consistency and catching unintended changes.

### Workflow
1. **Test execution** compiles sample inputs and runs the source generator
2. **Verify captures** the generated output and diagnostics  
3. **Comparison** against stored `.verified.*` files in `tests/.../Snapshots/`
4. **Failures** create `.received.*` files showing differences

### File Conventions
- **Verified snapshots**: `.verified.cs` files in `Snapshots/` folders
- **Failed comparisons**: `.received.cs` files (git-ignored)
- **Multi-targeting**: Files include TFM suffix (e.g., `.DotNet10_0#...verified.cs`)

### Updating Snapshots
When generator changes are intentional:

1. **Run tests** to generate `.received.*` files
2. **Review changes** carefully - inspect diff content
3. **Replace verified files**:
   ```bash
   # Option 1: Rename received file
   mv SnapshotTest.received.cs SnapshotTest.verified.cs
   
   # Option 2: Copy content to existing verified file
   cp SnapshotTest.received.cs SnapshotTest.verified.cs
   ```
4. **Re-run tests** to ensure no remaining diffs
5. **Commit updated** `.verified.*` files

### Best Practices
- Keep snapshots focused and minimal
- Review all changes before accepting
- Include diagnostic output in snapshots
- Test incremental changes rather than large overhauls

## Coding Standards & Conventions

### C# Style Guidelines
- **File-scoped namespaces** (enforced by `.editorconfig`)
- **Tabs for indentation** (4-space display width)
- **Global using directives** preferred for common imports
- **Target-typed new expressions** (`new()` instead of `new Type()`)
- **Collection expressions** (`[]` instead of `new List<T>()`)
- **Required braces** for all control structures
- **Nullable reference types** enabled project-wide

### Source Generator Specific
- **Static methods** preferred for performance
- **Incremental patterns** using `IncrementalValuesProvider<T>`
- **Semantic model caching** to avoid repeated expensive queries
- **`IndentedTextBuilder`** for clean code generation with proper formatting
- **Comprehensive nullability annotations** for reliability

### Project Organization
- **Feature-based folders** for endpoints (`Endpoints/Todos/GetById/`)
- **Co-located models** (RequestModel/ResponseModel in same folder as endpoint)
- **Intentional analyzer suppressions** with `#pragma warning disable/restore`
- **Small related types** can share files (DTOs, records)

## Development Workflows

### Adding New Endpoint Interface Variants

1. **Define interface** in `src/IeuanWalker.MinimalApi.Endpoints/Interfaces/IEndpoint.cs`
2. **Update generator** in `EndpointGenerator.cs`:
   - Add type name constant (e.g., `fullIEndpointWithCustomLogic`)
   - Update `ExtractTypeInfo()` method for detection
   - Modify `GenerateEndpointExtensions()` for code generation
3. **Create example usage** in `ExampleApi` project
4. **Add comprehensive tests** including integration tests
5. **Update documentation** and patterns

### Adding New Example Endpoints

1. **Create endpoint class** in appropriate `example/ExampleApi/Endpoints/` subfolder
2. **Implement required interface** with proper `Configure()` and `Handle()` methods
3. **Add integration test class** in `tests/ExampleApi.IntegrationTests/Endpoints/`
4. **Test comprehensively**: success cases, validation, edge cases, error handling
5. **Follow naming conventions** and organizational patterns

### Adding Diagnostic Rules

1. **Define `DiagnosticDescriptor`** in `EndpointGenerator.cs`
2. **Document in analyzer releases**:
   - New rules → `AnalyzerReleases.Unshipped.md`  
   - Shipped rules → `AnalyzerReleases.Shipped.md`
3. **Report diagnostics** using `context.ReportDiagnostic()` during generation
4. **Follow ID pattern**: `MINAPI###` with descriptive messages
5. **Include fix suggestions** where possible

### Modifying Generated Code

1. **Update `GenerateEndpointExtensions()`** method in `EndpointGenerator.cs`
2. **Use `IndentedTextBuilder`** for clean, properly indented output
3. **Include required headers**:
   - `#nullable enable`
   - `// <auto-generated/>`
   - Source generator attribution comments
4. **Test thoroughly** with snapshot tests
5. **Update verified snapshots** following established workflow

## Build & Test Commands

### Development Build
```bash
# Restore packages and build solution
dotnet restore
dotnet build

# Build specific project
dotnet build src/IeuanWalker.MinimalApi.Endpoints.Generator/
```

### Testing Commands
```bash
# Run all tests
dotnet test

# Run specific test projects
dotnet test tests/IeuanWalker.MinimalApi.Endpoints.Generator.Tests/
dotnet test tests/ExampleApi.IntegrationTests/

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"
dotnet test --logger trx --results-directory TestResults/

# Run tests with specific filter
dotnet test --filter "FullyQualifiedName~IntegrationTests"
```

### Release Preparation
```bash
# Pack NuGet packages
dotnet pack --configuration Release

# Validate package contents
dotnet tool install -g dotnet-validate
validate package src/IeuanWalker.MinimalApi.Endpoints/bin/Release/*.nupkg
```

## Example Implementations

### Complete Endpoint Example
```csharp
using IeuanWalker.MinimalApi.Endpoints;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ExampleApi.Endpoints.Users.GetById;

public record RequestModel(int Id);
public record ResponseModel(int Id, string Name, string Email);

public class GetUserByIdEndpoint : IEndpoint<RequestModel, Results<Ok<ResponseModel>, NotFound>>
{
    readonly IUserService _userService;
    
    public GetUserByIdEndpoint(IUserService userService)
    {
        _userService = userService;
    }
    
    public static void Configure(RouteHandlerBuilder builder)
    {
        builder
            .Group<UserEndpointGroup>()
            .Get("/{id:int}")
            .WithName("GetUserById")
            .WithSummary("Retrieve user by ID")
            .WithDescription("Returns user details for the specified ID")
            .Produces<ResponseModel>(200, "application/json")
            .Produces(404, "User not found")
            .WithOpenApi();
    }
    
    public async Task<Results<Ok<ResponseModel>, NotFound>> Handle(
        RequestModel request, 
        CancellationToken cancellationToken)
    {
        User? user = await _userService.GetByIdAsync(request.Id, cancellationToken);
        
        return user is null 
            ? TypedResults.NotFound() 
            : TypedResults.Ok(new ResponseModel(user.Id, user.Name, user.Email));
    }
}
```

### Complete Integration Test Example
```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace ExampleApi.IntegrationTests.Endpoints.Users;

public class GetUserByIdTests : IClassFixture<ExampleApiWebApplicationFactory>
{
    readonly ExampleApiWebApplicationFactory _factory;
    readonly HttpClient _client;

    public GetUserByIdTests(ExampleApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetUserById_WithExistingUser_ReturnsUser()
    {
        // Arrange
        TestUserStore? userStore = _factory.Services.GetRequiredService<IUserStore>() as TestUserStore;
        userStore!.Clear();
        
        User testUser = TestHelpers.CreateTestUser("Jane Doe", "jane@example.com");
        userStore.SeedData(testUser);

        // Act
        HttpResponseMessage response = await _client.GetAsync($"/api/v1/users/{testUser.Id}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        
        ResponseModel? result = await response.Content.ReadFromJsonAsync<ResponseModel>();
        result.ShouldNotBeNull();
        result.Id.ShouldBe(testUser.Id);
        result.Name.ShouldBe("Jane Doe");
        result.Email.ShouldBe("jane@example.com");
    }

    [Theory]
    [InlineData(999)]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetUserById_WithNonExistentUser_ReturnsNotFound(int userId)
    {
        // Arrange
        TestUserStore? userStore = _factory.Services.GetRequiredService<IUserStore>() as TestUserStore;
        userStore!.Clear();

        // Act
        HttpResponseMessage response = await _client.GetAsync($"/api/v1/users/{userId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("12.34")]
    [InlineData("")]
    public async Task GetUserById_WithInvalidId_ReturnsBadRequest(string invalidId)
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync($"/api/v1/users/{invalidId}");

        // Assert  
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
```

## Key Dependencies & Tools

### Core NuGet Packages
- **`Microsoft.CodeAnalysis.CSharp`** - Roslyn source generation APIs
- **`Microsoft.AspNetCore.App`** - ASP.NET Core framework reference
- **`Microsoft.Extensions.DependencyInjection.Abstractions`** - DI container abstractions

### Testing Dependencies
- **`Microsoft.AspNetCore.Mvc.Testing`** - Integration test hosting
- **`xunit`** / **`xunit.runner.visualstudio`** - Test framework and runner
- **`Shouldly`** - Fluent assertion library
- **`Verify.Xunit`** / **`Verify.SourceGenerators`** - Snapshot testing
- **`coverlet.collector`** - Code coverage collection

### Development Tools
- **`.editorconfig`** - Code style enforcement
- **`Directory.Build.props`** - Shared MSBuild properties
- **Roslyn Analyzers** - Static code analysis
- **NuGet packaging** - Library distribution
