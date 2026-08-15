import type { QueryResultData } from "../../api/types";

export function QueryResultTable({ result }: { result: QueryResultData }) {
  if (result.rows.length === 0) {
    return <p className="result-table__empty">No rows matched.</p>;
  }

  return (
    <div className="result-table__scroll" role="group" aria-label="Query results, scrollable">
      <table className="result-table">
        <caption className="sr-only">
          {result.rows.length} row{result.rows.length === 1 ? "" : "s"} returned
        </caption>
        <thead>
          <tr>
            {result.columns.map((col) => (
              <th key={col} scope="col">
                {col}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {result.rows.map((row, i) => (
            // eslint-disable-next-line react/no-array-index-key -- rows have no stable id in this demo dataset
            <tr key={i}>
              {result.columns.map((col) => (
                <td key={col}>{formatCell(row[col])}</td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function formatCell(value: unknown): string {
  if (value === null || value === undefined) return "—";
  if (typeof value === "boolean") return value ? "true" : "false";
  return String(value);
}
