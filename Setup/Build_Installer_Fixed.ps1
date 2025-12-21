# SR3H MACRO - Build Installer Script
# Created: 2025-01-31
# Updated: 2025-01-31

Write-Host "🚀 بناء ملف تثبيت SR3H MACRO..." -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Cyan

# المسارات
$ProjectRoot = "c:\MACRO_SR3H"
$SetupDir = "$ProjectRoot\Setup"
$OutputDir = "$SetupDir\Output"
$InnoSetupScript = "$SetupDir\SR3H_MACRO_Setup.iss"

# إنشاء مجلد الإخراج
if (!(Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force
    Write-Host "✅ تم إنشاء مجلد الإخراج: $OutputDir" -ForegroundColor Yellow
}

# التحقق من وجود Inno Setup
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
    Write-Host "❌ لم يتم العثور على Inno Setup!" -ForegroundColor Red
    Write-Host "📥 يرجى تحميل وتثبيت Inno Setup من: https://jrsoftware.org/isinfo.php" -ForegroundColor Yellow
    Write-Host "🔄 أو استخدم Chocolatey: choco install innosetup" -ForegroundColor Yellow
    
    # محاولة تثبيت Inno Setup باستخدام Chocolatey
    Write-Host "🔄 محاولة تثبيت Inno Setup تلقائياً..." -ForegroundColor Yellow
    try {
        if (Get-Command choco -ErrorAction SilentlyContinue) {
            choco install innosetup -y
            $InnoSetupPath = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
        } else {
            Write-Host "❌ Chocolatey غير مثبت. يرجى تثبيت Inno Setup يدوياً." -ForegroundColor Red
            exit 1
        }
    } catch {
        Write-Host "❌ فشل في تثبيت Inno Setup تلقائياً." -ForegroundColor Red
        exit 1
    }
}

Write-Host "✅ تم العثور على Inno Setup: $InnoSetupPath" -ForegroundColor Green

# التحقق من وجود ملفات التطبيق
$AppExePath = "$ProjectRoot\bin\Release\net6.0-windows\SR3H MACRO.exe"
if (!(Test-Path $AppExePath)) {
    Write-Host "❌ ملف التطبيق غير موجود: $AppExePath" -ForegroundColor Red
    Write-Host "🔄 بناء التطبيق أولاً..." -ForegroundColor Yellow
    
    Set-Location $ProjectRoot
    Write-Host "🔨 بناء التطبيق في وضع Release..." -ForegroundColor Yellow
    dotnet build MacroApp.csproj --configuration Release --verbosity quiet
    
    if (!(Test-Path $AppExePath)) {
        Write-Host "❌ فشل في بناء التطبيق!" -ForegroundColor Red
        exit 1
    }
}

Write-Host "✅ ملف التطبيق موجود: $AppExePath" -ForegroundColor Green

# تنظيف الملفات الحساسة والغير ضرورية
Write-Host "🧹 تنظيف الملفات الحساسة والغير ضرورية..." -ForegroundColor Yellow
$ReleaseDir = "$ProjectRoot\bin\Release\net6.0-windows"

# حذف ملفات السجلات
if (Test-Path "$ReleaseDir\logs") {
    Remove-Item "$ReleaseDir\logs" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "  ✅ تم حذف مجلد السجلات" -ForegroundColor Green
}

# حذف ملفات الاختبار
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
        Write-Host "  ✅ تم حذف: $($File.Name)" -ForegroundColor Green
    }
}

# حذف ملفات الاختبار من مجلدات اللغات
$LanguageDirs = Get-ChildItem "$ReleaseDir" -Directory | Where-Object { $_.Name -match "^[a-z]{2}(-[A-Z]{2})?$" }
foreach ($LangDir in $LanguageDirs) {
    $TestResourceFiles = Get-ChildItem "$($LangDir.FullName)\Microsoft.*Test*.resources.dll" -ErrorAction SilentlyContinue
    foreach ($File in $TestResourceFiles) {
        Remove-Item $File.FullName -Force -ErrorAction SilentlyContinue
        Write-Host "  ✅ تم حذف: $($LangDir.Name)\$($File.Name)" -ForegroundColor Green
    }
}

Write-Host "✅ تم تنظيف الملفات بنجاح" -ForegroundColor Green

# بناء ملف التثبيت
Write-Host "🔨 بناء ملف التثبيت..." -ForegroundColor Cyan
try {
    & $InnoSetupPath $InnoSetupScript
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "🎉 تم بناء ملف التثبيت بنجاح!" -ForegroundColor Green
        
        # البحث عن ملف التثبيت المُنشأ
        $InstallerFile = Get-ChildItem "$OutputDir\*.exe" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        
        if ($InstallerFile) {
            Write-Host "📁 ملف التثبيت: $($InstallerFile.FullName)" -ForegroundColor Yellow
            Write-Host "📊 حجم الملف: $([math]::Round($InstallerFile.Length / 1MB, 2)) MB" -ForegroundColor Yellow
            
            # فتح مجلد الإخراج
            Start-Process "explorer.exe" -ArgumentList $OutputDir
        }
    } else {
        Write-Host "❌ فشل في بناء ملف التثبيت!" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "❌ خطأ في بناء ملف التثبيت: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "🎯 ملخص العملية:" -ForegroundColor Cyan
Write-Host "✅ تم بناء التطبيق بنجاح" -ForegroundColor Green
Write-Host "✅ تم تنظيف الملفات الحساسة" -ForegroundColor Green
Write-Host "✅ تم إنشاء ملف التثبيت" -ForegroundColor Green
Write-Host "📁 مجلد الإخراج: $OutputDir" -ForegroundColor Yellow
Write-Host ""
Write-Host "🚀 ملف التثبيت جاهز للتوزيع!" -ForegroundColor Green