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
echo "Build complete. Launching the ARCYN setup wizard..."
echo

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=./wizard.sh
source "$SCRIPT_DIR/wizard.sh"

wiz_rc=0
arcyn_wizard_main || wiz_rc=$?
case "$wiz_rc" in
  0) ;;  # wizard completed (and may have exec'd into run-linux.sh)
  1) echo "Skipped interactive setup." ;;
  2) echo "Setup cancelled by user."; exit 1 ;;
  *) echo "Wizard exited with code $wiz_rc." ;;
esac

echo
echo "Setup complete."
echo "Run ARCYN with: ./scripts/run-linux.sh"
echo "Re-run the setup wizard with: ./scripts/wizard.sh"
