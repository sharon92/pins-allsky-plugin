#!/usr/bin/env bash

PINS_ALLSKY_REQUIRED_DOTNET_MAJOR="${PINS_ALLSKY_REQUIRED_DOTNET_MAJOR:-10}"

pins_allsky_print_dotnet_install_help() {
  local detected_arch="${1:-$(uname -m)}"
  local normalized_arch="unknown"
  local install_dir="${HOME}/.local/dotnet"
  local install_command="curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel ${PINS_ALLSKY_REQUIRED_DOTNET_MAJOR}.0 --install-dir ${install_dir}"

  case "$detected_arch" in
    aarch64 | arm64)
      normalized_arch="arm64"
      ;;
    armv7l | armv6l | armhf | arm)
      normalized_arch="arm32"
      ;;
    x86_64 | amd64)
      normalized_arch="x64"
      ;;
  esac

  echo "This script builds the backend from source and requires the .NET SDK ${PINS_ALLSKY_REQUIRED_DOTNET_MAJOR}.x." >&2
  echo "The .NET runtime alone is not enough." >&2
  echo "You can also point the script to a custom install with DOTNET_BIN=/path/to/dotnet." >&2
  echo >&2

  if [[ "$normalized_arch" == "arm32" ]]; then
    install_command="${install_command} --architecture arm"
    echo "Detected architecture: $detected_arch (32-bit ARM)." >&2
    echo "On 32-bit Raspberry Pi OS / Debian, Microsoft usually doesn't provide apt packages for the .NET SDK." >&2
  else
    echo "Detected architecture: $detected_arch." >&2
    echo "On 64-bit Debian / Raspberry Pi OS, the dotnet-install script above is the recommended path for this backend." >&2
  fi

  echo >&2
  echo "Recommended install steps:" >&2
  echo "  ${install_command}" >&2
  echo "  echo 'export DOTNET_ROOT=${install_dir}' >> ~/.bashrc" >&2
  echo "  echo 'export PATH=\$PATH:${install_dir}' >> ~/.bashrc" >&2
  echo "  source ~/.bashrc" >&2
}

pins_allsky_resolve_dotnet_bin() {
  local candidate=""

  if [[ -n "${DOTNET_BIN:-}" ]]; then
    candidate="$DOTNET_BIN"
    if [[ ! -x "$candidate" ]]; then
      echo "DOTNET_BIN is set but not executable: $candidate" >&2
      pins_allsky_print_dotnet_install_help
      return 1
    fi

    DOTNET_BIN="$candidate"
    export DOTNET_BIN
    return 0
  fi

  candidate="$HOME/.local/dotnet/dotnet"
  if [[ -x "$candidate" ]]; then
    DOTNET_BIN="$candidate"
    export DOTNET_BIN
    return 0
  fi

  candidate="$(command -v dotnet || true)"
  if [[ -n "$candidate" && -x "$candidate" ]]; then
    DOTNET_BIN="$candidate"
    export DOTNET_BIN
    return 0
  fi

  echo "dotnet binary not found." >&2
  pins_allsky_print_dotnet_install_help
  return 1
}

pins_allsky_require_dotnet_sdk_major() {
  local required_major="$1"
  local sdk_versions=""

  if ! sdk_versions="$("$DOTNET_BIN" --list-sdks 2>/dev/null)"; then
    echo "Failed to query installed .NET SDKs using: $DOTNET_BIN" >&2
    pins_allsky_print_dotnet_install_help
    return 1
  fi

  if [[ -z "$sdk_versions" ]]; then
    echo ".NET was found at $DOTNET_BIN, but no SDKs are installed." >&2
    echo "This repository builds the backend locally, so a runtime-only install will not work." >&2
    pins_allsky_print_dotnet_install_help
    return 1
  fi

  if ! grep -Eq "^${required_major}\\." <<<"$sdk_versions"; then
    echo "Found dotnet at $DOTNET_BIN, but the required SDK major version is missing." >&2
    echo "Required SDK major: $required_major" >&2
    echo "Installed SDKs:" >&2
    echo "$sdk_versions" >&2
    pins_allsky_print_dotnet_install_help
    return 1
  fi

  PINS_ALLSKY_DOTNET_SDK_VERSION="$(grep -E "^${required_major}\\." <<<"$sdk_versions" | head -n 1 | awk '{print $1}')"
  export PINS_ALLSKY_DOTNET_SDK_VERSION
}
