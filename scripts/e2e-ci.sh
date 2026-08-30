#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
COMPOSE_FILE="$ROOT_DIR/docker-compose.prod.yml"

export COMPOSE_PROJECT_NAME="${COMPOSE_PROJECT_NAME:-optilifts-e2e-ci}"

POSTGRES_CONTAINER="${POSTGRES_CONTAINER:-${COMPOSE_PROJECT_NAME}-postgres}"
POSTGRES_IMAGE="${POSTGRES_IMAGE:-postgres:15-alpine}"
POSTGRES_USER="${POSTGRES_USER:-postgres}"
POSTGRES_PASSWORD="${POSTGRES_PASSWORD:-test}"
POSTGRES_DB="${POSTGRES_DB:-optilifts_integration_tests_db}"
POSTGRES_HOST_PORT="${POSTGRES_HOST_PORT:-54321}"
JWT_SECRET="${JWT_SECRET:-test_secret_key_for_integration_tests_only}"
JWT_EXP_MINUTES="${JWT_EXP_MINUTES:-60}"
DB_ENCRYPTION_KEY="${DB_ENCRYPTION_KEY:-+8bGaoOpx4CEfxnMcX1RG2qrcJaT+RZO/0IIpSePZQA=}"
AZURE_STORAGE_CONNECTION="${CONNECTIONSTRINGS__AZURESTORAGE:-DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://azurite:10000/devstoreaccount1;QueueEndpoint=http://azurite:10001/devstoreaccount1;TableEndpoint=http://azurite:10002/devstoreaccount1;}"

export AUTH_COOKIE_SECURE="${AUTH_COOKIE_SECURE:-false}"
export DEV_SEEDING="${DEV_SEEDING:-true}"
export E2E_TESTING="true"
export FRONTEND_PORT="${FRONTEND_PORT:-5173}"
export POSTGRES_CONNECTION_STRING="Host=host.docker.internal;Port=${POSTGRES_HOST_PORT};Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"
export JWT_SECRET
export JWT_EXP_MINUTES
export DB_ENCRYPTION_KEY
export CONNECTIONSTRINGS__AZURESTORAGE="$AZURE_STORAGE_CONNECTION"

start_postgres() {
  docker rm -f "$POSTGRES_CONTAINER" >/dev/null 2>&1 || true
  docker run -d \
    --name "$POSTGRES_CONTAINER" \
    -e POSTGRES_USER="$POSTGRES_USER" \
    -e POSTGRES_PASSWORD="$POSTGRES_PASSWORD" \
    -e POSTGRES_DB="$POSTGRES_DB" \
    -p "${POSTGRES_HOST_PORT}:5432" \
    "$POSTGRES_IMAGE" >/dev/null
}

wait_postgres() {
  until docker exec "$POSTGRES_CONTAINER" pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DB" >/dev/null 2>&1; do
    printf '.'
    sleep 1
  done
  printf '\n'
}

start_app_stack() {
  cd "$ROOT_DIR"
  docker compose -f "$COMPOSE_FILE" up -d --no-deps redis azurite ai-api
  docker compose -f "$COMPOSE_FILE" up -d --no-deps core-api frontend
}

wait_app_stack() {
  curl --fail --retry 60 --retry-connrefused --retry-all-errors --retry-delay 1 http://localhost:5036/api/healthCheck
  curl --fail --retry 60 --retry-connrefused --retry-all-errors --retry-delay 1 http://localhost:5173/api/healthCheck
}

run_e2e_tests() {
  cd "$ROOT_DIR"
  if [[ "${PLAYWRIGHT_PROJECTS:-all}" = "chromium" ]]; then
    E2E_USE_EXISTING_SERVICES=1 pnpm exec playwright test --config=e2e/playwright.config.ts --project=chromium
  else
    E2E_USE_EXISTING_SERVICES=1 pnpm test:e2e
  fi
}

teardown() {
  cd "$ROOT_DIR"
  docker compose -f "$COMPOSE_FILE" down -v --remove-orphans >/dev/null 2>&1 || true
  docker rm -f "$POSTGRES_CONTAINER" >/dev/null 2>&1 || true
}

case "${1:-ci}" in
  start)
    start_postgres
    wait_postgres
    start_app_stack
    ;;
  wait)
    wait_app_stack
    ;;
  test)
    run_e2e_tests
    ;;
  down)
    teardown
    ;;
  ci)
    trap teardown EXIT
    teardown
    start_postgres
    wait_postgres
    start_app_stack
    wait_app_stack
    run_e2e_tests
    ;;
  *)
    echo "Usage: $0 {start|wait|test|down|ci}" >&2
    exit 1
    ;;
esac