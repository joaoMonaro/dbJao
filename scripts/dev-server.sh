#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(cd -- "${SCRIPT_DIR}/.." && pwd)"
ENV_FILE="${PROJECT_DIR}/backend/.env"

if [[ ! -f "${ENV_FILE}" ]]; then
  echo "Erro: ${ENV_FILE} não encontrado." >&2
  echo "Crie-o a partir de backend/.env.example antes de iniciar o servidor." >&2
  exit 1
fi

set -a
# shellcheck disable=SC1090
source "${ENV_FILE}"
set +a

export GAME_API_URL="${GAME_API_URL:-http://127.0.0.1:5000}"
export GAME_SERVER_ID="${GAME_SERVER_ID:-local-server-01}"
export GAME_AUTH_TIMEOUT_SECONDS="${GAME_AUTH_TIMEOUT_SECONDS:-10}"
export GAME_API_TIMEOUT_SECONDS="${GAME_API_TIMEOUT_SECONDS:-5}"

if [[ -z "${GAME_SERVER_API_KEY:-}" ]]; then
  echo "Erro: GAME_SERVER_API_KEY não está configurada em ${ENV_FILE}." >&2
  exit 1
fi

if [[ -n "${GODOT_BIN:-}" ]]; then
  if [[ ! -x "${GODOT_BIN}" ]]; then
    echo "Erro: GODOT_BIN não aponta para um executável: ${GODOT_BIN}" >&2
    exit 1
  fi
  GODOT_EXECUTABLE="${GODOT_BIN}"
elif command -v godot >/dev/null 2>&1; then
  GODOT_EXECUTABLE="$(command -v godot)"
elif command -v godot4 >/dev/null 2>&1; then
  GODOT_EXECUTABLE="$(command -v godot4)"
else
  echo "Erro: Godot não encontrado." >&2
  echo "Defina GODOT_BIN com o caminho do Godot 4.7.2 Mono." >&2
  exit 1
fi

exec "${GODOT_EXECUTABLE}" --headless --path "${PROJECT_DIR}" -- --server
