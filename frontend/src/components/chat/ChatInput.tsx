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
        placeholder="Ask about your event data, e.g. “how many failed signins in the last 24 hours?”"
        value={value}
        onChange={(e) => setValue(e.target.value)}
        onKeyDown={handleKeyDown}
        disabled={disabled}
        rows={2}
      />
      <button type="submit" className="chat-input__submit" disabled={disabled || !value.trim()}>
        {disabled ? (
          <>
            <span className="spinner spinner--on-accent" aria-hidden="true" /> Thinking…
          </>
        ) : (
          <>
            Ask
            <svg
              className="chat-input__submit-icon"
              aria-hidden="true"
              viewBox="0 0 16 16"
              width="16"
              height="16"
            >
              <path fill="currentColor" d="M3 3v4.2l7 .8-7 .8V13l12-5z" />
            </svg>
          </>
        )}
      </button>
    </form>
  );
}
