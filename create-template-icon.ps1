#!/usr/bin/env pwsh
# Create a simple 32x32 PNG icon for the template

Add-Type -AssemblyName System.Drawing

# Create a 32x32 bitmap
$bitmap = New-Object System.Drawing.Bitmap 32, 32

# Get graphics object
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias

# Fill background with a nice blue color (ASP.NET Core blue)
$bgBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 92, 107, 192))
$graphics.FillRectangle($bgBrush, 0, 0, 32, 32)

# Draw "E" for Endpoint in white
$font = New-Object System.Drawing.Font("Arial", 20, [System.Drawing.FontStyle]::Bold)
$textBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
$stringFormat = New-Object System.Drawing.StringFormat
$stringFormat.Alignment = [System.Drawing.StringAlignment]::Center
$stringFormat.LineAlignment = [System.Drawing.StringAlignment]::Center

$graphics.DrawString("E", $font, $textBrush, 16, 16, $stringFormat)

# Save the icon
$iconPath = "src/IeuanWalker.MinimalApi.Endpoints.TemplatePack/templates/endpoint/.template.config/icon.png"
$bitmap.Save($iconPath, [System.Drawing.Imaging.ImageFormat]::Png)

# Cleanup
$graphics.Dispose()
$bitmap.Dispose()
$bgBrush.Dispose()
$textBrush.Dispose()
$font.Dispose()

Write-Host "Icon created successfully at: $iconPath" -ForegroundColor Green
Write-Host "Icon size: 32x32 pixels" -ForegroundColor Green
Write-Host "" 
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Run: dotnet pack src/IeuanWalker.MinimalApi.Endpoints.TemplatePack/IeuanWalker.MinimalApi.Endpoints.TemplatePack.csproj -c Release" -ForegroundColor Yellow
Write-Host "2. Run: dotnet new uninstall IeuanWalker.MinimalApi.Endpoints.TemplatePack" -ForegroundColor Yellow
Write-Host "3. Run: dotnet new install ./src/IeuanWalker.MinimalApi.Endpoints.TemplatePack/bin/Release/IeuanWalker.MinimalApi.Endpoints.TemplatePack.1.0.0.nupkg" -ForegroundColor Yellow
Write-Host "4. Restart Visual Studio" -ForegroundColor Yellow
Write-Host "5. The template should now appear in Add New Item dialog!" -ForegroundColor Yellow
