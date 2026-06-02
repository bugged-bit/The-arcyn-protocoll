#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

if [[ "$(uname -s)" != "Linux" ]]; then
  echo "ARCYN can only run on Linux."
  exit 1
fi

if ! command -v dotnet >/dev/null 2>&1; then
  echo "Missing dependency: dotnet. Install the .NET 8 SDK."
  exit 1
fi

dotnet run --project ARCYN/ARCYN.Avalonia/ARCYN.Avalonia.csproj
