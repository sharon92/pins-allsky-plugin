#!/usr/bin/env bash
set -euo pipefail

REPO_URL="${PINS_ALLSKY_REPO_URL:-https://github.com/sharon92/pins-allsky-plugin.git}"
REPO_REF="${PINS_ALLSKY_GIT_REF:-main}"
HOME_DIR="${HOME:-/home/pi}"
DEFAULT_REPO_DIR="$HOME_DIR/pins-allsky-plugin"
ALT_REPO_DIR="$HOME_DIR/Projects/piplugin"
REPO_DIR="${PINS_ALLSKY_REPO_DIR:-}"

choose_existing_repo_dir() {
  local candidate
  for candidate in "$REPO_DIR" "$DEFAULT_REPO_DIR" "$ALT_REPO_DIR"; do
    if [[ -n "$candidate" && -d "$candidate/.git" ]]; then
      printf '%s\n' "$candidate"
      return 0
    fi
  done

  return 1
}

if ! command -v git >/dev/null 2>&1; then
  echo "git is required but was not found in PATH." >&2
  exit 1
fi

if ! command -v bash >/dev/null 2>&1; then
  echo "bash is required but was not found in PATH." >&2
  exit 1
fi

if existing_repo_dir="$(choose_existing_repo_dir)"; then
  REPO_DIR="$existing_repo_dir"
else
  REPO_DIR="${REPO_DIR:-$DEFAULT_REPO_DIR}"
fi

echo "Updating PINS AllSky backend from $REPO_URL ($REPO_REF)"
echo "Using repo directory: $REPO_DIR"

if [[ ! -d "$REPO_DIR/.git" ]]; then
  rm -rf "$REPO_DIR"
  git clone --branch "$REPO_REF" --single-branch "$REPO_URL" "$REPO_DIR"
fi

git -C "$REPO_DIR" fetch origin "$REPO_REF" --tags

if git -C "$REPO_DIR" show-ref --verify --quiet "refs/heads/$REPO_REF"; then
  git -C "$REPO_DIR" checkout "$REPO_REF"
else
  git -C "$REPO_DIR" checkout -B "$REPO_REF" "origin/$REPO_REF"
fi

git -C "$REPO_DIR" pull --ff-only origin "$REPO_REF"

cd "$REPO_DIR"
./scripts/install-backend-plugin.sh --restart-pins
