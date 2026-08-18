import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import type { ReactNode } from 'react';

export default function ProtectedRoute({ children, roles }: { children: ReactNode; roles?: string[] }) {
  const { user, loading, hasRole } = useAuth();
  const location = useLocation();

  if (loading) return <div className="lb-main">A carregar…</div>;
  if (!user) return <Navigate to="/login" replace />;
  if (user.passwordExpired && location.pathname !== '/change-password') {
    return <Navigate to="/change-password" replace />;
  }
  if (roles && !hasRole(...roles)) return <Navigate to="/" replace />;

  return <>{children}</>;
}
