import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import { getMyProfile } from "../../services/driverBookingService";
import type { DriverDto } from "../../types/driver";

function ProfilePage() {
  const { accessToken } = useAuth();

  const [profile, setProfile] = useState<DriverDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    if (!accessToken) return;
    (async () => {
      setIsLoading(true);
      setError(null);
      try {
        setProfile(await getMyProfile(accessToken));
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to load your profile.");
      } finally {
        setIsLoading(false);
      }
    })();
  }, [accessToken]);

  return (
    <div>
      <p>
        <Link to="/driver">&larr; Back to Dashboard</Link>
      </p>
      <h1>My Profile</h1>

      {error && <p role="alert">{error}</p>}
      {isLoading && <p>Loading profile...</p>}

      {profile && (
        <>
          <section style={{ marginBottom: "1.5rem" }}>
            <h2>Contact</h2>
            <p>
              {profile.firstName} {profile.lastName}
              <br />
              {profile.email}
              <br />
              {profile.phone}
            </p>
          </section>

          <section style={{ marginBottom: "1.5rem" }}>
            <h2>Status</h2>
            <p>Account: {profile.isActive ? "Active" : "Inactive"}</p>
            <p>Availability: {profile.isAvailable ? "Available" : "Unavailable"}</p>
          </section>

          <section style={{ marginBottom: "1.5rem" }}>
            <h2>Vehicle</h2>
            {profile.vehicle ? (
              <p>
                {profile.vehicle.make} {profile.vehicle.model} &mdash; {profile.vehicle.registrationNumber}
              </p>
            ) : (
              <p>No vehicle assigned.</p>
            )}
          </section>
        </>
      )}
    </div>
  );
}

export default ProfilePage;
