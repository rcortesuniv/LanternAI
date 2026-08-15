import { useMemo } from "react";
import type { ChatTurn } from "../chat/types";

export function UserStatistics({ turns, savedQuestions }: { turns: ChatTurn[]; savedQuestions: string[] }) {
  const completed = turns.filter((turn) => turn.status === "success");
  const failed = turns.filter((turn) => turn.status === "error");
  const rows = completed.reduce((total, turn) => total + (turn.response?.result.rows.length ?? 0), 0);
  const tokens = completed.reduce((total, turn) => total + (turn.response?.usage?.totalTokens ?? 0), 0);
  const sourceCounts = useMemo(() => completed.flatMap((turn) => turn.response?.plan.tables ?? [turn.response?.plan.table ?? "Unknown"]).reduce<Record<string, number>>((counts, source) => ({ ...counts, [source]: (counts[source] ?? 0) + 1 }), {}), [completed]);
  const topSources = Object.entries(sourceCounts).sort(([, left], [, right]) => right - left).slice(0, 5);

  return (
    <main className="statistics-view" aria-labelledby="statistics-heading">
      <div className="statistics-header">
        <div>
          <p className="eyebrow">PERSONAL WORKSPACE</p>
          <h1 id="statistics-heading">User statistics</h1>
          <p>Activity and usage across this local Lantern AI workspace.</p>
        </div>
        <span className="statistics-period">Current session</span>
      </div>
      <div className="statistics-grid">
        <Stat label="Queries run" value={turns.length} detail={`${completed.length} completed`} />
        <Stat label="Success rate" value={`${turns.length ? Math.round((completed.length / turns.length) * 100) : 0}%`} detail={`${failed.length} failed`} />
        <Stat label="Rows reviewed" value={rows.toLocaleString()} detail="Returned by queries" />
        <Stat label="Tokens used" value={tokens ? tokens.toLocaleString() : "—"} detail={tokens ? "Prompt and completion" : "No usage recorded"} />
        <Stat label="Saved queries" value={savedQuestions.length} detail="Personal shortcuts" />
      </div>
      <div className="statistics-panels">
        <section className="statistics-panel" aria-labelledby="source-usage-heading">
          <div className="statistics-panel__heading"><h2 id="source-usage-heading">Most used sources</h2><span>Query count</span></div>
          {topSources.length === 0 ? <p className="statistics-empty">Run a query to build source activity.</p> : topSources.map(([source, count]) => (
            <div className="statistics-source" key={source}><span>{source}</span><strong>{count}</strong><div><i style={{ width: `${Math.max(14, (count / topSources[0][1]) * 100)}%` }} /></div></div>
          ))}
        </section>
        <section className="statistics-panel" aria-labelledby="recent-heading">
          <div className="statistics-panel__heading"><h2 id="recent-heading">Recent activity</h2><span>Latest questions</span></div>
          {turns.length === 0 ? <p className="statistics-empty">No questions have been asked yet.</p> : turns.slice(-5).reverse().map((turn) => (
            <div className="statistics-activity" key={turn.id}><span className={`activity-status activity-status--${turn.status}`} aria-hidden="true" /><p>{turn.question}</p><small>{turn.status}</small></div>
          ))}
        </section>
      </div>
    </main>
  );
}

function Stat({ label, value, detail }: { label: string; value: string | number; detail: string }) {
  return <div className="statistics-stat"><span>{label}</span><strong>{value}</strong><small>{detail}</small></div>;
}
