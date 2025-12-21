; SR3H MACRO - Inno Setup Script
; تطبيق الماكرو المتقدم - سكريبت التثبيت
; Created: 2025-01-31
; Updated: 2025-01-31

#define MyAppName "SR3H MACRO"
#define MyAppVersion "2.0.0"
#define MyAppPublisher "SR3H Development Team"
#define MyAppURL "https://sr3h.com"
#define MyAppExeName "SR3H MACRO.exe"
#define MyAppDescription "تطبيق الماكرو المتقدم لأتمتة المهام والعمليات المتكررة"

[Setup]
; معلومات التطبيق الأساسية
AppId={{B8E5F8A2-1234-5678-9ABC-DEF012345678}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
AppCopyright=Copyright © 2025 SR3H Development Team

; مسارات التثبيت
DefaultDirName={autopf}\SR3H MACRO
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=
InfoBeforeFile=
InfoAfterFile=
OutputDir=c:\MACRO_SR3H\Setup\Output
OutputBaseFilename=SR3H_MACRO_Setup
SetupIconFile=c:\MACRO_SR3H\icon.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern

; الحد الأدنى لمتطلبات النظام
MinVersion=10.0
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

; خيارات التثبيت
PrivilegesRequired=admin
DisableDirPage=no
DisableReadyPage=no
ShowLanguageDialog=auto
UsePreviousAppDir=yes
UsePreviousGroup=yes
AlwaysRestart=no
RestartIfNeededByRun=no

; دعم اللغة العربية
LanguageDetectionMethod=locale

[Languages]
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "إنشاء اختصار على سطح المكتب"; GroupDescription: "اختصارات إضافية:"
Name: "quicklaunchicon"; Description: "إنشاء اختصار في شريط المهام السريع"; GroupDescription: "اختصارات إضافية:"; Flags: unchecked; OnlyBelowVersion: 6.1

[Files]
; الملف التنفيذي الرئيسي
Source: "c:\MACRO_SR3H\bin\Release\net6.0-windows\SR3H MACRO.exe"; DestDir: "{app}"; Flags: ignoreversion

; ملفات DLL الأساسية المطلوبة (استبعاد ملفات الاختبار)
Source: "c:\MACRO_SR3H\bin\Release\net6.0-windows\GlobalHotKey.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "c:\MACRO_SR3H\bin\Release\net6.0-windows\Microsoft.Extensions.DependencyInjection.Abstractions.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "c:\MACRO_SR3H\bin\Release\net6.0-windows\Microsoft.Extensions.Logging.Abstractions.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "c:\MACRO_SR3H\bin\Release\net6.0-windows\Microsoft.IdentityModel.Abstractions.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "c:\MACRO_SR3H\bin\Release\net6.0-windows\Microsoft.IdentityModel.JsonWebTokens.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "c:\MACRO_SR3H\bin\Release\net6.0-windows\Microsoft.IdentityModel.Logging.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "c:\MACRO_SR3H\bin\Release\net6.0-windows\Microsoft.IdentityModel.Tokens.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "c:\MACRO_SR3H\bin\Release\net6.0-windows\Microsoft.IO.RecyclableMemoryStream.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "c:\MACRO_SR3H\bin\Release\net6.0-windows\MimeMapping.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "c:\MACRO_SR3H\bin\Release\net6.0-windows\Newtonsoft.Json.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "c:\MACRO_SR3H\bin\Release\net6.0-windows\Supabase.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "c:\MACRO_SR3H\bin\Release\net6.0-windows\Supabase.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "c:\MACRO_SR3H\bin\Release\net6.0-windows\Supabase.Functions.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "c:\MACRO_SR3H\bin\Release\net6.0-windows\Supabase.Gotrue.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "c:\MACRO_SR3H\bin\Release\net6.0-windows\Supabase.Postgrest.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "c:\MACRO_SR3H\bin\Release\net6.0-windows\Supabase.Realtime.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "c:\MACRO_SR3H\bin\Release\net6.0-windows\Supabase.Storage.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "c:\MACRO_SR3H\bin\Release\net6.0-windows\System.CodeDom.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "c:\MACRO_SR3H\bin\Release\net6.0-windows\System.IdentityModel.Tokens.Jwt.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "c:\MACRO_SR3H\bin\Release\net6.0-windows\System.Management.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "c:\MACRO_SR3H\bin\Release\net6.0-windows\System.Reactive.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "c:\MACRO_SR3H\bin\Release\net6.0-windows\System.Threading.Channels.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "c:\MACRO_SR3H\bin\Release\net6.0-windows\Websocket.Client.dll"; DestDir: "{app}"; Flags: ignoreversion

; ملفات التكوين الأساسية
Source: "c:\MACRO_SR3H\bin\Release\net6.0-windows\SR3H MACRO.deps.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "c:\MACRO_SR3H\bin\Release\net6.0-windows\SR3H MACRO.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "c:\MACRO_SR3H\bin\Release\net6.0-windows\SR3H MACRO.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion

; مجلد runtimes (مطلوب لتشغيل التطبيق)
Source: "c:\MACRO_SR3H\bin\Release\net6.0-windows\runtimes\*"; DestDir: "{app}\runtimes"; Flags: ignoreversion recursesubdirs createallsubdirs

; الشعار والأيقونات
Source: "c:\MACRO_SR3H\bin\Release\net6.0-windows\logo.png"; DestDir: "{app}"; Flags: ignoreversion
Source: "c:\MACRO_SR3H\icon.ico"; DestDir: "{app}"; Flags: ignoreversion

; ملف التعليمات (بدون معلومات حساسة)
Source: "c:\MACRO_SR3H\Setup\README_INSTALLER.md"; DestDir: "{app}"; DestName: "اقرأني.txt"; Flags: ignoreversion isreadme

[Icons]
; اختصار في قائمة ابدأ
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Comment: "{#MyAppDescription}"; IconFilename: "{app}\icon.ico"
Name: "{group}\إلغاء تثبيت {#MyAppName}"; Filename: "{uninstallexe}"; Comment: "إلغاء تثبيت {#MyAppName}"

; اختصار على سطح المكتب (افتراضياً مفعل)
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Comment: "{#MyAppDescription}"; IconFilename: "{app}\icon.ico"; Tasks: desktopicon

; اختصار في شريط المهام السريع
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\icon.ico"; Tasks: quicklaunchicon

[Run]
; تشغيل التطبيق بعد التثبيت
Filename: "{app}\{#MyAppExeName}"; Description: "تشغيل {#MyAppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; حذف ملفات البيانات عند إلغاء التثبيت
Type: filesandordirs; Name: "{userappdata}\MACRO_SR3H"

[Registry]
; تسجيل التطبيق في النظام
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\App Paths\{#MyAppExeName}"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\App Paths\{#MyAppExeName}"; ValueType: string; ValueName: "Path"; ValueData: "{app}"; Flags: uninsdeletekey

[Code]
// كود مخصص للتحقق من متطلبات النظام
function InitializeSetup(): Boolean;
var
  Version: TWindowsVersion;
begin
  GetWindowsVersionEx(Version);
  
  // التحقق من إصدار Windows (Windows 10 أو أحدث)
  if Version.Major < 10 then
  begin
    MsgBox('هذا التطبيق يتطلب Windows 10 أو إصدار أحدث.', mbError, MB_OK);
    Result := False;
  end
  else
    Result := True;
end;

// رسالة ترحيب مخصصة
function NextButtonClick(CurPageID: Integer): Boolean;
begin
  if CurPageID = wpWelcome then
  begin
    MsgBox('مرحباً بك في معالج تثبيت SR3H MACRO!' + #13#10 + 
           'هذا التطبيق سيساعدك في أتمتة المهام المتكررة بسهولة.' + #13#10 +
           'اضغط التالي للمتابعة.', mbInformation, MB_OK);
  end;
  Result := True;
end;

// رسالة إنهاء مخصصة
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    MsgBox('تم تثبيت SR3H MACRO بنجاح!' + #13#10 +
           'يمكنك الآن تشغيل التطبيق من سطح المكتب أو قائمة ابدأ.' + #13#10 +
           'شكراً لاستخدامك SR3H MACRO!', mbInformation, MB_OK);
  end;
end;