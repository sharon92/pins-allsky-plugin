#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DOTNET_BIN="${DOTNET_BIN:-/home/pi/.local/dotnet/dotnet}"
BACKEND_PROJECT="$ROOT_DIR/backend/PinsAllSky/PinsAllSky.csproj"
BACKEND_OUTPUT="$ROOT_DIR/backend/PinsAllSky/bin/Release/net10.0"
FRONTEND_SOURCE="$ROOT_DIR/frontend/pins-allsky"
TNS_SOURCE="$ROOT_DIR/_vendor/Touch-N-Stars-d43bceb6d24956beb35097267b7c28712d8ccdcc"
TNS_PLUGIN_DIR="$TNS_SOURCE/src/plugins/pins-allsky"
ARTIFACTS_DIR="$ROOT_DIR/artifacts"
BUILD_BACKUPS_DIR="$ROOT_DIR/.build-backups"
BACKEND_PACKAGE_DIR="$ARTIFACTS_DIR/backend-plugin/PINS AllSky"
FRONTEND_PACKAGE_DIR="$ARTIFACTS_DIR/touch-n-stars-app"
TIMESTAMP="$(date +%Y%m%d-%H%M%S)"

require_cmd() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Missing required command: $1" >&2
    exit 1
  fi
}

require_cmd npm

if [[ ! -x "$DOTNET_BIN" ]]; then
  echo "dotnet binary not found at $DOTNET_BIN" >&2
  exit 1
fi

mkdir -p "$ARTIFACTS_DIR/backend-plugin" "$ARTIFACTS_DIR" "$BUILD_BACKUPS_DIR/tns-plugins"

"$DOTNET_BIN" build "$BACKEND_PROJECT" -c Release

if [[ -d "$BACKEND_PACKAGE_DIR" ]]; then
  mv "$BACKEND_PACKAGE_DIR" "${BACKEND_PACKAGE_DIR}.bak-$TIMESTAMP"
fi
mkdir -p "$BACKEND_PACKAGE_DIR/tools" "$BACKEND_PACKAGE_DIR/licenses" "$BACKEND_PACKAGE_DIR/scripts"

for file in \
  NINA.PINS.AllSky.dll \
  NINA.PINS.AllSky.deps.json \
  NINA.PINS.AllSky.runtimeconfig.json \
  NINA.PINS.AllSky.pdb \
  EmbedIO.dll \
  Swan.Lite.dll
do
  cp "$BACKEND_OUTPUT/$file" "$BACKEND_PACKAGE_DIR/"
done

cp "$BACKEND_OUTPUT/tools/keogram" "$BACKEND_PACKAGE_DIR/tools/"
cp "$BACKEND_OUTPUT/tools/startrails" "$BACKEND_PACKAGE_DIR/tools/"
cp "$ROOT_DIR/THIRD_PARTY_NOTICES.md" "$BACKEND_PACKAGE_DIR/"
cp "$ROOT_DIR/licenses/AllSky-LICENSE.txt" "$BACKEND_PACKAGE_DIR/licenses/"
cp "$ROOT_DIR/scripts/update-backend-plugin.sh" "$BACKEND_PACKAGE_DIR/scripts/"
chmod +x "$BACKEND_PACKAGE_DIR/scripts/update-backend-plugin.sh"

if [[ -d "$TNS_PLUGIN_DIR" ]]; then
  mv "$TNS_PLUGIN_DIR" "$BUILD_BACKUPS_DIR/tns-plugins/pins-allsky-$TIMESTAMP"
fi
cp -a "$FRONTEND_SOURCE" "$TNS_PLUGIN_DIR"

if [[ ! -d "$TNS_SOURCE/node_modules" ]]; then
  (cd "$TNS_SOURCE" && npm ci)
fi

(cd "$TNS_SOURCE" && npm run build:app)

if [[ -d "$FRONTEND_PACKAGE_DIR" ]]; then
  mv "$FRONTEND_PACKAGE_DIR" "${FRONTEND_PACKAGE_DIR}.bak-$TIMESTAMP"
fi
cp -a "$TNS_SOURCE/dist" "$FRONTEND_PACKAGE_DIR"

echo "Backend package: $BACKEND_PACKAGE_DIR"
echo "Frontend app bundle: $FRONTEND_PACKAGE_DIR"
