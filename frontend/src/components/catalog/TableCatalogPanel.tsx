import { useTables } from "../../api/hooks";
import type { TableSchema } from "../../api/types";

export function TableCatalogPanel() {
  const { data: tables, isLoading, isError, error } = useTables();

  return (
    <aside className="catalog-panel" aria-labelledby="catalog-heading">
      <h2 id="catalog-heading">Available tables</h2>
      <p className="catalog-panel__hint">
        Lantern AI can currently query across all of these simulated event tables.
      </p>

      {isLoading && <p role="status">Loading tables&hellip;</p>}

      {isError && (
        <p role="alert" className="error-text">
          <span aria-hidden="true">⚠</span> Couldn&rsquo;t load the table catalog
          {error instanceof Error ? `: ${error.message}` : "."}
        </p>
      )}

      {tables && (
        <ul className="catalog-panel__list">
          {tables.map((table) => (
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
        </summary>
        <p className="catalog-entry__description">{table.description}</p>
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
