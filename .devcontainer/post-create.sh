#!/usr/bin/env bash
# Runs once when the Codespace/devcontainer is first created. Sets up
# everything needed to run Lantern AI without further manual install steps.
set -euo pipefail

echo "==> Installing Ollama..."
curl -fsSL https://ollama.com/install.sh | sh

echo "==> Starting Ollama server..."
nohup ollama serve > /tmp/ollama.log 2>&1 &
for _ in $(seq 1 30); do
  curl -sf http://localhost:11434 > /dev/null 2>&1 && break
  sleep 1
done

echo "==> Pulling qwen2.5-coder model (first time only; can take a few minutes)..."
ollama pull qwen2.5-coder:1.5b

echo "==> Restoring backend dependencies..."
(cd backend && dotnet restore)

echo "==> Installing frontend dependencies..."
(cd frontend && npm install)
[ -f frontend/.env ] || cp frontend/.env.example frontend/.env

cat <<'EOF'

==> Setup complete. To run Lantern AI:

    # Terminal 1
    cd backend && dotnet run --project src/LanternAI.Api --urls http://localhost:5020

    # Terminal 2
    cd frontend && npm run dev -- --host 0.0.0.0

Then open the forwarded port 5173 from the "Ports" tab.
EOF
