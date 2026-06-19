export type LogisticCategory = "Land" | "Sea" | "Air" | "General";
export type ChatRole = "User" | "Assistant";

export interface ChatMessage {
  role: ChatRole;
  content: string;
}

export interface FAQRequest {
  query: string;
  history?: ChatMessage[];
}

export interface FAQResponse {
  answer: string;
  category: LogisticCategory;
  confidenceScore: number;
  escalationBoolean: boolean;
  groundingSources: string[];
  sessionId: string;
}

export interface DisplayMessage extends ChatMessage {
  id: string;
  escalation?: boolean;
  confidenceScore?: number;
  groundingSources?: string[];
}