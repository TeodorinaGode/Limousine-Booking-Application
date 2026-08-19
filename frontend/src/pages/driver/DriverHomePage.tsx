import { useNavigate } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";

function DriverHomePage() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate("/login");
  };

  return (
    <div>
      <h1>Driver Application</h1>
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

export default DriverHomePage;
