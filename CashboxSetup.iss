[Setup]
AppName=Cashbox Analyzer
AppVersion=1.0
AppPublisher=Cashbox Analyzer
DefaultDirName={autopf}\Cashbox Analyzer
DefaultGroupName=Cashbox Analyzer
OutputDir=.\InstallerOutput
OutputBaseFilename=CashboxAnalyzer_Setup_v2
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
SetupIconFile=icon.ico
UninstallDisplayIcon={app}\CashboxAnalyzer.exe

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "publish_output\CashboxAnalyzer.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Cashbox Analyzer"; Filename: "{app}\CashboxAnalyzer.exe"
Name: "{group}\{cm:UninstallProgram,Cashbox Analyzer}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Cashbox Analyzer"; Filename: "{app}\CashboxAnalyzer.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\CashboxAnalyzer.exe"; Description: "{cm:LaunchProgram,Cashbox Analyzer}"; Flags: nowait postinstall skipifsilent
