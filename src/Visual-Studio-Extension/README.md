# Visual Studio Extension for MinimalApi.Endpoints

This Visual Studio 2022 extension provides an item template for creating Minimal API endpoints using the MinimalApi.Endpoints library.

## Features

- **Item Template**: Quickly create new endpoint classes with request/response models and validation
- **Code Snippets**: Useful snippets for common endpoint patterns

## Installation

1. Build the solution in Release mode using Visual Studio 2022 with VSIX development workload
2. Double-click the generated `.vsix` file in `bin/Release` to install
3. Restart Visual Studio

## Usage

### Creating a New Endpoint

1. Right-click on a folder in your project
2. Select **Add** > **New Item**
3. Search for "Endpoint" or find it under the C# > ASP.NET Core category
4. Enter a name for your endpoint (e.g., "GetUser")
5. The template will generate three files:
   - `{Name}.cs` - The endpoint class with `IEndpoint<RequestModel, Results<Ok<ResponseModel>, NotFound>>`
   - `RequestModel.cs` - Request model with validator
   - `ResponseModel.cs` - Response model

### Generated Code Structure

The template generates a complete endpoint with:

```csharp
// GetUserEndpoint.cs
public class GetUserEndpoint : IEndpoint<RequestModel, Results<Ok<ResponseModel>, NotFound>>
{
    public static void Configure(RouteHandlerBuilder builder)
    {
        builder
            .Get("/route-path")  // Modify HTTP verb as needed
            .WithName("GetUser")
            .WithSummary("Summary for GetUser")
            .WithDescription("Detailed description for GetUser");
    }

    public async Task<Results<Ok<ResponseModel>, NotFound>> Handle(RequestModel request, CancellationToken ct)
    {
        // TODO: Implement endpoint logic
        throw new NotImplementedException();
    }
}

// RequestModel.cs
public sealed class RequestModel
{
    // TODO: Add request properties
}

sealed class RequestModelValidator : Validator<RequestModel>
{
    public RequestModelValidator()
    {
        // TODO: Add validation rules
    }
}

// ResponseModel.cs
public class ResponseModel
{
    // TODO: Add response properties
}
```

### Customizing the Generated Code

After generation, you can:

1. **Change HTTP Verb**: Modify `.Get()` to `.Post()`, `.Put()`, `.Delete()`, or `.Patch()`
2. **Change Interface**: Switch to `IEndpointWithoutRequest<TResponse>`, `IEndpointWithoutResponse<TRequest>`, or `IEndpoint` (no request/response)
3. **Remove Validation**: Delete the validator class if not needed
4. **Delete Files**: Remove `RequestModel.cs` or `ResponseModel.cs` if not using them

### Code Snippets

Type these shortcuts and press Tab to insert code:

- **minep** - Basic endpoint without request or response
- **minepfull** - Full endpoint with request and response models
- **minepval** - Validator for a request model

## Building the Extension

Prerequisites:
- Visual Studio 2022 with VSIX development workload
- .NET Framework 4.8

To build:
1. Open `VSExtension.sln` in Visual Studio 2022
2. Build in Release configuration
3. The VSIX will be generated in `bin/Release/MinimalApiEndpoints.vsix`

Note: MSBuild command-line builds may not work for VSIX projects - use Visual Studio IDE.

## Current Implementation

The current version provides a single template that generates:
- Endpoint with request and response models
- FluentValidation validator for the request model
- GET verb by default (easily changed by user)

This provides a fully-featured starting point that developers can modify as needed.

## Future Enhancements

The issue specification calls for an interactive UI wizard with:
1. **Dropdown** for HTTP verb selection (GET, POST, PUT, DELETE, PATCH)
2. **Checkbox** for including request model
3. **Checkbox** for including response model  
4. **Checkbox** for including validation (when response is enabled, defaulted to true)

### Implementation Approach for UI Wizard

To add the interactive UI:

1. **Create Custom Wizard**:
   ```csharp
   public class EndpointWizard : IWizard
   {
       // Show WPF dialog to collect user preferences
       // Pass selections as template parameters
   }
   ```

2. **Update vstemplate**:
   ```xml
   <WizardExtension>
       <Assembly>MinimalApiEndpoints, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null</Assembly>
       <FullClassName>MinimalApiEndpoints.EndpointWizard</FullClassName>
   </WizardExtension>
   ```

3. **Conditional Template Generation**:
   - Use wizard to pass boolean parameters
   - Generate only selected files
   - Use appropriate interface based on request/response selection
   - Include/exclude validator based on checkbox

### Reference Implementations

- **FastEndpoints Extension**: https://github.com/FastEndpoints/Visual-Studio-Extension
- **VS Templates Guide**: https://learn.microsoft.com/en-us/visualstudio/ide/how-to-create-item-templates

## Contributing

This extension is part of the MinimalApi.Endpoints project. Contributions welcome!

To implement the full UI wizard feature:
1. Fork the repository
2. Implement `IWizard` interface with WPF dialog
3. Add conditional logic to templates
4. Test thoroughly
5. Submit pull request

## License

See the LICENSE file in the root of the repository.

