import { useState } from "react";
import type { FormEvent } from "react";

interface TrackingNumberInputProps {
  onSubmit: (trackingNumber: string) => void;
  isLoading: boolean;
  placeholder?: string;
}

// Theme-agnostic by design: every visual value comes from CSS variables
// (--input-bg, --input-border, --input-text, --input-placeholder,
// --input-focus-ring) defined per-page via the .theme-manifest /
// .theme-signal classes in index.css. This component never branches on
// which page it's rendered inside.
export function TrackingNumberInput({ onSubmit, isLoading, placeholder }: TrackingNumberInputProps) {
  const [value, setValue] = useState("");

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    const trimmed = value.trim();
    if (!trimmed || isLoading) return;
    onSubmit(trimmed);
  };

  return (
    <form onSubmit={handleSubmit} className="flex gap-2">
      <input
        type="text"
        value={value}
        onChange={(e) => setValue(e.target.value)}
        placeholder={placeholder ?? "Enter tracking number"}
        disabled={isLoading}
        className="flex-1 rounded-md border px-3 py-2 text-sm font-mono focus:outline-none focus:ring-2 disabled:opacity-50"
        style={{
          backgroundColor: "var(--input-bg)",
          borderColor: "var(--input-border)",
          color: "var(--input-text)",
        }}
      />
      <button
        type="submit"
        disabled={isLoading || !value.trim()}
        className="rounded-md px-4 py-2 text-sm font-medium text-white disabled:opacity-50"
        style={{ backgroundColor: "var(--accent)" }}
      >
        {isLoading ? "Looking up..." : "Look up"}
      </button>
    </form>
  );
}