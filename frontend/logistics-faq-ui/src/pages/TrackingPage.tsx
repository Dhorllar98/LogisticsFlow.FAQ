import { useState } from "react";
import { useAuth } from "../context/AuthContext";
import { getTrackingStatus } from "../services/trackingApi";
import { TrackingNumberInput } from "../components/TrackingNumberInput";
import { StampBadge } from "../components/StampBadge";
import type { TrackingResponse } from "../types";

export function TrackingPage() {
  const { token, logout } = useAuth();
  const [result, setResult] = useState<TrackingResponse | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleLookup = async (trackingNumber: string) => {
    if (!token) return;
    setIsLoading(true);
    setError(null);
    setResult(null);
    try {
      const response = await getTrackingStatus({ trackingNumber }, token);
      setResult(response);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Lookup failed.");
    } finally {
      setIsLoading(false);
    }
  };

  const formattedUpdated = result
    ? new Date(result.lastUpdatedUtc).toLocaleString(undefined, {
        dateStyle: "medium",
        timeStyle: "short",
      })
    : null;

  return (
    <div className="theme-manifest h-full overflow-y-auto" style={{ backgroundColor: "var(--surface)", color: "var(--on-surface)" }}>
      <div className="mx-auto max-w-2xl px-6 py-10">
        <header className="mb-8">
          <h1 className="text-3xl" style={{ fontFamily: "var(--font-display)" }}>
            Shipment Tracking
          </h1>
          <p className="mt-2 text-sm" style={{ color: "var(--on-surface)", opacity: 0.7 }}>
            Look up the confirmed status of a shipment on your account.
          </p>
        </header>

        <TrackingNumberInput onSubmit={handleLookup} isLoading={isLoading} placeholder="e.g. TRK-DEMO-001" />

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
              <span className="text-lg font-mono" style={{ color: "var(--on-surface)" }}>
                {result.trackingNumber}
              </span>
              <StampBadge label={result.statusSummary.length > 0 ? "Confirmed" : "Unknown"} />
            </div>

            <div className="mt-6 border-t pt-4" style={{ borderColor: "var(--divider)" }}>
              <dl className="grid grid-cols-[auto_1fr] gap-x-6 gap-y-2 text-sm">
                <dt className="font-medium" style={{ opacity: 0.6 }}>Carrier</dt>
                <dd className="font-mono">{result.carrier}</dd>

                <dt className="font-medium" style={{ opacity: 0.6 }}>Mode</dt>
                <dd className="font-mono">{result.mode}</dd>

                <dt className="font-medium" style={{ opacity: 0.6 }}>Updated</dt>
                <dd className="font-mono">{formattedUpdated}</dd>
              </dl>
            </div>

            <p className="mt-6 text-sm leading-relaxed" style={{ fontFamily: "var(--font-body)" }}>
              {result.statusSummary}
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