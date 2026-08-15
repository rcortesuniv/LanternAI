# Production integration contracts

Lantern currently runs safely against simulated data and local Ollama. The
following configuration contracts expose the next production seams without
claiming that external access is enabled:

## Entra ID

Set `Authentication:Enabled=true`, `Authentication:Authority`, and
`Authentication:ClientId` (or matching environment variables) to enable JWT
validation. `/api/query` and `/api/tables` then require the `Lantern.User`
role, while `/api/audit/recent` requires `Lantern.Admin`. Every authenticated
request must carry the configured tenant claim (default `tid`) and a subject
claim. Table permissions should be derived from authenticated claims, not from
client-supplied table names.

When enabled, local anonymous requests are intentionally rejected. Leave it
disabled only for local single-user development.

## Azure Data Explorer

Set `Adx:ClusterUri` and `Adx:Database` to advertise an ADX-backed source. The
production executor should implement `IQueryExecutor` with the Azure.Identity
managed-identity credential and Kusto SDK, while retaining the existing typed
`QueryPlan` validation boundary. Do not accept raw KQL from the browser or the
model.

## Data-platform diagnostics

`POST /api/query` returns `diagnostics` with a cache-hit flag, cache-key
version, estimated rows scanned, work units, and a low/medium/high cost tier.
The current estimator is fixture-based; replace it with source statistics when
ADX is enabled.

Results are cached for five minutes using a normalized question and tenant
scope. The cache stores validated response data only and never executes cached
raw KQL. Replace the process-local cache with a distributed cache when running
multiple API instances.

The source capability contract reports whether a provider supports joins,
aggregations, and caching. The simulated source supports aggregation and
caching but not joins. An ADX provider should implement joins only after the
structured plan validator has explicit join-key and table authorization rules.

## Capability discovery

`GET /api/capabilities` reports whether authentication and ADX configuration are
present, the active language-model provider, and the catalog source count. It is
intended for operator diagnostics, not authorization.

## Audit events

Query completion events are retained in a bounded in-memory store for the demo,
including correlation ID, tenant, subject, row count, and duration. Before
production, replace `IAuditStore` with durable append-only storage and apply a
retention policy; question text should be treated as sensitive data.