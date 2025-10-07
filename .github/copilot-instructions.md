# GitHub Copilot Instructions for MinimalApi.Endpoints

## Project Overview

This is a **C# source generator library** for ASP.NET Core that brings clean, class-based endpoints to Minimal APIs. The project targets **.NET 10.0** and uses modern C# features.

### Project Structure

The repository consists of three main projects and four test projects:

**Main Projects:**

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

**Test Projects:**

4. **`tests/IeuanWalker.MinimalApi.Endpoints.Tests/`** - Unit tests for the main library (4 tests)

5. **`tests/IeuanWalker.MinimalApi.Endpoints.Generator.Tests/`** - Unit tests for the source generator (7 tests)

6. **`tests/ExampleApi.Tests/`** - Unit tests for ExampleApi (15 tests)

All test projects use **xUnit** as the test framework, **Shouldly** for assertions, and **NSubstitute** for mocking.

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

### Running Tests
```bash
# Run all tests
dotnet test

# Run tests with code coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/IeuanWalker.MinimalApi.Endpoints.Tests
```

### Test Projects

The solution includes **3 comprehensive test projects** with 26 tests:

1. **`tests/IeuanWalker.MinimalApi.Endpoints.Tests/`** - Unit tests for the main library
   - Tests for `FluentValidationFilter` covering all scenarios
   - Uses xUnit, Shouldly, and NSubstitute
   - 4 tests covering constructor validation, pass/fail scenarios

2. **`tests/IeuanWalker.MinimalApi.Endpoints.Generator.Tests/`** - Unit tests for the source generator
   - **Snapshot tests** for generated code (10 comprehensive scenarios using Verify)
   - String utility extension tests (Sanitize, ToLowerFirstLetter, ToUpperFirstLetter)
   - Uses xUnit, Shouldly, and **Verify** for snapshot testing
   - 17+ tests total

3. **`tests/ExampleApi.Tests/`** - Unit tests for ExampleApi
   - Tests for endpoint handlers with mocked dependencies
   - Complete test coverage for `InMemoryTodoStore` (all CRUD operations)
   - 15 tests covering endpoints and services

### Test Documentation

Comprehensive testing documentation is available in the `tests/` directory:
- **`tests/README.md`** - Overview of test projects and how to run tests
- **`tests/CONTRIBUTING.md`** - Guide for adding new tests with patterns and examples
- **`tests/TEST_SUMMARY.md`** - Detailed coverage statistics and recommendations

### Testing Best Practices

When writing tests for this project:
- Use **Shouldly** for assertions (e.g., `result.ShouldBe(expected)`, `result.ShouldNotBeNull()`)
- Use **Verify** for snapshot testing source generator output
- Follow **Arrange-Act-Assert** pattern
- Use descriptive test names: `MethodName_Scenario_ExpectedBehavior`
- Use **NSubstitute** for mocking dependencies
- Keep tests independent and isolated
- Test edge cases (null values, empty collections, not found scenarios)

### Snapshot Testing for Source Generators

The project uses **Verify** (https://github.com/VerifyTests/Verify) for snapshot testing the source generator:

**Key Files:**
- `SnapshotTests.cs` - Contains snapshot test scenarios for different endpoint configurations
- `TestHelper.cs` - Helper for running the generator and creating compilations
- `ModuleInitializer.cs` - Initializes Verify settings with `VerifySourceGenerators.Initialize()`

**How It Works:**
1. Test creates sample source code with endpoint definitions
2. `TestHelper.Verify()` runs the source generator on the code
3. Verify captures the generated output and stores it in `/Snapshots` directory
4. On subsequent runs, Verify compares new output with stored snapshots
5. If output changes, test fails and shows diff

**Running Snapshot Tests:**
```bash
dotnet test tests/IeuanWalker.MinimalApi.Endpoints.Generator.Tests
```

**Updating Snapshots:**
When generated code legitimately changes:
1. Review the diff in the test output
2. If changes are expected, update snapshots by accepting the new `.received.` files
3. Commit the updated `.verified.` snapshot files to version control

**Snapshot Test Scenarios:**
- Simple endpoints (`IEndpoint<TRequest, TResponse>`)
- Endpoints without request/response
- Endpoints with groups
- Endpoints with validators
- Multiple endpoints
- Complex group hierarchies
- Different request binding types (FromBody, AsParameters)

### CI/CD Integration

Tests run automatically via GitHub Actions on:
- Every push to master branch
- Every pull request to master
- Scheduled runs (every 3 months)

The workflow includes:
- **Test Execution**: All test projects run with `dotnet test`
- **Code Coverage**: Collected using XPlat Code Coverage
- **Coverage Enforcement**: Build fails if coverage drops below 80% (configurable via `MINIMUM_COVERAGE` env var)
- **PR Comments**: Automatic coverage reports posted to pull requests

### Code Coverage Thresholds

- **Minimum Required**: 80% (enforced - build fails below this)
- **Target**: 95% (warning level)
- **Tracked Metrics**: Both line and branch coverage

To adjust the minimum coverage threshold, update `MINIMUM_COVERAGE` in `.github/workflows/build.yml`.

### Testing Source Generator Changes

When modifying the source generator:
1. Update or add **snapshot tests** in `IeuanWalker.MinimalApi.Endpoints.Generator.Tests/SnapshotTests.cs`
2. Run generator tests: `dotnet test tests/IeuanWalker.MinimalApi.Endpoints.Generator.Tests`
3. Review snapshot diffs - if output changed, verify it's correct then update snapshots
4. Verify generated code in `example/ExampleApi/obj/.../EndpointExtensions.g.cs`
5. Run the ExampleApi project to verify runtime behavior
6. Check for any diagnostic warnings/errors in build output
7. Ensure code coverage remains above threshold

**Adding New Snapshot Tests:**
```csharp
[Fact]
public Task GeneratesEndpointExtensions_ForMyNewScenario()
{
    const string source = """
        using IeuanWalker.MinimalApi.Endpoints;
        // ... your test endpoint code
        """;
    
    return TestHelper.Verify(source);
}
```

The test will automatically create snapshot files in the `Snapshots/` directory on first run.

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
