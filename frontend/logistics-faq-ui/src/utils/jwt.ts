interface DecodedJwtPayload {
  exp?: number;
  [key: string]: unknown;
}

function decodeJwtPayload(token: string): DecodedJwtPayload | null {
  const parts = token.split(".");
  if (parts.length !== 3) return null;
  try {
    const payload = parts[1].replace(/-/g, "+").replace(/_/g, "/");
    const decoded = atob(payload);
    return JSON.parse(decoded) as DecodedJwtPayload;
  } catch {
    return null;
  }
}

export function getTokenExpiry(token: string): number | null {
  const payload = decodeJwtPayload(token);
  if (!payload?.exp) return null;
  return payload.exp * 1000;
}

export function isTokenExpired(token: string): boolean {
  const expiry = getTokenExpiry(token);
  if (expiry === null) return true;
  return Date.now() >= expiry;
}