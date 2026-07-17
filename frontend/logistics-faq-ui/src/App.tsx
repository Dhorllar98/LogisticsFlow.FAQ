import { Route, Routes } from "react-router-dom";
import { ChatWindow } from "./components/ChatWindow";
import { NavBar } from "./components/NavBar";
import { RequireAuth } from "./components/RequireAuth";
import { LoginPage } from "./pages/LoginPage";
import { TrackingPage } from "./pages/TrackingPage";
import { RiskAssessmentPage } from "./pages/RiskAssessmentPage";

function App() {
  return (
    <div className="flex h-screen flex-col">
      <NavBar />
      <div className="flex-1 overflow-hidden">
        <Routes>
          <Route path="/" element={<ChatWindow />} />
          <Route path="/login" element={<LoginPage />} />
          <Route
            path="/tracking"
            element={
              <RequireAuth>
                <TrackingPage />
              </RequireAuth>
            }
          />
          <Route
            path="/risk-assessment"
            element={
              <RequireAuth>
                <RiskAssessmentPage />
              </RequireAuth>
            }
          />
        </Routes>
      </div>
    </div>
  );
}

export default App;
