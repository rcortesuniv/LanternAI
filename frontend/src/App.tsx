import { useEffect, useState } from "react";
import { useBackendHealth, useRunQuery } from "./api/hooks";
import { ApiError } from "./api/client";
import { TableCatalogPanel } from "./components/catalog/TableCatalogPanel";
import { ChatInput } from "./components/chat/ChatInput";
import { MessageList } from "./components/chat/MessageList";
import { QueryHistoryPanel } from "./components/history/QueryHistoryPanel";
import { OperatorDashboard } from "./components/dashboard/OperatorDashboard";
import type { ChatTurn } from "./components/chat/types";

export default function App() {
  const [turns, setTurns] = useState<ChatTurn[]>([]);
  const [recentQuestions, setRecentQuestions] = useState<string[]>(() => loadQuestions("lantern-recent-queries"));
  const [savedQuestions, setSavedQuestions] = useState<string[]>(() => loadQuestions("lantern-saved-queries"));
  const [liveMessage, setLiveMessage] = useState("");
  const runQuery = useRunQuery();
  const backendHealth = useBackendHealth();

  const handleAsk = (question: string) => {
    setRecentQuestions((current) => [question, ...current.filter((item) => item !== question)].slice(0, 8));
    const id = crypto.randomUUID();
    setTurns((prev) => [...prev, { id, question, status: "loading" }]);
    setLiveMessage("Generating query…");

    runQuery.mutate(question, {
      onSuccess: (response) => {
        setTurns((prev) => prev.map((t) => (t.id === id ? { ...t, status: "success", response } : t)));
        setLiveMessage(`Query returned ${response.result.rows.length} row${response.result.rows.length === 1 ? "" : "s"}.`);
      },
      onError: (error) => {
        const message = error instanceof ApiError ? error.message : "Something went wrong. Please try again.";
        setTurns((prev) => prev.map((t) => (t.id === id ? { ...t, status: "error", errorMessage: message } : t)));
        setLiveMessage(`Query failed: ${message}`);
      },
    });
  };

  useEffect(() => localStorage.setItem("lantern-recent-queries", JSON.stringify(recentQuestions)), [recentQuestions]);
  useEffect(() => localStorage.setItem("lantern-saved-queries", JSON.stringify(savedQuestions)), [savedQuestions]);

  return (
    <div className="app-shell">
      <header className="app-header">
        <div className="brand-lockup">
          <img className="brand-mark" src="/lantern-logo.svg" alt="Lantern AI" />
          <div>
            <p className="brand-name">Lantern AI</p>
            <p className="brand-context">Merck data intelligence</p>
          </div>
        </div>
        <div className="header-meta">
          <button type="button" className="home-button" onClick={() => { setTurns([]); setLiveMessage(""); }}>
            <span aria-hidden="true">←</span> Back to home
          </button>
          <span className={`environment-pill environment-pill--${backendHealth.isError && !backendHealth.isFetching ? "offline" : "online"}`}>
            <span className="status-dot" aria-hidden="true" /> {backendHealth.isFetching ? "Checking backend" : backendHealth.isError ? "Backend offline" : "Backend ready"}
          </span>
          <span className="header-meta__version">Workspace 01</span>
        </div>
      </header>

      <div className="app-body">
        <aside className="workspace-sidebar">
          <TableCatalogPanel />
          <QueryHistoryPanel
            recentQuestions={recentQuestions}
            savedQuestions={savedQuestions}
            onAsk={handleAsk}
            onToggleSaved={(question) => setSavedQuestions((current) => current.includes(question) ? current.filter((item) => item !== question) : [question, ...current].slice(0, 8))}
            onClearRecent={() => setRecentQuestions([])}
          />
        </aside>

        <main className="chat-panel" aria-label="Query chat">
          <section className="workspace-intro">
            <div>
              <p className="eyebrow">EVENT INTELLIGENCE / NATURAL LANGUAGE</p>
              <h1>Make your event data answerable.</h1>
              <p className="workspace-intro__copy">
                Explore signals across your operational tables with a question. Lantern translates intent into a precise query and returns the evidence behind it.
              </p>
            </div>
            <div className="workspace-intro__metric" aria-label="Available data sources">
              <span className="metric-label">DATA SOURCES</span>
              <strong>LIVE</strong>
              <span>catalog connected</span>
            </div>
          </section>
          <OperatorDashboard turns={turns} />
          <div className="chat-panel__messages">
            <MessageList turns={turns} onAsk={handleAsk} />
          </div>
          <ChatInput onSubmit={handleAsk} disabled={runQuery.isPending} />
        </main>
      </div>

      {/* Announces async status changes to screen reader users without moving visual focus. */}
      <div className="sr-only" role="status" aria-live="polite" aria-atomic="true">
        {liveMessage}
      </div>
    </div>
  );
}

function loadQuestions(key: string): string[] {
  try {
    const value = JSON.parse(localStorage.getItem(key) ?? "[]");
    return Array.isArray(value) ? value.filter((item): item is string => typeof item === "string").slice(0, 8) : [];
  } catch {
    return [];
  }
}
