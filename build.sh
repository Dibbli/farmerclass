#!/usr/bin/env bash
# Build + package the mod into dist/farmerclass_<version>.zip
# Usage: ./build.sh            (uses VsDir=/opt/vintagestory)
#        VS_DIR=/path ./build.sh
set -euo pipefail
cd "$(dirname "$0")"

VS_DIR="${VS_DIR:-/opt/vintagestory}"
VERSION=$(grep -oP '"version"\s*:\s*"\K[^"]+' modinfo.json)
OUT="dist/farmerclass_${VERSION}.zip"

dotnet build -c Release -p:VsDir="$VS_DIR"

rm -rf dist/pack "$OUT"
mkdir -p dist/pack/assets
cp modinfo.json bin/Release/farmerclass.dll dist/pack/
cp -r assets/* dist/pack/assets/
( cd dist/pack && zip -qr "../farmerclass_${VERSION}.zip" modinfo.json farmerclass.dll assets )
rm -rf dist/pack

echo "built $OUT"
