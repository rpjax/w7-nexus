import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from './AuthContext';

export function RequireAuth() {
  const { isAuthenticated, isInitializing } = useAuth();
  const location = useLocation();

  if (isInitializing) {
    return (
      <div className="auth-loading" role="status" aria-live="polite">
        <div className="auth-loading-card card">
          <p className="auth-loading-title">Nexus</p>
          <p className="muted">Verificando sessão…</p>
        </div>
      </div>
    );
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
    return (
      <div className="auth-loading" role="status" aria-live="polite">
        <div className="auth-loading-card card">
          <p className="auth-loading-title">Nexus</p>
          <p className="muted">Verificando sessão…</p>
        </div>
      </div>
    );
  }

  if (isAuthenticated) {
    const params = new URLSearchParams(location.search);
    const redirect = params.get('redirect');
    const safeRedirect = redirect?.startsWith('/') ? redirect : '/dashboard';
    return <Navigate to={safeRedirect} replace />;
  }

  return <Outlet />;
}
