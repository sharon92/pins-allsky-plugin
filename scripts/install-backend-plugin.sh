#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DOTNET_BIN="${DOTNET_BIN:-/home/pi/.local/dotnet/dotnet}"
BACKEND_PROJECT="$ROOT_DIR/backend/PinsAllSky/PinsAllSky.csproj"
BACKEND_OUTPUT="$ROOT_DIR/backend/PinsAllSky/bin/Release/net10.0"
ARTIFACTS_DIR="$ROOT_DIR/artifacts"
BACKEND_PACKAGE_DIR="$ARTIFACTS_DIR/backend-plugin/PINS AllSky"
PLUGIN_ROOT="$HOME/.local/share/NINA/Plugins/3.0.0"
BACKEND_INSTALL_DIR="$PLUGIN_ROOT/PINS AllSky"
INSTALL_BACKUPS_DIR="$ROOT_DIR/.install-backups"
PINS_BIN="${PINS_BIN:-/home/pi/pins/NINA}"
PINS_WORKDIR="${PINS_WORKDIR:-$(dirname "$PINS_BIN")}"
PINS_LD_LIBRARY_PATH="${PINS_LD_LIBRARY_PATH:-/opt/opencvsharp/lib:/usr/lib/aarch64-linux-gnu}"
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

if [[ ! -x "$DOTNET_BIN" ]]; then
  echo "dotnet binary not found at $DOTNET_BIN" >&2
  exit 1
fi

"$DOTNET_BIN" build "$BACKEND_PROJECT" -c Release

rm -rf "$BACKEND_PACKAGE_DIR"
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

mkdir -p "$PLUGIN_ROOT" "$INSTALL_BACKUPS_DIR/plugins"

if [[ -d "$BACKEND_INSTALL_DIR" ]]; then
  mv "$BACKEND_INSTALL_DIR" "$INSTALL_BACKUPS_DIR/plugins/PINS AllSky-$TIMESTAMP"
fi

cp -a "$BACKEND_PACKAGE_DIR" "$BACKEND_INSTALL_DIR"

echo "Installed backend plugin to: $BACKEND_INSTALL_DIR"

if [[ "$RESTART_PINS" == true ]]; then
  old_pid="$(pgrep -xo -f "^$PINS_BIN$" || true)"
  if pgrep -f "^$PINS_BIN$" >/dev/null 2>&1; then
    pkill -f "^$PINS_BIN$"
    for _ in $(seq 1 30); do
      if ! pgrep -f "^$PINS_BIN$" >/dev/null 2>&1; then
        break
      fi
      sleep 1
    done
  fi

  if pgrep -f "^$PINS_BIN$" >/dev/null 2>&1; then
    echo "PINS did not exit cleanly after SIGTERM; aborting restart." >&2
    exit 1
  fi

  restarted_via_systemd=false
  if command -v systemctl >/dev/null 2>&1 && systemctl status pins.service >/dev/null 2>&1; then
    for _ in $(seq 1 60); do
      new_pid="$(pgrep -xo -f "^$PINS_BIN$" || true)"
      if [[ -n "$new_pid" && "$new_pid" != "$old_pid" ]]; then
        restarted_via_systemd=true
        break
      fi
      sleep 1
    done
  fi

  if [[ "$restarted_via_systemd" == true ]]; then
    echo "PINS restarted under systemd management."
  else
    nohup bash -lc "cd \"$PINS_WORKDIR\" && export LD_LIBRARY_PATH=\"$PINS_LD_LIBRARY_PATH\" && exec \"$PINS_BIN\"" >/tmp/pins-allsky-pins.log 2>&1 &
    echo "Restarted PINS using $PINS_BIN from $PINS_WORKDIR with LD_LIBRARY_PATH=$PINS_LD_LIBRARY_PATH"
  fi

  echo "PINS can take around a minute to finish loading plugins and bind its web ports."
else
  echo "PINS was not restarted. Restart it manually before using the backend plugin."
fi
