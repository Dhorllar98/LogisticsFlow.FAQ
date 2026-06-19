import type { DisplayMessage } from "../types";
import { EscalationCard } from "./EscalationCard";

const SUPPORT_EMAIL = "support@logisticsflow.example.com";

export function MessageBubble({ message }: { message: DisplayMessage }) {
  const isUser = message.role === "User";

  const handleContactSupport = () => {
    window.location.href = `mailto:${SUPPORT_EMAIL}?subject=Logistics Support Request&body=${encodeURIComponent(message.content)}`;
  };

  return (
    <div className={`flex ${isUser ? "justify-end" : "justify-start"}`}>
      <div className={`max-w-[80%] rounded-2xl px-4 py-3 ${isUser ? "bg-blue-600 text-white" : "bg-slate-100 text-slate-900"}`}>
        <p className="text-sm leading-relaxed">{message.content}</p>

        {!isUser && message.groundingSources && message.groundingSources.length > 0 && (
          <p className="mt-2 text-xs text-slate-500">Sources: {message.groundingSources.join(", ")}</p>
        )}

        {!isUser && message.escalation && <EscalationCard onContactSupport={handleContactSupport} />}
      </div>
    </div>
  );
}