# Building the Visual Studio Extension

This document explains how to build the MinimalApi.Endpoints Visual Studio extension from source.

## Prerequisites

- .NET 9.0 SDK or later
- Visual Studio Build Tools or Visual Studio 2022
- Windows OS (required for Windows Forms components)

## Build Steps

### 1. Clone the Repository

```bash
git clone https://github.com/IeuanWalker/MinimalApi.Endpoints.git
cd MinimalApi.Endpoints
```

### 2. Build the Extension Project

```bash
cd src/IeuanWalker.MinimalApi.Endpoints.VsExtension
dotnet build --configuration Release
```

### 3. Locate the VSIX Package

After a successful build, the VSIX package will be generated in:

```
src/IeuanWalker.MinimalApi.Endpoints.VsExtension/bin/Release/net9.0-windows/
```

Look for the file: `IeuanWalker.MinimalApi.Endpoints.VsExtension.vsix`

**Note**: The VSIX file is created automatically on all platforms (Windows, Linux, macOS) using a custom MSBuild target that packages the extension files.

### 4. Install the Extension

Double-click the `.vsix` file to install it in Visual Studio 2022.

## Build Configuration

The extension targets:
- **Framework**: .NET 9.0-windows
- **Visual Studio Version**: 2022 (v17.0+)
- **Architecture**: AMD64

## Troubleshooting

### VSIX File Generation

The project includes a custom MSBuild target (`CreateVsixManually`) that generates the VSIX file on non-Windows platforms. On Windows, the Microsoft VSSDK.BuildTools handles VSIX creation automatically. The custom target:

1. Creates a staging directory
2. Copies all necessary files (DLL, manifest, templates, assets)
3. Creates the `[Content_Types].xml` file required by VSIX format
4. Zips everything into a `.vsix` file

This ensures the extension can be built on any platform, though it can only be **installed and run** on Windows with Visual Studio 2022.

### Build Errors on Non-Windows Systems

The extension requires Windows-specific components (Windows Forms). If you're building on Linux/macOS, you'll see build errors. Cross-platform building is not supported for VS extensions.

### Missing SDK Error

If you see `NETSDK1045` errors about .NET 10.0:
1. Ensure you're building only the extension project, not the entire solution
2. The extension project targets .NET 9.0, which is compatible with the current SDK

### Package Restore Issues

If package restore fails:

```bash
dotnet restore src/IeuanWalker.MinimalApi.Endpoints.VsExtension/IeuanWalker.MinimalApi.Endpoints.VsExtension.csproj
```

### NU1701 Warning About .NET Framework Compatibility

The warning `NU1701: Package 'Microsoft.VisualStudio.TemplateWizardInterface' was restored using '.NETFramework...' instead of the project target framework 'net9.0-windows'` has been intentionally suppressed in the project file.

**Why this is safe:**
- Visual Studio SDK packages (`Microsoft.VisualStudio.TemplateWizardInterface`, `EnvDTE`) still target .NET Framework
- These packages are designed to work with modern .NET and are tested by Microsoft for compatibility
- Visual Studio extensions load these packages in the VS process, which handles the framework bridging
- This is a standard practice for VS extension development

The warning is suppressed using `<NoWarn>$(NoWarn);NU1701</NoWarn>` in the project file.

## Development

### Project Structure

```
src/IeuanWalker.MinimalApi.Endpoints.VsExtension/
├── EndpointWizard.cs              # IWizard implementation
├── WizardForm.cs                  # Windows Forms UI
├── source.extension.vsixmanifest  # VSIX manifest
├── Templates/
│   └── CSharp/
│       ├── EndpointTemplate.vstemplate  # Template definition
│       ├── Endpoint.cs                   # Endpoint template file
│       ├── RequestModel.cs               # Request model template
│       ├── ResponseModel.cs              # Response model template
│       └── icon.png                      # Template icon
└── README.md
```

### Making Changes

1. Modify the template files in `Templates/CSharp/`
2. Update the wizard form in `WizardForm.cs` if adding new options
3. Update the wizard logic in `EndpointWizard.cs` to handle new parameters
4. Build and test in Visual Studio

### Template Parameter Syntax

Template files use Visual Studio's parameter replacement syntax:

- `$rootnamespace$` - Project's root namespace
- `$fileinputname$` - Name entered by user
- `$if$ (condition)` ... `$endif$` - Conditional content
- `$else$` - Else clause for conditionals
- Custom parameters (e.g., `$httpVerb$`, `$withRequest$`) are added by the wizard

## Testing

### Manual Testing

1. Build the extension in Debug mode
2. Press F5 to launch Visual Studio Experimental Instance
3. Create or open a test project
4. Add a new item and select "Endpoint" template
5. Test various configurations in the wizard
6. Verify generated code is correct

### Debugging

- Set breakpoints in `EndpointWizard.cs` or `WizardForm.cs`
- Launch with F5 to debug in the experimental instance
- Check the Visual Studio Output window for errors

## Release Checklist

Before releasing a new version:

1. [ ] Update version in `source.extension.vsixmanifest`
2. [ ] Update version in `.csproj` file
3. [ ] Test all template variations
4. [ ] Update README.md with any new features
5. [ ] Build in Release configuration
6. [ ] Test the VSIX installation
7. [ ] Create GitHub release with VSIX file
8. [ ] Update main README.md with download link

## Additional Resources

- [Visual Studio Extension Development](https://learn.microsoft.com/en-us/visualstudio/extensibility/)
- [Item Template Schema Reference](https://learn.microsoft.com/en-us/visualstudio/extensibility/visual-studio-template-schema-reference)
- [Template Parameter Reference](https://learn.microsoft.com/en-us/visualstudio/ide/template-parameters)
