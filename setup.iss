[Setup]
AppName=Shopee Video Uploader
AppVersion=1.0
AppPublisher=Auto Tool
DefaultDirName={autopf}\ShopeeVideoUploader
DefaultGroupName=Shopee Video Uploader
OutputDir=.
OutputBaseFilename=ShopeeVideoUploader_Setup_Full
Compression=lzma2/ultra64
SolidCompression=yes
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
SetupIconFile=compiler:SetupClassicIcon.ico

[Tasks]
Name: "desktopicon"; Description: "Tạo biểu tượng trên màn hình Desktop"; GroupDescription: "Additional icons:"; Flags: checkablealone

[Dirs]
Name: "{app}"; Permissions: users-modify

[Files]
Source: "bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Excludes: "app.db"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\Release\net8.0-windows\win-x64\publish\ShopeeVideoUploader.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Shopee Video Uploader"; Filename: "{app}\ShopeeVideoUploader.exe"
Name: "{commondesktop}\Shopee Video Uploader"; Filename: "{app}\ShopeeVideoUploader.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\ShopeeVideoUploader.exe"; Description: "Khởi động Shopee Video Uploader"; Flags: nowait postinstall skipifsilent

[Code]
function InitializeSetup(): Boolean;
var
  ErrorCode: Integer;
begin
  Exec('taskkill.exe', '/F /IM adb.exe', '', SW_HIDE, ewWaitUntilTerminated, ErrorCode);
  Exec('taskkill.exe', '/F /IM ShopeeVideoUploader.exe', '', SW_HIDE, ewWaitUntilTerminated, ErrorCode);
  Result := True;
end;
