; Inno Setup Script for Daily Prayer Timer (Native) — 32-bit
#define AppName "Daily Prayer Timer"
#define AppVersion "2.3.2"
#define AppPublisher "Abiruzzaman Molla"
#define AppURL "https://github.com/AbiruzzamanMolla"
#define AppExeName "DailyPrayerTime.Native.exe"
#define SourcePath "J:\Web Development\abir\software\DailyPrayerTime\DailyPrayerTime.Native\bin\Release\net8.0-windows10.0.19041.0\win-x86\publish"

[Setup]
; Using the SAME AppId means x86 and x64 cannot co-exist — the newer install replaces the old.
; If you want side-by-side, change this AppId.
AppId={{628cbde9-f8bb-42b8-a69b-17912d7ba218}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
DefaultDirName={autopf}\Daily Prayer Timer
DisableProgramGroupPage=yes
PrivilegesRequired=admin
OutputBaseFilename=DailyPrayerTimer_Setup_v{#AppVersion}_x86
Compression=lzma
SolidCompression=yes
WizardStyle=modern
; Windows 10+ minimum (build 10240)
MinVersion=10.0

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#SourcePath}\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "DailyPrayerTime.DeskBand\bin\Release\net48\DailyPrayerTime.DeskBand.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "DailyPrayerTime.DeskBand\bin\Release\net48\Newtonsoft.Json.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon; WorkingDir: "{app}"

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
; Register DeskBand (use 32-bit regasm for 32-bit dll in 32-bit OS context, though dll is AnyCPU)
Filename: "{dotnet40}\regasm.exe"; Parameters: "/codebase ""{app}\DailyPrayerTime.DeskBand.dll"""; StatusMsg: "Registering Taskbar Extension..."; Flags: runhidden

[UninstallRun]
; Unregister DeskBand
Filename: "{dotnet40}\regasm.exe"; Parameters: "/u ""{app}\DailyPrayerTime.DeskBand.dll"""; RunOnceId: "UnregisterDeskBand"; Flags: runhidden

[Code]
function IsWindows11: Boolean;
var
  Version: TWindowsVersion;
begin
  GetWindowsVersionEx(Version);
  // Windows 11 is version 10.0, build 22000 or higher
  Result := (Version.Major = 10) and (Version.Build >= 22000);
end;
