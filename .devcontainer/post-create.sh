#!/usr/bin/env bash
# Runs once when the Codespace/devcontainer is first created. Sets up
# everything needed to run Lantern AI without further manual install steps.
set -euo pipefail

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

Set Ollama__ApiKey to your Ollama Cloud token before starting the backend.
EOF
