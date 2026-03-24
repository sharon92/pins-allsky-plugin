#!/usr/bin/env bash
set -euo pipefail

BACKEND_URL="${BACKEND_URL:-http://127.0.0.1:19091}"
LABEL="smoke-$(date +%Y%m%d-%H%M%S)"
WAIT_SECONDS="${WAIT_SECONDS:-16}"
POLL_INTERVAL=2
MAX_POLLS=20

require_cmd() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Missing required command: $1" >&2
    exit 1
  fi
}

require_cmd curl
require_cmd python3

read_json_field() {
  local json_input="$1"
  local python_expr="$2"
  JSON_INPUT="$json_input" python3 - "$python_expr" <<'PY'
import json
import os
import sys

expr = sys.argv[1]
data = json.loads(os.environ["JSON_INPUT"])
result = eval(expr, {"__builtins__": {}}, {"data": data})
if result is None:
    print("")
elif isinstance(result, bool):
    print("true" if result else "false")
else:
    print(result)
PY
}

status_json="$(curl -fsS "$BACKEND_URL/api/status")"
if [[ "$(read_json_field "$status_json" 'data.get("Success", data.get("success"))')" != "true" ]]; then
  echo "Backend status check failed: $status_json" >&2
  exit 1
fi

config_json="$(curl -fsS "$BACKEND_URL/api/config")"
config_payload="$(JSON_INPUT="$config_json" python3 <<'PY'
import json
import os

payload = json.loads(os.environ["JSON_INPUT"])
config = payload.get("Data") or payload.get("data") or {}
camera = config.setdefault("camera", {})
products = config.setdefault("products", {})

camera["intervalSeconds"] = 5
camera["captureTimeoutSeconds"] = 45
camera["width"] = 2028
camera["height"] = 1520
camera["warmupMilliseconds"] = 3000

products["keepFrames"] = True
products["timelapseEnabled"] = True
products["keogramEnabled"] = True
products["startrailsEnabled"] = True

print(json.dumps(config))
PY
)"

curl -fsS -X PUT \
  -H 'Content-Type: application/json' \
  -d "$config_payload" \
  "$BACKEND_URL/api/config" >/dev/null

curl -fsS -X POST \
  -H 'Content-Type: application/json' \
  -d "{\"label\":\"$LABEL\"}" \
  "$BACKEND_URL/api/session/start" >/dev/null

sleep "$WAIT_SECONDS"

curl -fsS -X POST \
  -H 'Content-Type: application/json' \
  -d '{"generateArtifacts":true}' \
  "$BACKEND_URL/api/session/stop" >/dev/null

for ((i = 1; i <= MAX_POLLS; i++)); do
  status_json="$(curl -fsS "$BACKEND_URL/api/status")"
  latest_session="$(JSON_INPUT="$status_json" SESSION_LABEL="$LABEL" python3 <<'PY'
import json
import os

payload = json.loads(os.environ["JSON_INPUT"])
label = os.environ["SESSION_LABEL"]
recent_sessions = (payload.get("Data") or payload.get("data") or {}).get("RecentSessions") or (payload.get("Data") or payload.get("data") or {}).get("recentSessions") or []
for session in recent_sessions:
    if session.get("label") == label or session.get("Label") == label:
        print(json.dumps(session))
        break
PY
)"

  if [[ -n "$latest_session" ]]; then
    timelapse_path="$(read_json_field "$latest_session" '((data.get("products") or data.get("Products") or {}).get("timelapse") or (data.get("products") or data.get("Products") or {}).get("Timelapse") or {}).get("relativePath") or (((data.get("products") or data.get("Products") or {}).get("timelapse") or (data.get("products") or data.get("Products") or {}).get("Timelapse") or {}).get("RelativePath"))')"
    keogram_path="$(read_json_field "$latest_session" '((data.get("products") or data.get("Products") or {}).get("keogram") or (data.get("products") or data.get("Products") or {}).get("Keogram") or {}).get("relativePath") or (((data.get("products") or data.get("Products") or {}).get("keogram") or (data.get("products") or data.get("Products") or {}).get("Keogram") or {}).get("RelativePath"))')"
    startrails_path="$(read_json_field "$latest_session" '((data.get("products") or data.get("Products") or {}).get("startrails") or (data.get("products") or data.get("Products") or {}).get("Startrails") or {}).get("relativePath") or (((data.get("products") or data.get("Products") or {}).get("startrails") or (data.get("products") or data.get("Products") or {}).get("Startrails") or {}).get("RelativePath"))')"

    if [[ -n "$timelapse_path" && -n "$keogram_path" && -n "$startrails_path" ]]; then
      echo "Smoke test session: $(read_json_field "$latest_session" 'data.get("id") or data.get("Id")')"
      echo "Timelapse: $BACKEND_URL/media/$timelapse_path"
      echo "Keogram: $BACKEND_URL/media/$keogram_path"
      echo "Startrails: $BACKEND_URL/media/$startrails_path"
      exit 0
    fi
  fi

  sleep "$POLL_INTERVAL"
done

echo "Timed out waiting for generated artifacts for session label $LABEL" >&2
exit 1
