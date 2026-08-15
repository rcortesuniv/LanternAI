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
import { QueryLibrary as LibraryPanel } from "./components/library/LibraryPanel";
import { QUERY_LIBRARY } from "./components/library/libraryData";
import type { QueryRequestPayload, PromptbookExecutionResult, AnomalyReport, IncidentSummary, SessionQuery } from "./api/types";
import { usePromptbooks, useExecutePromptbook, useDetectAnomalies, useIncidentSummary } from "./api/hooks";

type ViewName = "workspace" | "library" | "promptbooks" | "pulse" | "statistics" | "catalog";

export default function App() {
  const [theme, setTheme] = useState<"dark" | "clear">("dark");
  const [activeView, setActiveView] = useState<ViewName>("workspace");
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
  const promptbooks = usePromptbooks();
  const executePromptbook = useExecutePromptbook();
  const detectAnomalies = useDetectAnomalies();
  const incidentSummary = useIncidentSummary();
  const [activePromptbook, setActivePromptbook] = useState<string | null>(null);
  const [promptbookResult, setPromptbookResult] = useState<PromptbookExecutionResult | null>(null);
  const [anomalyReport, setAnomalyReport] = useState<AnomalyReport | null>(null);
  const [incidentReport, setIncidentReport] = useState<IncidentSummary | null>(null);

  const handleAsk = (question: string) => {
    if (modelReadiness.isError) {
      setLiveMessage("Query paused until the model is available. Start Ollama and retry the connection.");
      return;
    }
    setRecentQuestions((current) => [question, ...current.filter((item) => item !== question)].slice(0, 8));
    const id = crypto.randomUUID();
    setTurns((prev) => [...prev, { id, question, status: "loading" }]);
    setActiveView("workspace");

    const loadingMsg = summarize
      ? "Generating query and summarizing results…"
      : "Generating query…";
    setLiveMessage(loadingMsg);

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

  // Auto-detect anomalies after a successful query
  useEffect(() => {
    const lastSuccess = [...turns].reverse().find((t) => t.status === "success" && t.response);
    if (lastSuccess?.response && !anomalyReport) {
      detectAnomalies.mutate(
        { plan: lastSuccess.response.plan, result: lastSuccess.response.result },
        { onSuccess: setAnomalyReport }
      );
    }
    if (!lastSuccess?.response) {
      setAnomalyReport(null);
    }
  }, [turns]);

  const handleExecutePromptbook = (id: string) => {
    setActivePromptbook(id);
    setPromptbookResult(null);
    setTurns([]);
    setAnomalyReport(null);
    setIncidentReport(null);
    setActiveView("workspace");
    setLiveMessage("Running promptbook investigation…");
    executePromptbook.mutate(id, {
      onSuccess: (result) => {
        setPromptbookResult(result);
        // Inject each non-skipped step as a chat turn in the workspace
        const newTurns: ChatTurn[] = result.steps
          .filter(s => !s.skipped && s.plan && s.result)
          .map((s, idx) => ({
            id: `pb-${result.promptbookId}-${idx}`,
            question: s.question,
            status: "success" as const,
            response: {
              question: s.question,
              generatedKql: s.generatedKql ?? "",
              plan: s.plan!,
              result: s.result!,
              resultSummary: s.summary ?? null,
              usage: null,
              diagnostics: null,
              explanation: null,
              metrics: null,
              auditId: null,
            },
          }));
        setTurns(newTurns);
        setActiveView("workspace");
        setLiveMessage(`Promptbook complete: ${newTurns.length} steps loaded into workspace. Ask follow-up questions to drill down.`);
      },
      onError: (error) => {
        const message = error instanceof ApiError ? error.message : "Promptbook execution failed.";
        setLiveMessage(`Promptbook failed: ${message}`);
      },
    });
  };

  const handleGenerateIncidentSummary = () => {
    const queries: SessionQuery[] = turns
      .filter((t) => t.status === "success" && t.response)
      .map((t) => ({
        question: t.question,
        plan: t.response!.plan,
        rowCount: t.response!.result.rows.length,
        summary: t.response!.resultSummary ?? null,
      }));
    if (queries.length === 0) {
      setLiveMessage("Run at least one query before generating an incident summary.");
      return;
    }
    setLiveMessage("Generating incident summary…");
    incidentSummary.mutate(queries, {
      onSuccess: (summary) => {
        setIncidentReport(summary);
        setLiveMessage("Incident summary generated.");
      },
      onError: (error) => {
        setLiveMessage(`Incident summary failed: ${error instanceof ApiError ? error.message : "unknown error"}`);
      },
    });
  };

  useEffect(() => localStorage.setItem("lantern-recent-queries", JSON.stringify(recentQuestions)), [recentQuestions]);
  useEffect(() => localStorage.setItem("lantern-saved-queries", JSON.stringify(savedQuestions)), [savedQuestions]);

  const totalQueries = QUERY_LIBRARY.reduce((sum, cat) => sum + cat.queries.length, 0);

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
        <button type="button" className={activeView === "library" ? "is-active" : ""} onClick={() => setActiveView("library")}>Query library <span>{totalQueries}</span></button>
        <button type="button" className={activeView === "promptbooks" ? "is-active" : ""} onClick={() => setActiveView("promptbooks")}>Promptbooks <span>{promptbooks.data?.length ?? "—"}</span></button>
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
        </main> : activeView === "promptbooks" ? <main className="promptbook-view" aria-label="Promptbooks">
          <div className="promptbook-view__header">
            <div>
              <p className="eyebrow">AUTOMATED INVESTIGATIONS</p>
              <h1>Promptbooks</h1>
              <p className="promptbook-view__subtitle">Multi-step investigation sequences that chain queries automatically. Each step builds on the previous result's context.</p>
            </div>
          </div>
          {promptbooks.isLoading && <p className="status-loading"><span className="spinner" aria-hidden="true" /> Loading promptbooks…</p>}
          {promptbooks.data && (
            <div className="promptbook-list">
              {promptbooks.data.map((book) => (
                <div key={book.id} className="promptbook-card">
                  <div className="promptbook-card__header">
                    <div>
                      <h3>{book.name}</h3>
                      <p className="promptbook-card__category">{book.category}</p>
                    </div>
                    <button
                      type="button"
                      className="promptbook-card__run"
                      disabled={executePromptbook.isPending && activePromptbook === book.id}
                      onClick={() => handleExecutePromptbook(book.id)}
                    >
                      {executePromptbook.isPending && activePromptbook === book.id ? "Running…" : "Run"}
                    </button>
                  </div>
                  <p className="promptbook-card__desc">{book.description}</p>
                  <ol className="promptbook-card__steps">
                    {book.steps.map((step, idx) => (
                      <li key={idx}>
                        <span className="promptbook-step__num">{idx + 1}</span>
                        <div>
                          <p className="promptbook-step__q">{step.question}</p>
                          <p className="promptbook-step__desc">{step.description}</p>
                        </div>
                      </li>
                    ))}
                  </ol>
                </div>
              ))}
            </div>
          )}
          {promptbookResult && (
            <div className="promptbook-results">
              <h2>Results: {promptbookResult.promptbookName}</h2>
              <p className="promptbook-results__meta">
                {promptbookResult.steps.filter(s => !s.skipped).length} steps executed ·
                {" "}{Math.round(promptbookResult.totalDurationMs / 1000)}s ·
                {" ~"}{promptbookResult.totalTokens} tokens
              </p>
              {promptbookResult.steps.map((step) => (
                <div key={step.stepIndex} className={`promptbook-step-result ${step.skipped ? "is-skipped" : ""}`}>
                  <div className="promptbook-step-result__header">
                    <span className="promptbook-step__num">{step.stepIndex + 1}</span>
                    <span className="promptbook-step-result__q">{step.question}</span>
                    {step.skipped && <span className="promptbook-step-result__skipped">Skipped</span>}
                    {!step.skipped && <span className="promptbook-step-result__rows">{step.rowCount} rows</span>}
                  </div>
                  {step.summary && <p className="promptbook-step-result__summary">{step.summary}</p>}
                  {step.generatedKql && (
                    <details className="generated-kql">
                      <summary>View KQL</summary>
                      <pre><code>{step.generatedKql}</code></pre>
                    </details>
                  )}
                </div>
              ))}
            </div>
          )}
        </main> : activeView === "pulse" ? <main className="pulse-view" aria-label="Workspace pulse">
          <OperatorDashboard turns={turns} />
        </main> : activeView === "library" ? <main className="library-view" aria-label="Query library">
          <div className="library-view__header">
            <div>
              <p className="eyebrow">INVESTIGATION TEMPLATES</p>
              <h1>Query library</h1>
              <p className="library-view__subtitle">{totalQueries} curated questions across {QUERY_LIBRARY.length} categories, mapped to your {tables.data?.length ?? 16} data sources.</p>
            </div>
          </div>
          <LibraryPanel onAsk={handleAsk} />
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
          {anomalyReport && anomalyReport.hasFindings && (
            <div className="anomaly-banner" role="alert">
              <p className="anomaly-banner__title">⚠ Findings detected</p>
              <ul className="anomaly-banner__list">
                {anomalyReport.flags.map((flag, idx) => (
                  <li key={idx} className={`anomaly-flag anomaly-flag--${flag.severity}`}>
                    <strong>{flag.title}</strong> — {flag.description}
                  </li>
                ))}
              </ul>
            </div>
          )}
          <div className="chat-panel__messages">
            <MessageList turns={turns} onAsk={handleAsk} />
          </div>
          {turns.some(t => t.status === "success") && (
            <div className="chat-panel__actions">
              <button
                type="button"
                className="incident-btn"
                disabled={incidentSummary.isPending}
                onClick={handleGenerateIncidentSummary}
              >
                <span className="incident-btn__icon">{incidentSummary.isPending ? "⏳" : "📋"}</span>
                {incidentSummary.isPending ? "Generating report…" : "Generate incident report"}
              </button>
            </div>
          )}
          {incidentSummary.isPending && (
            <div className="incident-loading">
              <div className="incident-loading__header">
                <span className="spinner" aria-hidden="true" />
                Analyzing {turns.filter(t => t.status === "success").length} queries and generating incident report…
              </div>
              <div className="incident-loading__bar" />
            </div>
          )}
          {incidentReport && (
            <div className="incident-modal-overlay" onClick={() => setIncidentReport(null)} role="dialog" aria-modal="true" aria-label="Incident report">
              <div className="incident-modal" onClick={(e) => e.stopPropagation()}>
                <div className="incident-report" role="article">
                  <div className="incident-report__header">
                    <div className="incident-report__header-left">
                      <div className="incident-report__icon">📋</div>
                      <div>
                        <p className="incident-report__label">INCIDENT REPORT</p>
                        <h2>{incidentReport.title}</h2>
                      </div>
                    </div>
                    <button type="button" className="incident-report__close" onClick={() => setIncidentReport(null)} aria-label="Close report">×</button>
                  </div>
                  <div className="incident-report__body">
                    <div className="incident-report__overview">
                      <div className="incident-report__overview-icon">📝</div>
                      <p>{incidentReport.overview}</p>
                    </div>
                    {incidentReport.keyFindings.length > 0 && (
                      <div className="incident-report__section incident-report__section--findings">
                        <h3><span className="incident-report__section-icon">🔍</span> Key findings</h3>
                        <ul>
                          {incidentReport.keyFindings.map((f, i) => (
                            <li key={i}><span className="incident-report__bullet">{i + 1}</span><span>{f}</span></li>
                          ))}
                        </ul>
                      </div>
                    )}
                    <div className="incident-report__section incident-report__section--risk">
                      <h3><span className="incident-report__section-icon">⚠️</span> Risk assessment</h3>
                      <div className="incident-report__risk-badge">{incidentReport.riskAssessment.split(";")[0]}</div>
                      <p>{incidentReport.riskAssessment}</p>
                    </div>
                    {incidentReport.recommendedActions.length > 0 && (
                      <div className="incident-report__section incident-report__section--actions">
                        <h3><span className="incident-report__section-icon">✅</span> Recommended actions</h3>
                        <ul>
                          {incidentReport.recommendedActions.map((a, i) => (
                            <li key={i}><span className="incident-report__bullet incident-report__bullet--action">{i + 1}</span><span>{a}</span></li>
                          ))}
                        </ul>
                      </div>
                    )}
                  </div>
                  <div className="incident-report__footer">
                    <div className="incident-report__meta">
                      <span className="incident-report__meta-item">📊 {incidentReport.queryCount} queries</span>
                      <span className="incident-report__meta-sep">·</span>
                      <span className="incident-report__meta-item">📋 {incidentReport.totalRowsAnalyzed.toLocaleString()} rows analyzed</span>
                    </div>
                    <div className="incident-report__actions">
                      <button type="button" className="incident-report__action" onClick={() => {
                        const text = `# ${incidentReport.title}\n\n## Overview\n${incidentReport.overview}\n\n## Key Findings\n${incidentReport.keyFindings.map(f => `- ${f}`).join("\n")}\n\n## Risk Assessment\n${incidentReport.riskAssessment}\n\n## Recommended Actions\n${incidentReport.recommendedActions.map(a => `- ${a}`).join("\n")}`;
                        navigator.clipboard.writeText(text);
                      }}>Copy</button>
                      <button type="button" className="incident-report__action" onClick={() => {
                        const md = `# ${incidentReport.title}\n\n## Overview\n${incidentReport.overview}\n\n## Key Findings\n${incidentReport.keyFindings.map(f => `- ${f}`).join("\n")}\n\n## Risk Assessment\n${incidentReport.riskAssessment}\n\n## Recommended Actions\n${incidentReport.recommendedActions.map(a => `- ${a}`).join("\n")}`;
                        const blob = new Blob([md], { type: "text/markdown" });
                        const url = URL.createObjectURL(blob);
                        const link = document.createElement("a");
                        link.href = url;
                        link.download = `incident-report-${new Date().toISOString().slice(0, 10)}.md`;
                        link.click();
                        URL.revokeObjectURL(url);
                      }}>Download</button>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          )}
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
