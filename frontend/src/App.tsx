import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClientProvider } from '@tanstack/react-query';
import { queryClient } from './lib/queryClient';
import LoginPage from './pages/auth/LoginPage';
import RegisterPage from './pages/auth/RegisterPage';
import ForgotPasswordPage from './pages/auth/ForgotPasswordPage';
import ResetPasswordPage from './pages/auth/ResetPasswordPage';
import VerifyEmailPage from './pages/auth/VerifyEmailPage';
import DashboardPage from './pages/admin/DashboardPage';
import EmployeesPage from './pages/admin/EmployeesPage';
import EmployeeDetailPage from './pages/admin/EmployeeDetailPage';
import DocumentsPage from './pages/admin/DocumentsPage';
import AuditPage from './pages/admin/AuditPage';
import BackupPage from './pages/admin/BackupPage';
import ProfilePage from './pages/admin/ProfilePage';
import PlansPage from './pages/admin/PlansPage';
import SettingsPage from './pages/admin/SettingsPage';
import SuperAdminPage from './pages/admin/SuperAdminPage';
import SignPage from './pages/sign/SignPage';
import PrivacyPolicyPage from './pages/public/PrivacyPolicyPage';
import AdminLayout from './components/layout/AdminLayout';
import { useAuthStore } from './stores/auth';

function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const token = useAuthStore((s) => s.token);
  if (!token) return <Navigate to="/login" replace />;
  return <>{children}</>;
}

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Routes>
          {/* Public */}
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/forgot-password" element={<ForgotPasswordPage />} />
          <Route path="/reset-password" element={<ResetPasswordPage />} />
          <Route path="/verify-email" element={<VerifyEmailPage />} />
          <Route path="/sign/:token" element={<SignPage />} />
          <Route path="/privacy" element={<PrivacyPolicyPage />} />

          {/* Protected Admin */}
          <Route
            path="/admin"
            element={
              <ProtectedRoute>
                <AdminLayout />
              </ProtectedRoute>
            }
          >
            <Route index element={<DashboardPage />} />
            <Route path="employees" element={<EmployeesPage />} />
            <Route path="employees/:id" element={<EmployeeDetailPage />} />
            <Route path="documents" element={<DocumentsPage />} />
            <Route path="audit" element={<AuditPage />} />
            <Route path="backup" element={<BackupPage />} />
            <Route path="profile" element={<ProfilePage />} />
            <Route path="plans" element={<PlansPage />} />
            <Route path="settings" element={<SettingsPage />} />
            <Route path="super" element={<SuperAdminPage />} />
          </Route>

          {/* Fallback */}
          <Route path="*" element={<Navigate to="/login" replace />} />
        </Routes>
      </BrowserRouter>
    </QueryClientProvider>
  );
}

export default App;
