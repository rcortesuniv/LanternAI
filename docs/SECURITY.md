# Security & accessibility posture

This is a Phase 1 demo. This document is the honest account of what's
covered now, what's deliberately deferred, and what must be done before any
shared or production deployment — written for whoever reviews this MVP for
a production go-ahead.

## Why we never execute LLM-generated query text

The LLM is prompted to return a small **structured JSON query plan**
(`table`, `filters`, `columns`, `aggregation`, `timeRange`) — never a raw
KQL string that gets parsed or run. `QueryPlanService` validates every
table and column name in that plan against the real catalog schema before
anything executes; an unknown or hallucinated name is rejected with a 400,
not silently coerced or run. The KQL text shown in the UI is *rendered from
the validated plan*, purely for transparency — it is not itself executed.
This is the injection/hallucination guard, and it's also what makes the
Phase 2 swap to real ADX low-risk: the executor changes, the validation and
prompting layers don't.

## Covered now

- **Input validation**: empty/oversized questions rejected before any LLM
  call (`QueryEndpoints`, 400 with a specific message).
- **Schema-validated query plans**: see above.
- **CORS locked down**: only the configured frontend origin(s)
  (`Cors:AllowedOrigins`) may call the API — no wildcard.
- **Rate limiting**: `/api/query` (the one endpoint that triggers LLM
  inference) is capped via a fixed-window limiter (`RateLimiting.QueryPolicy`
  in `Program.cs`) with a bounded queue and `Retry-After` guidance to blunt
  trivial abuse/cost runaway.
- **Operational hardening**: `/health/live` and `/health/ready` probes are
  available for orchestration; readiness checks the configured Ollama service.
  Requests receive a correlation ID, secure response headers, a 16 KB payload
  limit, and structured query-duration/row-count logs without logging question
  content.
- **No secrets in source**: Ollama base URL/model come from configuration,
  overridable via environment variables; nothing is hardcoded.
- **Clean error responses**: `ApiExceptionHandler` maps known failure modes
  to specific `ProblemDetails` (400 for bad plans, 503 for an unreachable
  LLM) and logs everything else server-side without leaking stack traces or
  internals to the client.
- **No PII/query content at elevated log levels**: only warnings/errors are
  logged with exception detail; normal request logging stays at the
  framework default.
- **Accessibility (WCAG 2.1 AA target)**: semantic landmarks
  (`header`/`main`/`aside`), labeled form controls, an `aria-live` region
  announcing async query status to screen readers, native `<details>`
  disclosure widgets (keyboard-operable by default) for the table catalog,
  visible `:focus-visible` styling, no color-only signaling of errors (icon
  + text + `role="alert"`), and a light/dark palette checked for AA text
  contrast.

## Deliberately deferred (Phase 1 demo scope)

These are not oversights — they're out of scope for proving the NL→query
concept and are called out here so they aren't mistaken for "done":

- **No authentication.** Single-user local demo. An extension point is
  explicitly marked in `backend/src/LanternAI.Api/Program.cs` for adding
  Entra ID (Azure AD) via `Microsoft.Identity.Web` — the standard fit given
  ADX/Azure AD are already in play. **This must land before any deployment
  reachable by more than one trusted local user.**
- **No real ADX connectivity.** All data is in-memory mock fixtures
  (`Data/MockEventData.cs`). No credentials, no real cda-db access, no
  risk of the LLM's plan touching real data yet.
- **No Gemini integration.** `GeminiLlmProvider` is a documented stub only.
- **No table-level authorization.** Once real ADX/cda-db is wired in,
  per-table/row-level access control (whatever the source database already
  enforces, plus any app-level scoping) needs explicit design — not assumed
  from this codebase.
- **No persistent storage / audit log** of questions or generated queries.
  Worth adding before production, both for support/debugging and for
  demonstrating query provenance.

## Before production (checklist for the next phase)

- [ ] Entra ID authentication + authorization on all API endpoints.
- [ ] Real ADX/Kusto SDK executor behind `IQueryExecutor`, using
      Azure-native auth (managed identity) rather than static credentials.
- [ ] Threat-model the swap from mock to real data specifically for prompt
      injection via table/column *descriptions* if those ever become
      user-editable (they're currently static and developer-controlled).
- [ ] Centralized secret management (Key Vault) for any provider API keys
      (Gemini) instead of environment variables.
- [ ] Structured audit logging of questions asked and queries run.
- [ ] Automated accessibility testing (axe) in CI, not just manual checks.
- [ ] Load/rate-limit tuning based on real usage, not the current
      placeholder limiter values.
- [ ] Export correlation/audit events to a centralized sink with retention and
  access controls.
