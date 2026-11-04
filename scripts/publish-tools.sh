#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-/workspace/dotnet-home}"
export NUGET_PACKAGES="${NUGET_PACKAGES:-/workspace/nuget-packages}"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
DOTNET="${DOTNET:-/workspace/dotnet/dotnet}"
for mode in selfcontained framework; do
  self=true
  if [[ "$mode" == framework ]]; then self=false; fi
  "$DOTNET" publish mock-server/MockServer.csproj -c Release -r win-x64 --self-contained "$self" -o "artifacts/windows-$mode/Server"
  "$DOTNET" publish launcher/WinForms/Launcher.csproj -c Release -r win-x64 --self-contained "$self" -o "artifacts/windows-$mode/Launcher"
done
