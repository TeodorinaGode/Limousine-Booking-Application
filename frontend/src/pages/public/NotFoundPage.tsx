import { useNavigate } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";

/** Premium 404 (section 57) — same black/gray identity as the rest of the app. */
function NotFoundPage() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const homePath = user?.role === "Administrator" ? "/admin" : user?.role === "Driver" ? "/driver" : "/";
  const homeLabel = user ? "Return to Dashboard" : "Return Home";

  return (
    <div className="container" style={{ textAlign: "center", padding: "var(--space-16) var(--space-6)" }}>
      <p className="hero__eyebrow">404</p>
      <h1 style={{ textTransform: "uppercase", fontSize: "clamp(1.75rem, 5vw, 2.5rem)" }}>The road ends here.</h1>
      <p style={{ maxWidth: 420, margin: "0 auto var(--space-8)" }}>
        The page you requested could not be found.
      </p>
      <button type="button" onClick={() => navigate(homePath)}>
        {homeLabel}
      </button>
    </div>
  );
}

export default NotFoundPage;
