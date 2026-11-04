#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-/workspace/dotnet-home}"
export NUGET_PACKAGES="${NUGET_PACKAGES:-/workspace/nuget-packages}"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_ROOT="${DOTNET_ROOT:-/workspace/dotnet}"
DOTNET="${DOTNET:-/workspace/dotnet/dotnet}"
for project in mock-server/MockServer.csproj launcher/WinForms/Launcher.csproj tests/ClientProbe/ClientProbe.csproj tests/LauncherChecks/LauncherChecks.csproj tests/ApiChecks/ApiChecks.csproj; do
  "$DOTNET" build "$project" -c Release
done
"$DOTNET" run --project tests/CoreChecks/CoreChecks.csproj -c Release
"$DOTNET" run --project tests/DebugChecks/DebugChecks.csproj -c Release
