# SR3H MACRO - Download Prerequisites Script
# تحميل المتطلبات المسبقة للتطبيق
# Created: 2025-01-31

Write-Host "🔽 تحميل المتطلبات المسبقة لـ SR3H MACRO..." -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Cyan

# المسارات
$PrereqDir = "C:\MACRO_SR3H\Setup\Prerequisites"
$TempDir = "$env:TEMP\SR3H_Prerequisites"

# إنشاء المجلدات
if (!(Test-Path $PrereqDir)) {
    New-Item -ItemType Directory -Path $PrereqDir -Force
    Write-Host "✅ تم إنشاء مجلد المتطلبات: $PrereqDir" -ForegroundColor Yellow
}

if (!(Test-Path $TempDir)) {
    New-Item -ItemType Directory -Path $TempDir -Force
}

# URLs للتحميل
$Downloads = @{
    ".NET 6.0 Desktop Runtime (x64)" = @{
        Url = "https://download.microsoft.com/download/3/3/c/33c8de32-9f0b-4c1b-9b5d-0a9f8b2b5b5a/windowsdesktop-runtime-6.0.25-win-x64.exe"
        FileName = "windowsdesktop-runtime-6.0.25-win-x64.exe"
        Size = "~55 MB"
    }
    "Visual C++ 2015-2022 Redistributable (x64)" = @{
        Url = "https://aka.ms/vs/17/release/vc_redist.x64.exe"
        FileName = "vc_redist.x64.exe"
        Size = "~25 MB"
    }
}

# تحميل الملفات
foreach ($Item in $Downloads.GetEnumerator()) {
    $Name = $Item.Key
    $Url = $Item.Value.Url
    $FileName = $Item.Value.FileName
    $Size = $Item.Value.Size
    $FilePath = "$PrereqDir\$FileName"
    
    Write-Host "📥 تحميل: $Name ($Size)" -ForegroundColor Cyan
    
    if (Test-Path $FilePath) {
        Write-Host "  ✅ الملف موجود بالفعل: $FileName" -ForegroundColor Green
        continue
    }
    
    try {
        # تحميل الملف
        Write-Host "  🔄 جاري التحميل من: $Url" -ForegroundColor Yellow
        
        $WebClient = New-Object System.Net.WebClient
        $WebClient.DownloadFile($Url, $FilePath)
        $WebClient.Dispose()
        
        if (Test-Path $FilePath) {
            $FileSize = [math]::Round((Get-Item $FilePath).Length / 1MB, 2)
            Write-Host "  ✅ تم التحميل بنجاح: $FileName ($FileSize MB)" -ForegroundColor Green
        } else {
            Write-Host "  ❌ فشل في التحميل: $FileName" -ForegroundColor Red
        }
    }
    catch {
        Write-Host "  ❌ خطأ في التحميل: $($_.Exception.Message)" -ForegroundColor Red
        
        # محاولة بديلة باستخدام Invoke-WebRequest
        try {
            Write-Host "  🔄 محاولة بديلة..." -ForegroundColor Yellow
            Invoke-WebRequest -Uri $Url -OutFile $FilePath -UseBasicParsing
            
            if (Test-Path $FilePath) {
                $FileSize = [math]::Round((Get-Item $FilePath).Length / 1MB, 2)
                Write-Host "  ✅ تم التحميل بنجاح (المحاولة البديلة): $FileName ($fileSize MB)" -ForegroundColor Green
            }
        }
        catch {
            Write-Host "  ❌ فشل في المحاولة البديلة: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

# إنشاء ملف معلومات المتطلبات
$InfoContent = @"
معلومات المتطلبات المسبقة - SR3H MACRO
==========================================

📋 الملفات المطلوبة:

1. .NET 6.0 Desktop Runtime (x64)
   - الملف: windowsdesktop-runtime-6.0.25-win-x64.exe
   - الحجم: ~55 MB
   - الوصف: مطلوب لتشغيل التطبيق
   - التثبيت: تلقائي أثناء تثبيت SR3H MACRO

2. Visual C++ 2015-2022 Redistributable (x64)
   - الملف: vc_redist.x64.exe
   - الحجم: ~25 MB
   - الوصف: مكتبات C++ المطلوبة
   - التثبيت: يدوي (إذا لزم الأمر)

🔧 ملاحظات التثبيت:
- سيتم تثبيت .NET Runtime تلقائياً إذا لم يكن موجوداً
- قد تحتاج لتثبيت Visual C++ Redistributable يدوياً
- جميع الملفات آمنة ومن مصادر Microsoft الرسمية

⚠️ متطلبات النظام:
- Windows 10 version 1809 أو أحدث
- معمارية x64 (64-bit)
- صلاحيات إدارية للتثبيت

© 2025 SR3H Development Team
"@

$InfoContent | Out-File -FilePath "$PrereqDir\معلومات_المتطلبات.txt" -Encoding UTF8

# تنظيف المجلد المؤقت
if (Test-Path $TempDir) {
    Remove-Item $TempDir -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "🎯 ملخص التحميل:" -ForegroundColor Cyan
$DownloadedFiles = Get-ChildItem $PrereqDir -Filter "*.exe"
foreach ($File in $DownloadedFiles) {
    $Size = [math]::Round($File.Length / 1MB, 2)
    Write-Host "✅ $($File.Name) - $Size MB" -ForegroundColor Green
}

Write-Host ""
Write-Host "📁 مجلد المتطلبات: $PrereqDir" -ForegroundColor Yellow
Write-Host "🎉 تم تحميل المتطلبات المسبقة بنجاح!" -ForegroundColor Green