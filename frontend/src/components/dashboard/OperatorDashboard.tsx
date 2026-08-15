import { useMemo, useState } from "react";
import type { ChatTurn } from "../chat/types";
import { useCapabilities } from "../../api/hooks";

export function OperatorDashboard({ turns }: { turns: ChatTurn[] }) {
  const [isExpanded, setIsExpanded] = useState(true);
  const [range, setRange] = useState<"session" | "saved">("session");
  const [comment, setComment] = useState("");
  const [savedComments, setSavedComments] = useState<string[]>([]);
  const [alertThreshold, setAlertThreshold] = useState(1);
  const [alertsEnabled, setAlertsEnabled] = useState(true);
  const capabilities = useCapabilities();
  const completed = turns.filter((turn) => turn.status === "success");
  const errors = turns.filter((turn) => turn.status === "error");
  const rowCount = completed.reduce((total, turn) => total + (turn.response?.result.rows.length ?? 0), 0);
  const tokenUsage = completed.reduce((usage, turn) => ({
    prompt: usage.prompt + (turn.response?.usage?.promptTokens ?? 0),
    completion: usage.completion + (turn.response?.usage?.completionTokens ?? 0),
    total: usage.total + (turn.response?.usage?.totalTokens ?? 0),
  }), { prompt: 0, completion: 0, total: 0 });
  const hasTokenUsage = completed.some((turn) => turn.response?.usage?.totalTokens != null);
  const sourceCounts = useMemo(() => completed.flatMap((turn) => turn.response?.plan.tables ?? [turn.response?.plan.table ?? "Unknown"]).reduce<Record<string, number>>((counts, source) => ({ ...counts, [source]: (counts[source] ?? 0) + 1 }), {}), [completed]);
  const maxSourceCount = Math.max(1, ...Object.values(sourceCounts));
  const alertActive = alertsEnabled && errors.length >= alertThreshold;

  const shareSnapshot = async () => {
    const text = `Lantern AI workspace: ${completed.length} successful queries, ${rowCount} rows returned, ${tokenUsage.total.toLocaleString()} tokens used.`;
    if (navigator.share) await navigator.share({ title: "Lantern AI workspace", text });
    else await navigator.clipboard.writeText(text);
  };

  return (
    <section className={`operator-dashboard ${isExpanded ? "" : "operator-dashboard--collapsed"}`} aria-labelledby="dashboard-heading">
      <div className="operator-dashboard__header">
        <div>
          <p className="eyebrow">OPERATOR VIEW</p>
          <h2 id="dashboard-heading">Workspace pulse</h2>
        </div>
        <div className="dashboard-actions">
          <button type="button" className="dashboard-toggle" aria-expanded={isExpanded} aria-controls="workspace-pulse-content" onClick={() => setIsExpanded((expanded) => !expanded)}>
            <span aria-hidden="true">{isExpanded ? "⌃" : "⌄"}</span> {isExpanded ? "Hide" : "Show"}
          </button>
          <div className="dashboard-segmented" role="group" aria-label="Dashboard view">
            <button className={range === "session" ? "is-active" : ""} type="button" onClick={() => setRange("session")}>Session</button>
            <button className={range === "saved" ? "is-active" : ""} type="button" onClick={() => setRange("saved")}>Saved view</button>
          </div>
          <button type="button" className="dashboard-share" onClick={shareSnapshot}>Share snapshot</button>
        </div>
      </div>
      <div id="workspace-pulse-content" hidden={!isExpanded}>
        {capabilities.data && <div className="system-posture" aria-label="System posture">
          <div className="dashboard-subhead"><span>System posture</span><span className="dashboard-muted">Live capability report</span></div>
          <div className="posture-items">
            <PostureItem label="Identity" value={capabilities.data.authentication.configured ? "Entra ID configured" : "Local access"} tone={capabilities.data.authentication.configured ? "ready" : "caution"} />
            <PostureItem label="Data" value={capabilities.data.data.provider} tone={capabilities.data.data.configured ? "ready" : "caution"} />
            <PostureItem label="Model" value={`${capabilities.data.languageModel.provider} · ${capabilities.data.languageModel.model}`} tone="ready" />
            <PostureItem label="Sources" value={`${capabilities.data.sourceCount} available`} tone="ready" />
          </div>
        </div>}
        {range === "saved" && savedComments[0] && <p className="dashboard-note">{savedComments[0]}</p>}
        <div className={`dashboard-alert ${alertActive ? "dashboard-alert--active" : ""}`} role={alertActive ? "alert" : undefined}>
        <span aria-hidden="true">{alertActive ? "!" : "✓"}</span>
        <span>{alertActive ? `${errors.length} failed quer${errors.length === 1 ? "y" : "ies"} need attention.` : "No active query alerts."}</span>
        <label>Alert at <input type="number" min="1" value={alertThreshold} onChange={(event) => setAlertThreshold(Math.max(1, Number(event.target.value) || 1))} /> failures</label>
        <button type="button" onClick={() => setAlertsEnabled((enabled) => !enabled)}>{alertsEnabled ? "Mute" : "Enable"}</button>
        </div>
        <div className="dashboard-kpis">
        <Metric label="Queries run" value={turns.length} detail={`${completed.length} completed`} />
        <Metric label="Rows returned" value={rowCount} detail="Across this workspace" />
        <Metric label="Success rate" value={`${turns.length ? Math.round((completed.length / turns.length) * 100) : 0}%`} detail={errors.length ? `${errors.length} need attention` : "No failed runs"} />
        <Metric label="Sources touched" value={Object.keys(sourceCounts).length} detail="From result plans" />
        <Metric label="Tokens used" value={hasTokenUsage ? tokenUsage.total.toLocaleString() : "—"} detail={hasTokenUsage ? `${tokenUsage.prompt.toLocaleString()} in · ${tokenUsage.completion.toLocaleString()} out` : "Usage unavailable"} />
        </div>
        <div className="dashboard-lower-grid">
        <div className="dashboard-chart" aria-label="Source activity chart">
          <div className="dashboard-subhead"><span>Source activity</span><span className="dashboard-muted">Queries by source</span></div>
          {Object.keys(sourceCounts).length === 0 ? <p className="dashboard-empty">Run a query to see source activity.</p> : Object.entries(sourceCounts).map(([source, count]) => (
            <div className="source-bar" key={source}>
              <span>{source}</span><div><i style={{ width: `${(count / maxSourceCount) * 100}%` }} /></div><strong>{count}</strong>
            </div>
          ))}
        </div>
        <div className="dashboard-comment">
          <div className="dashboard-subhead"><span>Operator note</span><span className="dashboard-muted">Local workspace</span></div>
          <textarea value={comment} onChange={(event) => setComment(event.target.value)} placeholder="Add context for your next review..." rows={3} />
          <button type="button" className="dashboard-save" onClick={() => { setSavedComments((current) => [comment.trim(), ...current].slice(0, 5)); setComment(""); }} disabled={!comment.trim()}>Save note</button>
          {savedComments.length > 0 && <div className="dashboard-notes">{savedComments.map((note, index) => <p key={`${note}-${index}`}>{note}</p>)}</div>}
        </div>
        </div>
      </div>
    </section>
  );
}

function Metric({ label, value, detail }: { label: string; value: string | number; detail: string }) {
  return <div className="dashboard-metric"><span>{label}</span><strong>{value}</strong><small>{detail}</small></div>;
}

function PostureItem({ label, value, tone }: { label: string; value: string; tone: "ready" | "caution" }) {
  return <div className="posture-item"><span className={`posture-dot posture-dot--${tone}`} aria-hidden="true" /><div><small>{label}</small><strong>{value}</strong></div></div>;
}
