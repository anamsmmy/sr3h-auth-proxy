# Final Installer Builder - Automated
Write-Host "=== SR3H MACRO Installer Builder ===" -ForegroundColor Cyan

# Check Inno Setup
$InnoPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $InnoPath)) {
    Write-Host "ERROR: Inno Setup not found at $InnoPath" -ForegroundColor Red
    exit 1
}

# Create output directory
$OutputDir = "C:\MACRO_SR3H\Setup\Output"
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

# Build application
Write-Host "Building application..." -ForegroundColor Yellow
$buildResult = dotnet build "C:\MACRO_SR3H\MacroApp.csproj" --configuration Release --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "Build successful!" -ForegroundColor Green

# Create installer
Write-Host "Creating installer..." -ForegroundColor Yellow
$result = & $InnoPath "C:\MACRO_SR3H\Setup\SR3H_MACRO_Setup_v1.0.0.iss"
if ($LASTEXITCODE -eq 0) {
    Write-Host "SUCCESS! Installer created!" -ForegroundColor Green
    $installerFile = "$OutputDir\SR3H_MACRO_Setup.exe"
    if (Test-Path $installerFile) {
        $fileSize = [math]::Round((Get-Item $installerFile).Length / 1MB, 2)
        Write-Host "File: $installerFile" -ForegroundColor Cyan
        Write-Host "Size: $fileSize MB" -ForegroundColor Cyan
    }
} else {
    Write-Host "Installer creation failed!" -ForegroundColor Red
    exit 1
}