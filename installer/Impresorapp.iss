#ifndef AppVersion
  #error AppVersion is required
#endif
#ifndef InputDir
  #error InputDir is required
#endif
#ifndef OutputDir
  #error OutputDir is required
#endif
#define AppAuthor "Alberto Marín (albertuxo85)"
[Setup]
AppId={{AD41149D-8878-4B28-9D55-906BB703CA95}
AppName=Impresorapp
AppVersion={#AppVersion}
AppVerName=Impresorapp {#AppVersion}
AppPublisher={#AppAuthor}
AppPublisherURL=https://github.com/albertuxo085/Impresorapp
AppSupportURL=https://github.com/albertuxo085/Impresorapp/issues
AppUpdatesURL=https://github.com/albertuxo085/Impresorapp/releases
DefaultDirName={localappdata}\Programs\Impresorapp
DefaultGroupName=Impresorapp
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
WizardStyle=modern
DisableWelcomePage=no
SetupIconFile={#InputDir}\Impresorapp.ico
UninstallDisplayIcon={app}\Impresorapp.exe
OutputDir={#OutputDir}
OutputBaseFilename=Impresorapp-{#AppVersion}-Setup
Compression=lzma2
SolidCompression=yes
VersionInfoVersion={#AppVersion}
VersionInfoCompany={#AppAuthor}
VersionInfoDescription=Instalador de Impresorapp
VersionInfoCopyright=Copyright (c) 2026 Alberto Marín
CloseApplications=yes
RestartApplications=no
Uninstallable=not SmokeTest
UsePreviousAppDir=not SmokeTest

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Messages]
spanish.WelcomeLabel2=Este asistente instalará Impresorapp {#AppVersion} en tu equipo.%n%nAutor: Alberto Marín (albertuxo85).%n%nTus diseños y preferencias se conservarán al desinstalar el programa.

[Tasks]
Name: "desktopicon"; Description: "Crear un acceso directo en el escritorio"; GroupDescription: "Accesos directos:"; Flags: unchecked; Check: not SmokeTest

[Files]
Source: "{#InputDir}\Impresorapp-{#AppVersion}.exe"; DestDir: "{app}"; DestName: "Impresorapp.exe"; Flags: ignoreversion
Source: "{#InputDir}\Impresorapp-{#AppVersion}.exe.config"; DestDir: "{app}"; DestName: "Impresorapp.exe.config"; Flags: ignoreversion
Source: "{#InputDir}\QRCoder.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#InputDir}\QRCoder-LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#InputDir}\update-defaults.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#InputDir}\Impresorapp.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#InputDir}\LEEME.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#InputDir}\*.etiqueta"; DestDir: "{app}\Ejemplos"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\Impresorapp"; Filename: "{app}\Impresorapp.exe"; WorkingDir: "{app}"; Check: not SmokeTest
Name: "{autodesktop}\Impresorapp"; Filename: "{app}\Impresorapp.exe"; WorkingDir: "{app}"; Tasks: desktopicon; Check: not SmokeTest

[Run]
Filename: "{app}\Impresorapp.exe"; Description: "Abrir Impresorapp"; Flags: nowait postinstall skipifsilent

[Code]
function SmokeTest: Boolean;
begin
  Result := WizardSilent and (ExpandConstant('{param:SMOKETEST|0}') = '1');
end;

function InitializeSetup: Boolean;
var
  Release: Cardinal;
begin
  Result := True;
  if not RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) then
    Release := 0;
  if Release < 528040 then begin
    MsgBox('Impresorapp necesita .NET Framework 4.8. Instálalo desde Microsoft y vuelve a ejecutar este instalador.', mbError, MB_OK);
    Result := False;
  end;
end;
