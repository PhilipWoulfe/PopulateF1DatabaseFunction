#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="${1:-$ROOT_DIR/.env}"

if [[ ! -f "$ENV_FILE" ]]; then
    echo "ERROR: env file not found: $ENV_FILE" >&2
    exit 1
fi

read_env() {
    local key="$1"
    awk -F= -v search_key="$key" '
        $0 ~ /^[[:space:]]*#/ { next }
        $1 == search_key {
            sub(/^[^=]*=/, "", $0)
            print $0
            exit
        }
    ' "$ENV_FILE"
}

HOST_PORT="$(read_env HOST_PORT)"
HOST_PORT_WEB="$(read_env HOST_PORT_WEB)"
HOST_PORT="${HOST_PORT:-5000}"
HOST_PORT_WEB="${HOST_PORT_WEB:-5001}"

echo "Checking compose service state..."
docker compose --env-file "$ENV_FILE" ps

echo "Checking API health endpoint on port $HOST_PORT..."
curl --fail --silent --show-error "http://127.0.0.1:${HOST_PORT}/health" >/dev/null

echo "Checking web root on port $HOST_PORT_WEB..."
curl --fail --silent --show-error --head "http://127.0.0.1:${HOST_PORT_WEB}/" >/dev/null

echo "Checking worker state..."
worker_status="$(docker compose --env-file "$ENV_FILE" ps --status running --services 2>/dev/null | grep -x 'f1-data-sync-worker' || true)"
if [[ -n "$worker_status" ]]; then
    echo "- worker is running"
else
    worker_container_id="$(docker compose --env-file "$ENV_FILE" ps -q f1-data-sync-worker)"
    if [[ -n "$worker_container_id" ]]; then
        worker_exit_code="$(docker inspect --format '{{.State.ExitCode}}' "$worker_container_id")"
        if [[ "$worker_exit_code" == "0" ]]; then
            echo "- worker exited cleanly (one-shot mode or completed run)"
        else
            echo "ERROR: worker is not running and last exit code was $worker_exit_code" >&2
            exit 1
        fi
    else
        echo "ERROR: worker container not found in compose project" >&2
        exit 1
    fi
fi

echo "Smoke checks passed."
