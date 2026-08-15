# Production integration contracts

Lantern currently runs safely against simulated data and local Ollama. The
following configuration contracts expose the next production seams without
claiming that external access is enabled:

## Entra ID

Set `Authentication:Authority` and `Authentication:ClientId` (or the matching
environment variables) to turn the capability report on. The authentication
middleware must be added before exposing the API to shared users, followed by
`RequireAuthorization()` on `/api/query`, `/api/tables`, and
`/api/capabilities`. Table permissions should be derived from the authenticated
claims, not from client-supplied table names.

## Azure Data Explorer

Set `Adx:ClusterUri` and `Adx:Database` to advertise an ADX-backed source. The
production executor should implement `IQueryExecutor` with the Azure.Identity
managed-identity credential and Kusto SDK, while retaining the existing typed
`QueryPlan` validation boundary. Do not accept raw KQL from the browser or the
model.

## Capability discovery

`GET /api/capabilities` reports whether authentication and ADX configuration are
present, the active language-model provider, and the catalog source count. It is
intended for operator diagnostics, not authorization.