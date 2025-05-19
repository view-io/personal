[Setup]
AppId={{B31E2B3F-2C5D-4B3A-A676-94B4BC66DAA2}}
AppName=View Personal
AppVersion=1.0.0
DefaultDirName={autopf}\View Personal
DefaultGroupName=View Personal
OutputDir=.\Output
OutputBaseFilename=ViewPersonalSetup
Compression=lzma
SolidCompression=yes
SetupIconFile="Resource\icon.ico"
UninstallDisplayIcon={app}\icon.ico

[Files]
; Main application files from the Release build
Source: "..\..\src\View.Personal\bin\Release\net9.0\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; Icon file for desktop/start menu shortcuts
Source: "Resource\icon.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\View Personal"; Filename: "{app}\View.Personal.exe"; IconFilename: "{app}\icon.ico"
Name: "{commondesktop}\View Personal"; Filename: "{app}\View.Personal.exe"; IconFilename: "{app}\icon.ico"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce

[Run]
Filename: "{app}\View.Personal.exe"; Description: "{cm:LaunchProgram,View Personal}"; Flags: nowait postinstall skipifsilent