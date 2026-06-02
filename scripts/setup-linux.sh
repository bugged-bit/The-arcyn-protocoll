#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

if [[ "$(uname -s)" != "Linux" ]]; then
  echo "ARCYN setup must be run on Linux."
  exit 1
fi

if ! command -v dotnet >/dev/null 2>&1; then
  echo "Missing dependency: dotnet."
  echo "Install the .NET 8 SDK, then rerun ./scripts/setup-linux.sh."
  exit 1
fi

if ! dotnet --list-sdks | grep -q '^8\.'; then
  echo "Missing dependency: .NET 8 SDK."
  echo "Installed SDKs:"
  dotnet --list-sdks || true
  exit 1
fi

if ! command -v xdg-open >/dev/null 2>&1; then
  echo "Missing dependency: xdg-open."
  echo "Install xdg-utils with your package manager."
  exit 1
fi

echo "Restoring ARCYN..."
dotnet restore ARCYN/ARCYN.sln

echo "Building ARCYN..."
dotnet build ARCYN/ARCYN.sln -c Release --no-restore

echo
echo "Setup complete."
echo "Run ARCYN with: ./scripts/run-linux.sh"
echo "Optional config starter:"
echo "  mkdir -p ~/.config/ARCYN"
echo "  cp ARCYN/example.arcyn.json ~/.config/ARCYN/arcyn.json"
