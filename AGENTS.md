# AGENTS.md

This repo is the Lantern AI MVP: a .NET 8 backend that turns natural-language questions into a validated query plan and a React/Vite frontend that displays the generated KQL and result table.

## Primary references

- [README.md](README.md) — local setup, Docker, Codespaces, and overall flow.
- [docs/SECURITY.md](docs/SECURITY.md) — security posture, validation model, and production risks.
- [backend/src/LanternAI.Api/Program.cs](backend/src/LanternAI.Api/Program.cs) — DI wiring, CORS, rate limiting, and auth extension point.

## Architecture at a glance

- Backend: ASP.NET Core minimal API under [backend/src/LanternAI.Api](backend/src/LanternAI.Api)
  - `Endpoints/` exposes the HTTP surface.
  - `Services/Catalog/` owns table metadata and schema validation.
  - `Services/Execution/` executes a validated query plan against mock data.
  - `Services/Llm/` is the model-provider seam (`ILlmProvider`); Ollama is used in phase 1.
  - `Services/QueryPlanning/` validates the structured plan and renders KQL for UI display.
- Frontend: React + TypeScript + Vite under [frontend/src](frontend/src)
  - `src/api/` contains client wrappers and shared response types.
  - `src/components/` groups UI by feature, especially chat and catalog panels.

## Development conventions

- Keep the core safety model intact: the LLM produces a structured JSON query plan, not raw KQL text, and the plan is validated before execution.
- Prefer extending the existing interfaces (`IEventTableCatalog`, `IQueryExecutor`, `IQueryPlanService`, `ILlmProvider`) instead of bypassing them.
- Do not weaken schema validation or add direct raw-query execution paths without corresponding updates to the security docs and tests.
- Keep changes small and aligned with the Phase 1 demo scope; avoid introducing production-only assumptions into the MVP.

## Commands

Run these from the repo root unless noted otherwise.

- Backend tests:
  - `cd backend && dotnet test`
- Backend API:
  - `cd backend && dotnet run --project src/LanternAI.Api --urls http://localhost:5020`
- Frontend dev server:
  - `cd frontend && npm install && npm run dev`
- Frontend build:
  - `cd frontend && npm run build`
- Frontend lint:
  - `cd frontend && npm run lint`

## Testing expectations

- Backend tests live under [backend/tests/LanternAI.Api.Tests](backend/tests/LanternAI.Api.Tests).
- Prefer xUnit tests for query planning, validation errors, and execution behavior.
- For any change related to query plans, schema validation, or LLM output handling, add or update test coverage in the corresponding backend test suite.

## Environment notes

- Local development expects Ollama to be running at `http://localhost:11434` by default.
- Use environment variables or configuration overrides rather than hardcoding secrets or local URLs in code.
- CORS is intentionally restricted to configured origins; do not open it to wildcard origins for demo work without a clear security rationale.

## Use this repo-specific guidance

- If a change touches both backend and frontend, keep the API contract and frontend types aligned.
- If a change affects query-generation safety, update the relevant docs and tests together.
- When adding new features, favor the existing pattern of validation-first execution and transparent KQL rendering over shortcut logic.
