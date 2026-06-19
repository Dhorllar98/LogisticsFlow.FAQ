import type { FAQRequest, FAQResponse } from "../types";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || "http://localhost:5148";

export async function askFAQ(request: FAQRequest): Promise<FAQResponse> {
  const response = await fetch(`${API_BASE_URL}/api/faq/ask`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    const errorBody = await response.json().catch(() => null);
    const message = errorBody?.error ?? `Request failed with status ${response.status}`;
    throw new Error(message);
  }

  return response.json();
}