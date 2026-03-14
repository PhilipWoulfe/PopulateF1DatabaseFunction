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
HELP
}

if [[ "${1:-}" == "-h" || "${1:-}" == "--help" ]]; then
  show_help
  exit 0
fi

if [[ ! -f "F1Competition.sln" ]]; then
  echo "Run this script from the repository root (F1Competition)."
  exit 1
fi

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet is not installed or not in PATH."
  exit 1
fi

if ! command -v chromedriver >/dev/null 2>&1; then
  echo "chromedriver is not installed or not in PATH."
  exit 1
fi

CHROME_BIN_PATH=""
if command -v chrome >/dev/null 2>&1; then
  CHROME_BIN_PATH="$(command -v chrome)"
elif command -v google-chrome >/dev/null 2>&1; then
  CHROME_BIN_PATH="$(command -v google-chrome)"
elif command -v google-chrome-stable >/dev/null 2>&1; then
  CHROME_BIN_PATH="$(command -v google-chrome-stable)"
fi

if [[ -z "$CHROME_BIN_PATH" ]]; then
  echo "Chrome binary not found in PATH (tried: chrome, google-chrome, google-chrome-stable)."
  exit 1
fi

FILTER="${1:-FullyQualifiedName~Login_ShouldSucceed}"

export E2E_REQUIRED="${E2E_REQUIRED:-true}"
export E2E_BASE_URL="${E2E_BASE_URL:-http://localhost:5001}"
# For direct API container access use :5000 without /api prefix.
export E2E_API_BASE_URL="${E2E_API_BASE_URL:-http://localhost:5000}"
export E2E_HEADLESS="${E2E_HEADLESS:-true}"
export E2E_TIMEOUT_SECONDS="${E2E_TIMEOUT_SECONDS:-30}"
export CHROME_BIN="$CHROME_BIN_PATH"
export CHROMEDRIVER_LOG="${CHROMEDRIVER_LOG:-./TestResults/e2e/chromedriver.log}"

mkdir -p "$(dirname "$CHROMEDRIVER_LOG")"

echo "Using CHROME_BIN=$CHROME_BIN"
echo "Using E2E_BASE_URL=$E2E_BASE_URL"
echo "Using E2E_API_BASE_URL=$E2E_API_BASE_URL"
echo "Using E2E_HEADLESS=$E2E_HEADLESS"
"$CHROME_BIN" --version || true
chromedriver --version || true

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
