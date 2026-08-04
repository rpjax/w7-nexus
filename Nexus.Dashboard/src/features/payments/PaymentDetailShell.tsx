import type { ReactNode } from 'react';
import { PageHeader } from '@/components/layout/page-header';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { Skeleton } from '@/components/ui/skeleton';

type PaymentDetailShellProps = {
  kicker?: string;
  kickerVariant?: 'admin' | 'default';
  title?: string;
  description?: string;
  breadcrumbs: Array<{ label: string; href?: string }>;
  loading: boolean;
  error?: string | null;
  notFound?: boolean;
  children: ReactNode;
};

export function PaymentDetailShell({
  kicker,
  kickerVariant,
  title = 'Detalhe do pagamento',
  description,
  breadcrumbs,
  loading,
  error,
  notFound,
  children,
}: PaymentDetailShellProps) {
  return (
    <div className="flex min-h-0 flex-1 flex-col gap-3">
      <PageHeader
        kicker={kicker}
        kickerVariant={kickerVariant}
        title={title}
        description={description}
        breadcrumbs={breadcrumbs}
      />

      <div className="min-h-0 flex-1 overflow-y-auto">
        {loading ? (
          <div className="space-y-3">
            <Skeleton className="h-24 w-full" />
            <Skeleton className="h-48 w-full" />
            <Skeleton className="h-32 w-full" />
          </div>
        ) : error ? (
          <Alert variant="destructive">
            <AlertTitle>Não foi possível carregar o pagamento</AlertTitle>
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        ) : notFound ? (
          <Alert>
            <AlertTitle>Pagamento não encontrado</AlertTitle>
            <AlertDescription>
              O pagamento não existe ou você não tem acesso a ele.
            </AlertDescription>
          </Alert>
        ) : (
          children
        )}
      </div>
    </div>
  );
}
