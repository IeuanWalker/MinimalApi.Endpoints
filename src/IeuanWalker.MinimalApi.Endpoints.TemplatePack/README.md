# MinimalApi.Endpoints Template Pack

Template pack for MinimalApi.Endpoints to quickly scaffold endpoint feature filesets using `dotnet new` command line tool or Visual Studio's "Add New Item" dialog.

## Installation

### Command Line

```bash
dotnet new install IeuanWalker.MinimalApi.Endpoints.TemplatePack
```

Or install from a local build:

```bash
dotnet new install ./src/IeuanWalker.MinimalApi.Endpoints.TemplatePack/bin/Release/IeuanWalker.MinimalApi.Endpoints.TemplatePack.1.0.0.nupkg
```

### Visual Studio

After installing the template pack via `dotnet new install`, restart Visual Studio. The template will appear in:

**Add New Item ? Visual C# Items ? MinimalApi.Endpoints Feature Fileset**

or search for "endpoint" in the Add New Item dialog.

## Usage

### Visual Studio (Add New Item Dialog)

1. Right-click on your project or folder in Solution Explorer
2. Select **Add ? New Item...**
3. Search for "endpoint" or navigate to **Visual C# Items**
4. Select **MinimalApi.Endpoints Feature Fileset**
5. Enter the endpoint name (e.g., "GetUserById")
6. Configure options in the dialog:
   - **HTTP Method**: GET, POST, PUT, DELETE, or PATCH
   - **Route Path**: The endpoint route (e.g., "/api/users/{id}")
   - **Include Request Model**: Check to generate RequestModel.cs
   - **Include Response Model**: Check to generate ResponseModel.cs
   - **Include FluentValidation Validator**: Check to generate RequestModelValidator.cs
7. Click **Add**

**Note:** The namespace will be automatically inferred from the folder location in your project.

### Command Line

Generate a complete endpoint with request and response models:

```bash
dotnet new endpoint -n GetUserById --namespace MyProject.Endpoints.Users.GetById --method POST --route "/api/users" -o Features/Users/Create
```

### Available Options

```bash
dotnet new endpoint --help
```

| Option | Short | Type | Default | Description |
|--------|-------|------|---------|-------------|
| `--namespace` | `-ns` | string | (required) | The namespace for the generated files |
| `--method` | `-m` | choice | GET | HTTP method (GET, POST, PUT, DELETE, PATCH) |
| `--route` | `-r` | string | /api/route/here | Endpoint route path |
| `--withRequest` | `-req` | bool | true | Include a request model |
| `--withResponse` | `-res` | bool | true | Include a response model |
| `--validator` | `-v` | bool | false | Include FluentValidation validator |
| `--group` | `-g` | string | (empty) | Endpoint group class name (optional) |

### Examples

#### GET Endpoint with Request and Response

```bash
dotnet new endpoint -n GetUserById -ns MyApi.Endpoints.Users.GetById -m GET -r "/api/users/{id}" -o Endpoints/Users/GetById
```

Generates:
- `GetUserByIdEndpoint.cs`
- `RequestModel.cs`
- `ResponseModel.cs`

#### POST Endpoint with Validator

```bash
dotnet new endpoint -n CreateUser -ns MyApi.Endpoints.Users.Create -m POST -r "/api/users" -v true -o Endpoints/Users/Create
```

Generates:
- `CreateUserEndpoint.cs`
- `RequestModel.cs`
- `ResponseModel.cs`
- `RequestModelValidator.cs`

#### DELETE Endpoint without Response

```bash
dotnet new endpoint -n DeleteUser -ns MyApi.Endpoints.Users.Delete -m DELETE -r "/api/users/{id}" -res false -o Endpoints/Users/Delete
```

Generates:
- `DeleteUserEndpoint.cs`
- `RequestModel.cs`

#### GET Endpoint without Request (List All)

```bash
dotnet new endpoint -n GetAllUsers -ns MyApi.Endpoints.Users.GetAll -m GET -r "/api/users" -req false -o Endpoints/Users/GetAll
```

Generates:
- `GetAllUsersEndpoint.cs`
- `ResponseModel.cs`

#### Endpoint with Group

```bash
dotnet new endpoint -n GetUserProfile -ns MyApi.Endpoints.Users.GetProfile -m GET -r "/{id}/profile" -g UserEndpointGroup -o Endpoints/Users/GetProfile
```

Generates:
- `GetUserProfileEndpoint.cs` (configured with `.Group<UserEndpointGroup>()`)
- `RequestModel.cs`
- `ResponseModel.cs`

## Uninstallation

```bash
dotnet new uninstall IeuanWalker.MinimalApi.Endpoints.TemplatePack
```

## Building the Template Pack

To build and pack the templates:

**Important:** Before building, ensure the icon file exists:
```bash
# Generate the icon using the provided script (required for Visual Studio integration)
.\create-template-icon.ps1

# Then build and pack
dotnet pack src/IeuanWalker.MinimalApi.Endpoints.TemplatePack/IeuanWalker.MinimalApi.Endpoints.TemplatePack.csproj -c Release
```

The icon is required for the template to appear in Visual Studio's "Add New Item" dialog.

## Testing Templates Locally

After building, install from the local package:

```bash
dotnet new install ./src/IeuanWalker.MinimalApi.Endpoints.TemplatePack/bin/Release/IeuanWalker.MinimalApi.Endpoints.TemplatePack.1.0.0.nupkg
```

Then test creating endpoints:

```bash
dotnet new endpoint -n TestEndpoint -ns TestNamespace -m POST -r "/api/test" -o ./test-output
```

To uninstall the local version:

```bash
dotnet new uninstall IeuanWalker.MinimalApi.Endpoints.TemplatePack
```

## Template Structure

The generated files follow the MinimalApi.Endpoints conventions:

### Endpoint File

- Implements appropriate `IEndpoint<TRequest, TResponse>` interface variant
- Contains static `Configure()` method for route configuration
- Contains `Handle()` method with business logic
- Supports dependency injection via constructor

### Request Model

- Plain C# class for request data
- Can include validation attributes or FluentValidation rules
- Supports binding attributes (`[FromRoute]`, `[FromQuery]`, etc.)

### Response Model

- Plain C# class for response data
- Can include helper methods for mapping domain models

### Validator (Optional)

- Inherits from `Validator<RequestModel>`
- Uses FluentValidation syntax
- Automatically registered and applied by the source generator

## Contributing

Contributions are welcome! Please open an issue or submit a pull request on the [GitHub repository](https://github.com/IeuanWalker/MinimalApi.Endpoints).

## License

MIT License - see [LICENSE](LICENSE) file for details.
