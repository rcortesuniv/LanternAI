# Lantern AI

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://codespaces.new/rcortesuniv/LanternAI)

Lantern AI lets Azure Data Explorer users ask questions about their event
data in plain English instead of hand-writing KQL. This is a **Phase 1
MVP / demo**: it does not talk to a real ADX cluster yet. Instead, a small
set of realistic **simulated event tables** stand in for the `cda-db`
database, so the full "ask a question → see the query → see the results"
loop can be proven out end-to-end before any real data connection is wired
up.

## How it works

1. You ask a question in the chat UI (e.g. *"how many failed signins in the
   last 24 hours?"*).
2. The backend asks a local LLM (via [Ollama](https://ollama.com)) to turn
   that into a small **structured query plan** — not raw KQL text — scoped
   to the known table/column names.
3. The plan is validated against the table catalog (unknown tables/columns
   are rejected) and then executed against the in-memory mock data.
4. The UI shows both the **generated KQL** (for transparency — this is what
   would run against real ADX) and the **results table**.

See [docs/SECURITY.md](docs/SECURITY.md) for why we execute a validated
structured plan rather than raw LLM-generated query text, and what's
deferred until this moves past demo stage.

## Project layout

```
backend/        ASP.NET Core 8 Web API (LanternAI.Api) + xUnit tests
frontend/       React + TypeScript (Vite) chat UI
docs/           Security & architecture notes
.devcontainer/  GitHub Codespaces / VS Code Dev Containers setup
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/)
- An [Ollama Cloud](https://ollama.com) API key with access to the configured
  `qwen3-coder:480b` model.

## Running locally (without Docker)

```bash
# Terminal 1 — backend (defaults to http://localhost:5020)
read -s OLLAMA_API_KEY
export Ollama__ApiKey="$OLLAMA_API_KEY"
unset OLLAMA_API_KEY
cd backend
dotnet run --project src/LanternAI.Api --urls http://localhost:5020

# Terminal 2 — frontend (defaults to http://localhost:5173)
cd frontend
cp .env.example .env   # VITE_API_BASE_URL=http://localhost:5020
npm install
npm run dev
```

Then open http://localhost:5173. The backend uses Ollama Cloud by default. Set
`Ollama__BaseUrl`, `Ollama__Model`, and `Ollama__ApiKey` through environment
variables for a different Cloud model or endpoint. Never commit the API key.

## Running with Docker Compose

```bash
docker compose up
# provide the Cloud token to Compose; it is not stored in the repository:
export OLLAMA_API_KEY="<rotated-token>"
docker compose up
```

This starts the backend API (`:5020`) and a Vite dev server for the frontend
(`:5173`) with hot reload. The backend calls Ollama Cloud directly.

## Running in GitHub Codespaces / a Dev Container

The repo includes a `.devcontainer` config, so **Create codespace on main**
(green **Code** button → **Codespaces** tab, or the badge at the top of this
file) gives you a fully set-up environment with no manual install steps:

- .NET 8 SDK and Node.js 20 preinstalled in the container image.
- Ports `5020` (API) and `5173` (web) are pre-labeled for auto-forwarding.
- Docker is available inside the container too (`docker-in-docker` feature),
  so `docker compose up` also works here if you'd rather use that path
  instead of the steps below.
- No `.env` editing needed: the frontend detects Codespaces' forwarded-port
  hostname (`https://<codespace>-5173.app.github.dev`) and derives the
  backend's forwarded URL automatically, and the backend's CORS policy
  already allows `*.app.github.dev` / `*.github.dev` origins.

Once the Codespace finishes setting up, provide `Ollama__ApiKey` in the backend
terminal, then start the services:

```bash
read -s OLLAMA_API_KEY
export Ollama__ApiKey="$OLLAMA_API_KEY"
unset OLLAMA_API_KEY
cd backend && dotnet run --project src/LanternAI.Api --urls http://localhost:5020

# Terminal 2
cd frontend && npm run dev -- --host 0.0.0.0
```

Then open the **Ports** tab (bottom panel) and click the globe icon next to
`5173` to view the app in your browser.

The same `.devcontainer` config also works with VS Code's **Dev Containers**
extension for a local containerized environment, if you'd rather not use
Codespaces at all.

## Tests

```bash
cd backend
dotnet test
```

Covers query-plan validation (rejecting unknown tables/columns, malformed
LLM output) and the simulated query executor (filtering, aggregation, time
ranges, limits).

## Configuration reference

| Variable                            | Default                     | Purpose                                                                 |
|-------------------------------------|------------------------------|--------------------------------------------------------------------------|
| `Ollama__BaseUrl`                   | `https://ollama.com`        | Ollama Cloud server address                                               |
| `Ollama__Model`                     | `qwen3-coder:480b`          | Model used for NL → query-plan generation                                |
| `Ollama__ApiKey`                    | *(required)*                | Ollama Cloud bearer token; keep it out of source control                  |
| `Cors__AllowedOrigins__0`           | `http://localhost:5173`     | Frontend origin allowed to call the API (exact match)                    |
| `Cors__AllowedOriginSuffixes__0`    | `.app.github.dev`           | Additional HTTPS origin suffixes allowed (Codespaces forwarded ports)    |
| `VITE_API_BASE_URL` (frontend)      | auto-detected, else `http://localhost:5020` | Backend base URL the UI calls — only needed if auto-detection doesn't apply to your setup |

.NET configuration keys are overridden via double-underscore environment
variables (e.g. `Ollama__BaseUrl`) rather than editing `appsettings.json`
per environment.

## Roadmap beyond this phase

- Swap the simulated executor for the real Kusto SDK against `cda-db`
  (the validated `QueryPlan` → KQL path is already designed for this swap;
  see `docs/SECURITY.md`).
- Add a `GeminiLlmProvider` implementation (the `ILlmProvider` seam already
  exists — see `backend/src/LanternAI.Api/Services/Llm/GeminiLlmProvider.cs`).
- Add real authentication (Entra ID) before any shared/production
  deployment — see the "Auth extension point" note in
  `backend/src/LanternAI.Api/Program.cs` and `docs/SECURITY.md`.
- Table-scoped querying (pick specific tables rather than always querying
  across all of them).
