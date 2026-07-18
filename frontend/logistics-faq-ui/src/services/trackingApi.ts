import type { TrackingRequest, TrackingResponse } from "../types";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || "http://localhost:5148";

export async function getTrackingStatus(
  request: TrackingRequest,
  token: string
): Promise<TrackingResponse> {
  const response = await fetch(`${API_BASE_URL}/api/tracking/status`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    if (response.status === 404) {
      throw new Error("No shipment found for that tracking number on this account.");
    }
    const errorBody = await response.json().catch(() => null);
    const message = errorBody?.error ?? `Request failed with status ${response.status}`;
    throw new Error(message);
  }

  return response.json();
}