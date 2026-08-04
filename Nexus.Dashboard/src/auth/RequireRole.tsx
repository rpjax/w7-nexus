import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from './AuthContext';
import { hasAnyRole, hasRole, type AppRole } from './roles';
import { AuthLoadingCard } from '@/components/AuthLoadingCard';

type RequireRoleProps = {
  role: AppRole;
};

type RequireAnyRoleProps = {
  roles: AppRole[];
};

function RoleGate({ allowed }: { allowed: boolean }) {
  const { isInitializing } = useAuth();

  if (isInitializing) {
    return <AuthLoadingCard message="Verificando permissões…" />;
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
