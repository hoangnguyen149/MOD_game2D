$ErrorActionPreference='Stop'
Set-Location (Join-Path $PSScriptRoot '..')
foreach($project in @('mock-server/MockServer.csproj','launcher/WinForms/Launcher.csproj','tests/ClientProbe/ClientProbe.csproj','tests/LauncherChecks/LauncherChecks.csproj','tests/ApiChecks/ApiChecks.csproj')){
 dotnet build $project -c Release
 if($LASTEXITCODE -ne 0){throw "Build failed: $project"}
}
dotnet run --project tests/CoreChecks -c Release
if($LASTEXITCODE -ne 0){throw 'Core checks failed'}
dotnet run --project tests/DebugChecks -c Release
if($LASTEXITCODE -ne 0){throw 'Debug checks failed'}
python tests/smoke.py
if($LASTEXITCODE -ne 0){throw 'Integration checks failed'}
