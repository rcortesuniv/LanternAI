interface QueryHistoryPanelProps {
  recentQuestions: string[];
  savedQuestions: string[];
  onAsk: (question: string) => void;
  onToggleSaved: (question: string) => void;
  onClearRecent: () => void;
}

export function QueryHistoryPanel({ recentQuestions, savedQuestions, onAsk, onToggleSaved, onClearRecent }: QueryHistoryPanelProps) {
  return (
    <section className="history-panel" aria-labelledby="history-heading">
      <div className="history-panel__heading">
        <p className="eyebrow">WORKSPACE</p>
        <h2 id="history-heading">Query history</h2>
        {recentQuestions.length > 0 && <button type="button" className="history-clear" onClick={onClearRecent}>Clear</button>}
      </div>
      {savedQuestions.length > 0 && (
        <div className="history-group">
          <span className="history-label">Saved</span>
          {savedQuestions.map((question) => (
            <HistoryItem key={`saved-${question}`} question={question} saved onAsk={onAsk} onToggleSaved={onToggleSaved} />
          ))}
        </div>
      )}
      <div className="history-group">
        <span className="history-label">Recent</span>
        {recentQuestions.length === 0 ? <p className="history-empty">Your recent questions will appear here.</p> : recentQuestions.map((question) => (
          <HistoryItem key={`recent-${question}`} question={question} saved={savedQuestions.includes(question)} onAsk={onAsk} onToggleSaved={onToggleSaved} />
        ))}
      </div>
    </section>
  );
}

function HistoryItem({ question, saved, onAsk, onToggleSaved }: { question: string; saved: boolean; onAsk: (question: string) => void; onToggleSaved: (question: string) => void }) {
  return (
    <div className="history-item">
      <button type="button" className="history-item__question" onClick={() => onAsk(question)}>{question}</button>
      <button type="button" className="history-item__save" onClick={() => onToggleSaved(question)} aria-label={saved ? `Remove ${question} from saved queries` : `Save ${question}`}>
        {saved ? "★" : "☆"}
      </button>
    </div>
  );
}
