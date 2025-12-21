# Simple Installer Builder
Write-Host "Building SR3H MACRO Installer..." -ForegroundColor Cyan

# Check for Inno Setup
$InnoPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $InnoPath)) {
    Write-Host "Error: Inno Setup not found!" -ForegroundColor Red
    Write-Host "Please install Inno Setup 6 from: https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
    exit 1
}

# Create output directory
$OutputDir = "C:\MACRO_SR3H\Setup\Output"
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

# Build the application first
Write-Host "Building application..." -ForegroundColor Yellow
try {
    dotnet build "C:\MACRO_SR3H\MacroApp.csproj" --configuration Release --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed"
    }
    Write-Host "Application built successfully!" -ForegroundColor Green
} catch {
    Write-Host "Error building application!" -ForegroundColor Red
    exit 1
}

# Build the installer
Write-Host "Creating installer..." -ForegroundColor Yellow
try {
    & $InnoPath "C:\MACRO_SR3H\Setup\SR3H_MACRO_Setup_v1.0.0.iss"
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "SUCCESS! Installer created successfully!" -ForegroundColor Green
        Write-Host "Location: $OutputDir\SR3H_MACRO_Setup.exe" -ForegroundColor Cyan
        
        # Show file size
        $installerFile = "$OutputDir\SR3H_MACRO_Setup.exe"
        if (Test-Path $installerFile) {
            $fileSize = [math]::Round((Get-Item $installerFile).Length / 1MB, 2)
            Write-Host "File size: $fileSize MB" -ForegroundColor Cyan
        }
        
        # Open output folder
        Start-Process "explorer.exe" -ArgumentList $OutputDir
    } else {
        throw "Installer build failed"
    }
} catch {
    Write-Host "Error creating installer!" -ForegroundColor Red
    Write-Host "Please check the script file and required files." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")