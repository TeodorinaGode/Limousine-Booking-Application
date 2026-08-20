import { Route, Routes } from "react-router-dom";
import HomePage from "../pages/public/HomePage";
import ServicesPage from "../pages/public/ServicesPage";
import PublicRoutesPage from "../pages/public/RoutesPage";
import FleetPage from "../pages/public/FleetPage";
import AboutPage from "../pages/public/AboutPage";
import FAQPage from "../pages/public/FAQPage";
import ContactPage from "../pages/public/ContactPage";
import LegalPage from "../pages/public/LegalPage";
import BookingPage from "../pages/public/BookingPage";
import PaymentStatusPage from "../pages/public/PaymentStatusPage";
import PaymentSuccessPage from "../pages/public/PaymentSuccessPage";
import PaymentCancelledPage from "../pages/public/PaymentCancelledPage";
import LoginPage from "../pages/auth/LoginPage";
import UnauthorizedPage from "../pages/public/UnauthorizedPage";
import NotFoundPage from "../pages/public/NotFoundPage";
import DriverHomePage from "../pages/driver/DriverHomePage";
import AvailabilityPage from "../pages/driver/AvailabilityPage";
import SchedulePage from "../pages/driver/SchedulePage";
import TripDetailPage from "../pages/driver/TripDetailPage";
import ProfilePage from "../pages/driver/ProfilePage";
import AdminHomePage from "../pages/admin/AdminHomePage";
import RoutesPage from "../pages/admin/routes/RoutesPage";
import VehiclesPage from "../pages/admin/vehicles/VehiclesPage";
import DriversPage from "../pages/admin/drivers/DriversPage";
import DriverDetailsPage from "../pages/admin/drivers/DriverDetailsPage";
import BookingsPage from "../pages/admin/bookings/BookingsPage";
import BookingDetailPage from "../pages/admin/bookings/BookingDetailPage";
import ReportsPage from "../pages/admin/reports/ReportsPage";
import ProtectedRoute from "./ProtectedRoute";

function AppRoutes() {
  return (
    <Routes>
      <Route path="/" element={<HomePage />} />
      <Route path="/services" element={<ServicesPage />} />
      <Route path="/routes" element={<PublicRoutesPage />} />
      <Route path="/fleet" element={<FleetPage />} />
      <Route path="/about" element={<AboutPage />} />
      <Route path="/faq" element={<FAQPage />} />
      <Route path="/contact" element={<ContactPage />} />
      <Route path="/privacy-policy" element={<LegalPage titleKey="privacyTitle" />} />
      <Route path="/terms-and-conditions" element={<LegalPage titleKey="termsTitle" />} />
      <Route path="/cookie-policy" element={<LegalPage titleKey="cookieTitle" />} />
      <Route path="/booking" element={<BookingPage />} />
      <Route path="/booking/payment/success" element={<PaymentSuccessPage />} />
      <Route path="/booking/payment/cancelled" element={<PaymentCancelledPage />} />
      <Route path="/booking/payment/:bookingReference" element={<PaymentStatusPage />} />
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
        path="/driver/schedule"
        element={
          <ProtectedRoute allowedRoles={["Driver"]}>
            <SchedulePage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/driver/bookings/:id"
        element={
          <ProtectedRoute allowedRoles={["Driver"]}>
            <TripDetailPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/driver/profile"
        element={
          <ProtectedRoute allowedRoles={["Driver"]}>
            <ProfilePage />
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
      <Route
        path="/admin/reports"
        element={
          <ProtectedRoute allowedRoles={["Administrator"]}>
            <ReportsPage />
          </ProtectedRoute>
        }
      />
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
}

export default AppRoutes;
