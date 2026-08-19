import { Route, Routes } from "react-router-dom";
import HomePage from "../pages/public/HomePage";
import BookingPage from "../pages/public/BookingPage";
import LoginPage from "../pages/auth/LoginPage";
import UnauthorizedPage from "../pages/public/UnauthorizedPage";
import DriverHomePage from "../pages/driver/DriverHomePage";
import AvailabilityPage from "../pages/driver/AvailabilityPage";
import AdminHomePage from "../pages/admin/AdminHomePage";
import RoutesPage from "../pages/admin/routes/RoutesPage";
import VehiclesPage from "../pages/admin/vehicles/VehiclesPage";
import DriversPage from "../pages/admin/drivers/DriversPage";
import DriverDetailsPage from "../pages/admin/drivers/DriverDetailsPage";
import BookingsPage from "../pages/admin/bookings/BookingsPage";
import BookingDetailPage from "../pages/admin/bookings/BookingDetailPage";
import ProtectedRoute from "./ProtectedRoute";

function AppRoutes() {
  return (
    <Routes>
      <Route path="/" element={<HomePage />} />
      <Route path="/booking" element={<BookingPage />} />
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
        path="/driver/availability"
        element={
          <ProtectedRoute allowedRoles={["Driver"]}>
            <AvailabilityPage />
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
      <Route
        path="/admin/routes"
        element={
          <ProtectedRoute allowedRoles={["Administrator"]}>
            <RoutesPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/admin/vehicles"
        element={
          <ProtectedRoute allowedRoles={["Administrator"]}>
            <VehiclesPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/admin/drivers"
        element={
          <ProtectedRoute allowedRoles={["Administrator"]}>
            <DriversPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/admin/drivers/:id"
        element={
          <ProtectedRoute allowedRoles={["Administrator"]}>
            <DriverDetailsPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/admin/bookings"
        element={
          <ProtectedRoute allowedRoles={["Administrator"]}>
            <BookingsPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/admin/bookings/:id"
        element={
          <ProtectedRoute allowedRoles={["Administrator"]}>
            <BookingDetailPage />
          </ProtectedRoute>
        }
      />
    </Routes>
  );
}

export default AppRoutes;
