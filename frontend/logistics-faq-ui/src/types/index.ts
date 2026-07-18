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
export interface LoginRequest {
  accountId: string;
  secret: string;
}

export interface LoginResponse {
  token: string;
}

export type RiskLevel = "Unknown" | "Normal" | "Elevated";

export interface TrackingRequest {
  trackingNumber: string;
}

export interface TrackingResponse {
  trackingNumber: string;
  carrier: string;
  mode: string;
  statusSummary: string;
  lastUpdatedUtc: string;
}

export interface RiskAssessmentRequest {
  trackingNumber: string;
}

export interface RiskAssessmentResponse {
  trackingNumber: string;
  carrier: string;
  mode: string;
  elapsedDays: number;
  laneAverageDays: number | null;
  sampleSize: number;
  riskLevel: string;
  suggestedAction: string;
}

// riskLevel arrives as an open string - RiskAssessmentResponseDto.RiskLevel
// is a plain C# string, not an enum, so nothing constrains it at the type
// level server-side either. Narrow it defensively rather than trusting it.
export function normalizeRiskLevel(value: string): RiskLevel {
  if (value === "Normal" || value === "Elevated") return value;
  return "Unknown";
}