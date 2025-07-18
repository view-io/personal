[Setup]
AppId={{B31E2B3F-2C5D-4B3A-A676-94B4BC66DAA2}}
AppName=View Personal
#define MyAppVersion GetFileVersion("..\..\src\View.Personal\publish\View.Personal.exe")
AppVerName=View Personal v{#MyAppVersion}
DefaultDirName={autopf}\View Personal
PrivilegesRequired=admin
DefaultGroupName=View Personal
OutputDir=.\Output
OutputBaseFilename=ViewPersonalSetup
Compression=lzma
SolidCompression=yes
DisableWelcomePage=no
SetupIconFile="Resource\icon.ico"
WizardImageFile="Resource\wizard_image.bmp"
WizardSmallImageFile="Resource\wizard_small_icon.bmp"
UninstallDisplayIcon={app}\icon.ico

[Files]
Source: "..\..\src\_publish\View.Personal\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\..\src\_publish\ViewPersonal.Updater\*"; DestDir: "{app}\Updater"; Flags: ignoreversion recursesubdirs createallsubdirs



; Icon
Source: "Resource\icon.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\View Personal"; Filename: "{app}\View.Personal.exe"; IconFilename: "{app}\icon.ico"
Name: "{commondesktop}\View Personal"; Filename: "{app}\View.Personal.exe"; IconFilename: "{app}\icon.ico"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce

[Run]
Filename: "{app}\View.Personal.exe"; Description: "{cm:LaunchProgram,View Personal}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}\*"
Type: dirifempty; Name: "{app}"

[Code]
var
  InfoPage: TWizardPage;

procedure InitializeWizard;
begin
  WizardForm.WelcomeLabel1.Caption := 'Welcome to the View Personal Installer';
  WizardForm.WelcomeLabel2.Caption :=
    'This installer will guide you through the setup process for View Personal.' + #13#10 +
    'Please review the AI service requirements on the next screen.';

  InfoPage := CreateCustomPage(
    wpLicense,
    'An External AI Service is Required',
    'Important Information Before Installation'
  );

  with TLabel.Create(InfoPage) do
  begin
    Parent := InfoPage.Surface;
    Caption :=
      'To use this application, you need to have access to at least one of the following services:' + #13#10 + #13#10 +
      '1. OpenAI (API key required)' + #13#10 +
      '2. Claude by Anthropic (API key required)' + #13#10 +
      '3. Local Ollama deployment' + #13#10 +
      '4. View deployment' + #13#10 + #13#10 +
      'Please ensure you have access before proceeding.';
    WordWrap := True;
    SetBounds(0, 0, InfoPage.SurfaceWidth, 140);
  end;
end;
