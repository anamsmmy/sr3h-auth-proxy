; SR3H MACRO - Inno Setup Script
; Created for SR3H MACRO v1.0.0
; Arabic-supported installer

#define MyAppName "SR3H MACRO"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "منصة سرعة"
#define MyAppURL "https://www.SR3H.com"
#define MyAppExeName "SR3H MACRO.exe"
#define MyAppContact "7jgamer.ads@gmail.com"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
AppId={{8B5F2A1C-9D3E-4F7A-B8C6-1E2F3A4B5C6D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
AppContact={#MyAppContact}
DefaultDirName={autopf}\SR3H MACRO
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=
InfoBeforeFile=
InfoAfterFile=
OutputDir=C:\MACRO_SR3H\Setup\Output
OutputBaseFilename=SR3H_MACRO_Setup
SetupIconFile=C:\MACRO_SR3H\bin\Release\net6.0-windows\icon.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription=تطبيق الماكرو المتقدم
VersionInfoCopyright=© 2025 {#MyAppPublisher}
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}

; Modern wizard appearance (removing problematic images)
WizardImageFile=
WizardSmallImageFile=
WizardImageStretch=no
WizardImageBackColor=$FFFFFF

; Arabic language support
ShowLanguageDialog=auto

[Languages]
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
arabic.CreateDesktopIcon=إنشاء اختصار على سطح المكتب
arabic.CreateQuickLaunchIcon=إنشاء اختصار في شريط المهام السريع
arabic.ProgramOnTheWeb=موقع %1 على الإنترنت
arabic.UninstallProgram=إلغاء تثبيت %1
arabic.LaunchProgram=تشغيل %1
arabic.AssocFileExtension=ربط %1 بامتداد الملف %2
arabic.AssocingFileExtension=جاري ربط %1 بامتداد الملف %2...
arabic.AutoStartProgramGroupDescription=بدء التشغيل التلقائي:
arabic.AutoStartProgram=تشغيل %1 تلقائياً
arabic.AdditionalIcons=أيقونات إضافية:
arabic.CreateDesktopIcon=إنشاء أيقونة على &سطح المكتب
arabic.CreateQuickLaunchIcon=إنشاء أيقونة &تشغيل سريع

english.CreateDesktopIcon=Create a &desktop icon
english.CreateQuickLaunchIcon=Create a &Quick Launch icon
english.ProgramOnTheWeb=%1 on the Web
english.UninstallProgram=Uninstall %1
english.LaunchProgram=Launch %1
english.AssocFileExtension=&Associate %1 with the %2 file extension
english.AssocingFileExtension=Associating %1 with the %2 file extension...
english.AutoStartProgramGroupDescription=Startup:
english.AutoStartProgram=Automatically start %1
english.AdditionalIcons=Additional icons:

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode

[Files]
; Main application files
Source: "C:\MACRO_SR3H\bin\Release\net6.0-windows\SR3H MACRO.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\MACRO_SR3H\bin\Release\net6.0-windows\SR3H MACRO.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\MACRO_SR3H\bin\Release\net6.0-windows\SR3H MACRO.deps.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\MACRO_SR3H\bin\Release\net6.0-windows\SR3H MACRO.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion

; Essential Dependencies (excluding test platform files)
Source: "C:\MACRO_SR3H\bin\Release\net6.0-windows\GlobalHotKey.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\MACRO_SR3H\bin\Release\net6.0-windows\Microsoft.Extensions.DependencyInjection.Abstractions.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\MACRO_SR3H\bin\Release\net6.0-windows\Microsoft.Extensions.Logging.Abstractions.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\MACRO_SR3H\bin\Release\net6.0-windows\Microsoft.IdentityModel.Abstractions.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\MACRO_SR3H\bin\Release\net6.0-windows\Microsoft.IdentityModel.JsonWebTokens.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\MACRO_SR3H\bin\Release\net6.0-windows\Microsoft.IdentityModel.Logging.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\MACRO_SR3H\bin\Release\net6.0-windows\Microsoft.IdentityModel.Tokens.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\MACRO_SR3H\bin\Release\net6.0-windows\Microsoft.IO.RecyclableMemoryStream.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\MACRO_SR3H\bin\Release\net6.0-windows\MimeMapping.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\MACRO_SR3H\bin\Release\net6.0-windows\Newtonsoft.Json.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\MACRO_SR3H\bin\Release\net6.0-windows\Supabase.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\MACRO_SR3H\bin\Release\net6.0-windows\Supabase.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\MACRO_SR3H\bin\Release\net6.0-windows\Supabase.Functions.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\MACRO_SR3H\bin\Release\net6.0-windows\Supabase.Gotrue.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\MACRO_SR3H\bin\Release\net6.0-windows\Supabase.Postgrest.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\MACRO_SR3H\bin\Release\net6.0-windows\Supabase.Realtime.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\MACRO_SR3H\bin\Release\net6.0-windows\Supabase.Storage.dll"; DestDir: "{app}"; Flags: ignoreversion

Source: "C:\MACRO_SR3H\bin\Release\net6.0-windows\System.IdentityModel.Tokens.Jwt.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\MACRO_SR3H\bin\Release\net6.0-windows\System.Management.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\MACRO_SR3H\bin\Release\net6.0-windows\System.Reactive.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\MACRO_SR3H\bin\Release\net6.0-windows\System.Threading.Channels.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\MACRO_SR3H\bin\Release\net6.0-windows\Websocket.Client.dll"; DestDir: "{app}"; Flags: ignoreversion

; Runtime files (important for .NET applications)
Source: "C:\MACRO_SR3H\bin\Release\net6.0-windows\runtimes\*"; DestDir: "{app}\runtimes"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: DirExists('C:\MACRO_SR3H\bin\Release\net6.0-windows\runtimes')

; Configuration and resources (if exists)
; Source: "C:\MACRO_SR3H\bin\Release\net6.0-windows\appsettings.json"; DestDir: "{app}"; Flags: ignoreversion; Check: FileExists('C:\MACRO_SR3H\bin\Release\net6.0-windows\appsettings.json')

; Documentation
Source: "C:\MACRO_SR3H\اقرأني.txt"; DestDir: "{app}"; Flags: ignoreversion

; Icon files (embedded in app directory for protection)
Source: "C:\MACRO_SR3H\Setup\app_icon.ico"; DestDir: "{app}\Resources"; Flags: ignoreversion
Source: "C:\MACRO_SR3H\Setup\app_icon.png"; DestDir: "{app}\Resources"; DestName: "logo.png"; Flags: ignoreversion

; Application icon and logo
Source: "C:\MACRO_SR3H\bin\Release\net6.0-windows\icon.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\MACRO_SR3H\bin\Release\net6.0-windows\logo.png"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\icon.ico"
Name: "{group}\{cm:ProgramOnTheWeb,{#MyAppName}}"; Filename: "{#MyAppURL}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\icon.ico"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\icon.ico"; Tasks: quicklaunchicon

[Registry]
; Register application in Windows
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppName}"; ValueType: string; ValueName: "DisplayName"; ValueData: "{#MyAppName}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppName}"; ValueType: string; ValueName: "DisplayVersion"; ValueData: "{#MyAppVersion}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppName}"; ValueType: string; ValueName: "Publisher"; ValueData: "{#MyAppPublisher}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppName}"; ValueType: string; ValueName: "URLInfoAbout"; ValueData: "{#MyAppURL}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppName}"; ValueType: string; ValueName: "Contact"; ValueData: "{#MyAppContact}"; Flags: uninsdeletekey

; App Paths registration
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\App Paths\{#MyAppExeName}"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\App Paths\{#MyAppExeName}"; ValueType: string; ValueName: "Path"; ValueData: "{app}"; Flags: uninsdeletekey

[Run]
; Install .NET 6.0 Runtime if needed
Filename: "https://download.microsoft.com/download/6/0/f/60fc8c9b-2e9e-4d1c-8d8b-f8d06d06e1e1/dotnet-runtime-6.0.25-win-x64.exe"; Description: "تثبيت .NET 6.0 Runtime (مطلوب)"; Flags: shellexec runascurrentuser; Check: not IsDotNetInstalled

; Launch application after installation
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

; Show README file option
Filename: "notepad.exe"; Parameters: "{app}\اقرأني.txt"; Description: "عرض ملف التعليمات"; Flags: nowait postinstall skipifsilent unchecked

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
Type: filesandordirs; Name: "{userappdata}\SR3H MACRO"

[Code]
// Function to check if .NET 6.0 is installed
function IsDotNetInstalled: Boolean;
var
  Version: String;
begin
  Result := RegQueryStringValue(HKLM, 'SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x64\sharedhost', 'Version', Version) or
            RegQueryStringValue(HKLM, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost', 'Version', Version);
  // Simple version check - just check if .NET 6.0 exists
  Result := Result;
end;

// Simplified version check

// Custom initialization
procedure InitializeWizard;
begin
  // Arabic language support will be handled by Inno Setup automatically
end;

// Welcome message
function NextButtonClick(CurPageID: Integer): Boolean;
begin
  if CurPageID = wpWelcome then
  begin
    MsgBox('مرحباً بك في معالج تثبيت SR3H MACRO!' + #13#10 + 
           'تطبيق الماكرو المتقدم لأتمتة المهام والعمليات المتكررة.' + #13#10 +
           'اضغط التالي للمتابعة.', mbInformation, MB_OK);
  end;
  Result := True;
end;

// Custom messages for installation
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // Create application data directory
    CreateDir(ExpandConstant('{userappdata}\SR3H MACRO'));
    
    // Success message
    MsgBox('تم تثبيت SR3H MACRO بنجاح!' + #13#10 +
           'يمكنك الآن تشغيل التطبيق من سطح المكتب أو قائمة ابدأ.' + #13#10 +
           'شكراً لاستخدامك SR3H MACRO!', mbInformation, MB_OK);
  end;
end;

// System requirements check
function InitializeSetup(): Boolean;
var
  Version: TWindowsVersion;
begin
  GetWindowsVersionEx(Version);
  
  // Check Windows version (Windows 10 or newer)
  if Version.Major < 10 then
  begin
    MsgBox('هذا التطبيق يتطلب Windows 10 أو إصدار أحدث.' + #13#10 +
           'الرجاء تحديث نظام التشغيل قبل التثبيت.', mbError, MB_OK);
    Result := False;
  end
  else
    Result := True;
end;