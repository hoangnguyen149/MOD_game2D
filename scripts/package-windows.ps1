param([Parameter(Mandatory=$true)][string]$UnityEditor,[switch]$Development)
$ErrorActionPreference='Stop'
Set-Location (Join-Path $PSScriptRoot '..')
if(!(Test-Path -LiteralPath $UnityEditor -PathType Leaf)){throw 'Unity Editor path does not exist'}
dotnet publish mock-server/MockServer.csproj -c Release -r win-x64 --self-contained true -o artifacts/package/Server
if($LASTEXITCODE -ne 0){throw 'Server publish failed'}
dotnet publish launcher/WinForms/Launcher.csproj -c Release -r win-x64 --self-contained true -o artifacts/package/Launcher
if($LASTEXITCODE -ne 0){throw 'Launcher publish failed'}
$method='BuildGame.WindowsRelease';$client='artifacts/Client';$label='Release'
if($Development){$method='BuildGame.WindowsDevelopment';$client='artifacts/Client-Development';$label='Development'}
& $UnityEditor -batchmode -quit -projectPath "$PWD/game-client" -executeMethod $method -logFile "$PWD/artifacts/unity-$label.log"
if($LASTEXITCODE -ne 0){throw "Unity build failed; see artifacts/unity-$label.log"}
foreach($required in @('OwnedAdventure.exe','UnityPlayer.dll','GameAssembly.dll','OwnedAdventure_Data')){
 if(!(Test-Path -LiteralPath (Join-Path $client $required))){throw "Client artifact missing: $required"}
}
$destination="artifacts/OwnedAdventure-Windows-$label.zip"
Compress-Archive -Path $client,artifacts/package/Server,artifacts/package/Launcher -DestinationPath $destination -Force
Write-Output "Created $destination"
