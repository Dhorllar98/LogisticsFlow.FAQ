import { useAuth } from "../context/AuthContext";

export function TrackingPage() {
  const { accountId, logout } = useAuth();

  return (
    <div className="flex h-full flex-col items-center justify-center gap-4 bg-white">
      <h1 className="text-lg font-semibold text-slate-900">Tracking</h1>
      <p className="text-sm text-slate-500">Signed in as {accountId}.</p>
      <p className="text-sm text-slate-400">Full Tracking UI ships in the next phase.</p>
      <button onClick={logout} className="text-sm text-blue-600 underline">
        Log out
      </button>
    </div>
  );
}
