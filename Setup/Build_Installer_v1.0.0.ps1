# SR3H MACRO Installer Builder Script v1.0.0
# PowerShell script to build the installer

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "بناء ملف التثبيت SR3H MACRO v1.0.0" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check for Inno Setup
$InnoPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $InnoPath)) {
    Write-Host "خطأ: لم يتم العثور على Inno Setup!" -ForegroundColor Red
    Write-Host "الرجاء تثبيت Inno Setup 6 من الرابط التالي:" -ForegroundColor Yellow
    Write-Host "https://jrsoftware.org/isdl.php" -ForegroundColor Blue
    Write-Host ""
    Read-Host "اضغط Enter للخروج"
    exit 1
}

# Create output directory
$OutputDir = "C:\MACRO_SR3H\Setup\Output"
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

# Build the application
Write-Host "جاري بناء التطبيق..." -ForegroundColor Yellow
try {
    $buildResult = dotnet build "C:\MACRO_SR3H\MacroApp.csproj" --configuration Release
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed"
    }
    Write-Host "تم بناء التطبيق بنجاح ✅" -ForegroundColor Green
} catch {
    Write-Host "خطأ في بناء التطبيق!" -ForegroundColor Red
    Read-Host "اضغط Enter للخروج"
    exit 1
}

Write-Host ""
Write-Host "جاري إنشاء ملف التثبيت..." -ForegroundColor Yellow

# Build the installer
try {
    $installerResult = & $InnoPath "C:\MACRO_SR3H\Setup\SR3H_MACRO_Setup_v1.0.0.iss"
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "تم إنشاء ملف التثبيت بنجاح! ✅" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "الملف موجود في: $OutputDir\SR3H_MACRO_Setup.exe" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "خصائص ملف التثبيت:" -ForegroundColor Yellow
        Write-Host "- يدعم اللغة العربية" -ForegroundColor White
        Write-Host "- يتحقق من .NET 6.0 Runtime" -ForegroundColor White
        Write-Host "- ينشئ اختصار على سطح المكتب" -ForegroundColor White
        Write-Host "- يسجل في قائمة البرامج" -ForegroundColor White
        Write-Host "- يحمي الأيقونات من التلاعب" -ForegroundColor White
        Write-Host ""
        
        # Open output folder
        Start-Process "explorer.exe" -ArgumentList $OutputDir
        
        # Show file size
        $installerFile = "$OutputDir\SR3H_MACRO_Setup.exe"
        if (Test-Path $installerFile) {
            $fileSize = [math]::Round((Get-Item $installerFile).Length / 1MB, 2)
            Write-Host "حجم ملف التثبيت: $fileSize MB" -ForegroundColor Cyan
        }
    } else {
        throw "Installer build failed"
    }
} catch {
    Write-Host ""
    Write-Host "خطأ في إنشاء ملف التثبيت!" -ForegroundColor Red
    Write-Host "الرجاء التحقق من ملف السكريبت والملفات المطلوبة." -ForegroundColor Yellow
}

Write-Host ""
Read-Host "اضغط Enter للخروج"