import { Route, Routes } from "react-router-dom";
import HomePage from "../pages/public/HomePage";
import LoginPage from "../pages/auth/LoginPage";
import DriverHomePage from "../pages/driver/DriverHomePage";
import AdminHomePage from "../pages/admin/AdminHomePage";

function AppRoutes() {
  return (
    <Routes>
      <Route path="/" element={<HomePage />} />
      <Route path="/login" element={<LoginPage />} />
      <Route path="/driver" element={<DriverHomePage />} />
      <Route path="/admin" element={<AdminHomePage />} />
    </Routes>
  );
}

export default AppRoutes;
