import { useState, type FormEvent, type KeyboardEvent } from "react";

interface ChatInputProps {
  onSubmit: (question: string) => void;
  disabled: boolean;
}

export function ChatInput({ onSubmit, disabled }: ChatInputProps) {
  const [value, setValue] = useState("");

  const submit = () => {
    const question = value.trim();
    if (!question || disabled) return;
    onSubmit(question);
    setValue("");
  };

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    submit();
  };

  const handleKeyDown = (e: KeyboardEvent<HTMLTextAreaElement>) => {
    // Enter submits, Shift+Enter inserts a newline — standard chat-input convention.
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      submit();
    }
  };

  return (
    <form className="chat-input" onSubmit={handleSubmit}>
      <label htmlFor="chat-question" className="sr-only">
        Ask a question about your event data
      </label>
      <textarea
        id="chat-question"
        className="chat-input__textarea"
        placeholder="Ask a question about your event data..."
        value={value}
        onChange={(e) => setValue(e.target.value)}
        onKeyDown={handleKeyDown}
        disabled={disabled}
        rows={2}
      />
      <button type="submit" className="chat-input__submit" disabled={disabled || !value.trim()} aria-label={disabled ? "Generating query" : "Run query"}>
        <span>{disabled ? "Working" : "Run query"}</span>
        <span className="button-arrow" aria-hidden="true">↗</span>
      </button>
    </form>
  );
}
