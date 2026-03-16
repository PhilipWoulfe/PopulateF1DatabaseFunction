#!/usr/bin/env bash
set -euo pipefail

show_help() {
  cat <<'HELP'
Usage:
  ./scripts/e2e-debug-vm.sh [test-filter]

Examples:
  ./scripts/e2e-debug-vm.sh
  ./scripts/e2e-debug-vm.sh "FullyQualifiedName~SubmitSelection_ShouldPersistServerSide"
  ./scripts/e2e-debug-vm.sh all

Notes:
  - This script is intended for SSH VM Selenium debugging.
  - Keep an SSH tunnel open from your laptop: ssh -L 9222:localhost:9222 <user>@<vm>
  - While tests run, inspect Chrome targets at: http://localhost:9222/json/list
  - Optional: E2E_DEBUG_HOLD_SECONDS=120 keeps each test browser alive before teardown.
HELP
}

load_dotenv_file() {
  local dotenv_path="$1"
  while IFS= read -r line || [[ -n "$line" ]]; do
    line="${line%$'\r'}"

    if [[ -z "$line" || "$line" =~ ^[[:space:]]*# ]]; then
      continue
    fi

    if [[ "$line" != *=* ]]; then
      continue
    fi

    local key="${line%%=*}"
    local value="${line#*=}"

    key="${key#${key%%[![:space:]]*}}"
    key="${key%${key##*[![:space:]]}}"

    if [[ -z "$key" ]]; then
      continue
    fi

    if [[ ( "$value" == \"*\" && "$value" == *\" ) || ( "$value" == \'.*\' && "$value" == *\' ) ]]; then
      value="${value:1:${#value}-2}"
    fi

    export "$key=$value"
  done < "$dotenv_path"
}

if [[ "${1:-}" == "-h" || "${1:-}" == "--help" ]]; then
  show_help
  exit 0
fi

if [[ ! -f "F1Competition.sln" ]]; then
  echo "Run this script from the repository root (F1Competition)."
  exit 1
fi

ENV_FILE="${E2E_ENV_FILE:-.env}"
if [[ -f "$ENV_FILE" ]]; then
  load_dotenv_file "$ENV_FILE"
  echo "Loaded environment from $ENV_FILE"
else
  echo "Environment file not found at $ENV_FILE; using current shell environment and script defaults."
fi

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet is not installed or not in PATH."
  exit 1
fi

CHROME_BIN_PATH="${CHROME_BIN:-}"
if [[ -n "$CHROME_BIN_PATH" && ! -x "$CHROME_BIN_PATH" ]]; then
  echo "CHROME_BIN is set but not executable: $CHROME_BIN_PATH"
  echo "Falling back to auto-detect Chrome/Chromium on this host."
  CHROME_BIN_PATH=""
fi

if [[ -z "$CHROME_BIN_PATH" ]]; then
  for candidate in chrome google-chrome google-chrome-stable chromium chromium-browser; do
    if command -v "$candidate" >/dev/null 2>&1; then
      CHROME_BIN_PATH="$(command -v "$candidate")"
      break
    fi
  done
fi

if [[ -z "$CHROME_BIN_PATH" && -x "/snap/bin/chromium" ]]; then
  CHROME_BIN_PATH="/snap/bin/chromium"
fi

# Snap shim binaries can fail under ChromeDriver execvp; prefer the real Chromium binary path.
if [[ "$CHROME_BIN_PATH" == "/snap/bin/chromium" ]]; then
  for snap_candidate in \
    "/snap/chromium/current/usr/lib/chromium-browser/chrome" \
    "/snap/chromium/current/usr/lib/chromium-browser/chromium-browser"; do
    if [[ -x "$snap_candidate" ]]; then
      CHROME_BIN_PATH="$snap_candidate"
      break
    fi
  done
fi

if [[ -z "$CHROME_BIN_PATH" ]]; then
  echo "Chrome/Chromium binary not found."
  echo "Tried: CHROME_BIN env, chrome, google-chrome, google-chrome-stable, chromium, chromium-browser, /snap/bin/chromium."
  echo "Install one of these, then rerun:"
  echo "  sudo snap install chromium"
  echo "  # or install Google Chrome and set CHROME_BIN=/path/to/chrome"
  exit 1
fi

FILTER="${1:-FullyQualifiedName~Login_ShouldSucceed}"

export E2E_REQUIRED="${E2E_REQUIRED:-true}"
export E2E_BASE_URL="${E2E_BASE_URL:-http://localhost:5001}"
# For direct API container access use :5000 without /api prefix.
export E2E_API_BASE_URL="${E2E_API_BASE_URL:-http://localhost:5000}"
export E2E_HEADLESS="${E2E_HEADLESS:-true}"
export E2E_TIMEOUT_SECONDS="${E2E_TIMEOUT_SECONDS:-30}"
export E2E_DEBUG_HOLD_SECONDS="${E2E_DEBUG_HOLD_SECONDS:-10}"
export CHROME_BIN="$CHROME_BIN_PATH"
export CHROMEDRIVER_LOG="${CHROMEDRIVER_LOG:-./TestResults/e2e/chromedriver.log}"

if [[ "$E2E_HEADLESS" == "false" && -z "${DISPLAY:-}" && -z "${WAYLAND_DISPLAY:-}" ]]; then
  echo "E2E_HEADLESS=false but no DISPLAY or WAYLAND_DISPLAY is available on this host."
  echo "Set E2E_HEADLESS=true for SSH/VM runs, or start a desktop/Xvfb session before running headed Chrome."
  exit 1
fi

mkdir -p "$(dirname "$CHROMEDRIVER_LOG")"

echo "Using CHROME_BIN=$CHROME_BIN"
echo "Using E2E_BASE_URL=$E2E_BASE_URL"
echo "Using E2E_API_BASE_URL=$E2E_API_BASE_URL"
echo "Using E2E_HEADLESS=$E2E_HEADLESS"
echo "Using E2E_DEBUG_HOLD_SECONDS=$E2E_DEBUG_HOLD_SECONDS"
echo "UI requests will go through $E2E_BASE_URL and direct API verification will go through $E2E_API_BASE_URL"
"$CHROME_BIN" --version || true
if command -v chromedriver >/dev/null 2>&1; then
  chromedriver --version || true
elif command -v chromium.chromedriver >/dev/null 2>&1; then
  chromium.chromedriver --version || true
else
  echo "chromedriver not found in PATH; Selenium Manager fallback will be used."
fi

if [[ "$FILTER" == "all" ]]; then
  dotnet test tests/F1.E2E.Tests/F1.E2E.Tests.csproj \
    --configuration Release \
    --nologo \
    --verbosity minimal
else
  dotnet test tests/F1.E2E.Tests/F1.E2E.Tests.csproj \
    --configuration Release \
    --nologo \
    --verbosity minimal \
    --filter "$FILTER"
fi
