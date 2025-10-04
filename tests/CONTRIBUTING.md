# Contributing to Tests

This document provides guidance on adding new tests to the MinimalApi.Endpoints test suite.

## Test Project Organization

### IeuanWalker.MinimalApi.Endpoints.Tests
Tests for the main library functionality including:
- Filters (FluentValidationFilter, etc.)
- Extension methods
- Core interfaces and implementations

**When to add tests here:**
- When adding new filter implementations
- When adding new extension methods with actual logic
- When adding core library functionality

**Naming Convention:**
- `{ClassName}Tests.cs` for unit tests

### IeuanWalker.MinimalApi.Endpoints.Generator.Tests
Tests for the source generator including:
- Code generation for different endpoint types
- Diagnostic reporting
- Helper utilities

**When to add tests here:**
- When adding new endpoint interface types
- When adding new diagnostic rules
- When modifying code generation logic
- When adding helper utilities

**Naming Convention:**
- `{ClassName}Tests.cs` for unit tests
- `DiagnosticTests.cs` for diagnostic-related tests
- `{Scenario}GenerationTests.cs` for code generation scenarios

**Test Patterns:**
```csharp
// Basic generation test
[Fact]
public void GeneratesCode_ForScenario()
{
    string source = """
        // C# code to test
        """;

    VerifySourceGenerator(source, result =>
    {
        result.GeneratedTrees.Length.ShouldBe(1);
        result.Diagnostics.ShouldBeEmpty();
        
        string generatedCode = result.GeneratedTrees[0].ToString();
        generatedCode.ShouldContain("ExpectedContent");
    });
}

// Diagnostic test
[Fact]
public void ReportsDiagnostic_WhenCondition()
{
    string source = """
        // Invalid C# code
        """;

    GeneratorDriverRunResult result = RunGenerator(source);
    result.Diagnostics.ShouldContain(d => d.Id == "MINAPI001");
}
```

### ExampleApi.Tests
Unit tests for ExampleApi endpoints and services:
- Individual endpoint handlers
- Service layer logic
- Business logic

**When to add tests here:**
- When adding new endpoints to ExampleApi
- When adding new services or business logic
- When testing request/response models

**Naming Convention:**
- `{EndpointName}Tests.cs` for endpoint tests
- `{ServiceName}Tests.cs` for service tests

**Test Patterns:**
```csharp
[Fact]
public async Task Handle_Scenario_ExpectedBehavior()
{
    // Arrange - Mock dependencies
    ITodoStore todoStore = Substitute.For<ITodoStore>();
    todoStore.GetByIdAsync(1, Arg.Any<CancellationToken>())
        .Returns(expectedTodo);

    MyEndpoint endpoint = new(todoStore);
    
    // Act
    var result = await endpoint.Handle(request, CancellationToken.None);

    // Assert
    result.ShouldNotBeNull();
    result.Property.ShouldBe(expectedValue);
}
```

### ExampleApi.IntegrationTests
Integration tests for ExampleApi:
- Full HTTP request/response flow
- Validation behavior
- Error handling
- Serialization/deserialization

**When to add tests here:**
- When adding new API endpoints
- When testing end-to-end scenarios
- When testing middleware behavior

**Naming Convention:**
- `{Feature}IntegrationTests.cs` for feature tests

**Test Patterns:**
```csharp
public class MyFeatureIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    readonly WebApplicationFactory<Program> _factory;

    public MyFeatureIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Endpoint_WithValidRequest_ReturnsSuccess()
    {
        // Arrange
        HttpClient client = _factory.CreateClient();
        var requestData = new { /* data */ };

        // Act
        var response = await client.PostAsJsonAsync("/api/endpoint", requestData);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
```

## Testing Best Practices

### 1. Test Naming
Use descriptive test names that follow the pattern:
- `MethodName_Scenario_ExpectedBehavior`
- Example: `Handle_WithValidId_ReturnsTodo`

### 2. Arrange-Act-Assert Pattern
Structure tests clearly:
```csharp
[Fact]
public void TestName()
{
    // Arrange - Set up test data and dependencies
    
    // Act - Execute the method under test
    
    // Assert - Verify the expected outcome
}
```

### 3. Use Shouldly
Prefer Shouldly for more readable assertions:
```csharp
// Good
result.ShouldNotBeNull();
result.Count.ShouldBe(5);
result.ShouldContain(x => x.Id == 1);

// Avoid
Assert.NotNull(result);
Assert.Equal(5, result.Count);
```

### 4. Mock External Dependencies
Use NSubstitute for mocking:
```csharp
ITodoStore store = Substitute.For<ITodoStore>();
store.GetByIdAsync(1, Arg.Any<CancellationToken>())
    .Returns(expectedTodo);
```

### 5. Test Edge Cases
Don't just test the happy path:
- Null values
- Empty collections
- Boundary conditions
- Error scenarios

### 6. Keep Tests Independent
Each test should be self-contained and not depend on other tests.

### 7. Test One Thing at a Time
Each test should verify a single behavior or scenario.

## Running Tests

### All Tests
```bash
dotnet test
```

### Specific Project
```bash
dotnet test tests/IeuanWalker.MinimalApi.Endpoints.Tests
```

### With Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Filter by Name
```bash
dotnet test --filter "FullyQualifiedName~GetAllTodos"
```

## Common Test Scenarios

### Testing Async Methods
```csharp
[Fact]
public async Task AsyncMethod_Test()
{
    // Use async/await
    var result = await methodAsync();
    result.ShouldNotBeNull();
}
```

### Testing Exceptions
```csharp
[Fact]
public void Method_ThrowsException()
{
    Action act = () => method();
    Should.Throw<ArgumentNullException>(act)
        .WithParameterName("parameterName");
}
```

### Testing Validators
```csharp
[Fact]
public void Validator_ValidInput_Passes()
{
    var validator = new MyValidator();
    var model = new MyModel { /* valid data */ };
    
    var result = validator.Validate(model);
    
    result.IsValid.ShouldBeTrue();
}
```

### Using Theory for Multiple Test Cases
```csharp
[Theory]
[InlineData("input1", "expected1")]
[InlineData("input2", "expected2")]
public void Method_WithDifferentInputs(string input, string expected)
{
    var result = method(input);
    result.ShouldBe(expected);
}
```

## Requirements for .NET 10.0

Currently, the test projects target .NET 9.0 but reference main projects that target .NET 10.0. You'll need .NET 10.0 SDK installed to build and run all tests.

Once installed:
```bash
dotnet --version  # Should show 10.x.x
dotnet test
```

## Additional Resources

- [xUnit Documentation](https://xunit.net/)
- [Shouldly Documentation](https://docs.shouldly.org/)
- [NSubstitute Documentation](https://nsubstitute.github.io/)
- [ASP.NET Core Testing](https://learn.microsoft.com/en-us/aspnet/core/test/)
