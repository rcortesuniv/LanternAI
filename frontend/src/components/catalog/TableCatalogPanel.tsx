import { useTables } from "../../api/hooks";
import type { TableSchema } from "../../api/types";
import { useState } from "react";

export function TableCatalogPanel({ compact = false }: { compact?: boolean }) {
  const [search, setSearch] = useState("");
  const { data: tables, isLoading, isError, error } = useTables();
  const filteredTables = tables?.filter((table) => `${table.name} ${table.description}`.toLowerCase().includes(search.toLowerCase())) ?? [];

  return (
    <aside className={`catalog-panel ${compact ? "catalog-panel--compact" : ""}`} aria-labelledby="catalog-heading">
      <div className="catalog-panel__header">
        <div>
          <p className="eyebrow">DATA CATALOG</p>
          <h2 id="catalog-heading">Available tables</h2>
        </div>
        <span className="catalog-count">{tables?.length ?? "--"}</span>
      </div>
        <p className="catalog-panel__hint">
        Security telemetry across identity, access, audit, workloads, and network controls.
      </p>
      <label className="catalog-search">
        <span className="sr-only">Filter data sources</span>
        <span aria-hidden="true">⌕</span>
        <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Filter sources" />
      </label>

      {isLoading && (
        <p role="status" className="status-loading">
          <span className="spinner" aria-hidden="true" /> Loading tables&hellip;
        </p>
      )}

      {isError && (
        <p role="alert" className="error-text">
          <span aria-hidden="true">⚠</span> Couldn&rsquo;t load the table catalog
          {error instanceof Error ? `: ${error.message}` : "."}
        </p>
      )}

      {tables && (
        <ul className="catalog-panel__list">
          {filteredTables.map((table) => (
            <TableEntry key={table.name} table={table} />
          ))}
        </ul>
      )}
    </aside>
  );
}

function TableEntry({ table }: { table: TableSchema }) {
  return (
    <li>
      <details className="catalog-entry">
        <summary className="catalog-entry__summary">
          <span className="catalog-entry__name">{table.name}</span>
          <span className="catalog-entry__chevron" aria-hidden="true">
            ›
          </span>
        </summary>
        <p className="catalog-entry__description">{table.description}</p>
        <div className="catalog-entry__stats">
          <span>{table.rowCount?.toLocaleString() ?? "—"} sample rows</span>
          <span>{table.columns.length} columns</span>
        </div>
        <table className="catalog-entry__schema">
          <caption className="sr-only">Columns in {table.name}</caption>
          <thead>
            <tr>
              <th scope="col">Column</th>
              <th scope="col">Type</th>
              <th scope="col">Description</th>
            </tr>
          </thead>
          <tbody>
            {table.columns.map((col) => (
              <tr key={col.name}>
                <td>
                  <code>{col.name}</code>
                </td>
                <td>{col.kqlType}</td>
                <td>{col.description}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </details>
    </li>
  );
}
