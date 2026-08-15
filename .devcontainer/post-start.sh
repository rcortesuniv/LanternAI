#!/usr/bin/env bash
# Runs on every codespace start/resume (not just first creation).
# Ensures Ollama is running, the model is available, and dependencies
# are restored — so the environment is ready to go without manual steps.
set -euo pipefail

project_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$project_dir"

# --- Ollama ---
if ! curl -sf http://localhost:11434 >/dev/null 2>&1; then
  echo "==> Starting Ollama server..."
  nohup ollama serve > /tmp/ollama.log 2>&1 &
  for _ in $(seq 1 30); do
    curl -sf http://localhost:11434 >/dev/null 2>&1 && break
    sleep 1
  done
fi

if curl -sf http://localhost:11434 >/dev/null 2>&1; then
  echo "==> Ensuring qwen2.5-coder model is available..."
  ollama pull qwen2.5-coder >/dev/null 2>&1 || true
else
  echo "⚠ Ollama did not become ready; LLM features will be unavailable until manually started." >&2
fi

# --- Backend dependencies (fast no-op if already restored) ---
echo "==> Restoring backend dependencies..."
(cd backend && dotnet restore)

# --- Frontend dependencies (fast no-op if node_modules exists and lockfile unchanged) ---
if [ ! -d frontend/node_modules ] || [ frontend/package-lock.json -nt frontend/node_modules/.package-lock.json ] 2>/dev/null; then
  echo "==> Installing frontend dependencies..."
  (cd frontend && npm install)
fi

# --- Frontend .env ---
[ -f frontend/.env ] || cp frontend/.env.example frontend/.env

echo "==> Lantern AI environment ready."
echo "    Backend:  cd backend && dotnet run --project src/LanternAI.Api --urls http://localhost:5020"
echo "    Frontend: cd frontend && npm run dev -- --host 0.0.0.0"
