import { useNavigate } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";

function UnauthorizedPage() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const homePath = user?.role === "Administrator" ? "/admin" : user?.role === "Driver" ? "/driver" : "/";
  const homeLabel = user ? "Return to Dashboard" : "Return Home";

  return (
    <div className="container" style={{ textAlign: "center", padding: "var(--space-16) var(--space-6)" }}>
      <h1 style={{ textTransform: "uppercase", fontSize: "1.75rem" }}>Unauthorized</h1>
      <p style={{ maxWidth: 420, margin: "0 auto var(--space-8)" }}>You do not have permission to view this page.</p>
      <button type="button" onClick={() => navigate(homePath)}>
        {homeLabel}
      </button>
    </div>
  );
}

export default UnauthorizedPage;
