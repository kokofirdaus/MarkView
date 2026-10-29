; MarkView Inno Setup Script
; Generates standard Windows Setup wizard for MarkView (x64 and ARM64)

#define MyAppName "MarkView"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Koko Firdaus"
#define MyAppURL "https://github.com/kokofirdaus/MarkView"
#define MyAppExeName "MarkView.exe"

[Setup]
AppId={{5E4B4C33-810B-4819-A338-7B6B0E090F11}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
ChangesAssociations=yes
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE
OutputDir=..\dist
OutputBaseFilename=MarkView-Setup-{#MyAppVersion}
SetupIconFile=..\src\MarkView\Assets\icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible arm64
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "..\dist\MarkView-win-x64.exe"; DestDir: "{app}"; DestName: "{#MyAppExeName}"; Check: not IsArm64; Flags: ignoreversion
Source: "..\dist\MarkView-win-arm64.exe"; DestDir: "{app}"; DestName: "{#MyAppExeName}"; Check: IsArm64; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; Associate .md
Root: HKA; Subkey: "Software\Classes\.md"; ValueType: string; ValueName: ""; ValueData: "MarkView.Markdown"; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Classes\.markdown"; ValueType: string; ValueName: ""; ValueData: "MarkView.Markdown"; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Classes\MarkView.Markdown"; ValueType: string; ValueName: ""; ValueData: "Markdown Document"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\MarkView.Markdown\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"
Root: HKA; Subkey: "Software\Classes\MarkView.Markdown\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
function IsArm64: Boolean;
begin
  Result := (ProcessorArchitecture = paARM64);
end;
