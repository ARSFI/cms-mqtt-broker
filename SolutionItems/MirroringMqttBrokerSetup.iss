; Script generated by the Inno Script Studio Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "Mirroring MQTT Broker"
#define MyAppPublisher "Amateur Radio Safety Foundation, Inc"
#define MyAppURL "http://www.winlink.org/"
#define MyAppExeName "MirroringMqttBroker.exe"
#define SourceDir "D:\Projects\cms-mqtt-broker\MirroringMqttBroker\bin\Release\net5.0\publish\"

#define VerExe 'D:\Projects\cms-mqtt-broker\MirroringMqttBroker\bin\Release\net5.0\publish\MirroringMqttBroker.exe'
#define MyAppVersion GetFileVersion(VerExe)

#define VerMajor
#define VerMinor
#define VerRev
#define VerBuild
#expr ParseVersion(VerExe, VerMajor, VerMinor, VerRev, VerBuild)

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{16146D43-BC31-4D65-8725-6742BC6D6492}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={commonpf}\Winlink 2000\MirroringMqttBroker
DisableDirPage=yes
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputBaseFilename={#MyAppName} {#VerMajor}.{#VerMinor}.{#VerRev}
Compression=lzma
SolidCompression=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "{#SourceDir}*.exe"; DestDir: "{app}"
Source: "{#SourceDir}*.dll"; DestDir: "{app}"
Source: "{#SourceDir}*.json"; DestDir: "{app}"
Source: "{#SourceDir}*.pdb"; DestDir: "{app}"
Source: "{#SourceDir}web.config"; DestDir: "{app}"
Source: "{#SourceDir}NLog.config"; DestDir: "{app}"; AfterInstall: ChangeLogServer('{app}\NLog.config')

[Run]
Filename: {sys}\sc.exe; Parameters: "create MirroringMqttBroker DisplayName= ""Mirroring MQTT Broker"" \
  start= {code:GetServiceStartMode} \
  binPath= ""{app}\MirroringMqttBroker.exe""" ; Flags: runhidden; \
  AfterInstall: SelectSettingsFileForServer()
            

[UninstallRun]
Filename: {sys}\sc.exe; Parameters: "stop MirroringMqttBroker" ; Flags: runhidden
Filename: {sys}\sc.exe; Parameters: "delete MirroringMqttBroker" ; Flags: runhidden

;code to uninstall previous version
[Code]
/////////////////////////////////////////////////////////////////////
function GetUninstallString(): String;
var
  sUnInstPath: String;
  sUnInstallString: String;
begin
  sUnInstPath := ExpandConstant('Software\Microsoft\Windows\CurrentVersion\Uninstall\{#emit SetupSetting("AppId")}_is1');
  sUnInstallString := '';
  if not RegQueryStringValue(HKLM, sUnInstPath, 'UninstallString', sUnInstallString) then
    RegQueryStringValue(HKCU, sUnInstPath, 'UninstallString', sUnInstallString);
  Result := sUnInstallString;
end;


/////////////////////////////////////////////////////////////////////
function IsUpgrade(): Boolean;
begin
  Result := (GetUninstallString() <> '');
end;


/////////////////////////////////////////////////////////////////////
function UnInstallOldVersion(): Integer;
var
  sUnInstallString: String;
  iResultCode: Integer;
begin
// Return Values:
// 1 - uninstall string is empty
// 2 - error executing the UnInstallString
// 3 - successfully executed the UnInstallString

  // default return value
  Result := 0;

  // get the uninstall string of the old app
  sUnInstallString := GetUninstallString();
  if sUnInstallString <> '' then begin
    sUnInstallString := RemoveQuotes(sUnInstallString);
    if Exec(sUnInstallString, '/SILENT /NORESTART /SUPPRESSMSGBOXES','', SW_HIDE, ewWaitUntilTerminated, iResultCode) then
      Result := 3
    else
      Result := 2;
  end else
    Result := 1;
end;

/////////////////////////////////////////////////////////////////////
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep=ssInstall) then
  begin
    if (IsUpgrade()) then
    begin
      UnInstallOldVersion();
    end;
  end;
end;

//change log server url in nlog.config if installing to test server
procedure ChangeLogServer(Filename: String);
var
    FileData: AnsiString;
    FileDataU: String;
    TestSite: String;
begin
    //read c:\cms\cms.ini and look at test server entry
    //if test server change url to graylog.winlink.org
    TestSite:= GetIniString('Settings', 'TestSite', 'FALSE', 'C:\CMS\CMS Settings.ini');
    if UpperCase(Testsite)<>'FALSE' then begin
        Filename:= ExpandConstant(Filename);
        LoadStringFromFile(Filename, FileData);
        FileDataU:= String(FileData); //because StringChangeEx expects a unicode string param 
        StringChangeEx(FileDataU, '172.31.22.82', 'graylog.winlink.org', True);
        //cast file data back to ansi string
        SaveStringToFile(FileName, AnsiString(FileDataU), False);
    end;
end;

//select a appsettings.json file for the current server 
procedure SelectSettingsFileForServer();
var
    NodeName: String;
    Path: String;
begin
    //read c:\cms\cms.ini and get node name property
    NodeName:= GetIniString('Settings', 'NodeName', '', 'C:\CMS\CMS Settings.ini');
    NodeName:= UpperCase(NodeName);
    Path:= ExpandConstant('{app}');
    Path:= AddBackslash(Path);
    //MsgBox(Path, mbInformation, MB_OK);

    case (NodeName) of
      'CMS-A' : begin 
                  FileCopy(Path + 'appSettings.cms-a.json', Path + 'appSettings.json', False);
                end;
      'CMS-B' : begin 
                  FileCopy(Path + 'appSettings.cms-b.json', Path + 'appSettings.json', False);
                end;
      'CMS-Z' : begin 
                  FileCopy(Path + 'appSettings.cms-z.json', Path + 'appSettings.json', False);
                end;
    end;
end;

//test server type (test or production) and return start mode for service (demand or auto)
function GetServiceStartMode(Param: String): String;
var
    TestSite: String;
begin 
    TestSite:= GetIniString('Settings', 'TestSite', 'TRUE', 'C:\CMS\CMS Settings.ini');
    TestSite:= UpperCase(TestSite);
    if TestSite = 'TRUE' then begin
        Result:= 'demand';
    end else begin
        Result:= 'auto';
    end;
end;
