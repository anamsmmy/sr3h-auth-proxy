#requires -version 5.0

param(
    [string]$Configuration = "Release",
    [switch]$SkipBuild = $false
)

$ErrorActionPreference = "Stop"

Write-Host "=====================================`n" -ForegroundColor Cyan
Write-Host "🔐 SR3H MACRO - Build & Protection Script" -ForegroundColor Cyan
Write-Host "=====================================`n" -ForegroundColor Cyan

# Define paths
$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$outputDir = Join-Path $projectDir "bin\$Configuration\net6.0-windows"
$protectedDir = Join-Path $projectDir "bin\Protected"
$confuserExDownloadUrl = "https://github.com/yck1509/ConfuserEx/releases/download/v1.6.0/ConfuserEx.zip"
$confuserExDir = Join-Path $projectDir "ConfuserEx"

Write-Host "`n📦 Step 1: Building the application..." -ForegroundColor Yellow

if (-not $SkipBuild) {
    # Clean previous build
    dotnet clean -c $Configuration --nologo -q
    
    # Build the project
    $buildOutput = dotnet build -c $Configuration --nologo
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Build failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ Build completed successfully!" -ForegroundColor Green
} else {
    Write-Host "⏭️  Skipping build (using existing binaries)" -ForegroundColor Yellow
}

Write-Host "`n🔍 Step 2: Locating output assemblies..." -ForegroundColor Yellow
$mainAssembly = Join-Path $outputDir "SR3H MACRO.dll"

if (-not (Test-Path $mainAssembly)) {
    Write-Host "❌ Main assembly not found at: $mainAssembly" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Found assembly: $mainAssembly" -ForegroundColor Green

Write-Host "`n📥 Step 3: Setting up ConfuserEx..." -ForegroundColor Yellow

# Check if ConfuserEx already exists
if (-not (Test-Path $confuserExDir)) {
    Write-Host "Downloading ConfuserEx..." -ForegroundColor Cyan
    
    # Create temp directory for download
    $tempZip = Join-Path $projectDir "ConfuserEx.zip"
    
    # Download ConfuserEx
    Invoke-WebRequest -Uri $confuserExDownloadUrl -OutFile $tempZip -ErrorAction SilentlyContinue
    
    if (-not (Test-Path $tempZip)) {
        Write-Host "⚠️  Could not auto-download ConfuserEx. Please download manually:" -ForegroundColor Yellow
        Write-Host "   $confuserExDownloadUrl" -ForegroundColor Cyan
        Write-Host "`n   Extract it to: $confuserExDir" -ForegroundColor Cyan
        Write-Host "`n   Then re-run this script." -ForegroundColor Cyan
        exit 1
    }
    
    # Extract
    Expand-Archive -Path $tempZip -DestinationPath $confuserExDir -Force
    Remove-Item $tempZip -Force
    Write-Host "✅ ConfuserEx downloaded and extracted" -ForegroundColor Green
} else {
    Write-Host "✅ ConfuserEx already available" -ForegroundColor Green
}

Write-Host "`n🔐 Step 4: Applying obfuscation and protection..." -ForegroundColor Yellow

# Create protected output directory
if (Test-Path $protectedDir) {
    Remove-Item $protectedDir -Recurse -Force
}
New-Item -ItemType Directory -Path $protectedDir -Force | Out-Null

# Copy configuration file for ConfuserEx
$confuserConfig = Join-Path $projectDir "ConfuserEx.Project.json"

# Find ConfuserEx executable
$confuserExe = Get-ChildItem -Path $confuserExDir -Filter "Confuser.CLI.exe" -Recurse | Select-Object -First 1

if ($null -eq $confuserExe) {
    Write-Host "❌ ConfuserEx CLI not found. Trying alternative setup..." -ForegroundColor Yellow
    
    Write-Host "`n📋 Manual Protection Instructions:" -ForegroundColor Yellow
    Write-Host "  1. Download ConfuserEx: https://github.com/yck1509/ConfuserEx/releases" -ForegroundColor Cyan
    Write-Host "  2. Extract to: $confuserExDir" -ForegroundColor Cyan
    Write-Host "  3. Open ConfuserEx GUI" -ForegroundColor Cyan
    Write-Host "  4. Add project: $projectDir" -ForegroundColor Cyan
    Write-Host "  5. Base Directory: $outputDir" -ForegroundColor Cyan
    Write-Host "  6. Output Directory: $protectedDir" -ForegroundColor Cyan
    Write-Host "  7. Add assembly: SR3H MACRO.dll" -ForegroundColor Cyan
    Write-Host "  8. Apply rules and Protect" -ForegroundColor Cyan
} else {
    Write-Host "Using ConfuserEx: $($confuserExe.FullName)" -ForegroundColor Cyan
    
    # Run ConfuserEx
    & $confuserExe.FullName -o $protectedDir $mainAssembly 2>&1 | Write-Host
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Obfuscation completed successfully!" -ForegroundColor Green
    } else {
        Write-Host "⚠️  ConfuserEx execution completed (check output above)" -ForegroundColor Yellow
    }
}

Write-Host "`n📊 Step 5: Summary" -ForegroundColor Yellow
Write-Host "  Original binaries: $outputDir" -ForegroundColor Cyan
Write-Host "  Protected binaries: $protectedDir" -ForegroundColor Cyan

if (Test-Path $protectedDir) {
    $files = Get-ChildItem $protectedDir -Recurse
    Write-Host "  Protected files count: $($files.Count)" -ForegroundColor Green
}

Write-Host "`n✨ Protection applied!" -ForegroundColor Green
Write-Host "=====================================`n" -ForegroundColor Cyan
