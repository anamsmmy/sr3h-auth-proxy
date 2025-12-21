; SR3H MACRO - Inno Setup Script (Final Version)
; تطبيق الماكرو المتقدم - سكريبت التثبيت النهائي
; Created: 2024-09-30
; Updated: 2024-09-30

#define MyAppName "SR3H MACRO"
#define MyAppVersion "2.0.0"
#define MyAppPublisher "فريق منصة سرعة | SR3H Team"
#define MyAppURL "https://www.sr3h.com"
#define MyAppExeName "SR3H MACRO.exe"
#define MyAppDescription "تطبيق الماكرو المتقدم لأتمتة المهام والعمليات المتكررة"
#define SourcePath "C:\2_DEVELOPER_VERSION\SOURCE_CODE\bin\Release\net6.0-windows"

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
OutputDir=C:\2_DEVELOPER_VERSION\SOURCE_CODE\Setup\Output
OutputBaseFilename=SR3H_MACRO_Setup_v{#MyAppVersion}
SetupIconFile=C:\2_DEVELOPER_VERSION\SOURCE_CODE\icon.ico
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern

; الحد الأدنى لمتطلبات النظام
MinVersion=10.0
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

; خيارات التثبيت
PrivilegesRequired=admin
DisableDirPage=no
DisableReadyPage=no
ShowLanguageDialog=auto
UsePreviousAppDir=yes
UsePreviousGroup=yes
AlwaysRestart=no
RestartIfNeededByRun=no
UninstallDisplayIcon={app}\icon.ico

; دعم اللغة العربية
LanguageDetectionMethod=locale

[Languages]
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "إنشاء اختصار على سطح المكتب"; GroupDescription: "اختصارات إضافية:"; Flags: checkedonce
Name: "quicklaunchicon"; Description: "إنشاء اختصار في شريط المهام السريع"; GroupDescription: "اختصارات إضافية:"; Flags: unchecked; OnlyBelowVersion: 6.1

[Files]
; الملف التنفيذي الرئيسي
Source: "{#SourcePath}\SR3H MACRO.exe"; DestDir: "{app}"; Flags: ignoreversion

; ملفات DLL الأساسية المطلوبة (استبعاد ملفات الاختبار)
Source: "{#SourcePath}\GlobalHotKey.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Microsoft.Extensions.DependencyInjection.Abstractions.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Microsoft.Extensions.Logging.Abstractions.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Microsoft.IdentityModel.Abstractions.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Microsoft.IdentityModel.JsonWebTokens.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Microsoft.IdentityModel.Logging.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Microsoft.IdentityModel.Tokens.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Microsoft.IO.RecyclableMemoryStream.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\MimeMapping.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Newtonsoft.Json.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Supabase.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Supabase.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Supabase.Functions.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Supabase.Gotrue.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Supabase.Postgrest.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Supabase.Realtime.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Supabase.Storage.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\System.IdentityModel.Tokens.Jwt.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\System.Management.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\System.Reactive.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\System.Threading.Channels.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Websocket.Client.dll"; DestDir: "{app}"; Flags: ignoreversion

; ملفات التكوين الأساسية
Source: "{#SourcePath}\SR3H MACRO.deps.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\SR3H MACRO.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\SR3H MACRO.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion

; مجلد runtimes (مطلوب لتشغيل التطبيق)
Source: "{#SourcePath}\runtimes\*"; DestDir: "{app}\runtimes"; Flags: ignoreversion recursesubdirs createallsubdirs

; الشعار والأيقونات
Source: "{#SourcePath}\logo.png"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\icon.ico"; DestDir: "{app}"; Flags: ignoreversion

; ملف التعليمات
Source: "C:\2_DEVELOPER_VERSION\SOURCE_CODE\Setup\README_INSTALLER.md"; DestDir: "{app}"; DestName: "اقرأني.txt"; Flags: ignoreversion isreadme; Check: FileExists('C:\2_DEVELOPER_VERSION\SOURCE_CODE\Setup\README_INSTALLER.md')

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
Type: filesandordirs; Name: "{userappdata}\MacroApp"

[Registry]
; تسجيل التطبيق في النظام
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\App Paths\{#MyAppExeName}"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\App Paths\{#MyAppExeName}"; ValueType: string; ValueName: "Path"; ValueData: "{app}"; Flags: uninsdeletekey

; تسجيل معلومات التطبيق
Root: HKLM; Subkey: "Software\SR3H\{#MyAppName}"; ValueType: string; ValueName: "Version"; ValueData: "{#MyAppVersion}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\SR3H\{#MyAppName}"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"; Flags: uninsdeletekey

[Code]
// كود مخصص للتحقق من متطلبات النظام
function CheckDotNetInstalled(): Boolean;
var
  DotNetPath: String;
  FindRec: TFindRec;
  VersionFound: Boolean;
begin
  Result := False;
  DotNetPath := ExpandConstant('{commonpf}\dotnet\shared\Microsoft.WindowsDesktop.App');
  
  // التحقق من وجود مجلد .NET Desktop Runtime
  if DirExists(DotNetPath) then
  begin
    // البحث عن أي إصدار 6.x أو 7.x أو 8.x أو 9.x
    if FindFirst(DotNetPath + '\*', FindRec) then
    begin
      try
        repeat
          if (FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY <> 0) and 
             (FindRec.Name <> '.') and (FindRec.Name <> '..') then
          begin
            // فحص إذا كان الإصدار يبدأ بـ 6. أو 7. أو 8. أو 9.
            if (Pos('6.', FindRec.Name) = 1) or 
               (Pos('7.', FindRec.Name) = 1) or 
               (Pos('8.', FindRec.Name) = 1) or 
               (Pos('9.', FindRec.Name) = 1) then
            begin
              Result := True;
              Break;
            end;
          end;
        until not FindNext(FindRec);
      finally
        FindClose(FindRec);
      end;
    end;
  end;
end;

function InitializeSetup(): Boolean;
var
  Version: TWindowsVersion;
  ResultCode: Integer;
begin
  GetWindowsVersionEx(Version);
  
  // التحقق من إصدار Windows (Windows 10 أو أحدث)
  if Version.Major < 10 then
  begin
    MsgBox('هذا التطبيق يتطلب Windows 10 أو إصدار أحدث.' + #13#10 + 
           'This application requires Windows 10 or newer.', mbError, MB_OK);
    Result := False;
    Exit;
  end;
  
  // التحقق من وجود .NET Desktop Runtime (6.0 أو أحدث)
  if not CheckDotNetInstalled() then
  begin
    if MsgBox('هذا التطبيق يتطلب .NET Desktop Runtime (الإصدار 6.0 أو أحدث).' + #13#10 +
              'This application requires .NET Desktop Runtime (version 6.0 or newer).' + #13#10 + #13#10 +
              'يُنصح بتثبيت أحدث إصدار (.NET 9.0)' + #13#10 +
              'We recommend installing the latest version (.NET 9.0)' + #13#10 + #13#10 +
              'هل تريد فتح صفحة التحميل الآن؟' + #13#10 +
              'Do you want to open the download page now?', mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet/9.0', '', '', SW_SHOW, ewNoWait, ResultCode);
    end;
    Result := False;
    Exit;
  end;
  
  Result := True;
end;

// التحقق من وجود ملف
function FileExists(const FileName: string): Boolean;
begin
  Result := FileOrDirExists(FileName);
end;