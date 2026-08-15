import { QueryResultTable } from "./QueryResultTable";
import type { ChatTurn } from "./types";

export function MessageList({ turns }: { turns: ChatTurn[] }) {
  if (turns.length === 0) {
    return (
      <div className="message-list__empty">
        <p>Ask a question about the simulated event data to get started.</p>
        <p className="message-list__empty-hint">
          Try: &ldquo;how many failed signins in the last 24 hours?&rdquo; or &ldquo;average request duration by
          endpoint&rdquo;.
        </p>
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
              <p role="alert" className="error-text">
                <span aria-hidden="true">⚠</span> {turn.errorMessage}
              </p>
            )}

            {turn.status === "success" && turn.response && (
              <>
                <details className="generated-kql" open>
                  <summary>Generated KQL</summary>
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
