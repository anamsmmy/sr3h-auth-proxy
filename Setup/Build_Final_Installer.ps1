# SR3H MACRO - Build Final Installer Script
# سكريبت بناء ملف التثبيت النهائي
# Created: 2025-02-01
# Updated: 2025-02-01

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║         🚀 بناء ملف تثبيت SR3H MACRO النهائي 🚀          ║" -ForegroundColor Cyan
Write-Host "║      Building SR3H MACRO Final Installer Package         ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# المسارات
$ProjectRoot = "C:\2_DEVELOPER_VERSION\SOURCE_CODE"
$SetupDir = "$ProjectRoot\Setup"
$OutputDir = "$SetupDir\Output"
$InnoSetupScript = "$SetupDir\SR3H_MACRO_Setup_Final.iss"
$ReleaseDir = "$ProjectRoot\bin\Release\net6.0-windows"

# إنشاء مجلد الإخراج
if (!(Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    Write-Host "✅ تم إنشاء مجلد الإخراج" -ForegroundColor Green
    Write-Host "   Created output directory: $OutputDir" -ForegroundColor Gray
}

# الخطوة 1: بناء التطبيق في وضع Release
Write-Host ""
Write-Host "📦 الخطوة 1: بناء التطبيق في وضع Release..." -ForegroundColor Yellow
Write-Host "   Step 1: Building application in Release mode..." -ForegroundColor Gray

Set-Location $ProjectRoot

# تنظيف البناء السابق
Write-Host "   🧹 تنظيف البناء السابق..." -ForegroundColor Gray
dotnet clean MacroApp.csproj --configuration Release --verbosity quiet

# بناء التطبيق
Write-Host "   🔨 بناء التطبيق..." -ForegroundColor Gray
$buildOutput = dotnet build MacroApp.csproj --configuration Release --verbosity minimal 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "❌ فشل في بناء التطبيق!" -ForegroundColor Red
    Write-Host "   Build failed!" -ForegroundColor Red
    Write-Host ""
    Write-Host "تفاصيل الخطأ:" -ForegroundColor Yellow
    Write-Host $buildOutput
    exit 1
}

# التحقق من وجود ملف التطبيق
$AppExePath = "$ReleaseDir\SR3H MACRO.exe"
if (!(Test-Path $AppExePath)) {
    Write-Host ""
    Write-Host "❌ ملف التطبيق غير موجود بعد البناء!" -ForegroundColor Red
    Write-Host "   Application file not found after build!" -ForegroundColor Red
    Write-Host "   Expected path: $AppExePath" -ForegroundColor Gray
    exit 1
}

Write-Host "✅ تم بناء التطبيق بنجاح" -ForegroundColor Green
Write-Host "   Application built successfully" -ForegroundColor Gray

# الخطوة 2: تنظيف الملفات غير الضرورية
Write-Host ""
Write-Host "🧹 الخطوة 2: تنظيف الملفات غير الضرورية..." -ForegroundColor Yellow
Write-Host "   Step 2: Cleaning unnecessary files..." -ForegroundColor Gray

# حذف ملفات السجلات
if (Test-Path "$ReleaseDir\logs") {
    Remove-Item "$ReleaseDir\logs" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "   ✅ تم حذف مجلد السجلات" -ForegroundColor Green
}

# حذف ملفات PDB (رموز التصحيح)
$PdbFiles = Get-ChildItem "$ReleaseDir\*.pdb" -ErrorAction SilentlyContinue
foreach ($File in $PdbFiles) {
    Remove-Item $File.FullName -Force -ErrorAction SilentlyContinue
    Write-Host "   ✅ حذف: $($File.Name)" -ForegroundColor Green
}

# حذف ملفات الاختبار
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

# حذف ملفات الاختبار من مجلدات اللغات
$LanguageDirs = Get-ChildItem "$ReleaseDir" -Directory | Where-Object { $_.Name -match "^[a-z]{2}(-[A-Z]{2})?$" }
foreach ($LangDir in $LanguageDirs) {
    $TestResourceFiles = Get-ChildItem "$($LangDir.FullName)\Microsoft.*Test*.resources.dll" -ErrorAction SilentlyContinue
    foreach ($File in $TestResourceFiles) {
        Remove-Item $File.FullName -Force -ErrorAction SilentlyContinue
        $CleanedCount++
    }
    
    # حذف المجلد إذا أصبح فارغاً
    if ((Get-ChildItem $LangDir.FullName -ErrorAction SilentlyContinue).Count -eq 0) {
        Remove-Item $LangDir.FullName -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "✅ تم تنظيف $CleanedCount ملف غير ضروري" -ForegroundColor Green
Write-Host "   Cleaned $CleanedCount unnecessary files" -ForegroundColor Gray

# الخطوة 3: التحقق من Inno Setup
Write-Host ""
Write-Host "🔍 الخطوة 3: التحقق من Inno Setup..." -ForegroundColor Yellow
Write-Host "   Step 3: Checking for Inno Setup..." -ForegroundColor Gray

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
    Write-Host "❌ لم يتم العثور على Inno Setup!" -ForegroundColor Red
    Write-Host "   Inno Setup not found!" -ForegroundColor Red
    Write-Host ""
    Write-Host "📥 يرجى تحميل وتثبيت Inno Setup من:" -ForegroundColor Yellow
    Write-Host "   Please download and install Inno Setup from:" -ForegroundColor Yellow
    Write-Host "   https://jrsoftware.org/isinfo.php" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "🔄 أو استخدم Chocolatey:" -ForegroundColor Yellow
    Write-Host "   Or use Chocolatey:" -ForegroundColor Yellow
    Write-Host "   choco install innosetup" -ForegroundColor Cyan
    Write-Host ""
    
    # محاولة تثبيت Inno Setup باستخدام Chocolatey
    if (Get-Command choco -ErrorAction SilentlyContinue) {
        $response = Read-Host "هل تريد تثبيت Inno Setup تلقائياً باستخدام Chocolatey؟ (Y/N)"
        if ($response -eq "Y" -or $response -eq "y") {
            Write-Host "🔄 جاري تثبيت Inno Setup..." -ForegroundColor Yellow
            choco install innosetup -y
            $InnoSetupPath = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
            
            if (!(Test-Path $InnoSetupPath)) {
                Write-Host "❌ فشل في تثبيت Inno Setup تلقائياً." -ForegroundColor Red
                exit 1
            }
        } else {
            exit 1
        }
    } else {
        exit 1
    }
}

Write-Host "✅ تم العثور على Inno Setup" -ForegroundColor Green
Write-Host "   Found Inno Setup: $InnoSetupPath" -ForegroundColor Gray

# الخطوة 4: بناء ملف التثبيت
Write-Host ""
Write-Host "🔨 الخطوة 4: بناء ملف التثبيت..." -ForegroundColor Yellow
Write-Host "   Step 4: Building installer package..." -ForegroundColor Gray

try {
    $buildProcess = Start-Process -FilePath $InnoSetupPath -ArgumentList "`"$InnoSetupScript`"" -Wait -PassThru -NoNewWindow
    
    if ($buildProcess.ExitCode -eq 0) {
        Write-Host ""
        Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Green
        Write-Host "║           🎉 تم بناء ملف التثبيت بنجاح! 🎉              ║" -ForegroundColor Green
        Write-Host "║        Installer package built successfully!             ║" -ForegroundColor Green
        Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Green
        Write-Host ""
        
        # البحث عن ملف التثبيت المُنشأ
        $InstallerFile = Get-ChildItem "$OutputDir\*.exe" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        
        if ($InstallerFile) {
            $FileSizeMB = [math]::Round($InstallerFile.Length / 1MB, 2)
            
            Write-Host "📊 معلومات ملف التثبيت:" -ForegroundColor Cyan
            Write-Host "   Installer Information:" -ForegroundColor Gray
            Write-Host ""
            Write-Host "   📁 اسم الملف / File Name:" -ForegroundColor Yellow
            Write-Host "      $($InstallerFile.Name)" -ForegroundColor White
            Write-Host ""
            Write-Host "   📂 المسار الكامل / Full Path:" -ForegroundColor Yellow
            Write-Host "      $($InstallerFile.FullName)" -ForegroundColor White
            Write-Host ""
            Write-Host "   📊 حجم الملف / File Size:" -ForegroundColor Yellow
            Write-Host "      $FileSizeMB MB" -ForegroundColor White
            Write-Host ""
            Write-Host "   📅 تاريخ الإنشاء / Created:" -ForegroundColor Yellow
            Write-Host "      $($InstallerFile.LastWriteTime)" -ForegroundColor White
            Write-Host ""
            
            # فتح مجلد الإخراج
            Write-Host "📂 فتح مجلد الإخراج..." -ForegroundColor Cyan
            Write-Host "   Opening output directory..." -ForegroundColor Gray
            Start-Process "explorer.exe" -ArgumentList $OutputDir
        }
    } else {
        Write-Host ""
        Write-Host "❌ فشل في بناء ملف التثبيت!" -ForegroundColor Red
        Write-Host "   Failed to build installer package!" -ForegroundColor Red
        Write-Host "   Exit Code: $($buildProcess.ExitCode)" -ForegroundColor Gray
        exit 1
    }
} catch {
    Write-Host ""
    Write-Host "❌ خطأ في بناء ملف التثبيت:" -ForegroundColor Red
    Write-Host "   Error building installer:" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor Gray
    exit 1
}

# ملخص العملية
Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                    🎯 ملخص العملية                       ║" -ForegroundColor Cyan
Write-Host "║                   Operation Summary                       ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "✅ تم بناء التطبيق بنجاح" -ForegroundColor Green
Write-Host "   Application built successfully" -ForegroundColor Gray
Write-Host ""
Write-Host "✅ تم تنظيف الملفات غير الضرورية" -ForegroundColor Green
Write-Host "   Unnecessary files cleaned" -ForegroundColor Gray
Write-Host ""
Write-Host "✅ تم إنشاء ملف التثبيت" -ForegroundColor Green
Write-Host "   Installer package created" -ForegroundColor Gray
Write-Host ""
Write-Host "📁 مجلد الإخراج / Output Directory:" -ForegroundColor Yellow
Write-Host "   $OutputDir" -ForegroundColor White
Write-Host ""
Write-Host "🚀 ملف التثبيت جاهز للتوزيع!" -ForegroundColor Green
Write-Host "   Installer is ready for distribution!" -ForegroundColor Gray
Write-Host ""
Write-Host "💡 ملاحظة: يتطلب التطبيق .NET 6.0 Desktop Runtime" -ForegroundColor Yellow
Write-Host "   Note: Application requires .NET 6.0 Desktop Runtime" -ForegroundColor Gray
Write-Host ""