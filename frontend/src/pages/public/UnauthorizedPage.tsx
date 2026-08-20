import { useNavigate } from "react-router-dom";

function UnauthorizedPage() {
  const navigate = useNavigate();

  return (
    <div className="container" style={{ textAlign: "center", padding: "var(--space-16) var(--space-6)" }}>
      <h1 style={{ textTransform: "uppercase", fontSize: "1.75rem" }}>Unauthorized</h1>
      <p style={{ maxWidth: 420, margin: "0 auto var(--space-8)" }}>You do not have permission to view this page.</p>
      <button type="button" onClick={() => navigate("/")}>
        Return Home
      </button>
    </div>
  );
}

export default UnauthorizedPage;
