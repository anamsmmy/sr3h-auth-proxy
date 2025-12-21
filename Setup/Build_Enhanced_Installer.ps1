# SR3H MACRO - Build Enhanced Installer Script
# Created: 2025-01-31

Write-Host "Building SR3H MACRO Enhanced Installer..." -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Cyan

# Paths
$ProjectRoot = "c:\MACRO_SR3H"
$SetupDir = "$ProjectRoot\Setup"
$OutputDir = "$SetupDir\Output"
$InnoSetupScript = "$SetupDir\SR3H_MACRO_Setup_Enhanced.iss"

# Create output directory
if (!(Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force
    Write-Host "Created output directory: $OutputDir" -ForegroundColor Yellow
}

# Check for Inno Setup
$InnoSetupPath = ""
$PossiblePaths = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 5\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 5\ISCC.exe"
)

foreach ($Path in $PossiblePaths) {
    if (Test-Path $Path) {
        $InnoSetupPath = $Path
        break
    }
}

if ($InnoSetupPath -eq "") {
    Write-Host "Inno Setup not found!" -ForegroundColor Red
    Write-Host "Please download and install Inno Setup from: https://jrsoftware.org/isinfo.php" -ForegroundColor Yellow
    exit 1
}

Write-Host "Found Inno Setup: $InnoSetupPath" -ForegroundColor Green

# Check application files
$AppExePath = "$ProjectRoot\bin\Release\net6.0-windows\SR3H MACRO.exe"
if (!(Test-Path $AppExePath)) {
    Write-Host "Application file not found: $AppExePath" -ForegroundColor Red
    Write-Host "Building application first..." -ForegroundColor Yellow
    
    Set-Location $ProjectRoot
    Write-Host "Building application in Release mode..." -ForegroundColor Yellow
    dotnet build MacroApp.csproj --configuration Release --verbosity quiet
    
    if (!(Test-Path $AppExePath)) {
        Write-Host "Failed to build application!" -ForegroundColor Red
        exit 1
    }
}

Write-Host "Application file found: $AppExePath" -ForegroundColor Green

# Clean sensitive and unnecessary files
Write-Host "Cleaning sensitive and unnecessary files..." -ForegroundColor Yellow
$ReleaseDir = "$ProjectRoot\bin\Release\net6.0-windows"

# Remove log files
if (Test-Path "$ReleaseDir\logs") {
    Remove-Item "$ReleaseDir\logs" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "  Removed logs directory" -ForegroundColor Green
}

# Remove test files
$TestFiles = @(
    "Microsoft.TestPlatform*.dll",
    "Microsoft.VisualStudio.TestPlatform*.dll",
    "Microsoft.VisualStudio.CodeCoverage*.dll",
    "testhost.dll"
)

foreach ($Pattern in $TestFiles) {
    $Files = Get-ChildItem "$ReleaseDir\$Pattern" -ErrorAction SilentlyContinue
    foreach ($File in $Files) {
        Remove-Item $File.FullName -Force -ErrorAction SilentlyContinue
        Write-Host "  Removed: $($File.Name)" -ForegroundColor Green
    }
}

# Remove test resource files from language directories
$LanguageDirs = Get-ChildItem "$ReleaseDir" -Directory | Where-Object { $_.Name -match "^[a-z]{2}(-[A-Z]{2})?$" }
foreach ($LangDir in $LanguageDirs) {
    $TestResourceFiles = Get-ChildItem "$($LangDir.FullName)\Microsoft.*Test*.resources.dll" -ErrorAction SilentlyContinue
    foreach ($File in $TestResourceFiles) {
        Remove-Item $File.FullName -Force -ErrorAction SilentlyContinue
        Write-Host "  Removed: $($LangDir.Name)\$($File.Name)" -ForegroundColor Green
    }
}

Write-Host "File cleanup completed successfully" -ForegroundColor Green

# Check prerequisites
Write-Host "Checking prerequisites..." -ForegroundColor Yellow
$DotNetRuntimePath = "$SetupDir\Prerequisites\windowsdesktop-runtime-6.0.25-win-x64.exe"
if (!(Test-Path $DotNetRuntimePath)) {
    Write-Host "  .NET Runtime not found, downloading..." -ForegroundColor Yellow
    PowerShell -ExecutionPolicy Bypass -File "$SetupDir\Download_Prerequisites_Simple.ps1"
}

if (Test-Path $DotNetRuntimePath) {
    $RuntimeSize = [math]::Round((Get-Item $DotNetRuntimePath).Length / 1MB, 2)
    Write-Host "  .NET Runtime ready: $RuntimeSize MB" -ForegroundColor Green
} else {
    Write-Host "  Warning: .NET Runtime not available for bundling" -ForegroundColor Yellow
}

# Build installer
Write-Host "Building enhanced installer..." -ForegroundColor Cyan
try {
    & $InnoSetupPath $InnoSetupScript
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Enhanced installer built successfully!" -ForegroundColor Green
        
        # Find the created installer file
        $InstallerFile = Get-ChildItem "$OutputDir\*Enhanced*.exe" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        
        if ($InstallerFile) {
            Write-Host "Installer file: $($InstallerFile.FullName)" -ForegroundColor Yellow
            Write-Host "File size: $([math]::Round($InstallerFile.Length / 1MB, 2)) MB" -ForegroundColor Yellow
            
            # Open output directory
            Start-Process "explorer.exe" -ArgumentList $OutputDir
        }
    } else {
        Write-Host "Failed to build installer!" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "Error building installer: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Operation Summary:" -ForegroundColor Cyan
Write-Host "Application built successfully" -ForegroundColor Green
Write-Host "Files cleaned and optimized" -ForegroundColor Green
Write-Host "Prerequisites prepared" -ForegroundColor Green
Write-Host "Enhanced installer created" -ForegroundColor Green
Write-Host "Output directory: $OutputDir" -ForegroundColor Yellow
Write-Host ""
Write-Host "Enhanced installer ready for distribution!" -ForegroundColor Green