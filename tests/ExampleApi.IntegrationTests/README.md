# ExampleApi Integration Tests

This project contains integration tests for the ExampleApi application, following Microsoft's ASP.NET Core integration testing guidelines.

## Overview

Integration tests verify that the app's components work correctly when assembled together, including:
- HTTP endpoints and routing
- Request/response handling  
- Middleware pipeline
- Dependency injection
- API versioning
- Validation logic
- Error handling

## Key Components

### ExampleApiWebApplicationFactory
- Inherits from `WebApplicationFactory<Program>`
- Configures test-specific services (e.g., test database)
- Sets up test environment configuration
- Provides `TestTodoStore` for deterministic test data

### TestTodoStore
- Test implementation of `ITodoStore`
- In-memory storage for predictable test scenarios
- Helper methods for seeding and clearing test data

## Running the Tests

### Prerequisites
- .NET 10.0 SDK
- All ExampleApi dependencies restored

### Commands

```bash
# Run all integration tests
dotnet test tests/ExampleApi.IntegrationTests

# Run with verbose output
dotnet test tests/ExampleApi.IntegrationTests --logger "console;verbosity=detailed"

# Run specific test class
dotnet test tests/ExampleApi.IntegrationTests --filter "FullyQualifiedName~TodoEndpointsTests"

# Run tests with coverage (if coverlet is configured)
dotnet test tests/ExampleApi.IntegrationTests --collect:"XPlat Code Coverage"
```

## Test Categories

### CRUD Operations (`TodoEndpointsTests`)
- GET `/api/v1/todos` - Retrieve all todos
- GET `/api/v1/todos/{id}` - Retrieve specific todo
- POST `/api/v1/todos` - Create new todo
- PUT `/api/v1/todos/{id}` - Update entire todo
- PATCH `/api/v1/todos/{id}` - Partial todo update
- DELETE `/api/v1/todos/{id}` - Delete todo
- GET `/api/v1/todos/export` - Export todos

### Validation (`TodoValidationTests`)
- FluentValidation endpoint testing
- DataAnnotation validation testing
- Invalid request format handling
- Content type validation

### API Versioning (`WeatherForecastEndpointsTests`)
- URL path versioning (`/api/v1/` vs `/api/v2/`)
- Header-based versioning (`X-Version`)
- Query parameter versioning (`?api-version=1`)
- Invalid version handling

### Infrastructure (`InfrastructureTests`)
- OpenAPI document generation
- Scalar API documentation UI
- Middleware pipeline functionality
- Error handling and 404 responses

## Test Data Management

Each test method should:
1. Clear existing test data using `TestTodoStore.Clear()`
2. Seed required test data using `TestTodoStore.SeedData()`
3. Perform the test action
4. Assert the expected results
5. Verify side effects (e.g., data was actually stored/deleted)

Example:
```csharp
[Fact]
public async Task GetTodoById_WithValidId_ReturnsTodo()
{
    // Arrange
    var todoStore = _factory.Services.GetRequiredService<ITodoStore>() as TestTodoStore;
    todoStore!.Clear();
    
    var todo = TestHelpers.CreateTestTodo("Test Todo");
    todoStore.SeedData(todo);

    // Act
    var response = await _client.GetAsync($"/api/v1/todos/{todo.Id}");

    // Assert
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    var result = await response.Content.ReadFromJsonAsync<GetTodoById.ResponseModel>();
    result.ShouldNotBeNull();
    result.Title.ShouldBe("Test Todo");
}
```

## Test Helpers

The `TestHelpers` class provides utility methods:
- `CreateJsonContent<T>()` - JSON serialization with custom options
- `ReadFromJsonAsync<T>()` - JSON deserialization with custom options  
- `GenerateRandomTitle()` - Generate test data
- `CreateTestTodo()` - Create test todo objects
- `WaitForConditionAsync()` - Async condition waiting
- `AssertJsonContainsAsync()` - JSON response validation

## Best Practices

### Test Isolation
- Each test should be independent and not rely on other tests
- Clear test data before each test to ensure clean state
- Use deterministic test data (avoid random values in assertions)

### Assertions
- Use Shouldly for fluent assertions
- Test both successful and error scenarios
- Verify HTTP status codes and response content
- Check side effects (e.g., data persistence)

### Performance
- Share `WebApplicationFactory` across tests using `IClassFixture`
- Use `ICollectionFixture` for expensive setup that can be shared across multiple test classes
- Keep test data minimal and focused on the scenario being tested

### Error Testing
- Test validation failures with meaningful error messages
- Verify proper HTTP status codes for different error conditions
- Test malformed requests and unsupported content types

## Continuous Integration

These integration tests are designed to run in CI/CD pipelines:
- No external dependencies (uses in-memory test doubles)
- Fast execution with shared test fixtures
- Deterministic results with controlled test data
- Comprehensive coverage of HTTP endpoints and middleware

## Troubleshooting

### Common Issues

**Tests fail with "Address already in use"**
- Multiple test runs might conflict. Ensure previous test processes are terminated.

**JSON serialization/deserialization errors**
- Check that request/response models match expected format
- Use `TestHelpers.ReadFromJsonAsync()` with proper options

**Validation tests failing unexpectedly**
- Verify that validation attributes/rules match between test and actual implementation
- Check that test request data actually violates the expected validation rules

**API versioning tests failing**
- Ensure both V1 and V2 endpoints are properly configured
- Verify that response models for different versions have expected differences
