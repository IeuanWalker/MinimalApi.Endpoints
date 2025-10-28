# Visual Studio "Add New Item" Integration Fix

## Problem Summary

The MinimalApi.Endpoints template works perfectly from the command line but doesn't appear in Visual Studio's "Add New Item" dialog.

**Root Cause:** Missing `icon.png` file

Visual Studio requires a valid 32x32 pixel PNG icon for item templates to be visible in the "Add New Item" dialog. The template configuration references `icon.png` in `ide.host.json`, but only a placeholder text file (`icon.png.txt`) existed.

## Solution

### Step 1: Create the Icon

Run the provided PowerShell script to generate a simple icon:

```powershell
.\create-template-icon.ps1
```

This creates a 32x32 pixel PNG with:
- Blue background (ASP.NET Core theme color)
- White letter "E" for "Endpoint"
- Located at: `src/IeuanWalker.MinimalApi.Endpoints.TemplatePack/templates/endpoint/.template.config/icon.png`

**Alternative:** You can create your own custom 32x32 PNG icon and place it at the same location.

### Step 2: Rebuild and Reinstall

```bash
# 1. Pack the template with the new icon
dotnet pack src/IeuanWalker.MinimalApi.Endpoints.TemplatePack/IeuanWalker.MinimalApi.Endpoints.TemplatePack.csproj -c Release

# 2. Uninstall old version
dotnet new uninstall IeuanWalker.MinimalApi.Endpoints.TemplatePack

# 3. Install updated version
dotnet new install ./src/IeuanWalker.MinimalApi.Endpoints.TemplatePack/bin/Release/IeuanWalker.MinimalApi.Endpoints.TemplatePack.1.0.0.nupkg

# 4. Restart Visual Studio completely (important!)
```

### Step 3: Verify in Visual Studio

1. Open Visual Studio
2. Right-click on a project or folder in Solution Explorer
3. Select **Add ? New Item...**
4. Search for "endpoint" in the search box
5. The template should now appear with the icon

## Files Changed

1. **Created:** `create-template-icon.ps1` - Script to generate the icon
2. **Removed:** `icon.png.txt` - Old placeholder file
3. **Updated:** `TESTING.md` - Added troubleshooting section for Visual Studio integration
4. **Updated:** `README.md` - Added icon requirement note

## Technical Details

### Why the Icon is Required

From Visual Studio's template system perspective:
- `template.json` defines the template with `"type": "item"`
- `ide.host.json` provides Visual Studio-specific configuration
- The `"icon": "icon.png"` property in `ide.host.json` is **required**
- If the icon file is missing or invalid, Visual Studio silently fails to load the template in the UI
- Command-line `dotnet new` doesn't require the icon, which is why it worked there

### Icon Specifications

- **Size:** 32x32 pixels (recommended)
- **Format:** PNG with transparency support
- **Location:** `.template.config/icon.png` (relative to template root)
- **Design:** Should be simple and recognizable at small size

## Testing Checklist

After applying the fix:

- [x] Icon file exists and is valid PNG
- [ ] Template packs without errors
- [ ] Template installs via `dotnet new install`
- [ ] Template appears in `dotnet new list endpoint`
- [ ] Command-line generation works: `dotnet new endpoint -n Test ...`
- [ ] Visual Studio shows template in "Add New Item" dialog
- [ ] Visual Studio displays icon next to template name
- [ ] Template can be created from Visual Studio UI
- [ ] All parameters appear in Visual Studio dialog

## Next Steps

1. **Run the icon creation script** (takes a few seconds)
2. **Rebuild and reinstall the template** (commands above)
3. **Restart Visual Studio** (important - VS caches template info)
4. **Test in Visual Studio** - Template should now appear!

## Additional Notes

- The icon is embedded in the NuGet package during `dotnet pack`
- Visual Studio caches template metadata, so restart is essential
- The same icon appears in both "Add New Item" dialog and search results
- You can customize the icon design to match your branding

## Troubleshooting

If the template still doesn't appear after following all steps:

1. **Verify icon exists:**
   ```powershell
   Test-Path "src/IeuanWalker.MinimalApi.Endpoints.TemplatePack/templates/endpoint/.template.config/icon.png"
   ```
   Should return `True`

2. **Check icon size:**
   ```powershell
   (Get-Item "src/IeuanWalker.MinimalApi.Endpoints.TemplatePack/templates/endpoint/.template.config/icon.png").Length
   ```
   Should be > 100 bytes (not just a text file)

3. **Verify NuGet package contains icon:**
   ```powershell
   # Extract and inspect the .nupkg file
   Expand-Archive ./src/IeuanWalker.MinimalApi.Endpoints.TemplatePack/bin/Release/IeuanWalker.MinimalApi.Endpoints.TemplatePack.1.0.0.nupkg -DestinationPath temp-inspect
   Test-Path temp-inspect/content/endpoint/.template.config/icon.png
   ```

4. **Check Visual Studio Output window:**
   - View ? Output
   - Show output from: Template Discovery
   - Look for any errors related to template loading

5. **Clear Visual Studio template cache:**
   ```powershell
   # Close Visual Studio first, then:
   Remove-Item -Recurse -Force "$env:LOCALAPPDATA\Microsoft\VisualStudio\*\ComponentModelCache"
   ```
   Then restart Visual Studio.

## References

- [Visual Studio Template Documentation](https://learn.microsoft.com/en-us/visualstudio/ide/template-parameters)
- [.NET Template Configuration](https://learn.microsoft.com/en-us/dotnet/core/tools/custom-templates)
- [Template IDE Host Configuration](https://github.com/dotnet/templating/wiki/IDE-Host-Files)
