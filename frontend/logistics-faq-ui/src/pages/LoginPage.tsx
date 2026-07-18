import { useState } from "react";
import type { FormEvent } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

export function LoginPage() {
  const { login, isLoading, error } = useAuth();
  const [accountId, setAccountId] = useState("");
  const [secret, setSecret] = useState("");
  const [showSecret, setShowSecret] = useState(false);
  const navigate = useNavigate();
  const location = useLocation();

  const from = (location.state as { from?: { pathname: string } })?.from?.pathname ?? "/tracking";

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    const success = await login(accountId.trim(), secret);
    if (success) {
      navigate(from, { replace: true });
    }
  };

  return (
    <div className="flex h-full items-center justify-center bg-slate-50">
      <div className="w-full max-w-sm rounded-lg border border-slate-200 bg-white p-8 shadow-sm">
        <h1 className="text-lg font-semibold text-slate-900">Client Login</h1>
        <p className="mt-1 text-sm text-slate-500">
          Sign in with your account credentials to access Tracking and Risk Assessment.
        </p>

        <form onSubmit={handleSubmit} className="mt-6 space-y-4">
          <div>
            <label htmlFor="accountId" className="block text-sm font-medium text-slate-700">
              Account ID
            </label>
            <input
              id="accountId"
              type="text"
              value={accountId}
              onChange={(e) => setAccountId(e.target.value)}
              required
              disabled={isLoading}
              className="mt-1 w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          <div>
            <label htmlFor="secret" className="block text-sm font-medium text-slate-700">
              Secret
            </label>
            <div className="relative mt-1">
              <input
                id="secret"
                type={showSecret ? "text" : "password"}
                value={secret}
                onChange={(e) => setSecret(e.target.value)}
                required
                disabled={isLoading}
                className="w-full rounded-md border border-slate-300 px-3 py-2 pr-16 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
              <button
                type="button"
                onClick={() => setShowSecret((prev) => !prev)}
                tabIndex={-1}
                className="absolute right-2 top-1/2 -translate-y-1/2 text-xs font-medium text-slate-500 hover:text-slate-700"
              >
                {showSecret ? "Hide" : "Show"}
              </button>
            </div>
          </div>

          {error && (
            <div className="rounded-md border border-red-300 bg-red-50 p-3 text-sm text-red-700">
              {error}
            </div>
          )}

          <button
            type="submit"
            disabled={isLoading || !accountId.trim() || !secret}
            className="w-full rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text-white disabled:opacity-50"
          >
            {isLoading ? "Signing in..." : "Sign In"}
          </button>
        </form>

        <p className="mt-6 text-center text-xs text-slate-400">
          Do not have demo credentials? Reach out - see contact details in the project README.
        </p>
      </div>
    </div>
  );
}