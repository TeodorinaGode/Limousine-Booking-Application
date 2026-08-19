import { Route, Routes } from "react-router-dom";
import HomePage from "../pages/public/HomePage";
import LoginPage from "../pages/auth/LoginPage";
import UnauthorizedPage from "../pages/public/UnauthorizedPage";
import DriverHomePage from "../pages/driver/DriverHomePage";
import AdminHomePage from "../pages/admin/AdminHomePage";
import ProtectedRoute from "./ProtectedRoute";

function AppRoutes() {
  return (
    <Routes>
      <Route path="/" element={<HomePage />} />
      <Route path="/login" element={<LoginPage />} />
      <Route path="/unauthorized" element={<UnauthorizedPage />} />
      <Route
        path="/driver"
        element={
          <ProtectedRoute allowedRoles={["Driver"]}>
            <DriverHomePage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/admin"
        element={
          <ProtectedRoute allowedRoles={["Administrator"]}>
            <AdminHomePage />
          </ProtectedRoute>
        }
      />
    </Routes>
  );
}

export default AppRoutes;
