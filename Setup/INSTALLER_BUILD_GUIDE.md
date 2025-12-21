# 📦 دليل بناء ملف التثبيت - SR3H MACRO
# SR3H MACRO Installer Build Guide

---

## 📋 جدول المحتويات | Table of Contents

1. [المتطلبات الأساسية](#المتطلبات-الأساسية)
2. [خطوات البناء](#خطوات-البناء)
3. [هيكل الملفات](#هيكل-الملفات)
4. [التخصيص](#التخصيص)
5. [استكشاف الأخطاء](#استكشاف-الأخطاء)
6. [الأسئلة الشائعة](#الأسئلة-الشائعة)

---

## 🔧 المتطلبات الأساسية

### 1. البرامج المطلوبة

#### أ) .NET SDK 6.0 أو أحدث
```powershell
# التحقق من التثبيت
dotnet --version

# التحميل من (يُنصح بأحدث إصدار)
https://dotnet.microsoft.com/download/dotnet
```

#### ب) Inno Setup 6.x
```powershell
# التحميل من
https://jrsoftware.org/isinfo.php

# أو باستخدام Chocolatey
choco install innosetup
```

#### ج) PowerShell 5.1 أو أحدث
```powershell
# التحقق من الإصدار
$PSVersionTable.PSVersion
```

### 2. الأذونات المطلوبة

- ✅ صلاحيات المسؤول (Administrator)
- ✅ حق الكتابة في مجلد المشروع
- ✅ حق تنفيذ سكريبتات PowerShell

---

## 🚀 خطوات البناء

### الطريقة 1: استخدام ملف BAT (الأسهل)

```batch
# 1. افتح مجلد Setup
cd C:\2_DEVELOPER_VERSION\SOURCE_CODE\Setup

# 2. شغّل ملف BAT
BUILD_INSTALLER.bat
```

### الطريقة 2: استخدام PowerShell مباشرة

```powershell
# 1. افتح PowerShell كمسؤول
# 2. انتقل إلى مجلد Setup
Set-Location "C:\2_DEVELOPER_VERSION\SOURCE_CODE\Setup"

# 3. شغّل السكريبت
.\Build_Final_Installer.ps1
```

### الطريقة 3: البناء اليدوي (للمطورين المتقدمين)

#### الخطوة 1: بناء التطبيق
```powershell
cd C:\2_DEVELOPER_VERSION\SOURCE_CODE
dotnet clean MacroApp.csproj --configuration Release
dotnet build MacroApp.csproj --configuration Release
```

#### الخطوة 2: تنظيف الملفات
```powershell
# حذف ملفات الاختبار
Remove-Item "bin\Release\net6.0-windows\Microsoft.TestPlatform*.dll"
Remove-Item "bin\Release\net6.0-windows\testhost.dll"

# حذف ملفات PDB
Remove-Item "bin\Release\net6.0-windows\*.pdb"

# حذف السجلات
Remove-Item "bin\Release\net6.0-windows\logs" -Recurse -Force
```

#### الخطوة 3: بناء ملف التثبيت
```powershell
# تشغيل Inno Setup Compiler
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" "C:\2_DEVELOPER_VERSION\SOURCE_CODE\Setup\SR3H_MACRO_Setup_Final.iss"
```

---

## 📁 هيكل الملفات

```
C:\2_DEVELOPER_VERSION\SOURCE_CODE\
│
├── Setup\
│   ├── SR3H_MACRO_Setup_Final.iss      # سكريبت Inno Setup الرئيسي
│   ├── Build_Final_Installer.ps1       # سكريبت البناء الآلي
│   ├── BUILD_INSTALLER.bat             # ملف BAT للتشغيل السريع
│   ├── README_INSTALLER.md             # دليل المستخدم
│   ├── INSTALLER_BUILD_GUIDE.md        # هذا الملف
│   │
│   ├── Output\                         # مجلد الإخراج (يُنشأ تلقائياً)
│   │   └── SR3H_MACRO_Setup_v2.0.0.exe # ملف التثبيت النهائي
│   │
│   └── Prerequisites\                  # المتطلبات الإضافية (اختياري)
│       └── windowsdesktop-runtime-6.0.25-win-x64.exe
│
├── bin\Release\net6.0-windows\         # ملفات التطبيق المبنية
│   ├── SR3H MACRO.exe                  # الملف التنفيذي
│   ├── *.dll                           # المكتبات المطلوبة
│   ├── runtimes\                       # مكتبات Runtime
│   ├── logo.png                        # الشعار
│   └── icon.ico                        # الأيقونة
│
└── icon.ico                            # أيقونة التطبيق الرئيسية
```

---

## 🎨 التخصيص

### 1. تغيير معلومات التطبيق

افتح ملف `SR3H_MACRO_Setup_Final.iss` وعدّل:

```pascal
#define MyAppName "SR3H MACRO"              ; اسم التطبيق
#define MyAppVersion "2.0.0"                ; رقم الإصدار
#define MyAppPublisher "SR3H Development"   ; الناشر
#define MyAppURL "https://sr3h.com"         ; الموقع
```

### 2. تغيير مسار التثبيت الافتراضي

```pascal
DefaultDirName={autopf}\SR3H MACRO          ; المسار الافتراضي
```

الخيارات المتاحة:
- `{autopf}` = `C:\Program Files` (موصى به)
- `{localappdata}` = `%LocalAppData%`
- `{userappdata}` = `%AppData%`
- `{commonappdata}` = `C:\ProgramData`

### 3. تخصيص الأيقونة

```pascal
SetupIconFile=C:\2_DEVELOPER_VERSION\SOURCE_CODE\icon.ico
UninstallDisplayIcon={app}\icon.ico
```

### 4. إضافة ملفات إضافية

```pascal
[Files]
Source: "المسار\الملف.ext"; DestDir: "{app}"; Flags: ignoreversion
```

### 5. تخصيص الاختصارات

```pascal
[Icons]
Name: "{autodesktop}\اسم الاختصار"; Filename: "{app}\SR3H MACRO.exe"
Name: "{group}\اسم الاختصار"; Filename: "{app}\SR3H MACRO.exe"
```

### 6. تغيير اللغات المدعومة

```pascal
[Languages]
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
```

---

## 🔍 استكشاف الأخطاء

### المشكلة 1: "Inno Setup not found"

**الحل:**
```powershell
# تثبيت Inno Setup
choco install innosetup

# أو تحميل يدوياً من
https://jrsoftware.org/isinfo.php
```

### المشكلة 2: "Application file not found"

**الحل:**
```powershell
# بناء التطبيق أولاً
cd C:\2_DEVELOPER_VERSION\SOURCE_CODE
dotnet build MacroApp.csproj --configuration Release
```

### المشكلة 3: "Access Denied"

**الحل:**
```powershell
# تشغيل PowerShell كمسؤول
# انقر بزر الماوس الأيمن > Run as Administrator
```

### المشكلة 4: "Script execution is disabled"

**الحل:**
```powershell
# تفعيل تنفيذ السكريبتات
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### المشكلة 5: حجم الملف كبير جداً

**الحل:**
```pascal
; في ملف .iss، استخدم ضغط أقوى
Compression=lzma2/ultra64
SolidCompression=yes
```

### المشكلة 6: ملفات الاختبار مضمنة في التثبيت

**الحل:**
```powershell
# السكريبت يحذفها تلقائياً، لكن يمكنك حذفها يدوياً
Remove-Item "bin\Release\net6.0-windows\Microsoft.TestPlatform*.dll"
Remove-Item "bin\Release\net6.0-windows\testhost.dll"
```

---

## ❓ الأسئلة الشائعة

### س1: كم يستغرق بناء ملف التثبيت؟

**ج:** عادةً 2-5 دقائق حسب سرعة الجهاز:
- بناء التطبيق: 1-2 دقيقة
- تنظيف الملفات: 10-30 ثانية
- بناء ملف التثبيت: 1-2 دقيقة

### س2: ما هو حجم ملف التثبيت المتوقع؟

**ج:** حوالي 15-25 MB حسب:
- عدد المكتبات المضمنة
- مستوى الضغط المستخدم
- وجود ملفات إضافية

### س3: هل يمكنني تضمين .NET Runtime في التثبيت؟

**ج:** نعم، لكن سيزيد حجم الملف إلى ~150 MB:

```pascal
[Files]
Source: "Prerequisites\windowsdesktop-runtime-6.0.25-win-x64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall

[Run]
Filename: "{tmp}\windowsdesktop-runtime-6.0.25-win-x64.exe"; Parameters: "/quiet /norestart"; StatusMsg: "Installing .NET Runtime..."; Check: not IsDotNetInstalled
```

### س4: كيف أقوم بتوقيع ملف التثبيت رقمياً؟

**ج:** تحتاج إلى شهادة توقيع رقمي:

```pascal
[Setup]
SignTool=signtool sign /f "certificate.pfx" /p "password" /t "http://timestamp.digicert.com" $f
```

### س5: هل يمكنني إنشاء تثبيت صامت (Silent)?

**ج:** نعم، المستخدم يمكنه تشغيل:

```batch
SR3H_MACRO_Setup_v2.0.0.exe /VERYSILENT /NORESTART
```

### س6: كيف أقوم بإنشاء تثبيت محمول (Portable)?

**ج:** استخدم `dotnet publish` بدلاً من Inno Setup:

```powershell
dotnet publish MacroApp.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

---

## 📊 معلومات إضافية

### خيارات سطر الأوامر لملف التثبيت

| الخيار | الوصف |
|--------|-------|
| `/SILENT` | تثبيت صامت مع شريط تقدم |
| `/VERYSILENT` | تثبيت صامت بالكامل |
| `/NORESTART` | عدم إعادة التشغيل |
| `/DIR="path"` | تحديد مسار التثبيت |
| `/GROUP="name"` | تحديد اسم المجموعة |
| `/NOICONS` | عدم إنشاء اختصارات |
| `/TASKS="tasks"` | تحديد المهام المطلوبة |

### مثال على التثبيت الصامت الكامل

```batch
SR3H_MACRO_Setup_v2.0.0.exe /VERYSILENT /NORESTART /DIR="C:\MyApps\SR3H MACRO" /NOICONS
```

---

## 🔐 الأمان

### التحقق من سلامة الملف

```powershell
# حساب SHA256 Hash
Get-FileHash "SR3H_MACRO_Setup_v2.0.0.exe" -Algorithm SHA256
```

### فحص الفيروسات

قبل التوزيع، قم بفحص الملف على:
- Windows Defender
- VirusTotal.com
- أي برنامج مكافحة فيروسات موثوق

---

## 📝 ملاحظات مهمة

### ⚠️ تحذيرات

1. **لا تقم بتضمين معلومات حساسة** في ملف التثبيت:
   - مفاتيح API
   - كلمات المرور
   - بيانات اعتماد قاعدة البيانات

2. **تأكد من حذف ملفات الاختبار** قبل البناء:
   - Microsoft.TestPlatform*.dll
   - testhost.dll
   - ملفات .pdb

3. **اختبر ملف التثبيت** على جهاز نظيف قبل التوزيع

### ✅ أفضل الممارسات

1. **استخدم رقم إصدار واضح** (Semantic Versioning):
   - MAJOR.MINOR.PATCH (مثال: 2.0.0)

2. **احتفظ بنسخة احتياطية** من كل إصدار

3. **وثّق التغييرات** في ملف CHANGELOG

4. **اختبر على أنظمة مختلفة**:
   - Windows 10 (21H2, 22H2)
   - Windows 11 (21H2, 22H2, 23H2)

5. **قدم دعماً فنياً واضحاً**:
   - بريد إلكتروني
   - موقع ويب
   - توثيق شامل

---

## 🎯 قائمة التحقق قبل الإصدار

- [ ] تم بناء التطبيق بنجاح في وضع Release
- [ ] تم حذف جميع ملفات الاختبار
- [ ] تم حذف ملفات PDB
- [ ] تم اختبار التطبيق على جهاز نظيف
- [ ] تم تحديث رقم الإصدار
- [ ] تم تحديث ملف README
- [ ] تم اختبار ملف التثبيت
- [ ] تم اختبار إلغاء التثبيت
- [ ] تم فحص الفيروسات
- [ ] تم حساب Hash للملف
- [ ] تم توثيق التغييرات
- [ ] تم إنشاء ملاحظات الإصدار

---

## 📞 الدعم

إذا واجهت أي مشاكل:

1. **راجع قسم استكشاف الأخطاء** أعلاه
2. **تحقق من السجلات** في مجلد Setup\Output
3. **اتصل بالدعم الفني**:
   - Email: support@sr3h.com
   - Website: https://sr3h.com

---

## 📜 الترخيص

هذا الدليل جزء من مشروع SR3H MACRO  
Copyright © 2025 SR3H Development Team  
جميع الحقوق محفوظة | All Rights Reserved

---

**آخر تحديث:** 2024-09-30  
**الإصدار:** 2.0.0  
**المؤلف:** SR3H Development Team