import { Navigate, Outlet } from 'react-router-dom';
import { useMandateHub, type HubAccess } from '@/auth/MandateContext';
import { AuthLoadingCard } from '@/components/AuthLoadingCard';

export function RequireCapability({ allow }: { allow: (access: HubAccess) => boolean }) {
  const { access, loading } = useMandateHub();

  if (loading) {
    return <AuthLoadingCard message="Verificando mandato…" />;
  }

  if (!allow(access)) {
    return <Navigate to="/dashboard" replace />;
  }

  return <Outlet />;
}
