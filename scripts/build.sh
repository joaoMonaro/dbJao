#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(cd -- "${SCRIPT_DIR}/.." && pwd)"
DOTNET_CONFIGURATION="${DOTNET_CONFIGURATION:-Debug}"

echo "[BUILD] Projeto Godot C# (${DOTNET_CONFIGURATION})"
dotnet build "${PROJECT_DIR}/dbjao.csproj" \
  --configuration "${DOTNET_CONFIGURATION}" \
  --no-incremental \
  --nologo

echo "[BUILD] Backend e testes (${DOTNET_CONFIGURATION})"
dotnet build "${PROJECT_DIR}/backend/GameBackend.sln" \
  --configuration "${DOTNET_CONFIGURATION}" \
  --no-incremental \
  --nologo
