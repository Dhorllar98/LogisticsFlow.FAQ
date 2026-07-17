import { Link, useLocation } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

export function NavBar() {
  const { isAuthenticated, accountId, logout } = useAuth();
  const location = useLocation();

  const linkClass = (path: string) =>
    `text-sm font-medium ${
      location.pathname === path ? "text-blue-600" : "text-slate-600 hover:text-slate-900"
    }`;

  return (
    <nav className="flex items-center justify-between border-b border-slate-200 bg-white px-6 py-3">
      <div className="flex items-center gap-6">
        <span className="text-sm font-semibold text-slate-900">LogisticsFlow</span>
        <Link to="/" className={linkClass("/")}>FAQ</Link>
        <Link to="/tracking" className={linkClass("/tracking")}>Tracking</Link>
        <Link to="/risk-assessment" className={linkClass("/risk-assessment")}>Risk Assessment</Link>
      </div>

      <div className="text-sm">
        {isAuthenticated ? (
          <div className="flex items-center gap-3">
            <span className="text-slate-500">{accountId}</span>
            <button onClick={logout} className="text-blue-600 underline">Log out</button>
          </div>
        ) : (
          <Link to="/login" className="text-blue-600 underline">Login</Link>
        )}
      </div>
    </nav>
  );
}
