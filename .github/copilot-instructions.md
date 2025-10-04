# GitHub Copilot Instructions for MinimalApi.Endpoints

## Project Overview

This is a **C# source generator library** for ASP.NET Core that brings clean, class-based endpoints to Minimal APIs. The project targets **.NET 10.0** and uses modern C# features.

### Project Structure

The repository consists of three main projects:

1. **`src/IeuanWalker.MinimalApi.Endpoints/`** - The main library project containing:
   - Interface definitions (`IEndpoint`, `IEndpointGroup`, `Validator`)
   - Extension methods and filters for users to consume
   - No source generation logic (that's in the Generator project)

2. **`src/IeuanWalker.MinimalApi.Endpoints.Generator/`** - The source generator project:
   - `EndpointGenerator.cs` - Main incremental source generator implementation
   - Analyzes user code at compile-time for classes implementing endpoint interfaces
   - Generates extension methods (`AddEndpointsFrom{AssemblyName}()` and `MapEndpointsFrom{AssemblyName}()`)
   - Reports diagnostics for invalid endpoint configurations
   - Uses Roslyn's incremental generator API

3. **`example/ExampleApi/`** - Example ASP.NET Core project demonstrating usage:
   - Shows various endpoint patterns and configurations
   - Includes versioning, Scalar API documentation, and validators
   - Organized by feature with `/Endpoints` folder structure

## Architecture & Design Principles

### Source Generator Approach
- **Zero runtime reflection** - all registration code is generated at compile-time
- **Incremental generation** - uses `IIncrementalGenerator` for optimal performance
- **Diagnostic reporting** - validates endpoint configuration and reports clear errors
- Generates two main extension methods for dependency injection and route mapping

### Endpoint Interfaces
Users implement one of these interfaces in the library project:
- `IEndpoint<TRequest, TResponse>` - Standard endpoint with request and response
- `IEndpointWithoutRequest<TResponse>` - No request body
- `IEndpointWithoutResponse<TRequest>` - No response (void)
- `IEndpoint` - No request or response

Each endpoint must implement:
- Static `Configure(RouteHandlerBuilder builder)` method for route configuration
- `Handle()` method with appropriate signature based on interface

### Endpoint Groups
- Implement `IEndpointGroup` interface
- Static `Configure(WebApplication app)` returns `RouteGroupBuilder`
- Endpoints reference groups via `.Group<TEndpointGroup>()` in their Configure method

## Coding Standards

### General C# Style
- **File-scoped namespaces** (enforced by `.editorconfig`)
- **Tabs for indentation** (size 4)
- **Global using directives** preferred
- **Target-typed new** expressions where possible
- **Collection expressions** (`[]`) for collection initialization
- **Required braces** for all control flow statements

### Source Generator Specific
- Use `static` methods where possible for generator performance
- Leverage `IncrementalValuesProvider` and `IncrementalValueProvider` patterns
- Cache and reuse semantic model queries
- Use `IndentedTextBuilder` helper class for generating properly formatted code
- Always include nullable reference type annotations

### Code Organization
- Keep related types in the same file only when they're small DTOs/records
- Use `#pragma warning disable/restore` for intentional analyzer suppressions
- Endpoint classes should be in feature-based folders (e.g., `Endpoints/Todos/GetById/`)
- Each endpoint typically has its own RequestModel and ResponseModel in the same folder

## Common Tasks

### Adding a New Endpoint Interface Variant
1. Add interface to `src/IeuanWalker.MinimalApi.Endpoints/Interfaces/IEndpoint.cs`
2. Update `EndpointGenerator.cs` to handle the new interface type
3. Add constant for full type name (e.g., `fullIEndpointXxx`)
4. Update `ExtractTypeInfo()` to detect the new interface
5. Update code generation in `GenerateEndpointExtensions()` method
6. Add example usage in the ExampleApi project

### Adding Diagnostic Rules
1. Define `DiagnosticDescriptor` static field in `EndpointGenerator.cs`
2. Add rule to `AnalyzerReleases.Shipped.md` or `AnalyzerReleases.Unshipped.md`
3. Report diagnostic using `context.ReportDiagnostic()` during generation
4. Follow ID pattern: `MINAPI###` (see existing diagnostics)

### Modifying Generated Code
- Update `GenerateEndpointExtensions()` in `EndpointGenerator.cs`
- Use `IndentedTextBuilder` for clean, indented output
- Generated file is always named `EndpointExtensions.g.cs`
- Include `#nullable enable` and source generator attribution comments

## Building & Testing

### Build Commands
```bash
dotnet restore
dotnet build
```

### Testing Source Generator Changes
Since there's no dedicated test project, test changes by:
1. Build the solution to trigger source generation
2. Inspect generated code in `example/ExampleApi/obj/.../EndpointExtensions.g.cs`
3. Run the ExampleApi project to verify runtime behavior
4. Check for any diagnostic warnings/errors in build output

### Diagnostic IDs
Current diagnostic rules (from `AnalyzerReleases.Shipped.md`):
- `MINAPI001` - Missing HTTP verb in Configure method
- `MINAPI002` - Multiple HTTP verbs configured
- `MINAPI003` - No MapGroup configured
- `MINAPI004` - Multiple MapGroup calls
- `MINAPI005` - Multiple Group calls
- `MINAPI006` - Unused endpoint group (warning)
- `MINAPI007` - Multiple validators for same type
- `MINAPI008` - Multiple request types configured

## Key Patterns & Anti-Patterns

### ✅ DO
- Use file-scoped namespaces
- Use collection expressions `[]` instead of `new List<T>()`
- Use target-typed `new()` for type instantiation
- Keep generated code clean and readable
- Cache semantic model lookups in source generators
- Use incremental generator patterns for performance
- Provide clear diagnostic messages with suggested fixes

### ❌ DON'T
- Don't use reflection in the library code
- Don't modify user's code - only generate new files
- Don't create non-incremental generators
- Don't generate code that requires additional runtime dependencies
- Don't ignore semantic model nullability
- Don't generate code without proper indentation

## Example Endpoint Pattern

```csharp
using IeuanWalker.MinimalApi.Endpoints;

namespace MyApp.Endpoints.Users.GetById;

public class GetUserEndpoint : IEndpoint<RequestModel, Results<Ok<ResponseModel>, NotFound>>
{
	readonly IUserService _userService;
	
	public GetUserEndpoint(IUserService userService)
	{
		_userService = userService;
	}
	
	public static void Configure(RouteHandlerBuilder builder)
	{
		builder
			.Group<UserEndpointGroup>()
			.Get("/{id}")
			.WithName("GetUserById")
			.WithSummary("Get user by ID")
			.Produces<ResponseModel>(200)
			.Produces(404);
	}
	
	public async Task<Results<Ok<ResponseModel>, NotFound>> Handle(RequestModel request, CancellationToken ct)
	{
		var user = await _userService.GetByIdAsync(request.Id, ct);
		return user is null 
			? TypedResults.NotFound() 
			: TypedResults.Ok(ResponseModel.FromUser(user));
	}
}
```

## Dependencies & Tools

### Main NuGet Packages
- `Microsoft.CodeAnalysis.CSharp` - Roslyn source generation APIs
- ASP.NET Core Minimal APIs (in library project)

### Build Requirements
- .NET 10.0 SDK
- C# 13 language features

## Additional Resources

- [Project Wiki](https://github.com/IeuanWalker/MinimalApi.Endpoints/wiki) - Comprehensive documentation
- [FastEndpoints](https://github.com/FastEndpoints/FastEndpoints) - Inspiration for this library
- [Roslyn Source Generators Cookbook](https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.cookbook.md)

## Contributing Guidelines

When making changes:
1. Follow the existing code style (enforced by `.editorconfig`)
2. Test with the ExampleApi project
3. Update documentation if adding new features
4. Add diagnostic rules to the shipped/unshipped markdown files
5. Keep changes minimal and focused
6. Ensure backward compatibility when possible
