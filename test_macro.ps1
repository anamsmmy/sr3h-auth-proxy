# Testing Macro Application
Write-Host "Testing Application..." -ForegroundColor Green

# Check build
Write-Host "Building..." -ForegroundColor Yellow
dotnet build MACRO_SR3H.sln --configuration Debug
if ($LASTEXITCODE -eq 0) {
    Write-Host "Build Success" -ForegroundColor Green
} else {
    Write-Host "Build Failed" -ForegroundColor Red
    exit 1
}

# Check required files
Write-Host "Checking files..." -ForegroundColor Yellow
$requiredFiles = @(
    "Views\MainWindow.xaml",
    "Views\MainWindow.xaml.cs",
    "Views\KeyCaptureDialog.xaml",
    "Views\KeyCaptureDialog.xaml.cs",
    "Services\EnhancedMacroService.cs",
    "Models\MacroConfiguration.cs"
)

foreach ($file in $requiredFiles) {
    if (Test-Path $file) {
        Write-Host "OK: $file" -ForegroundColor Green
    } else {
        Write-Host "MISSING: $file" -ForegroundColor Red
    }
}

# Run application for testing
Write-Host "Running Application..." -ForegroundColor Yellow
Write-Host "Test Instructions:" -ForegroundColor Cyan
Write-Host "1. Check all UI elements are visible" -ForegroundColor White
Write-Host "2. Test key selection buttons" -ForegroundColor White
Write-Host "3. Test start/stop macro" -ForegroundColor White
Write-Host "4. Check status display" -ForegroundColor White
Write-Host "5. Press Ctrl+C to stop test" -ForegroundColor White

# Run the application
dotnet run --project MacroApp.csproj