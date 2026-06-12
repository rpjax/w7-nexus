import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from './AuthContext';
import { hasAnyRole, hasRole, type AppRole } from './roles';

type RequireRoleProps = {
  role: AppRole;
};

type RequireAnyRoleProps = {
  roles: AppRole[];
};

function RoleGate({ allowed }: { allowed: boolean }) {
  const { isInitializing } = useAuth();

  if (isInitializing) {
    return (
      <div className="auth-loading" role="status" aria-live="polite">
        <div className="auth-loading-card card">
          <p className="auth-loading-title">Nexus</p>
          <p className="muted">Verificando permissões…</p>
        </div>
      </div>
    );
  }

  if (!allowed) {
    return <Navigate to="/dashboard" replace />;
  }

  return <Outlet />;
}

export function RequireRole({ role }: RequireRoleProps) {
  const { user } = useAuth();
  return <RoleGate allowed={hasRole(user, role)} />;
}

export function RequireAnyRole({ roles }: RequireAnyRoleProps) {
  const { user } = useAuth();
  return <RoleGate allowed={hasAnyRole(user, roles)} />;
}
