import { useState } from "react";
import { QueryResultTable } from "./QueryResultTable";
import type { ChatTurn } from "./types";

interface MessageListProps {
  turns: ChatTurn[];
  onAsk: (question: string) => void;
}

export function MessageList({ turns, onAsk }: MessageListProps) {
  const [copiedTurnId, setCopiedTurnId] = useState<string | null>(null);

  if (turns.length === 0) {
    return (
      <div className="message-list__empty">
        <div className="empty-state__icon" aria-hidden="true">⌁</div>
        <p className="empty-state__title">What would you like to investigate?</p>
        <p className="message-list__empty-hint">Start with a plain-language question. The generated KQL and result set will appear here.</p>
        <div className="prompt-chips" aria-label="Example questions">
          {["Failed sign-ins in the last 24 hours", "Average request duration by endpoint", "Total latency across app, database, and API dependencies", "Recent critical security events"].map((prompt) => (
            <button key={prompt} type="button" onClick={() => onAsk(prompt)}>{prompt}</button>
          ))}
        </div>
      </div>
    );
  }

  return (
    <ol className="message-list" aria-label="Conversation">
      {turns.map((turn) => (
        <li key={turn.id} className="message-turn">
          <div className="message message--user">
            <span className="sr-only">You asked:</span>
            {turn.question}
          </div>

          <div className="message message--assistant">
            {turn.status === "loading" && (
              <p role="status" className="status-loading">
                <span className="spinner" aria-hidden="true" /> Generating query&hellip;
              </p>
            )}

            {turn.status === "error" && (
              <div className="error-state" role="alert">
                <p className="error-text"><span aria-hidden="true">⚠</span> {turn.errorMessage}</p>
                <button type="button" className="message-action" onClick={() => onAsk(turn.question)}>Try again</button>
              </div>
            )}

            {turn.status === "success" && turn.response && (
              <>
                <div className="result-meta">
                  <span>{turn.response.plan.tables?.length ?? 1} source{(turn.response.plan.tables?.length ?? 1) === 1 ? "" : "s"}</span>
                  <span aria-hidden="true">·</span>
                  <span>{turn.response.result.rows.length} row{turn.response.result.rows.length === 1 ? "" : "s"}</span>
                  {turn.response.diagnostics && <><span aria-hidden="true">·</span><span>{turn.response.diagnostics.cacheHit ? "cached" : `${turn.response.diagnostics.costTier} cost`}</span></>}
                  <button type="button" className="message-action" onClick={async () => {
                    await navigator.clipboard.writeText(turn.response!.generatedKql);
                    setCopiedTurnId(turn.id);
                    window.setTimeout(() => setCopiedTurnId((current) => current === turn.id ? null : current), 1800);
                  }}>{copiedTurnId === turn.id ? "Copied" : "Copy KQL"}</button>
                  <button type="button" className="message-action" onClick={() => onAsk(turn.question)}>Run again</button>
                </div>
                <details className="generated-kql">
                  <summary>View generated KQL</summary>
                  <pre>
                    <code>{turn.response.generatedKql}</code>
                  </pre>
                </details>
                <QueryResultTable result={turn.response.result} />
              </>
            )}
          </div>
        </li>
      ))}
    </ol>
  );
}
