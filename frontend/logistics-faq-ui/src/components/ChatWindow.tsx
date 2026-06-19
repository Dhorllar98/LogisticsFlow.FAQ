import { useState, useRef, useEffect } from "react";
import { useConversation } from "../hooks/useConversation";
import { MessageBubble } from "./MessageBubble";
import { TypingIndicator } from "./TypingIndicator";

export function ChatWindow() {
  const { messages, isLoading, error, sendMessage } = useConversation();
  const [input, setInput] = useState("");
  const bottomRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages, isLoading]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const trimmed = input.trim();
    if (!trimmed || isLoading) return;
    setInput("");
    sendMessage(trimmed);
  };

  return (
    <div className="flex h-screen flex-col bg-white">
      <header className="border-b border-slate-200 px-6 py-4">
        <h1 className="text-lg font-semibold text-slate-900">LogisticsFlow Support Assistant</h1>
        <p className="text-sm text-slate-500">Ask about Land, Sea, or Air freight — shipping rules, pricing, and tracking.</p>
      </header>

      <div className="flex-1 overflow-y-auto px-6 py-4 space-y-4">
        {messages.length === 0 && (
          <p className="text-sm text-slate-400">Try asking: "What's the difference between LTL and FTL shipping?"</p>
        )}
        {messages.map((message) => <MessageBubble key={message.id} message={message} />)}
        {isLoading && <TypingIndicator />}
        {error && (
          <div className="rounded-lg border border-red-300 bg-red-50 p-4 text-sm text-red-700">{error}</div>
        )}
        <div ref={bottomRef} />
      </div>

      <form onSubmit={handleSubmit} className="border-t border-slate-200 p-4">
        <div className="flex gap-2">
          <input
            type="text"
            value={input}
            onChange={(e) => setInput(e.target.value)}
            placeholder="Type your question..."
            disabled={isLoading}
            className="flex-1 rounded-full border border-slate-300 px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
          <button
            type="submit"
            disabled={isLoading || !input.trim()}
            className="rounded-full bg-blue-600 px-6 py-2 text-sm font-medium text-white disabled:opacity-50"
          >
            Send
          </button>
        </div>
      </form>
    </div>
  );
}