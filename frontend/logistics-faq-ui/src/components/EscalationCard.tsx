interface EscalationCardProps {
  onContactSupport: () => void;
}

export function EscalationCard({ onContactSupport }: EscalationCardProps) {
  return (
    <div className="mt-2 rounded-lg border border-amber-300 bg-amber-50 p-4">
      <p className="text-sm text-amber-900">
        This question falls outside what I can confidently answer from our knowledge base. Let's connect you with our support team directly.
      </p>
      <button
        onClick={onContactSupport}
        className="mt-3 rounded-md bg-amber-600 px-4 py-2 text-sm font-medium text-white hover:bg-amber-700"
      >
        Talk to a Human
      </button>
    </div>
  );
}