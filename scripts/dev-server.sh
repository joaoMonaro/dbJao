#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(cd -- "${SCRIPT_DIR}/.." && pwd)"
ENV_FILE="${PROJECT_DIR}/backend/.env"
DEFAULT_GODOT_BIN="/home/jao/Downloads/Godot_v4.7.1-stable_mono_linux_x86_64/Godot_v4.7.1-stable_mono_linux.x86_64"

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
  GODOT_EXECUTABLE="${GODOT_BIN}"
elif command -v godot >/dev/null 2>&1; then
  GODOT_EXECUTABLE="$(command -v godot)"
elif [[ -x "${DEFAULT_GODOT_BIN}" ]]; then
  GODOT_EXECUTABLE="${DEFAULT_GODOT_BIN}"
else
  echo "Erro: Godot não encontrado." >&2
  echo "Defina GODOT_BIN com o caminho do Godot 4.7.1 Mono." >&2
  exit 1
fi

exec "${GODOT_EXECUTABLE}" --headless --path "${PROJECT_DIR}" -- --server
