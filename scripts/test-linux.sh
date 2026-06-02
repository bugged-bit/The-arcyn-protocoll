#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

if [[ "$(uname -s)" != "Linux" ]]; then
  echo "ARCYN tests are intended for Linux."
  exit 1
fi

dotnet restore ARCYN/ARCYN.sln
dotnet build ARCYN/ARCYN.sln -c Release --no-restore
dotnet test ARCYN/ARCYN.sln -c Release --no-build
