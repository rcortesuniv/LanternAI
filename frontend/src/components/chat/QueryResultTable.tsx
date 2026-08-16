import { useMemo, useState } from "react";
import type { QueryResultData } from "../../api/types";

export function QueryResultTable({ result }: { result: QueryResultData }) {
  const [search, setSearch] = useState("");
  const [sortColumn, setSortColumn] = useState<string | null>(null);
  const [sortDescending, setSortDescending] = useState(false);

  const visibleRows = useMemo(() => {
    const filtered = result.rows.filter((row) => !search || result.columns.some((column) => formatCell(row[column]).toLowerCase().includes(search.toLowerCase())));
    if (!sortColumn) return filtered;
    return [...filtered].sort((left, right) => String(left[sortColumn] ?? "").localeCompare(String(right[sortColumn] ?? ""), undefined, { numeric: true }) * (sortDescending ? -1 : 1));
  }, [result, search, sortColumn, sortDescending]);

  const toggleSort = (column: string) => {
    if (sortColumn === column) setSortDescending((current) => !current);
    else { setSortColumn(column); setSortDescending(false); }
  };

  if (result.rows.length === 0) {
    return <p className="result-table__empty">No rows matched.</p>;
  }

  return (
    <div className="result-table__container">
      <div className="result-table__toolbar">
        <span className="result-table__count">{visibleRows.length} of {result.rows.length} row{result.rows.length === 1 ? "" : "s"}</span>
        <label className="result-search">
          <span className="sr-only">Filter query results</span>
          <span aria-hidden="true">⌕</span>
          <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Filter rows" />
        </label>
        <button type="button" className="result-table__export" onClick={() => downloadCsv(result)}>
          <span aria-hidden="true">↓</span> Export CSV
        </button>
        <button type="button" className="result-table__export" onClick={() => downloadJson(result)}>
          <span aria-hidden="true">↓</span> JSON
        </button>
      </div>
      <div className="result-table__scroll" role="group" aria-label="Query results, scrollable">
        {visibleRows.length === 0 ? (
          <p className="result-table__empty">
            No rows match <strong>{search}</strong>.
            <button type="button" className="result-table__clear" onClick={() => setSearch("")}>Clear filter</button>
          </p>
        ) : <table className="result-table">
          <caption className="sr-only">
            {result.rows.length} row{result.rows.length === 1 ? "" : "s"} returned
          </caption>
          <thead>
            <tr>
              {result.columns.map((col) => (
                <th key={col} scope="col" aria-sort={sortColumn === col ? (sortDescending ? "descending" : "ascending") : "none"}>
                  <button type="button" className="result-table__sort" onClick={() => toggleSort(col)} aria-label={`Sort by ${col}${sortColumn === col ? (sortDescending ? ", descending" : ", ascending") : ""}`}>
                  {col}
                    <span aria-hidden="true">{sortColumn === col ? (sortDescending ? " ↓" : " ↑") : ""}</span>
                  </button>
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {visibleRows.map((row, i) => (
              // eslint-disable-next-line react/no-array-index-key -- rows have no stable id in this demo dataset
              <tr key={i}>
                {result.columns.map((col) => (
                  <td key={col}>{formatCell(row[col])}</td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>}
      </div>
    </div>
  );
}

function downloadJson(result: QueryResultData) {
  const blob = new Blob([JSON.stringify(result.rows, null, 2)], { type: "application/json;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = `lantern-query-results-${new Date().toISOString().slice(0, 10)}.json`;
  link.click();
  URL.revokeObjectURL(url);
}

function downloadCsv(result: QueryResultData) {
  const csv = [result.columns, ...result.rows.map((row) => result.columns.map((column) => row[column]))]
    .map((row) => row.map(toCsvValue).join(","))
    .join("\r\n");
  const blob = new Blob(["\uFEFF", csv], { type: "text/csv;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = `lantern-query-results-${new Date().toISOString().slice(0, 10)}.csv`;
  link.click();
  URL.revokeObjectURL(url);
}

function toCsvValue(value: unknown): string {
  if (value === null || value === undefined) return "";
  const text = typeof value === "object" ? JSON.stringify(value) : String(value);
  return /[",\r\n]/.test(text) ? `"${text.replaceAll('"', '""')}"` : text;
}

function formatCell(value: unknown): string {
  if (value === null || value === undefined) return "—";
  if (typeof value === "boolean") return value ? "true" : "false";
  if (typeof value === "number" && Number.isFinite(value)) {
    return new Intl.NumberFormat(undefined, { maximumFractionDigits: 2 }).format(value);
  }
  return String(value);
}
