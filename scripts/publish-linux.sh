#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

if [[ "$(uname -s)" != "Linux" ]]; then
  echo "ARCYN publish must be run on Linux."
  exit 1
fi

OUT_DIR="$ROOT_DIR/dist/ARCYN-linux-x64"
rm -rf "$OUT_DIR"
mkdir -p "$OUT_DIR"

dotnet publish ARCYN/ARCYN.Avalonia/ARCYN.Avalonia.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -o "$OUT_DIR"

echo
echo "Published ARCYN:"
echo "  $OUT_DIR/ARCYN"
