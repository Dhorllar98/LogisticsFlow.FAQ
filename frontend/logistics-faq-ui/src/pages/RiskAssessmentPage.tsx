import { useState } from "react";
import { useAuth } from "../context/AuthContext";
import { getRiskAssessment } from "../services/riskAssessmentApi";
import { TrackingNumberInput } from "../components/TrackingNumberInput";
import { RiskMeter } from "../components/RiskMeter";
import { normalizeRiskLevel } from "../types";
import type { RiskAssessmentResponse } from "../types";

const RISK_LABEL_COLOR: Record<string, string> = {
  Normal: "var(--signal-normal)",
  Elevated: "var(--accent)",
  Unknown: "var(--signal-unknown)",
};

export function RiskAssessmentPage() {
  const { token, logout } = useAuth();
  const [result, setResult] = useState<RiskAssessmentResponse | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleLookup = async (trackingNumber: string) => {
    if (!token) return;
    setIsLoading(true);
    setError(null);
    setResult(null);
    try {
      const response = await getRiskAssessment({ trackingNumber }, token);
      setResult(response);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Assessment failed.");
    } finally {
      setIsLoading(false);
    }
  };

  const riskLevel = result ? normalizeRiskLevel(result.riskLevel) : null;

  return (
    <div className="theme-signal h-full overflow-y-auto" style={{ backgroundColor: "var(--surface)", color: "var(--on-surface)" }}>
      <div className="mx-auto max-w-2xl px-6 py-10">
        <header className="mb-8">
          <h1 className="text-3xl" style={{ fontFamily: "var(--font-display)" }}>
            Risk Assessment
          </h1>
          <p className="mt-2 text-sm" style={{ opacity: 0.7 }}>
            Check a shipment's delay risk against pooled lane-history data.
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

        {result && riskLevel && (
          <div className="mt-8 rounded-lg border p-6" style={{ borderColor: "var(--divider)" }}>
            <div className="flex items-center justify-between gap-4">
              <span className="text-xs font-mono" style={{ opacity: 0.6 }}>
                {result.trackingNumber}
              </span>
              <span
                className="text-2xl font-bold uppercase tracking-wide"
                style={{ fontFamily: "var(--font-display)", color: RISK_LABEL_COLOR[riskLevel] }}
              >
                {riskLevel}
              </span>
            </div>

            <div className="mt-6">
              <RiskMeter elapsedDays={result.elapsedDays} laneAverageDays={result.laneAverageDays} />
              {result.laneAverageDays === null && (
                <p className="mt-2 text-xs" style={{ opacity: 0.6 }}>
                  Insufficient lane history to assess risk (fewer than 5 delivered shipments on this lane).
                </p>
              )}
            </div>

            <dl className="mt-6 grid grid-cols-3 gap-4 border-t pt-4 text-sm font-mono" style={{ borderColor: "var(--divider)" }}>
              <div>
                <dt className="text-xs" style={{ opacity: 0.6 }}>Elapsed</dt>
                <dd>{result.elapsedDays}d</dd>
              </div>
              <div>
                <dt className="text-xs" style={{ opacity: 0.6 }}>Lane avg</dt>
                <dd>{result.laneAverageDays !== null ? `${result.laneAverageDays}d` : "—"}</dd>
              </div>
              <div>
                <dt className="text-xs" style={{ opacity: 0.6 }}>Sample</dt>
                <dd>n={result.sampleSize}</dd>
              </div>
            </dl>

            <p className="mt-6 text-sm leading-relaxed" style={{ fontFamily: "var(--font-body)" }}>
              {result.suggestedAction}
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