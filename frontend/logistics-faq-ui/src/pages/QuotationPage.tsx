// src/pages/QuotationPage.tsx
import { useState } from "react";
import { useAuth } from "../context/AuthContext";
import { getQuotation } from "../services/quotationApi";
import { StampBadge } from "../components/StampBadge";
import type { QuotationResponse } from "../types";

export function QuotationPage() {
  const { token, logout } = useAuth();
  const [customerQuery, setCustomerQuery] = useState("");
  const [result, setResult] = useState<QuotationResponse | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleGetQuote = async () => {
    if (!token) return;
    setIsLoading(true);
    setError(null);
    setResult(null);
    try {
      const response = await getQuotation(
        { customerQuery: customerQuery.trim() || undefined },
        token
      );
      setResult(response);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Quote request failed.");
    } finally {
      setIsLoading(false);
    }
  };

  const formattedRate = result
    ? new Intl.NumberFormat(undefined, { style: "currency", currency: "USD" }).format(
        result.negotiatedRate
      )
    : null;

  return (
    <div
      className="theme-manifest h-full overflow-y-auto"
      style={{ backgroundColor: "var(--surface)", color: "var(--on-surface)" }}
    >
      <div className="mx-auto max-w-2xl px-6 py-10">
        <header className="mb-8">
          <h1 className="text-3xl" style={{ fontFamily: "var(--font-display)" }}>
            Quotation
          </h1>
          <p className="mt-2 text-sm" style={{ opacity: 0.7 }}>
            Get your current negotiated rate agreement, composed into a
            plain-English summary.
          </p>
        </header>

        <div className="flex flex-col gap-3">
          <textarea
            value={customerQuery}
            onChange={(e) => setCustomerQuery(e.target.value)}
            placeholder="Optional — anything you'd like confirmed alongside your quote (e.g. handling instructions)"
            maxLength={500}
            rows={3}
            className="rounded-md border px-3 py-2 text-sm"
            style={{
              backgroundColor: "var(--input-bg)",
              borderColor: "var(--input-border)",
              color: "var(--input-text)",
            }}
          />
          <button
            onClick={handleGetQuote}
            disabled={isLoading}
            className="self-start rounded-md border px-4 py-2 text-sm font-medium disabled:opacity-50"
            style={{ borderColor: "var(--accent)", color: "var(--accent)" }}
          >
            {isLoading ? "Fetching quote…" : "Get My Quote"}
          </button>
        </div>

        {error && (
          <div
            className="mt-6 rounded-md border px-4 py-3 text-sm"
            style={{ borderColor: "var(--accent)", color: "var(--accent)" }}
          >
            {error}
          </div>
        )}

        {result && (
          <div className="mt-8 rounded-lg border p-6" style={{ borderColor: "var(--divider)" }}>
            <div className="flex items-start justify-between gap-4">
              <span className="text-lg font-mono">{formattedRate}</span>
              <StampBadge label="Confirmed" />
            </div>

            <div className="mt-6 border-t pt-4" style={{ borderColor: "var(--divider)" }}>
              <dl className="grid grid-cols-[auto_1fr] gap-x-6 gap-y-2 text-sm">
                <dt className="font-medium" style={{ opacity: 0.6 }}>Origin</dt>
                <dd className="font-mono">{result.originAddress}</dd>

                <dt className="font-medium" style={{ opacity: 0.6 }}>Destination</dt>
                <dd className="font-mono">{result.destinationAddress}</dd>

                {result.specialHandlingInstructions && (
                  <>
                    <dt className="font-medium" style={{ opacity: 0.6 }}>Handling</dt>
                    <dd className="font-mono">{result.specialHandlingInstructions}</dd>
                  </>
                )}
              </dl>
            </div>

            <p className="mt-6 text-sm leading-relaxed" style={{ fontFamily: "var(--font-body)" }}>
              {result.composedMessage}
            </p>
          </div>
        )}

        <button onClick={logout} className="mt-10 text-sm underline" style={{ color: "var(--accent)" }}>
          Log out
        </button>
      </div>
    </div>
  );
}