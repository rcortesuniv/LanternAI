import { useState } from "react";
import { useRunQuery } from "./api/hooks";
import { ApiError } from "./api/client";
import { TableCatalogPanel } from "./components/catalog/TableCatalogPanel";
import { ChatInput } from "./components/chat/ChatInput";
import { MessageList } from "./components/chat/MessageList";
import type { ChatTurn } from "./components/chat/types";

export default function App() {
  const [turns, setTurns] = useState<ChatTurn[]>([]);
  const [liveMessage, setLiveMessage] = useState("");
  const runQuery = useRunQuery();

  const handleAsk = (question: string) => {
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

  return (
    <div className="app-shell">
      <header className="app-header">
        <div className="app-header__brand">
          <span className="app-header__logo" aria-hidden="true">
            🏮
          </span>
          <div>
            <h1>Lantern AI</h1>
            <p className="app-header__tagline">
              Ask questions about your event data in plain English — no KQL required.
            </p>
          </div>
        </div>
      </header>

      <div className="app-body">
        <TableCatalogPanel />

        <main className="chat-panel" aria-label="Query chat">
          <div className="chat-panel__messages">
            <MessageList turns={turns} />
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
