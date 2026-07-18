interface RiskMeterProps {
  elapsedDays: number;
  laneAverageDays: number | null;
  elevatedRiskMultiplier?: number;
}

// Risk Assessment's signature element - a horizontal meter with the
// actual ElevatedRiskMultiplier (see RiskAssessmentService.
// DetermineRiskLevel in the backend) drawn as a literal threshold line.
// This is the real business rule made visible, not decoration. Renders
// in the "unknown" signal color with no threshold marker when
// laneAverageDays is null (fewer than the minimum sample size of 5).
export function RiskMeter({ elapsedDays, laneAverageDays, elevatedRiskMultiplier = 1.5 }: RiskMeterProps) {
  if (laneAverageDays === null) {
    return (
      <div
        role="img"
        aria-label="Insufficient lane history to assess risk"
        className="h-3 w-full rounded-full"
        style={{ backgroundColor: "var(--signal-unknown)", opacity: 0.4 }}
      />
    );
  }

  const threshold = laneAverageDays * elevatedRiskMultiplier;
  const maxScale = Math.max(elapsedDays, threshold) * 1.15;
  const fillPercent = Math.min((elapsedDays / maxScale) * 100, 100);
  const thresholdPercent = Math.min((threshold / maxScale) * 100, 100);
  const isElevated = elapsedDays > threshold;

  return (
    <div
      role="img"
      aria-label={`Elapsed ${elapsedDays} days against a lane average of ${laneAverageDays} days. Elevated-risk threshold is ${threshold.toFixed(1)} days.`}
      className="relative h-3 w-full rounded-full"
      style={{ backgroundColor: "var(--divider)", opacity: 0.3 }}
    >
      <div
        className="absolute left-0 top-0 h-full rounded-full"
        style={{
          width: `${fillPercent}%`,
          backgroundColor: isElevated ? "var(--accent)" : "var(--signal-normal)",
          opacity: 1,
        }}
      />
      <div
        className="absolute top-0 h-full w-0.5"
        style={{ left: `${thresholdPercent}%`, backgroundColor: "var(--on-surface)" }}
      />
    </div>
  );
}