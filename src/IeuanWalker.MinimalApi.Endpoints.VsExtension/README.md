# MinimalApi.Endpoints Visual Studio Extension

This Visual Studio extension provides item templates for creating MinimalApi.Endpoints classes with a guided wizard.

## Features

- **Interactive Wizard**: Customize endpoint generation through a user-friendly dialog
- **HTTP Verb Selection**: Choose from GET, POST, PUT, DELETE, or PATCH
- **Optional Request Model**: Include or exclude request model with validation support
- **Optional Response Model**: Include or exclude response model
- **FluentValidation Support**: Optionally generate validator classes for request models

## Installation

### From VSIX File

1. Close all instances of Visual Studio
2. Double-click the `IeuanWalker.MinimalApi.Endpoints.VsExtension.vsix` file
3. Follow the installation wizard
4. Restart Visual Studio

### Manual Installation

1. Build the extension project:
   ```bash
   cd src/IeuanWalker.MinimalApi.Endpoints.VsExtension
   dotnet build --configuration Release
   ```
2. Navigate to `src/IeuanWalker.MinimalApi.Endpoints.VsExtension/bin/Release/net9.0-windows/`
3. Double-click the generated `IeuanWalker.MinimalApi.Endpoints.VsExtension.vsix` file
4. Follow the installation wizard
5. Restart Visual Studio

**Note**: The VSIX file is automatically generated during the build process on all platforms (Windows, Linux, macOS).

## Usage

1. Right-click on a folder in your ASP.NET Core project in Solution Explorer
2. Select **Add** → **New Item...**
3. In the Add New Item dialog, search for or navigate to **Endpoint**
4. Click **Add**
5. Configure your endpoint in the wizard:
   - **HTTP Verb**: Select the HTTP method (GET, POST, PUT, DELETE, PATCH)
   - **Include Request**: Check to generate a RequestModel class
   - **Include Response**: Check to generate a ResponseModel class
   - **Include Validation**: Check to generate FluentValidation validator (only available when Request is included)
   - **Route**: Enter the route path for the endpoint
6. Click **OK** to generate the files

## Generated Files

Depending on your selections, the following files will be generated:

### Endpoint File (Always Generated)

```csharp
public class {Name}Endpoint : IEndpoint<RequestModel, Results<Ok<ResponseModel>, NotFound>>
{
    public static void Configure(RouteHandlerBuilder builder)
    {
        builder
            .Get("/route")
            .WithName("EndpointName")
            .WithSummary("Summary for endpoint")
            .WithDescription("Detailed description for endpoint");
    }

    public async Task<Results<Ok<ResponseModel>, NotFound>> Handle(RequestModel request, CancellationToken ct)
    {
        // TODO: Implement endpoint logic
        throw new NotImplementedException();
    }
}
```

### RequestModel File (Optional)

Generated when "Include Request" is checked:

```csharp
public sealed class RequestModel
{
    // TODO: Add request properties
}

// If validation is enabled:
sealed class RequestModelValidator : Validator<RequestModel>
{
    public RequestModelValidator()
    {
        // TODO: Add validation rules
    }
}
```

### ResponseModel File (Optional)

Generated when "Include Response" is checked:

```csharp
public class ResponseModel
{
    // TODO: Add response properties
}
```

## Interface Variants

The generated endpoint class implements different interfaces based on your selections:

| Request | Response | Interface |
|---------|----------|-----------|
| ✓ | ✓ | `IEndpoint<RequestModel, Results<Ok<ResponseModel>, NotFound>>` |
| ✓ | ✗ | `IEndpointWithoutResponse<RequestModel>` |
| ✗ | ✓ | `IEndpointWithoutRequest<Results<Ok<ResponseModel>, NotFound>>` |
| ✗ | ✗ | `IEndpoint` |

## Requirements

- Visual Studio 2022 (version 17.0 or later)
- .NET 9.0 or later
- IeuanWalker.MinimalApi.Endpoints package installed in your project

## Troubleshooting

### Template Not Appearing

1. Ensure the extension is installed (check **Extensions** → **Manage Extensions**)
2. Restart Visual Studio
3. Clear the template cache by deleting `%LocalAppData%\Microsoft\VisualStudio\17.0_[instanceid]\ItemTemplatesCache`

### Build Errors After Generation

Ensure you have the IeuanWalker.MinimalApi.Endpoints NuGet package installed in your project:

```bash
dotnet add package IeuanWalker.MinimalApi.Endpoints
```

## Contributing

Contributions are welcome! Please see the main repository for contribution guidelines.

## License

This extension is licensed under the same license as the MinimalApi.Endpoints library.

## Links

- [Main Repository](https://github.com/IeuanWalker/MinimalApi.Endpoints)
- [Documentation](https://github.com/IeuanWalker/MinimalApi.Endpoints/blob/main/README.md)
- [Report Issues](https://github.com/IeuanWalker/MinimalApi.Endpoints/issues)
