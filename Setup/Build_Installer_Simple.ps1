# SR3H MACRO - Simple Build Installer Script
# Created: 2025-01-31

Write-Host "Building SR3H MACRO Installer..." -ForegroundColor Green

# Paths
$ProjectRoot = "c:\MACRO_SR3H"
$SetupDir = "$ProjectRoot\Setup"
$OutputDir = "$SetupDir\Output"
$InnoSetupScript = "$SetupDir\SR3H_MACRO_Setup.iss"

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
    Write-Host "Please download from: https://jrsoftware.org/isinfo.php" -ForegroundColor Yellow
    Write-Host "Or install with Chocolatey: choco install innosetup" -ForegroundColor Yellow
    
    # Try to install with Chocolatey
    Write-Host "Attempting to install Inno Setup automatically..." -ForegroundColor Yellow
    try {
        if (Get-Command choco -ErrorAction SilentlyContinue) {
            choco install innosetup -y
            $InnoSetupPath = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
        } else {
            Write-Host "Chocolatey not installed. Please install Inno Setup manually." -ForegroundColor Red
            exit 1
        }
    } catch {
        Write-Host "Failed to install Inno Setup automatically." -ForegroundColor Red
        exit 1
    }
}

Write-Host "Found Inno Setup: $InnoSetupPath" -ForegroundColor Green

# Check application files
$AppExePath = "$ProjectRoot\bin\Release\net6.0-windows\SR3H MACRO.exe"
if (!(Test-Path $AppExePath)) {
    Write-Host "Application file not found: $AppExePath" -ForegroundColor Red
    Write-Host "Building application first..." -ForegroundColor Yellow
    
    Set-Location $ProjectRoot
    dotnet build MacroApp.csproj --configuration Release
    
    if (!(Test-Path $AppExePath)) {
        Write-Host "Failed to build application!" -ForegroundColor Red
        exit 1
    }
}

Write-Host "Application file found: $AppExePath" -ForegroundColor Green

# Build installer
Write-Host "Building installer..." -ForegroundColor Cyan
try {
    & $InnoSetupPath $InnoSetupScript
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Installer built successfully!" -ForegroundColor Green
        
        # Find the created installer file
        $InstallerFile = Get-ChildItem "$OutputDir\*.exe" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        
        if ($InstallerFile) {
            Write-Host "Installer file: $($InstallerFile.FullName)" -ForegroundColor Yellow
            Write-Host "File size: $([math]::Round($InstallerFile.Length / 1MB, 2)) MB" -ForegroundColor Yellow
            
            # Open output folder
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
Write-Host "Process Summary:" -ForegroundColor Cyan
Write-Host "Application built successfully" -ForegroundColor Green
Write-Host "Installer created successfully" -ForegroundColor Green
Write-Host "Output directory: $OutputDir" -ForegroundColor Yellow
Write-Host ""
Write-Host "Installer ready for distribution!" -ForegroundColor Green