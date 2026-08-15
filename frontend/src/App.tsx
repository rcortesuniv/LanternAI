import { useEffect, useState } from "react";
import { useBackendHealth, useModelReadiness, useRunQuery, useTables } from "./api/hooks";
import { ApiError } from "./api/client";
import { TableCatalogPanel } from "./components/catalog/TableCatalogPanel";
import { ChatInput } from "./components/chat/ChatInput";
import { MessageList } from "./components/chat/MessageList";
import { QueryHistoryPanel } from "./components/history/QueryHistoryPanel";
import { OperatorDashboard } from "./components/dashboard/OperatorDashboard";
import { UserStatistics } from "./components/statistics/UserStatistics";
import type { ChatTurn } from "./components/chat/types";
import type { QueryRequestPayload } from "./api/types";

export default function App() {
  const [theme, setTheme] = useState<"dark" | "clear">("dark");
  const [activeView, setActiveView] = useState<"workspace" | "catalog" | "pulse" | "statistics">("workspace");
  const [turns, setTurns] = useState<ChatTurn[]>([]);
  const [recentQuestions, setRecentQuestions] = useState<string[]>(() => loadQuestions("lantern-recent-queries"));
  const [savedQuestions, setSavedQuestions] = useState<string[]>(() => loadQuestions("lantern-saved-queries"));
  const [liveMessage, setLiveMessage] = useState("");
  const [selectedTimeRange, setSelectedTimeRange] = useState<number | null>(24);
  const [summarize, setSummarize] = useState(false);
  const runQuery = useRunQuery();
  const backendHealth = useBackendHealth();
  const modelReadiness = useModelReadiness();
  const tables = useTables();

  const handleAsk = (question: string) => {
    if (modelReadiness.isError) {
      setLiveMessage("Query paused until the model is available. Start Ollama and retry the connection.");
      return;
    }
    setRecentQuestions((current) => [question, ...current.filter((item) => item !== question)].slice(0, 8));
    const id = crypto.randomUUID();
    setTurns((prev) => [...prev, { id, question, status: "loading" }]);

    const loadingMsg = summarize
      ? "Generating query and summarizing results…"
      : "Generating query…";
    setLiveMessage(loadingMsg);

    // Build follow-up context from the last successful turn.
    const lastSuccess = [...turns].reverse().find((t) => t.status === "success" && t.response);
    const payload: QueryRequestPayload = {
      question,
      timeRangeHours: selectedTimeRange,
      summarize,
      previousQuestion: lastSuccess?.question ?? null,
      previousPlan: lastSuccess?.response?.plan ?? null,
      previousSummary: lastSuccess?.response?.resultSummary ?? null,
    };

    runQuery.mutate(payload, {
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
    <div className={`app-shell app-shell--${theme}`}>
      <header className="app-header">
        <div className="brand-lockup">
          <img className="brand-mark" src="/lantern-logo.svg" alt="Lantern AI" />
          <div>
            <p className="brand-name"><span className="brand-company">MSD</span> Lantern AI</p>
            <p className="brand-context">Security data intelligence</p>
          </div>
        </div>
        <div className="header-meta">
          <button type="button" className="theme-toggle" onClick={() => setTheme((current) => current === "dark" ? "clear" : "dark")}>
            <span aria-hidden="true">{theme === "dark" ? "◐" : "◑"}</span> {theme === "dark" ? "Clear mode" : "Dark mode"}
          </button>
          <button type="button" className="home-button" onClick={() => { setActiveView("workspace"); setTurns([]); setLiveMessage(""); }}>
            <span aria-hidden="true">←</span> Back to home
          </button>
          <span className={`environment-pill environment-pill--${backendHealth.isError && !backendHealth.isFetching ? "offline" : "online"}`}>
            <span className="status-dot" aria-hidden="true" /> {backendHealth.isFetching ? "Checking API" : backendHealth.isError ? "API offline" : "API online"}
          </span>
          <span className={`environment-pill environment-pill--${modelReadiness.isError && !modelReadiness.isFetching ? "offline" : "online"}`} title="Requires Ollama for natural-language query generation">
            <span className="status-dot" aria-hidden="true" /> {modelReadiness.isFetching ? "Checking model" : modelReadiness.isError ? "Model unavailable" : "Model ready"}
          </span>
          {(backendHealth.isError || modelReadiness.isError) && (
            <button type="button" className="status-retry" onClick={() => { void backendHealth.refetch(); void modelReadiness.refetch(); }}>
              Retry
            </button>
          )}
          <span className="header-meta__version">Workspace 01</span>
        </div>
      </header>

      <nav className="app-tabs" aria-label="Primary navigation">
        <button type="button" className={activeView === "workspace" ? "is-active" : ""} onClick={() => setActiveView("workspace")}>Workspace</button>
        <button type="button" className={activeView === "pulse" ? "is-active" : ""} onClick={() => setActiveView("pulse")}>Pulse</button>
        <button type="button" className={activeView === "statistics" ? "is-active" : ""} onClick={() => setActiveView("statistics")}>User statistics</button>
        <button type="button" className={activeView === "catalog" ? "is-active" : ""} onClick={() => setActiveView("catalog")}>Data catalog <span>{tables.isLoading ? "…" : tables.data?.length ?? "—"}</span></button>
      </nav>

      <div className="app-body">
        <aside className="workspace-sidebar">
          <TableCatalogPanel compact />
          <QueryHistoryPanel
            recentQuestions={recentQuestions}
            savedQuestions={savedQuestions}
            onAsk={handleAsk}
            onToggleSaved={(question) => setSavedQuestions((current) => current.includes(question) ? current.filter((item) => item !== question) : [question, ...current].slice(0, 8))}
            onClearRecent={() => setRecentQuestions([])}
          />
        </aside>

        {activeView === "statistics" ? <UserStatistics turns={turns} savedQuestions={savedQuestions} /> : activeView === "catalog" ? <main className="catalog-view" aria-label="Data catalog">
          <TableCatalogPanel />
        </main> : activeView === "pulse" ? <main className="pulse-view" aria-label="Workspace pulse">
          <OperatorDashboard turns={turns} />
        </main> : <main className="chat-panel" aria-label="Query chat">
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
              <strong>{tables.isLoading ? "…" : tables.data?.length ?? "—"}</strong>
              <span>{tables.isError ? "catalog unavailable" : "tables connected"}</span>
            </div>
          </section>
          {modelReadiness.isError && (
            <div className="workspace-warning" role="status">
              <span aria-hidden="true">!</span>
              <p><strong>Natural-language queries are paused.</strong> Start Ollama, then use Retry in the header to reconnect the model.</p>
            </div>
          )}
          <div className="chat-panel__messages">
            <MessageList turns={turns} onAsk={handleAsk} />
          </div>
          <ChatInput
            onSubmit={handleAsk}
            disabled={runQuery.isPending || modelReadiness.isError}
            selectedTimeRange={selectedTimeRange}
            onTimeRangeChange={setSelectedTimeRange}
            summarize={summarize}
            onSummarizeChange={setSummarize}
          />
        </main>}
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
