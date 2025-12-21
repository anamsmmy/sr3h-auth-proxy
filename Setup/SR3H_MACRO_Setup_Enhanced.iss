; SR3H MACRO - Enhanced Inno Setup Script
; تطبيق الماكرو المتقدم - سكريبت التثبيت المحسن
; Created: 2025-01-31
; Updated: 2025-01-31 - Enhanced Version

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
AppCopyright=© 2025 {#MyAppPublisher}
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppDescription}

; مسارات التثبيت والإخراج
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=
InfoBeforeFile=
InfoAfterFile=
OutputDir=c:\MACRO_SR3H\Setup\Output
OutputBaseFilename=SR3H_MACRO_Setup_Enhanced
SetupIconFile=c:\MACRO_SR3H\icon.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern

; صور معالج التثبيت (اختيارية)
; WizardImageFile=c:\MACRO_SR3H\Setup\wizard_image.bmp
; WizardSmallImageFile=c:\MACRO_SR3H\Setup\wizard_small.bmp

; إعدادات النظام والأمان
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
DisableWelcomePage=no
DisableFinishedPage=no

; إعدادات إضافية
ChangesAssociations=no
RestartIfNeededByRun=no
CloseApplications=yes
SetupLogging=yes

[Languages]
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "إنشاء اختصار على سطح المكتب"; GroupDescription: "اختصارات إضافية:"
Name: "quicklaunchicon"; Description: "إنشاء اختصار في شريط المهام السريع"; GroupDescription: "اختصارات إضافية:"; Flags: unchecked; OnlyBelowVersion: 6.1
Name: "startmenu"; Description: "إضافة إلى قائمة ابدأ"; GroupDescription: "اختصارات إضافية:"

[Files]
; الملف التنفيذي الرئيسي
Source: "c:\MACRO_SR3H\bin\Release\net6.0-windows\SR3H MACRO.exe"; DestDir: "{app}"; Flags: ignoreversion

; ملفات DLL الأساسية المطلوبة
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

; ملف اقرأني بسيط ودقيق
Source: "c:\MACRO_SR3H\Setup\README_SIMPLE.md"; DestDir: "{app}"; DestName: "اقرأني.txt"; Flags: ignoreversion isreadme

; .NET 6.0 Desktop Runtime (سيتم تحميله إذا لم يكن موجوداً)
Source: "c:\MACRO_SR3H\Setup\Prerequisites\windowsdesktop-runtime-6.0.25-win-x64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: not IsDotNet60Installed

[Icons]
; اختصار في قائمة ابدأ
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Comment: "{#MyAppDescription}"; IconFilename: "{app}\icon.ico"; Tasks: startmenu
Name: "{group}\إلغاء تثبيت {#MyAppName}"; Filename: "{uninstallexe}"; Comment: "إلغاء تثبيت {#MyAppName}"; Tasks: startmenu

; اختصار على سطح المكتب
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Comment: "{#MyAppDescription}"; IconFilename: "{app}\icon.ico"; Tasks: desktopicon

; اختصار في شريط المهام السريع
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\icon.ico"; Tasks: quicklaunchicon

[Registry]
; تسجيل التطبيق في النظام
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\App Paths\{#MyAppExeName}"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\App Paths\{#MyAppExeName}"; ValueType: string; ValueName: "Path"; ValueData: "{app}"; Flags: uninsdeletekey

[Run]
; تثبيت .NET Runtime إذا لم يكن موجوداً
Filename: "{tmp}\windowsdesktop-runtime-6.0.25-win-x64.exe"; Parameters: "/quiet /norestart"; StatusMsg: "تثبيت .NET 6.0 Runtime..."; Check: not IsDotNet60Installed; Flags: waituntilterminated

; تشغيل التطبيق بعد التثبيت
Filename: "{app}\{#MyAppExeName}"; Description: "تشغيل {#MyAppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; حذف الملفات المؤقتة عند إلغاء التثبيت
Type: filesandordirs; Name: "{app}\logs"
Type: filesandordirs; Name: "{app}\temp"

[Code]
// التحقق من وجود .NET 6.0 Runtime
function IsDotNet60Installed: Boolean;
var
  Version: String;
begin
  Result := RegQueryStringValue(HKLM, 'SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x64\sharedhost', 'Version', Version) or
            RegQueryStringValue(HKLM, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost', 'Version', Version);
  // Simplified check - if any version is found, assume it's compatible
  // In production, you might want to implement proper version comparison
end;

// رسالة ترحيب مخصصة
function InitializeSetup(): Boolean;
begin
  Result := True;
  if MsgBox('مرحباً بك في معالج تثبيت SR3H MACRO!' + #13#10 + 
            'هذا التطبيق سيساعدك في أتمتة المهام المتكررة.' + #13#10#13#10 +
            'هل تريد المتابعة؟', mbConfirmation, MB_YESNO) = IDNO then
    Result := False;
end;

// رسالة إنهاء مخصصة
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    MsgBox('تم تثبيت SR3H MACRO بنجاح!' + #13#10 + 
           'يمكنك الآن تشغيل التطبيق من سطح المكتب أو قائمة ابدأ.' + #13#10#13#10 +
           'شكراً لاختيارك SR3H MACRO!', mbInformation, MB_OK);
  end;
end;