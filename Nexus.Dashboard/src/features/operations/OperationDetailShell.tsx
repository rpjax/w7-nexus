import type { ReactNode } from 'react';
import { PageHeader } from '@/components/layout/page-header';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { Skeleton } from '@/components/ui/skeleton';
import { listPath, type OperationScope } from '@/features/operations/operationPaths';

type OperationDetailShellProps = {
  scope: OperationScope;
  listLabel?: string;
  operationName?: string | null;
  loading: boolean;
  notFound: boolean;
  children: ReactNode;
};

const SCOPE_LIST_LABELS: Record<OperationScope, string> = {
  'global-admin': 'Todas as operações',
  'operation-admin': 'Administração de operações',
  'team-leader': 'Liderança de equipes',
  operator: 'Minhas operações',
};

export function OperationDetailShell({
  scope,
  listLabel,
  operationName,
  loading,
  notFound,
  children,
}: OperationDetailShellProps) {
  const operationsPath = listPath(scope);
  const resolvedListLabel = listLabel ?? SCOPE_LIST_LABELS[scope];

  return (
    <div className="flex min-h-0 flex-1 flex-col gap-6">
      <PageHeader
        title={loading ? 'Carregando…' : operationName ?? 'Gerenciamento de operação'}
        description={!loading && !notFound && operationName
          ? 'Configure equipes, operadores, repasses e credenciais de gateway.'
          : undefined}
        breadcrumbs={[
          { label: 'Dashboard', href: '/dashboard' },
          { label: resolvedListLabel, href: operationsPath },
          { label: loading ? '…' : operationName ?? 'Operação' },
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
            <AlertTitle>Operação não encontrada</AlertTitle>
            <AlertDescription>
              A operação não existe ou você não tem acesso a ela.
            </AlertDescription>
          </Alert>
        ) : (
          children
        )}
      </div>
    </div>
  );
}
