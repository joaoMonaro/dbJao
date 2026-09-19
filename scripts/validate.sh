#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(cd -- "${SCRIPT_DIR}/.." && pwd)"

find_godot() {
  if [[ -n "${GODOT_BIN:-}" ]]; then
    if [[ ! -x "${GODOT_BIN}" ]]; then
      echo "[FAIL] GODOT_BIN não aponta para um executável: ${GODOT_BIN}" >&2
      return 2
    fi
    printf '%s\n' "${GODOT_BIN}"
    return 0
  fi

  if command -v godot >/dev/null 2>&1; then
    command -v godot
    return 0
  fi

  if command -v godot4 >/dev/null 2>&1; then
    command -v godot4
    return 0
  fi

  return 1
}

"${SCRIPT_DIR}/build.sh"
"${SCRIPT_DIR}/test.sh"

if GODOT_EXECUTABLE="$(find_godot)"; then
  PROJECT_FILE_CONTENTS="$(<"${PROJECT_DIR}/dbjao.csproj")"
  if [[ ! "${PROJECT_FILE_CONTENTS}" =~ Godot.NET.Sdk/([0-9]+\.[0-9]+\.[0-9]+) ]]; then
    echo "[FAIL] Não foi possível identificar a versão Godot em dbjao.csproj." >&2
    exit 1
  fi
  EXPECTED_GODOT_VERSION="${BASH_REMATCH[1]}"

  GODOT_VERSION_OUTPUT="$("${GODOT_EXECUTABLE}" --version)"
  if [[ ! "${GODOT_VERSION_OUTPUT}" =~ ^([0-9]+\.[0-9]+\.[0-9]+) ]]; then
    echo "[FAIL] Não foi possível identificar a versão do executável Godot." >&2
    exit 1
  fi
  ACTUAL_GODOT_VERSION="${BASH_REMATCH[1]}"

  if [[ "${ACTUAL_GODOT_VERSION}" != "${EXPECTED_GODOT_VERSION}" ]]; then
    echo "[GODOT] Importação headless de assets e smoke da cena principal"
    echo "[SKIP] Godot ${ACTUAL_GODOT_VERSION} difere do SDK ${EXPECTED_GODOT_VERSION};" \
      "o editor poderia reescrever dbjao.csproj. Use a versão exata ou atualize o projeto separadamente."
  else
    GODOT_LOG="$(mktemp -t dbjao-godot-smoke.XXXXXX.log)"
    trap 'rm -f -- "${GODOT_LOG}"' EXIT

    echo "[GODOT] Importação headless de assets"
    if ! "${GODOT_EXECUTABLE}" \
      --headless \
      --editor \
      --path "${PROJECT_DIR}" \
      --import \
      --quit 2>&1 | tee "${GODOT_LOG}"; then
      echo "[FAIL] A importação do Godot encerrou com código de erro." >&2
      exit 1
    fi

    if rg -n 'ERROR:|SCRIPT ERROR:|Unhandled exception|Failed to load' "${GODOT_LOG}"; then
      echo "[FAIL] A importação do Godot registrou erro grave." >&2
      exit 1
    fi

    : > "${GODOT_LOG}"
    echo "[GODOT] Smoke headless da cena principal"
    if ! "${GODOT_EXECUTABLE}" \
      --headless \
      --path "${PROJECT_DIR}" \
      --quit-after 2 2>&1 | tee "${GODOT_LOG}"; then
      echo "[FAIL] Godot encerrou com código de erro." >&2
      exit 1
    fi

    if rg -n 'ERROR:|SCRIPT ERROR:|Unhandled exception|Failed to load' "${GODOT_LOG}"; then
      echo "[FAIL] O smoke Godot registrou erro grave." >&2
      exit 1
    fi

    echo "[PASS] Cena principal iniciou em Godot headless."

    : > "${GODOT_LOG}"
    echo "[GODOT] Teste de integração de XP do Sidra"
    if ! "${GODOT_EXECUTABLE}" \
      --headless \
      --path "${PROJECT_DIR}" \
      --quit-after 10 \
      res://tests/godot/SidraXpIntegrationTest.tscn \
      -- \
      --server 2>&1 | tee "${GODOT_LOG}"; then
      echo "[FAIL] Teste de integração do Sidra encerrou com código de erro." >&2
      exit 1
    fi

    if rg -n 'ERROR:|SCRIPT ERROR:|Unhandled exception|Failed to load' "${GODOT_LOG}"; then
      echo "[FAIL] Teste de integração do Sidra registrou erro grave." >&2
      exit 1
    fi

    if ! rg -F '[PASS] Integração Sidra -> killer -> progressão validada.' "${GODOT_LOG}"; then
      echo "[FAIL] Teste de integração do Sidra não confirmou o fluxo esperado." >&2
      exit 1
    fi

    : > "${GODOT_LOG}"
    echo "[GODOT] Teste dos comandos de debug de XP"
    if ! "${GODOT_EXECUTABLE}" \
      --headless \
      --path "${PROJECT_DIR}" \
      --quit-after 10 \
      res://tests/godot/DebugXpCommandsIntegrationTest.tscn \
      -- \
      --server 2>&1 | tee "${GODOT_LOG}"; then
      echo "[FAIL] Teste dos comandos de debug de XP encerrou com código de erro." >&2
      exit 1
    fi

    if rg -n 'ERROR:|SCRIPT ERROR:|Unhandled exception|Failed to load' "${GODOT_LOG}"; then
      echo "[FAIL] Teste dos comandos de debug de XP registrou erro grave." >&2
      exit 1
    fi

    if ! rg -F '[PASS] Comandos de debug de XP validados.' "${GODOT_LOG}"; then
      echo "[FAIL] Teste dos comandos de debug de XP não confirmou o fluxo esperado." >&2
      exit 1
    fi

    : > "${GODOT_LOG}"
    echo "[GODOT] Teste do HUD e modal de perfil"
    if ! "${GODOT_EXECUTABLE}" \
      --headless \
      --path "${PROJECT_DIR}" \
      --quit-after 10 \
      res://tests/godot/HudProfileIntegrationTest.tscn \
      2>&1 | tee "${GODOT_LOG}"; then
      echo "[FAIL] Teste do HUD e perfil encerrou com código de erro." >&2
      exit 1
    fi

    if rg -n 'ERROR:|SCRIPT ERROR:|Unhandled exception|Failed to load' "${GODOT_LOG}"; then
      echo "[FAIL] Teste do HUD e perfil registrou erro grave." >&2
      exit 1
    fi

    if ! rg -F '[PASS] HUD compacto e modal de perfil validados.' "${GODOT_LOG}"; then
      echo "[FAIL] Teste do HUD e perfil não confirmou o fluxo esperado." >&2
      exit 1
    fi
  fi
else
  GODOT_STATUS=$?
  if [[ "${GODOT_STATUS}" -eq 2 ]]; then
    exit 1
  fi
  echo "[GODOT] Importação headless de assets e smoke da cena principal"
  echo "[SKIP] Godot Mono não encontrado; instale godot/godot4 ou defina GODOT_BIN."
fi

echo "[DIFF] Verificação de whitespace"
git -C "${PROJECT_DIR}" diff --check
git -C "${PROJECT_DIR}" diff --cached --check

echo "[PASS] Validações automatizadas concluídas."
echo "[REVIEW] Execute git status --short e git diff antes de concluir a tarefa."
