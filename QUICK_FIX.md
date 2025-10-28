# Quick Fix Guide - Template Not Showing in Visual Studio

## 3-Step Fix

### 1?? Create Icon
```powershell
.\create-template-icon.ps1
```

### 2?? Rebuild & Reinstall
```bash
dotnet pack src/IeuanWalker.MinimalApi.Endpoints.TemplatePack/IeuanWalker.MinimalApi.Endpoints.TemplatePack.csproj -c Release
dotnet new uninstall IeuanWalker.MinimalApi.Endpoints.TemplatePack
dotnet new install ./src/IeuanWalker.MinimalApi.Endpoints.TemplatePack/bin/Release/IeuanWalker.MinimalApi.Endpoints.TemplatePack.1.0.0.nupkg
```

### 3?? Restart Visual Studio
Close and reopen Visual Studio completely.

## Verify It Works

**Visual Studio:**
- Right-click project ? Add ? New Item...
- Search for "endpoint"
- Template should appear with icon ?

**Command Line (should still work):**
```bash
dotnet new endpoint -n GetUserById --namespace TestApi.Endpoints.Users.GetById --method GET --route "/api/users/{id}"
```

## What Was the Problem?

- **Missing:** `icon.png` file
- **Had:** Only placeholder text file (`icon.png.txt`)
- **Result:** VS silently ignores templates without valid icons
- **Fix:** Generated proper 32x32 PNG icon

## Files You'll See Changed

```
? create-template-icon.ps1 (new)
? VISUAL_STUDIO_FIX.md (new)
? QUICK_FIX.md (new)
? TESTING.md (updated with troubleshooting)
? README.md (updated with icon requirement)
? icon.png.txt (removed placeholder)
```

## After Running create-template-icon.ps1

You'll have:
```
src/IeuanWalker.MinimalApi.Endpoints.TemplatePack/
  templates/
    endpoint/
      .template.config/
     ? icon.png (32x32 PNG with blue background and white "E")
```

## Common Mistakes to Avoid

? Forgetting to restart Visual Studio
? Skipping the uninstall step before reinstalling
? Not running create-template-icon.ps1 first
? Follow all three steps in order

## Need Help?

See `VISUAL_STUDIO_FIX.md` for detailed troubleshooting.
