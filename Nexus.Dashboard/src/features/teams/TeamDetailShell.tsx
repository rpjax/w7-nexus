import type { ReactNode } from 'react';
import { PageHeader } from '@/components/layout/page-header';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { Skeleton } from '@/components/ui/skeleton';
import { detailPath, listPath } from '@/features/operations/operationPaths';
import type { TeamScope } from './teamPaths';

type TeamDetailShellProps = {
  scope: TeamScope;
  operationId: string;
  operationName?: string | null;
  teamName?: string | null;
  loading: boolean;
  notFound: boolean;
  children: ReactNode;
};

const SCOPE_LIST_LABELS: Record<TeamScope, string> = {
  'global-admin': 'Todas as operações',
  'operation-admin': 'Administração de operações',
};

export function TeamDetailShell({
  scope,
  operationId,
  operationName,
  teamName,
  loading,
  notFound,
  children,
}: TeamDetailShellProps) {
  const operationsPath = listPath(scope);
  const operationPath = detailPath(scope, operationId);

  return (
    <div className="flex min-h-0 flex-1 flex-col gap-6">
      <PageHeader
        title={loading ? 'Carregando…' : teamName ?? 'Gerenciamento de equipe'}
        description={!loading && !notFound && operationName
          ? `Operação · ${operationName}`
          : undefined}
        breadcrumbs={[
          { label: 'Dashboard', href: '/dashboard' },
          { label: SCOPE_LIST_LABELS[scope], href: operationsPath },
          { label: operationName ?? 'Operação', href: operationPath },
          { label: loading ? '…' : teamName ?? 'Equipe' },
        ]}
      />

      <div className="min-h-0 flex-1 overflow-y-auto">
        {loading ? (
          <div className="space-y-3">
            <Skeleton className="h-16 w-full" />
            <Skeleton className="h-32 w-full" />
          </div>
        ) : notFound ? (
          <Alert>
            <AlertTitle>Equipe não encontrada</AlertTitle>
            <AlertDescription>
              A equipe não existe ou você não tem acesso a ela.
            </AlertDescription>
          </Alert>
        ) : (
          children
        )}
      </div>
    </div>
  );
}
