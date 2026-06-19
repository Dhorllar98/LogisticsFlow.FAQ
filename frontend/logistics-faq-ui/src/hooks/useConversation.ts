import { useState, useCallback } from "react";
import { askFAQ } from "../services/faqApi";
import type { ChatMessage, DisplayMessage } from "../types";

const MAX_HISTORY_TURNS = 6;

export function useConversation() {
  const [messages, setMessages] = useState<DisplayMessage[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const sendMessage = useCallback(async (query: string) => {
    setError(null);
    setIsLoading(true);

    const userMessage: DisplayMessage = { id: crypto.randomUUID(), role: "User", content: query };
    setMessages((prev) => [...prev, userMessage]);

    try {
      // Capped at 6 prior turns, mirroring the backend's own
      // ConversationSession limit — keeps both sides conceptually aligned.
      const history: ChatMessage[] = messages
        .slice(-MAX_HISTORY_TURNS)
        .map(({ role, content }) => ({ role, content }));

      const response = await askFAQ({ query, history });

      const assistantMessage: DisplayMessage = {
        id: crypto.randomUUID(),
        role: "Assistant",
        content: response.answer,
        escalation: response.escalationBoolean,
        confidenceScore: response.confidenceScore,
        groundingSources: response.groundingSources,
      };

      setMessages((prev) => [...prev, assistantMessage]);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Something went wrong. Please try again.");
    } finally {
      setIsLoading(false);
    }
  }, [messages]);

  return { messages, isLoading, error, sendMessage };
}