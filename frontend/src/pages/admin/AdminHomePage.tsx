import { useNavigate } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";

function AdminHomePage() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate("/login");
  };

  return (
    <div>
      <h1>Administrator Application</h1>
      {user && (
        <p>
          Logged in as {user.firstName} {user.lastName} ({user.email})
        </p>
      )}
      <button type="button" onClick={handleLogout}>
        Logout
      </button>
    </div>
  );
}

export default AdminHomePage;
