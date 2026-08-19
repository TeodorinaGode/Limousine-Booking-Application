import { Link, useNavigate } from "react-router-dom";
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
      <nav>
        <Link to="/driver/availability">My Availability</Link>
      </nav>
      <button type="button" onClick={handleLogout}>
        Logout
      </button>
    </div>
  );
}

export default DriverHomePage;
