#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PLUGIN_ROOT="$HOME/.local/share/NINA/Plugins/3.0.0"
INSTALL_BACKUPS_DIR="$ROOT_DIR/.install-backups"
BACKEND_PACKAGE_DIR="$ROOT_DIR/artifacts/backend-plugin/PINS AllSky"
FRONTEND_PACKAGE_DIR="$ROOT_DIR/artifacts/touch-n-stars-app"
BACKEND_INSTALL_DIR="$PLUGIN_ROOT/PINS AllSky"
TNS_APP_DIR="$PLUGIN_ROOT/Touch-N-Stars/app"
PINS_BIN="${PINS_BIN:-/home/pi/pins/NINA}"
PINS_WORKDIR="${PINS_WORKDIR:-$(dirname "$PINS_BIN")}"
RESTART_PINS=false
TIMESTAMP="$(date +%Y%m%d-%H%M%S)"

for arg in "$@"; do
  case "$arg" in
    --restart-pins)
      RESTART_PINS=true
      ;;
    *)
      echo "Unknown argument: $arg" >&2
      exit 1
      ;;
  esac
done

if [[ ! -d "$BACKEND_PACKAGE_DIR" || ! -d "$FRONTEND_PACKAGE_DIR" ]]; then
  echo "Build artifacts are missing. Run scripts/build-plugin.sh first." >&2
  exit 1
fi

mkdir -p "$PLUGIN_ROOT"
mkdir -p "$INSTALL_BACKUPS_DIR/plugins" "$INSTALL_BACKUPS_DIR/touch-n-stars"

if [[ -d "$BACKEND_INSTALL_DIR" ]]; then
  mv "$BACKEND_INSTALL_DIR" "$INSTALL_BACKUPS_DIR/plugins/PINS AllSky-$TIMESTAMP"
fi
cp -a "$BACKEND_PACKAGE_DIR" "$BACKEND_INSTALL_DIR"

if [[ -d "$TNS_APP_DIR" ]]; then
  mv "$TNS_APP_DIR" "$INSTALL_BACKUPS_DIR/touch-n-stars/app-$TIMESTAMP"
fi
cp -a "$FRONTEND_PACKAGE_DIR" "$TNS_APP_DIR"

echo "Installed backend plugin to: $BACKEND_INSTALL_DIR"
echo "Installed Touch-N-Stars app bundle to: $TNS_APP_DIR"

if [[ "$RESTART_PINS" == true ]]; then
  if pgrep -f "^$PINS_BIN$" >/dev/null 2>&1; then
    pkill -f "^$PINS_BIN$"
    sleep 5
  fi

  nohup bash -lc "cd \"$PINS_WORKDIR\" && exec \"$PINS_BIN\"" >/tmp/pins-allsky-pins.log 2>&1 &
  sleep 10
  echo "Restarted PINS using $PINS_BIN from $PINS_WORKDIR"
else
  echo "PINS was not restarted. Restart it manually before using the plugin."
fi
