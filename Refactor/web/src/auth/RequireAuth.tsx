import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '@/auth/AuthContext';
import { AuthLoadingCard } from '@/components/AuthLoadingCard';

export function RequireAuth() {
  const { isAuthenticated, isInitializing } = useAuth();
  const location = useLocation();

  if (isInitializing) {
    return <AuthLoadingCard message="Verificando sessão…" />;
  }

  if (!isAuthenticated) {
    const redirect = encodeURIComponent(`${location.pathname}${location.search}`);
    return <Navigate to={`/auth?redirect=${redirect}`} replace />;
  }

  return <Outlet />;
}

export function RedirectIfAuthenticated() {
  const { isAuthenticated, isInitializing } = useAuth();
  const location = useLocation();

  if (isInitializing) {
    return <AuthLoadingCard message="Verificando sessão…" />;
  }

  if (isAuthenticated) {
    const params = new URLSearchParams(location.search);
    const redirect = params.get('redirect');
    const safeRedirect = redirect?.startsWith('/') ? redirect : '/dashboard';
    return <Navigate to={safeRedirect} replace />;
  }

  return <Outlet />;
}
