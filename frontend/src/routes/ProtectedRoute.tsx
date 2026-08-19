import type { ReactNode } from "react";
import { Navigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import type { UserRole } from "../types/auth";

interface ProtectedRouteProps {
  allowedRoles: UserRole[];
  children: ReactNode;
}

/**
 * Frontend-only route protection for UX purposes (redirect to /login, show an
 * unauthorized page). The backend enforces [Authorize(Roles = ...)] on every
 * protected endpoint regardless of what the frontend does — this guard is not
 * the security boundary.
 */
function ProtectedRoute({ allowedRoles, children }: ProtectedRouteProps) {
  const { isAuthenticated, user } = useAuth();

  if (!isAuthenticated || !user) {
    return <Navigate to="/login" replace />;
  }

  if (!allowedRoles.includes(user.role)) {
    return <Navigate to="/unauthorized" replace />;
  }

  return <>{children}</>;
}

export default ProtectedRoute;
