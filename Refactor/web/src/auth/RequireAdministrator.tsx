import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '@/auth/AuthContext';
import { AuthLoadingCard } from '@/components/AuthLoadingCard';
import { isAdministrator } from '@/utils/accountAccess';

export function RequireAdministrator() {
  const { user, isInitializing } = useAuth();

  if (isInitializing) {
    return <AuthLoadingCard message="Verificando sessão…" />;
  }

  if (!isAdministrator(user?.roles)) {
    return <Navigate to="/dashboard" replace />;
  }

  return <Outlet />;
}
