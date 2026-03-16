#!/usr/bin/env bash
set -euo pipefail

show_help() {
  cat <<'HELP'
Usage:
  ./scripts/e2e-auth-preflight.sh [--env-file path] [--path /users/me] [--url https://host/path]

Purpose:
  Validate Cloudflare Access service-token auth without running Selenium.

Environment variables used:
  E2E_BASE_URL          Optional if --url is provided (for example: https://f1-vps-tunnel.cloudflareaccess.com)
  E2E_CF_CLIENT_ID      Required
  E2E_CF_CLIENT_SECRET  Required

Options:
  --env-file <path>     Load variables from file (default: .env if present)
  --path <endpoint>     Endpoint path to call (default: /users/me)
  --url <full-url>      Full URL to call (overrides E2E_BASE_URL + --path)
  -h, --help            Show this help

Notes:
  - This script never prints secret values.
  - It writes debug artifacts under TestResults/e2e/preflight/.
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

    if [[ ( "$value" == \"*\" && "$value" == *\" ) || ( "$value" == \'*\' && "$value" == *\' ) ]]; then
      value="${value:1:${#value}-2}"
    fi

    export "$key=$value"
  done < "$dotenv_path"
}

env_file=".env"
endpoint_path="/users/me"
full_url=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --env-file)
      env_file="${2:-}"
      shift 2
      ;;
    --path)
      endpoint_path="${2:-}"
      shift 2
      ;;
    --url)
      full_url="${2:-}"
      shift 2
      ;;
    -h|--help)
      show_help
      exit 0
      ;;
    *)
      echo "Unknown argument: $1"
      show_help
      exit 1
      ;;
  esac
done

if [[ -n "$env_file" && -f "$env_file" ]]; then
  load_dotenv_file "$env_file"
  echo "Loaded environment from $env_file"
fi

missing=0
for var_name in E2E_CF_CLIENT_ID E2E_CF_CLIENT_SECRET; do
  if [[ -z "${!var_name:-}" ]]; then
    echo "Missing required env var: $var_name"
    missing=1
  fi
done

if [[ -z "$full_url" && -z "${E2E_BASE_URL:-}" ]]; then
  echo "Missing required input: provide --url or set E2E_BASE_URL"
  missing=1
fi

if [[ "$missing" -ne 0 ]]; then
  echo "Set the required variables and retry."
  exit 1
fi

if [[ "$endpoint_path" != /* ]]; then
  endpoint_path="/$endpoint_path"
fi

if [[ -n "$full_url" ]]; then
  request_url="$full_url"
else
  base_url="${E2E_BASE_URL%/}"
  request_url="${base_url}${endpoint_path}"
fi
out_dir="TestResults/e2e/preflight"
mkdir -p "$out_dir"

stamp="$(date -u +%Y%m%d%H%M%S)"
headers_file="$out_dir/headers-${stamp}.txt"
body_file="$out_dir/body-${stamp}.txt"

http_code="$({
  curl -sS \
    -D "$headers_file" \
    -o "$body_file" \
    -w '%{http_code}' \
    -H "CF-Access-Client-Id: $E2E_CF_CLIENT_ID" \
    -H "CF-Access-Client-Secret: $E2E_CF_CLIENT_SECRET" \
    "$request_url"
} || true)"

if [[ -z "$http_code" ]]; then
  echo "Request failed before receiving an HTTP response."
  echo "Artifacts: $headers_file, $body_file"
  exit 2
fi

status_line="$(head -n 1 "$headers_file" || true)"
content_type="$(grep -i '^content-type:' "$headers_file" | tail -n 1 | sed 's/^[Cc]ontent-[Tt]ype:[[:space:]]*//;s/\r$//' || true)"
location_header="$(grep -i '^location:' "$headers_file" | tail -n 1 | sed 's/^[Ll]ocation:[[:space:]]*//;s/\r$//' || true)"

is_cf_login=0
if grep -qiE 'Cloudflare Access|Get a login code emailed to you|Send me a code' "$body_file"; then
  is_cf_login=1
fi

if [[ -n "$location_header" ]] && echo "$location_header" | grep -qiE 'cdn-cgi/access/login|cloudflareaccess\.com'; then
  is_cf_login=1
fi

echo "Preflight URL: $request_url"
echo "HTTP: ${http_code} (${status_line})"
if [[ -n "$content_type" ]]; then
  echo "Content-Type: $content_type"
fi
if [[ -n "$location_header" ]]; then
  echo "Location: $location_header"
fi
echo "Artifacts: $headers_file, $body_file"

if [[ "$is_cf_login" -eq 1 ]]; then
  echo "Result: FAILED - Cloudflare login page was returned."
  echo "Check Access policy include rules for your service token and app hostname."
  exit 3
fi

if [[ "$http_code" -ge 200 && "$http_code" -lt 300 ]]; then
  echo "Result: PASS - service token request bypassed interactive login."
  exit 0
fi

if [[ "$http_code" -ge 300 && "$http_code" -lt 400 ]]; then
  echo "Result: FAILED - received redirect status $http_code. Treating redirects as non-pass to avoid false positives."
  exit 5
fi

echo "Result: FAILED - received non-success status $http_code."
exit 4
