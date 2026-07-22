// src/services/quotationApi.ts
import type { QuotationRequest, QuotationResponse } from "../types";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || "http://localhost:5148";

export async function getQuotation(
  request: QuotationRequest,
  token: string
): Promise<QuotationResponse> {
  const response = await fetch(`${API_BASE_URL}/api/quotation/quote`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    if (response.status === 404) {
      throw new Error("No active rate agreement found for this account.");
    }
    const errorBody = await response.json().catch(() => null);
    const message = errorBody?.error ?? `Request failed with status ${response.status}`;
    throw new Error(message);
  }

  return response.json();
}