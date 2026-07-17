import { createContext, useContext, useEffect, useMemo, useState } from "react";
import type { ReactNode } from "react";
import { login as loginRequest } from "../services/authApi";
import { getTokenExpiry, isTokenExpired } from "../utils/jwt";

const TOKEN_STORAGE_KEY = "logisticsflow_auth_token";
const ACCOUNT_STORAGE_KEY = "logisticsflow_auth_account";

interface AuthContextValue {
  token: string | null;
  accountId: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
  login: (accountId: string, secret: string) => Promise<boolean>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

function readStoredToken(): { token: string; accountId: string } | null {
  const token = sessionStorage.getItem(TOKEN_STORAGE_KEY);
  const accountId = sessionStorage.getItem(ACCOUNT_STORAGE_KEY);
  if (!token || !accountId) return null;
  if (isTokenExpired(token)) {
    sessionStorage.removeItem(TOKEN_STORAGE_KEY);
    sessionStorage.removeItem(ACCOUNT_STORAGE_KEY);
    return null;
  }
  return { token, accountId };
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const stored = readStoredToken();
  const [token, setToken] = useState<string | null>(stored?.token ?? null);
  const [accountId, setAccountId] = useState<string | null>(stored?.accountId ?? null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const logout = () => {
    sessionStorage.removeItem(TOKEN_STORAGE_KEY);
    sessionStorage.removeItem(ACCOUNT_STORAGE_KEY);
    setToken(null);
    setAccountId(null);
  };

  const login = async (accountIdInput: string, secret: string): Promise<boolean> => {
    setIsLoading(true);
    setError(null);
    try {
      const response = await loginRequest({ accountId: accountIdInput, secret });
      sessionStorage.setItem(TOKEN_STORAGE_KEY, response.token);
      sessionStorage.setItem(ACCOUNT_STORAGE_KEY, accountIdInput);
      setToken(response.token);
      setAccountId(accountIdInput);
      return true;
    } catch (err) {
      setError(err instanceof Error ? err.message : "Login failed.");
      return false;
    } finally {
      setIsLoading(false);
    }
  };

  // If the token dies mid-session (tab left open past the 15-minute
  // expiry), clear auth state proactively rather than waiting for a
  // 401 from the next API call.
  useEffect(() => {
    if (!token) return;
    const expiry = getTokenExpiry(token);
    if (expiry === null) return;
    const msUntilExpiry = expiry - Date.now();
    if (msUntilExpiry <= 0) {
      logout();
      return;
    }
    const timer = setTimeout(logout, msUntilExpiry);
    return () => clearTimeout(timer);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token]);

  const value = useMemo<AuthContextValue>(
    () => ({
      token,
      accountId,
      isAuthenticated: token !== null,
      isLoading,
      error,
      login,
      logout,
    }),
    [token, accountId, isLoading, error]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error("useAuth must be used within an AuthProvider.");
  }
  return context;
}
