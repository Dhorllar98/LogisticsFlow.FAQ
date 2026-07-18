interface StampBadgeProps {
  label: string;
}

// Tracking's signature element - a rotated ink-stamp treatment, since
// Tracking answers a confirmed, already-happened fact (like a manifest
// stamp), not a live/predictive one. See design notes: manifest vs
// signal as tonal inversions of the same token set.
export function StampBadge({ label }: StampBadgeProps) {
  return (
    <span
      className="inline-block rounded-full border-2 px-4 py-1 text-xs font-mono font-medium uppercase tracking-wide"
      style={{
        borderColor: "var(--accent)",
        color: "var(--accent)",
        transform: "rotate(-4deg)",
      }}
    >
      {label}
    </span>
  );
}