# test-templates.ps1
# PowerShell script to test MinimalApi.Endpoints Template Pack

param(
    [switch]$SkipBuild = $false,
    [switch]$SkipCleanup = $false
)

$ErrorActionPreference = "Stop"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host " MinimalApi.Endpoints Template Pack Tester" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Build and Pack
if (-not $SkipBuild) {
    Write-Host "[1/7] Building and packing template..." -ForegroundColor Yellow
    dotnet pack src/IeuanWalker.MinimalApi.Endpoints.TemplatePack/IeuanWalker.MinimalApi.Endpoints.TemplatePack.csproj -c Release
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed!" -ForegroundColor Red
     exit 1
    }
    Write-Host "? Build successful" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host "[1/7] Skipping build (using existing package)" -ForegroundColor Yellow
    Write-Host ""
}

# Step 2: Uninstall previous version (if exists)
Write-Host "[2/7] Uninstalling previous version (if any)..." -ForegroundColor Yellow
dotnet new uninstall IeuanWalker.MinimalApi.Endpoints.TemplatePack 2>$null
Write-Host "? Cleanup complete" -ForegroundColor Green
Write-Host ""

# Step 3: Install template
Write-Host "[3/7] Installing template..." -ForegroundColor Yellow
$packagePath = "src/IeuanWalker.MinimalApi.Endpoints.TemplatePack/bin/Release/IeuanWalker.MinimalApi.Endpoints.TemplatePack.1.0.0.nupkg"
dotnet new install $packagePath
if ($LASTEXITCODE -ne 0) {
    Write-Host "Installation failed!" -ForegroundColor Red
    exit 1
}
Write-Host "? Template installed" -ForegroundColor Green
Write-Host ""

# Step 4: Create test directory
Write-Host "[4/7] Creating test directory..." -ForegroundColor Yellow
$testDir = "test-output"
if (Test-Path $testDir) {
  Remove-Item -Recurse -Force $testDir
}
New-Item -ItemType Directory -Force -Path $testDir | Out-Null
Write-Host "? Test directory created: $testDir" -ForegroundColor Green
Write-Host ""

# Step 5: Run test cases
Write-Host "[5/7] Running test cases..." -ForegroundColor Yellow
Write-Host ""

$testCases = @(
    @{
        Name = "GET with Request and Response"
        Args = "-n GetUserById -ns TestApi.Endpoints.Users.GetById -m GET -r `"/api/users/{id}`" -o test-output/GetUserById"
        ExpectedFiles = @("GetUserByIdEndpoint.cs", "RequestModel.cs", "ResponseModel.cs")
    },
    @{
        Name = "POST with Validator"
     Args = "-n CreateUser -ns TestApi.Endpoints.Users.Create -m POST -r `"/api/users`" --validator true -o test-output/CreateUser"
      ExpectedFiles = @("CreateUserEndpoint.cs", "RequestModel.cs", "ResponseModel.cs", "RequestModelValidator.cs")
    },
    @{
        Name = "DELETE without Response"
Args = "-n DeleteUser -ns TestApi.Endpoints.Users.Delete -m DELETE -r `"/api/users/{id}`" --withResponse false -o test-output/DeleteUser"
  ExpectedFiles = @("DeleteUserEndpoint.cs", "RequestModel.cs")
    },
    @{
        Name = "GET without Request"
        Args = "-n GetAllUsers -ns TestApi.Endpoints.Users.GetAll -m GET -r `"/api/users`" --withRequest false -o test-output/GetAllUsers"
ExpectedFiles = @("GetAllUsersEndpoint.cs", "ResponseModel.cs")
  },
    @{
        Name = "Simple Endpoint (No Request/Response)"
        Args = "-n Ping -ns TestApi.Endpoints.Health.Ping -m GET -r `"/api/health/ping`" --withRequest false --withResponse false -o test-output/Ping"
    ExpectedFiles = @("PingEndpoint.cs")
    },
    @{
      Name = "Endpoint with Group"
        Args = "-n GetUserProfile -ns TestApi.Endpoints.Users.GetProfile -m GET -r `"/{id}/profile`" --group UserEndpointGroup -o test-output/GetUserProfile"
        ExpectedFiles = @("GetUserProfileEndpoint.cs", "RequestModel.cs", "ResponseModel.cs")
    }
)

$passed = 0
$failed = 0

foreach ($test in $testCases) {
    Write-Host "  Testing: $($test.Name)" -ForegroundColor Cyan
    
    # Execute template command
    $cmd = "dotnet new endpoint $($test.Args)"
    Invoke-Expression $cmd | Out-Null
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  ? FAILED: Template execution failed" -ForegroundColor Red
        $failed++
        continue
    }
    
    # Verify expected files
    $allFilesExist = $true
    $outputDir = ($test.Args -split '-o ')[1].Trim('"')
  
 foreach ($file in $test.ExpectedFiles) {
        $filePath = Join-Path $outputDir $file
        if (-not (Test-Path $filePath)) {
  Write-Host "  ? FAILED: Missing file $file" -ForegroundColor Red
            $allFilesExist = $false
        }
    }
    
    if ($allFilesExist) {
        Write-Host "  ? PASSED: All files generated correctly" -ForegroundColor Green
        $passed++
    } else {
        $failed++
    }
    
    Write-Host ""
}

Write-Host "Test Results: $passed passed, $failed failed" -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Yellow" })
Write-Host ""

# Step 6: List generated files
Write-Host "[6/7] Generated files:" -ForegroundColor Yellow
Get-ChildItem -Path $testDir -Recurse -File | ForEach-Object {
    $relativePath = $_.FullName.Substring((Get-Location).Path.Length + 1)
    Write-Host "  - $relativePath" -ForegroundColor Gray
}
Write-Host ""

# Step 7: Cleanup instructions
Write-Host "[7/7] Test complete!" -ForegroundColor Yellow
Write-Host ""
Write-Host "Review generated files in: $testDir" -ForegroundColor Cyan
Write-Host ""

if (-not $SkipCleanup) {
    Write-Host "Cleanup commands:" -ForegroundColor Yellow
    Write-Host "  • Remove test output:   Remove-Item -Recurse -Force $testDir" -ForegroundColor Gray
    Write-Host "  • Uninstall template:   dotnet new uninstall IeuanWalker.MinimalApi.Endpoints.TemplatePack" -ForegroundColor Gray
    Write-Host ""
}

if ($failed -eq 0) {
    Write-Host "? All tests passed!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "? Some tests failed. Please review the output above." -ForegroundColor Red
    exit 1
}
