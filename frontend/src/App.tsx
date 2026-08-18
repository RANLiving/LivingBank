import { Routes, Route } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import ProtectedRoute from './components/ProtectedRoute';
import Layout from './components/Layout';
import LoginPage from './pages/LoginPage';
import DashboardPage from './pages/DashboardPage';
import AccountDetailPage from './pages/AccountDetailPage';
import UsersPage from './pages/UsersPage';
import LogsPage from './pages/LogsPage';
import SchedulePage from './pages/SchedulePage';
import LinkAccountPage from './pages/LinkAccountPage';
import LinkCallbackPage from './pages/LinkCallbackPage';
import PrivacyPage from './pages/PrivacyPage';
import TermsPage from './pages/TermsPage';
import ChangePasswordPage from './pages/ChangePasswordPage';
import CompaniesPage from './pages/CompaniesPage';
import SetPasswordPage from './pages/SetPasswordPage';

export default function App() {
  return (
    <AuthProvider>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/privacy" element={<PrivacyPage />} />
        <Route path="/terms" element={<TermsPage />} />
        <Route path="/set-password" element={<SetPasswordPage />} />
        <Route
          element={
            <ProtectedRoute>
              <Layout />
            </ProtectedRoute>
          }
        >
          <Route path="/" element={<DashboardPage />} />
          <Route path="/change-password" element={<ChangePasswordPage />} />
          <Route path="/accounts/:id" element={<AccountDetailPage />} />
          <Route
            path="/link"
            element={
              <ProtectedRoute roles={['Admin']}>
                <LinkAccountPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/link/callback"
            element={
              <ProtectedRoute roles={['Admin']}>
                <LinkCallbackPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/companies"
            element={
              <ProtectedRoute roles={['Admin', 'Manager']}>
                <CompaniesPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/users"
            element={
              <ProtectedRoute roles={['Admin']}>
                <UsersPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/logs"
            element={
              <ProtectedRoute roles={['Admin']}>
                <LogsPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/schedule"
            element={
              <ProtectedRoute roles={['Admin']}>
                <SchedulePage />
              </ProtectedRoute>
            }
          />
        </Route>
      </Routes>
    </AuthProvider>
  );
}
