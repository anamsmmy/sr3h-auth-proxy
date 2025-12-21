# SR3H MACRO - Download Prerequisites Script
# Download required prerequisites for the application

Write-Host "Downloading prerequisites for SR3H MACRO..." -ForegroundColor Green

# Paths
$PrereqDir = "C:\MACRO_SR3H\Setup\Prerequisites"

# Create directories
if (!(Test-Path $PrereqDir)) {
    New-Item -ItemType Directory -Path $PrereqDir -Force
    Write-Host "Created prerequisites directory: $PrereqDir" -ForegroundColor Yellow
}

# Download .NET 6.0 Desktop Runtime
$DotNetUrl = "https://download.microsoft.com/download/3/3/c/33c8de32-9f0b-4c1b-9b5d-0a9f8b2b5b5a/windowsdesktop-runtime-6.0.25-win-x64.exe"
$DotNetFile = "$PrereqDir\windowsdesktop-runtime-6.0.25-win-x64.exe"

Write-Host "Downloading .NET 6.0 Desktop Runtime..." -ForegroundColor Cyan

if (!(Test-Path $DotNetFile)) {
    try {
        Invoke-WebRequest -Uri $DotNetUrl -OutFile $DotNetFile -UseBasicParsing
        Write-Host "Successfully downloaded .NET Runtime" -ForegroundColor Green
    }
    catch {
        Write-Host "Failed to download .NET Runtime: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "Please download manually from: https://dotnet.microsoft.com/download/dotnet/6.0" -ForegroundColor Yellow
    }
} else {
    Write-Host ".NET Runtime already exists" -ForegroundColor Green
}

# Create info file
$InfoContent = @"
Prerequisites Information - SR3H MACRO
=====================================

Required Files:

1. .NET 6.0 Desktop Runtime (x64)
   - File: windowsdesktop-runtime-6.0.25-win-x64.exe
   - Size: ~55 MB
   - Description: Required to run the application
   - Installation: Automatic during SR3H MACRO setup

Installation Notes:
- .NET Runtime will be installed automatically if not present
- All files are safe and from official Microsoft sources

System Requirements:
- Windows 10 version 1809 or later
- x64 (64-bit) architecture
- Administrator privileges for installation

© 2025 SR3H Development Team
"@

$InfoContent | Out-File -FilePath "$PrereqDir\Prerequisites_Info.txt" -Encoding UTF8

Write-Host ""
Write-Host "Prerequisites download completed!" -ForegroundColor Green
Write-Host "Directory: $PrereqDir" -ForegroundColor Yellow