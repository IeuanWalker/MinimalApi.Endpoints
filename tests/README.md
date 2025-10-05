# Test Projects

This directory contains four test projects covering the MinimalApi.Endpoints solution:

## Test Projects

### 1. IeuanWalker.MinimalApi.Endpoints.Tests
**Unit tests for the main library project**

Tests the core functionality of the MinimalApi.Endpoints library including:
- FluentValidationFilter
- Extension methods
- Interfaces and their implementations

**Test Files:**
- `FluentValidationFilterTests.cs` - Tests for the FluentValidation filter implementation

### 2. IeuanWalker.MinimalApi.Endpoints.Generator.Tests
**Unit tests for the source generator with snapshot testing**

Tests the source generator functionality including:
- Code generation for different endpoint types
- Diagnostic reporting
- Support for validators and endpoint groups
- Helper utilities (StringExtensions, etc.)

**Test Files:**
- `EndpointGeneratorTests.cs` - Comprehensive tests for endpoint generation scenarios
- `StringExtensionsTests.cs` - Tests for string manipulation utilities

### 3. ExampleApi.Tests  
**Unit tests for the ExampleApi project**

Tests individual endpoint classes and business logic in isolation:
- Endpoint handlers
- Request/response models
- Service layer logic

**Test Files:**
- `GetAllTodosEndpointTests.cs` - Tests for the GetAllTodos endpoint

## Building and Running Tests

### Prerequisites
- .NET 10.0 SDK or later (required for all projects)

### Build Tests
```bash
dotnet build
```

### Run All Tests
```bash
dotnet test
```

### Run Specific Test Project
```bash
dotnet test tests/IeuanWalker.MinimalApi.Endpoints.Tests
dotnet test tests/IeuanWalker.MinimalApi.Endpoints.Generator.Tests  
dotnet test tests/ExampleApi.Tests
```

### Run Tests with Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Test Frameworks and Libraries

- **xUnit** - Test framework
- **Shouldly** - Assertion library for more readable tests
- **NSubstitute** - Mocking library for unit tests
- **Microsoft.AspNetCore.Mvc.Testing** - For integration tests

## Note on .NET SDK Version

All test projects now target .NET 10.0, matching the main projects. .NET 10.0 SDK is required to build and run the complete test suite.

Once .NET 10.0 is officially released, all tests can be built and executed normally.
