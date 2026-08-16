import { useState, type FormEvent, type KeyboardEvent } from "react";

export interface TimeRangeOption {
  label: string;
  hours: number;
}

export const TIME_RANGE_PRESETS: TimeRangeOption[] = [
  { label: "1h", hours: 1 },
  { label: "24h", hours: 24 },
  { label: "7d", hours: 168 },
  { label: "30d", hours: 720 },
];

interface ChatInputProps {
  onSubmit: (question: string) => void;
  disabled: boolean;
  selectedTimeRange: number | null;
  onTimeRangeChange: (hours: number | null) => void;
  summarize: boolean;
  onSummarizeChange: (enabled: boolean) => void;
}

export function ChatInput({ onSubmit, disabled, selectedTimeRange, onTimeRangeChange, summarize, onSummarizeChange }: ChatInputProps) {
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
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      submit();
    }
  };

  return (
    <form className="chat-input" onSubmit={handleSubmit}>
      <div className="chat-input__row">
        <div className="chat-input__toolbar">
          <div className="chat-input__time-range" role="group" aria-label="Time range">
            {TIME_RANGE_PRESETS.map((preset) => (
              <button
                key={preset.label}
                type="button"
                className={`time-pill ${selectedTimeRange === preset.hours ? "time-pill--active" : ""}`}
                onClick={() => onTimeRangeChange(preset.hours)}
                aria-pressed={selectedTimeRange === preset.hours}
              >
                {preset.label}
              </button>
            ))}
          </div>
          <label className="chat-input__summarize">
            <input
              type="checkbox"
              checked={summarize}
              onChange={(e) => onSummarizeChange(e.target.checked)}
            />
            <span>Summarize</span>
          </label>
        </div>
      </div>
      <div className="chat-input__row chat-input__row--bottom">
        <label htmlFor="chat-question" className="sr-only">
          Ask a question about your event data
        </label>
        <div className="chat-input__field">
          <textarea
            id="chat-question"
            className="chat-input__textarea"
            placeholder="Ask a question about your event data..."
            value={value}
            onChange={(e) => setValue(e.target.value)}
            onKeyDown={handleKeyDown}
            disabled={disabled}
            rows={2}
            maxLength={500}
          />
          <div className="chat-input__hint"><span>Enter to run</span><span>Shift + Enter for a new line</span><span>{value.length}/500</span></div>
        </div>
        <button type="submit" className="chat-input__submit" disabled={disabled || !value.trim()} aria-label={disabled ? "Generating query" : "Run query"}>
          <span>{disabled ? "Working" : "Run query"}</span>
          <span className="button-arrow" aria-hidden="true">↗</span>
        </button>
      </div>
    </form>
  );
}
