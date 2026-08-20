import { useEffect, useState } from "react";
import { useAuth } from "../../context/AuthContext";
import { getMyProfile } from "../../services/driverBookingService";
import DriverNav from "../../components/DriverNav";
import PageHeader from "../../components/PageHeader";
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
    <div className="app-shell">
      <DriverNav />
      <main className="app-main app-main--narrow">
      <PageHeader title="My Profile" />

      {error && <p role="alert">{error}</p>}
      {isLoading && <div className="skeleton skeleton-line" style={{ height: 40, maxWidth: 300 }} />}

      {profile && (
        <>
          <section className="card" style={{ marginBottom: "1.5rem" }}>
            <h2>Contact</h2>
            <p>
              {profile.firstName} {profile.lastName}
              <br />
              {profile.email}
              <br />
              {profile.phone}
            </p>
          </section>

          <section className="card" style={{ marginBottom: "1.5rem" }}>
            <h2>Status</h2>
            <p>Account: {profile.isActive ? "Active" : "Inactive"}</p>
            <p>Availability: {profile.isAvailable ? "Available" : "Unavailable"}</p>
          </section>

          <section className="card">
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
      </main>
    </div>
  );
}

export default ProfilePage;
