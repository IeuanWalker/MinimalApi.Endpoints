# Testing the Template Pack Locally

This guide will help you test the MinimalApi.Endpoints Template Pack locally before publishing.

## Prerequisites

- .NET 10 SDK or later installed
- PowerShell or Bash terminal
- **Visual Studio 2022 or later** (for testing Visual Studio integration)

## Step 0: Create the Icon (Required for Visual Studio)

**Important:** Visual Studio requires a valid 32x32 PNG icon for item templates to appear in the "Add New Item" dialog.

### Using PowerShell (Windows/macOS/Linux)

```powershell
# Run the included script to generate a simple icon
.\create-template-icon.ps1
```

This will create `icon.png` at:
```
src/IeuanWalker.MinimalApi.Endpoints.TemplatePack/templates/endpoint/.template.config/icon.png
```

### Manual Creation (Alternative)

If you prefer to create a custom icon:

1. Create a 32x32 pixel PNG image
2. Save it as `icon.png` in:
   ```
 src/IeuanWalker.MinimalApi.Endpoints.TemplatePack/templates/endpoint/.template.config/icon.png
   ```
3. Recommended design: Simple letter "E" on blue background (ASP.NET Core theme)

## Step 1: Build and Pack the Template

```bash
# Build the template pack project
dotnet build src/IeuanWalker.MinimalApi.Endpoints.TemplatePack/IeuanWalker.MinimalApi.Endpoints.TemplatePack.csproj -c Release

# Pack the template
dotnet pack src/IeuanWalker.MinimalApi.Endpoints.TemplatePack/IeuanWalker.MinimalApi.Endpoints.TemplatePack.csproj -c Release
```

## Step 2: Install the Template Locally

```bash
# Install from the local package
dotnet new install ./src/IeuanWalker.MinimalApi.Endpoints.TemplatePack/bin/Release/IeuanWalker.MinimalApi.Endpoints.TemplatePack.1.0.0.nupkg
```

You should see output similar to:
```
The following template packages will be installed:
   IeuanWalker.MinimalApi.Endpoints.TemplatePack

Success: IeuanWalker.MinimalApi.Endpoints.TemplatePack::1.0.0 installed the following templates:
Template Name   Short Name  Language  Tags
-------------------------------------------  ----------  --------  -------------------------------
MinimalApi.Endpoints Feature Fileset    endpoint    [C#]  MinimalApi/Endpoints/ASP.NET Core
```

## Step 3: Test the Template

### List Available Templates

```bash
dotnet new list endpoint
```

### Test Case 1: Full Endpoint (GET with Request and Response)

```bash
# Create test directory
mkdir -p test-output/GetUserById
cd test-output/GetUserById

# Generate endpoint
dotnet new endpoint -n GetUserById --namespace TestApi.Endpoints.Users.GetById --method GET --route "/api/users/{id}"

# Review generated files
ls -la
cat GetUserByIdEndpoint.cs
cat RequestModel.cs
cat ResponseModel.cs
```

Expected files:
- `GetUserByIdEndpoint.cs`
- `RequestModel.cs`
- `ResponseModel.cs`

### Test Case 2: POST Endpoint with Validator

```bash
cd ../../
mkdir -p test-output/CreateUser
cd test-output/CreateUser

dotnet new endpoint -n CreateUser --namespace TestApi.Endpoints.Users.Create --method POST --route "/api/users" --validator true

ls -la
cat CreateUserEndpoint.cs
cat RequestModel.cs
cat ResponseModel.cs
cat RequestModelValidator.cs
```

Expected files:
- `CreateUserEndpoint.cs`
- `RequestModel.cs`
- `ResponseModel.cs`
- `RequestModelValidator.cs`

### Test Case 3: DELETE Endpoint (Request only, no Response)

```bash
cd ../../
mkdir -p test-output/DeleteUser
cd test-output/DeleteUser

dotnet new endpoint -n DeleteUser --namespace TestApi.Endpoints.Users.Delete --method DELETE --route "/api/users/{id}" --withResponse false

ls -la
cat DeleteUserEndpoint.cs
cat RequestModel.cs
```

Expected files:
- `DeleteUserEndpoint.cs` (should implement `IEndpointWithoutResponse<RequestModel>`)
- `RequestModel.cs`

### Test Case 4: GET All Endpoint (Response only, no Request)

```bash
cd ../../
mkdir -p test-output/GetAllUsers
cd test-output/GetAllUsers

dotnet new endpoint -n GetAllUsers --namespace TestApi.Endpoints.Users.GetAll --method GET --route "/api/users" --withRequest false

ls -la
cat GetAllUsersEndpoint.cs
cat ResponseModel.cs
```

Expected files:
- `GetAllUsersEndpoint.cs` (should implement `IEndpointWithoutRequest<TResponse>`)
- `ResponseModel.cs`

### Test Case 5: Simple Endpoint (No Request or Response)

```bash
cd ../../
mkdir -p test-output/PingEndpoint
cd test-output/PingEndpoint

dotnet new endpoint -n Ping --namespace TestApi.Endpoints.Health.Ping --method GET --route "/api/health/ping" --withRequest false --withResponse false

ls -la
cat PingEndpoint.cs
```

Expected files:
- `PingEndpoint.cs` (should implement `IEndpoint`)

### Test Case 6: Endpoint with Group

```bash
cd ../../
mkdir -p test-output/GetUserProfile
cd test-output/GetUserProfile

dotnet new endpoint -n GetUserProfile --namespace TestApi.Endpoints.Users.GetProfile --method GET --route "/{id}/profile" --group UserEndpointGroup

ls -la
cat GetUserProfileEndpoint.cs
```

Expected:
- `GetUserProfileEndpoint.cs` should contain `.Group<UserEndpointGroup>()`

## Step 4: Verify Generated Code

Check that:
1. All files have correct namespaces
2. Endpoint classes implement correct interfaces
3. HTTP verbs are correctly applied (Get, Post, Put, Delete, Patch)
4. Routes are correctly set
5. Validator is generated only when requested
6. Group configuration appears when specified
7. Files are excluded when `withRequest` or `withResponse` is false

## Step 5: Integration Test (Optional)

Copy generated files into the ExampleApi project and verify they compile:

```bash
# From repository root
cp -r test-output/GetUserById example/ExampleApi/Endpoints/Users/

# Build to verify no compilation errors
dotnet build example/ExampleApi/ExampleApi.csproj
```

## Step 6: Cleanup

### Remove test output

```bash
rm -rf test-output/
```

### Uninstall the template

```bash
dotnet new uninstall IeuanWalker.MinimalApi.Endpoints.TemplatePack
```

## Common Issues

### Issue: Template not appearing in Visual Studio "Add New Item" dialog

**Symptoms:**
- Template works fine from command line (`dotnet new endpoint`)
- Template doesn't show in Visual Studio's "Add New Item" dialog

**Root Cause:** Missing or invalid `icon.png` file

**Solution:**
1. **Create the icon** using the provided script:
   ```powershell
   .\create-template-icon.ps1
   ```

2. **Rebuild the template pack**:
   ```bash
   dotnet pack src/IeuanWalker.MinimalApi.Endpoints.TemplatePack/IeuanWalker.MinimalApi.Endpoints.TemplatePack.csproj -c Release
   ```

3. **Uninstall old version**:
   ```bash
   dotnet new uninstall IeuanWalker.MinimalApi.Endpoints.TemplatePack
   ```

4. **Install updated version**:
   ```bash
   dotnet new install ./src/IeuanWalker.MinimalApi.Endpoints.TemplatePack/bin/Release/IeuanWalker.MinimalApi.Endpoints.TemplatePack.1.0.0.nupkg
   ```

5. **Restart Visual Studio completely** (important!)

6. **Test in Visual Studio**:
   - Right-click project/folder → Add → New Item...
   - Search for "endpoint"
   - Template should now appear with an icon

**Verification:**
The icon file should exist at:
```
src/IeuanWalker.MinimalApi.Endpoints.TemplatePack/templates/endpoint/.template.config/icon.png
```

If the file exists, check:
- File size should be > 0 bytes (not a placeholder text file)
- File should be a valid PNG image (32x32 pixels recommended)
- File extension is `.png` (not `.png.txt`)

### Issue: Template already installed

**Solution:** Uninstall first, then reinstall:
```bash
dotnet new uninstall IeuanWalker.MinimalApi.Endpoints.TemplatePack
dotnet new install ./src/IeuanWalker.MinimalApi.Endpoints.TemplatePack/bin/Release/IeuanWalker.MinimalApi.Endpoints.TemplatePack.1.0.0.nupkg
```

### Issue: Template not found

**Solution:** Check the package was built correctly:
```bash
ls -la ./src/IeuanWalker.MinimalApi.Endpoints.TemplatePack/bin/Release/
```

### Issue: Generated code doesn't compile

**Solution:** 
1. Check namespace is valid C# identifier
2. Ensure using statements are added to project if needed
3. Verify IeuanWalker.MinimalApi.Endpoints package is referenced

## Troubleshooting Visual Studio Integration

- If templates do not appear in Visual Studio **"Add New Item"** dialog:
  - Ensure you have run `create-template-icon.ps1` to generate an icon.
  - Restart Visual Studio after installing the template.
  - Check the **Output** window in Visual Studio for any errors during template installation.

## Automated Test Script

For convenience, use this PowerShell script to run all tests:

```powershell
# test-templates.ps1

# Verify icon exists
Write-Host "Verifying icon.png exists..." -ForegroundColor Cyan
$iconPath = "src/IeuanWalker.MinimalApi.Endpoints.TemplatePack/templates/endpoint/.template.config/icon.png"
if (-not (Test-Path $iconPath)) {
    Write-Host "ERROR: icon.png not found!" -ForegroundColor Red
    Write-Host "Run .\create-template-icon.ps1 first to create the icon." -ForegroundColor Yellow
    Write-Host "The icon is required for Visual Studio integration." -ForegroundColor Yellow
    exit 1
}
$iconSize = (Get-Item $iconPath).Length
if ($iconSize -lt 100) {
    Write-Host "WARNING: icon.png seems too small ($iconSize bytes). May be a placeholder file." -ForegroundColor Yellow
    Write-Host "Run .\create-template-icon.ps1 to create a proper icon." -ForegroundColor Yellow
}
Write-Host "Icon verified: $iconSize bytes`n" -ForegroundColor Green

# Build and pack
dotnet pack src/IeuanWalker.MinimalApi.Endpoints.TemplatePack/IeuanWalker.MinimalApi.Endpoints.TemplatePack.csproj -c Release

# Install
dotnet new install ./src/IeuanWalker.MinimalApi.Endpoints.TemplatePack/bin/Release/IeuanWalker.MinimalApi.Endpoints.TemplatePack.1.0.0.nupkg

# Create test directory
New-Item -ItemType Directory -Force -Path "test-output"

# Test Case 1: Full GET endpoint
Write-Host "Test 1: GET with Request and Response" -ForegroundColor Green
dotnet new endpoint -n GetUserById --namespace TestApi.Endpoints.Users.GetById --method GET --route "/api/users/{id}" -o test-output/GetUserById

# Test Case 2: POST with validator
Write-Host "Test 2: POST with Validator" -ForegroundColor Green
dotnet new endpoint -n CreateUser --namespace TestApi.Endpoints.Users.Create --method POST --route "/api/users" --validator true -o test-output/CreateUser

# Test Case 3: DELETE without response
Write-Host "Test 3: DELETE without Response" -ForegroundColor Green
dotnet new endpoint -n DeleteUser --namespace TestApi.Endpoints.Users.Delete --method DELETE --route "/api/users/{id}" --withResponse false -o test-output/DeleteUser

# Test Case 4: GET all without request
Write-Host "Test 4: GET without Request" -ForegroundColor Green
dotnet new endpoint -n GetAllUsers --namespace TestApi.Endpoints.Users.GetAll --method GET --route "/api/users" --withRequest false -o test-output/GetAllUsers

# Test Case 5: Simple endpoint
Write-Host "Test 5: Simple Endpoint" -ForegroundColor Green
dotnet new endpoint -n Ping --namespace TestApi.Endpoints.Health.Ping --method GET --route "/api/health/ping" --withRequest false --withResponse false -o test-output/Ping

# Test Case 6: With group
Write-Host "Test 6: Endpoint with Group" -ForegroundColor Green
dotnet new endpoint -n GetUserProfile --namespace TestApi.Endpoints.Users.GetProfile --method GET --route "/{id}/profile" --group UserEndpointGroup -o test-output/GetUserProfile

Write-Host "`nAll tests completed! Check test-output/ directory." -ForegroundColor Green
Write-Host "To cleanup: Remove-Item -Recurse -Force test-output" -ForegroundColor Yellow
```

Save as `test-templates.ps1` and run with:
```powershell
.\test-templates.ps1
```

Or for Bash:

```bash
#!/bin/bash
# test-templates.sh

set -e

# Build and pack
dotnet pack src/IeuanWalker.MinimalApi.Endpoints.TemplatePack/IeuanWalker.MinimalApi.Endpoints.TemplatePack.csproj -c Release

# Install
dotnet new install ./src/IeuanWalker.MinimalApi.Endpoints.TemplatePack/bin/Release/IeuanWalker.MinimalApi.Endpoints.TemplatePack.1.0.0.nupkg

# Create test directory
mkdir -p test-output

# Test Case 1
echo "Test 1: GET with Request and Response"
dotnet new endpoint -n GetUserById --namespace TestApi.Endpoints.Users.GetById --method GET --route "/api/users/{id}" -o test-output/GetUserById

# Test Case 2
echo "Test 2: POST with Validator"
dotnet new endpoint -n CreateUser --namespace TestApi.Endpoints.Users.Create --method POST --route "/api/users" --validator true -o test-output/CreateUser

# Test Case 3
echo "Test 3: DELETE without Response"
dotnet new endpoint -n DeleteUser --namespace TestApi.Endpoints.Users.Delete --method DELETE --route "/api/users/{id}" --withResponse false -o test-output/DeleteUser

# Test Case 4
echo "Test 4: GET without Request"
dotnet new endpoint -n GetAllUsers --namespace TestApi.Endpoints.Users.GetAll --method GET --route "/api/users" --withRequest false -o test-output/GetAllUsers

# Test Case 5
echo "Test 5: Simple Endpoint"
dotnet new endpoint -n Ping --namespace TestApi.Endpoints.Health.Ping --method GET --route "/api/health/ping" --withRequest false --withResponse false -o test-output/Ping

# Test Case 6
echo "Test 6: Endpoint with Group"
dotnet new endpoint -n GetUserProfile --namespace TestApi.Endpoints.Users.GetProfile --method GET --route "/{id}/profile" --group UserEndpointGroup -o test-output/GetUserProfile

echo "All tests completed! Check test-output/ directory."
echo "To cleanup: rm -rf test-output"
```

