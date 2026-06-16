import { Navigate, Outlet } from 'react-router-dom';
import { useOperationCapabilities } from './OperationCapabilitiesContext';

function ScopeLoading() {
  return (
    <div className="auth-loading" role="status" aria-live="polite">
      <div className="auth-loading-card card">
        <p className="auth-loading-title">Nexus</p>
        <p className="muted">Verificando permissões…</p>
      </div>
    </div>
  );
}

export function RequireOperationAdministratorScope() {
  const { operationAdministrator, loading } = useOperationCapabilities();

  if (loading) return <ScopeLoading />;
  if (!operationAdministrator) return <Navigate to="/dashboard" replace />;

  return <Outlet />;
}

export function RequireTeamLeaderScope() {
  const { teamLeader, loading } = useOperationCapabilities();

  if (loading) return <ScopeLoading />;
  if (!teamLeader) return <Navigate to="/dashboard" replace />;

  return <Outlet />;
}
