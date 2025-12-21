# SR3H MACRO - Build Final Installer Script
# Created: 2025-02-01

Write-Host ""
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "  Building SR3H MACRO Final Installer Package" -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host ""

# Paths
$ProjectRoot = "C:\2_DEVELOPER_VERSION\SOURCE_CODE"
$SetupDir = "$ProjectRoot\Setup"
$OutputDir = "$SetupDir\Output"
$InnoSetupScript = "$SetupDir\SR3H_MACRO_Setup_Final.iss"
$ReleaseDir = "$ProjectRoot\bin\Release\net6.0-windows"

# Create output directory
if (!(Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    Write-Host "[OK] Created output directory" -ForegroundColor Green
    Write-Host "     $OutputDir" -ForegroundColor Gray
}

# Step 1: Build application in Release mode
Write-Host ""
Write-Host "[STEP 1] Building application in Release mode..." -ForegroundColor Yellow

Set-Location $ProjectRoot

# Clean previous build
Write-Host "     Cleaning previous build..." -ForegroundColor Gray
dotnet clean MacroApp.csproj --configuration Release --verbosity quiet

# Build application
Write-Host "     Building application..." -ForegroundColor Gray
$buildOutput = dotnet build MacroApp.csproj --configuration Release --verbosity minimal 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "[ERROR] Build failed!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Error details:" -ForegroundColor Yellow
    Write-Host $buildOutput
    exit 1
}

# Verify application file exists
$AppExePath = "$ReleaseDir\SR3H MACRO.exe"
if (!(Test-Path $AppExePath)) {
    Write-Host ""
    Write-Host "[ERROR] Application file not found after build!" -ForegroundColor Red
    Write-Host "        Expected path: $AppExePath" -ForegroundColor Gray
    exit 1
}

Write-Host "[OK] Application built successfully" -ForegroundColor Green

# Step 2: Clean unnecessary files
Write-Host ""
Write-Host "[STEP 2] Cleaning unnecessary files..." -ForegroundColor Yellow

# Delete logs folder
if (Test-Path "$ReleaseDir\logs") {
    Remove-Item "$ReleaseDir\logs" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "     [OK] Deleted logs folder" -ForegroundColor Green
}

# Delete PDB files (debug symbols)
$PdbFiles = Get-ChildItem "$ReleaseDir\*.pdb" -ErrorAction SilentlyContinue
foreach ($File in $PdbFiles) {
    Remove-Item $File.FullName -Force -ErrorAction SilentlyContinue
    Write-Host "     [OK] Deleted: $($File.Name)" -ForegroundColor Green
}

# Delete test files
$TestFiles = @(
    "Microsoft.TestPlatform*.dll",
    "Microsoft.VisualStudio.TestPlatform*.dll",
    "Microsoft.VisualStudio.CodeCoverage*.dll",
    "testhost.dll"
)

$CleanedCount = 0
foreach ($Pattern in $TestFiles) {
    $Files = Get-ChildItem "$ReleaseDir\$Pattern" -ErrorAction SilentlyContinue
    foreach ($File in $Files) {
        Remove-Item $File.FullName -Force -ErrorAction SilentlyContinue
        $CleanedCount++
    }
}

# Delete test files from language folders
$LanguageDirs = Get-ChildItem "$ReleaseDir" -Directory | Where-Object { $_.Name -match "^[a-z]{2}(-[A-Z]{2})?$" }
foreach ($LangDir in $LanguageDirs) {
    $TestResourceFiles = Get-ChildItem "$($LangDir.FullName)\Microsoft.*Test*.resources.dll" -ErrorAction SilentlyContinue
    foreach ($File in $TestResourceFiles) {
        Remove-Item $File.FullName -Force -ErrorAction SilentlyContinue
        $CleanedCount++
    }
    
    # Delete folder if empty
    if ((Get-ChildItem $LangDir.FullName -ErrorAction SilentlyContinue).Count -eq 0) {
        Remove-Item $LangDir.FullName -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "[OK] Cleaned $CleanedCount unnecessary files" -ForegroundColor Green

# Step 3: Check for Inno Setup
Write-Host ""
Write-Host "[STEP 3] Checking for Inno Setup..." -ForegroundColor Yellow

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
    Write-Host ""
    Write-Host "[ERROR] Inno Setup not found!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please download and install Inno Setup from:" -ForegroundColor Yellow
    Write-Host "https://jrsoftware.org/isinfo.php" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Or use Chocolatey:" -ForegroundColor Yellow
    Write-Host "choco install innosetup" -ForegroundColor Cyan
    Write-Host ""
    
    # Try to install using Chocolatey
    if (Get-Command choco -ErrorAction SilentlyContinue) {
        $response = Read-Host "Do you want to install Inno Setup automatically using Chocolatey? (Y/N)"
        if ($response -eq "Y" -or $response -eq "y") {
            Write-Host "Installing Inno Setup..." -ForegroundColor Yellow
            choco install innosetup -y
            $InnoSetupPath = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
            
            if (!(Test-Path $InnoSetupPath)) {
                Write-Host "[ERROR] Failed to install Inno Setup automatically." -ForegroundColor Red
                exit 1
            }
        } else {
            exit 1
        }
    } else {
        exit 1
    }
}

Write-Host "[OK] Found Inno Setup" -ForegroundColor Green
Write-Host "     $InnoSetupPath" -ForegroundColor Gray

# Step 4: Build installer package
Write-Host ""
Write-Host "[STEP 4] Building installer package..." -ForegroundColor Yellow

try {
    $buildProcess = Start-Process -FilePath $InnoSetupPath -ArgumentList "`"$InnoSetupScript`"" -Wait -PassThru -NoNewWindow
    
    if ($buildProcess.ExitCode -eq 0) {
        Write-Host ""
        Write-Host "========================================================" -ForegroundColor Green
        Write-Host "  Installer package built successfully!" -ForegroundColor Green
        Write-Host "========================================================" -ForegroundColor Green
        Write-Host ""
        
        # Find the created installer file
        $InstallerFile = Get-ChildItem "$OutputDir\*.exe" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        
        if ($InstallerFile) {
            $FileSizeMB = [math]::Round($InstallerFile.Length / 1MB, 2)
            
            Write-Host "Installer Information:" -ForegroundColor Cyan
            Write-Host ""
            Write-Host "  File Name:" -ForegroundColor Yellow
            Write-Host "  $($InstallerFile.Name)" -ForegroundColor White
            Write-Host ""
            Write-Host "  Full Path:" -ForegroundColor Yellow
            Write-Host "  $($InstallerFile.FullName)" -ForegroundColor White
            Write-Host ""
            Write-Host "  File Size:" -ForegroundColor Yellow
            Write-Host "  $FileSizeMB MB" -ForegroundColor White
            Write-Host ""
            Write-Host "  Created:" -ForegroundColor Yellow
            Write-Host "  $($InstallerFile.LastWriteTime)" -ForegroundColor White
            Write-Host ""
            
            # Calculate SHA256 hash
            Write-Host "  SHA256 Hash:" -ForegroundColor Yellow
            $hash = Get-FileHash $InstallerFile.FullName -Algorithm SHA256
            Write-Host "  $($hash.Hash)" -ForegroundColor White
            Write-Host ""
            
            # Open output directory
            Write-Host "Opening output directory..." -ForegroundColor Cyan
            Start-Process "explorer.exe" -ArgumentList $OutputDir
        }
    } else {
        Write-Host ""
        Write-Host "[ERROR] Failed to build installer package!" -ForegroundColor Red
        Write-Host "        Exit Code: $($buildProcess.ExitCode)" -ForegroundColor Gray
        exit 1
    }
} catch {
    Write-Host ""
    Write-Host "[ERROR] Error building installer:" -ForegroundColor Red
    Write-Host "        $($_.Exception.Message)" -ForegroundColor Gray
    exit 1
}

# Summary
Write-Host ""
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "  Operation Summary" -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "[OK] Application built successfully" -ForegroundColor Green
Write-Host "[OK] Unnecessary files cleaned" -ForegroundColor Green
Write-Host "[OK] Installer package created" -ForegroundColor Green
Write-Host ""
Write-Host "Output Directory:" -ForegroundColor Yellow
Write-Host "$OutputDir" -ForegroundColor White
Write-Host ""
Write-Host "Installer is ready for distribution!" -ForegroundColor Green
Write-Host ""
Write-Host "Note: Application requires .NET 6.0 Desktop Runtime" -ForegroundColor Yellow
Write-Host ""