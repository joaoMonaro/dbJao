#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(cd -- "${SCRIPT_DIR}/.." && pwd)"
DOTNET_CONFIGURATION="${DOTNET_CONFIGURATION:-Debug}"

echo "[TEST] Testes automatizados do backend (${DOTNET_CONFIGURATION})"
dotnet test "${PROJECT_DIR}/backend/GameBackend.sln" \
  --configuration "${DOTNET_CONFIGURATION}" \
  --nologo
