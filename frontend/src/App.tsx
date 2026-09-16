import { useEffect } from 'react';
import { BrowserRouter, Routes, Route, Navigate, useLocation, Outlet, useNavigate } from 'react-router-dom';
import { useAuthStore } from '@/store/authStore';
import { Header } from '@/components/Header';
import { ToastProvider } from '@/hooks/useToast';
import { AdminLayout } from '@/components/AdminLayout';

import { LoginPage } from '@/pages/Login';
import { RegisterPage } from '@/pages/Register';
import { HomePage } from '@/pages/Home';
import { ProtectedRoute } from '@/components/ProtectedRoute';
import { MovieDetailsPage } from '@/pages/MovieDetails';
import { BookingPage } from '@/pages/Booking';
import { BookingPaymentPage } from '@/pages/BookingPayment';
import { BookingSuccessPage } from '@/pages/BookingSuccess';
import { UserProfilePage } from '@/pages/UserProfile';

import { AdminDashboardPage } from '@/pages/AdminDashboard';
import { AdminHallsPage } from '@/pages/AdminHalls';
import { AdminSessionsPage } from '@/pages/AdminSessions';
import { AdminMoviesPage } from '@/pages/AdminMovies';
import { AdminStaffPage } from '@/pages/AdminStaff';
import { AdminCinemasPage } from '@/pages/AdminCinemas';
import { CashierScannerPage } from '@/pages/CashierScanner';
import { AdminAuditLogsPage } from '@/pages/AdminAuditLogs';
import { AdminPromoCodesPage } from '@/pages/AdminPromoCodes';

const AdminLayoutWrapper = () => (
  <AdminLayout>
    <Outlet />
  </AdminLayout>
);

function AppContent() {
  const { isAuthenticated } = useAuthStore();
  const location = useLocation();
  const navigate = useNavigate();

  const isAdminOrCashierPage = location.pathname.startsWith('/admin') || location.pathname.startsWith('/cashier');
  const isAuthPage = location.pathname === '/login' || location.pathname === '/register';

  return (
    <div className="min-h-screen bg-dark-bg text-white font-sans antialiased flex flex-col">
      {!isAdminOrCashierPage && <Header />}

      <main className={`flex-grow ${isAuthPage ? 'p-4 flex items-center justify-center' : ''}`}>
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/movies/:id" element={<MovieDetailsPage />} />

          <Route
            path="/login"
            element={!isAuthenticated ? <LoginPage onSwitchToRegister={() => navigate('/register')} /> : <Navigate to="/" replace />}
          />
          <Route
            path="/register"
            element={!isAuthenticated ? <RegisterPage onSwitchToLogin={() => navigate('/login')} /> : <Navigate to="/" replace />}
          />

          <Route element={<ProtectedRoute />}>
            <Route path="/profile" element={<UserProfilePage />} />
            <Route path="/booking/success" element={<BookingSuccessPage />} />
            <Route path="/booking/:sessionId" element={<BookingPage />} />
            <Route path="/booking/payment" element={<BookingPaymentPage />} />
          </Route>

          <Route element={<AdminLayoutWrapper />}>

            <Route element={<ProtectedRoute allowedRoles={['SuperAdmin', 'CinemaManager']} />}>
              <Route path="/admin/halls" element={<AdminHallsPage />} />
              <Route path="/admin/sessions" element={<AdminSessionsPage />} />
              <Route path="/admin/movies" element={<AdminMoviesPage />} />
            </Route>

            <Route element={<ProtectedRoute allowedRoles={['SuperAdmin', 'CinemaManager', 'Cashier']} />}>
              <Route path="/cashier/scan" element={<CashierScannerPage />} />
            </Route>

            <Route element={<ProtectedRoute allowedRoles={['SuperAdmin']} />}>
              <Route path="/admin/dashboard" element={<AdminDashboardPage />} />
              <Route path="/admin/staff" element={<AdminStaffPage />} />
              <Route path="/admin/cinemas" element={<AdminCinemasPage />} />
              <Route path="/admin/audit" element={<AdminAuditLogsPage />} />
              <Route path="/admin/promocodes" element={<AdminPromoCodesPage />} />
            </Route>

          </Route>

          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </main>
    </div>
  );
}

function App() {
  const checkAuth = useAuthStore((state) => state.checkAuth);
  const isInitialized = useAuthStore((state) => state.isInitialized);

  useEffect(() => {
    checkAuth();
  }, [checkAuth]);

  if (!isInitialized) {
    return (
      <div className="min-h-screen bg-dark-bg text-white flex items-center justify-center">
        <div className="w-8 h-8 border-4 border-accent-gold border-t-transparent rounded-full animate-spin"></div>
      </div>
    );
  }

  return (
    <BrowserRouter>
      <ToastProvider>
        <AppContent />
      </ToastProvider>
    </BrowserRouter>
  );
}

export default App;