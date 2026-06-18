import type { ReactNode } from 'react';
import { EmptyState } from '../../components/EmptyState';
import { PageHeading } from '../../layouts/PageHeading';

const OPERATION_DETAIL_TITLE = 'Gerenciamento de operação';

type OperationDetailShellProps = {
  listPath: string;
  listLabel?: string;
  loading: boolean;
  notFound: boolean;
  children: ReactNode;
};

export function OperationDetailShell({
  listPath,
  listLabel = 'Voltar à listagem',
  loading,
  notFound,
  children,
}: OperationDetailShellProps) {
  return (
    <div className="ops-page ops-detail-page">
      <PageHeading
        title={loading ? 'Carregando…' : OPERATION_DETAIL_TITLE}
        backLink={{ to: listPath, label: listLabel }}
      />

      <div className="ops-detail">
        {loading ? (
          <p className="muted ops-detail__status">Carregando operação…</p>
        ) : notFound ? (
          <EmptyState
            title="Operação não encontrada"
            message="A operação não existe ou você não tem acesso a ela."
          />
        ) : (
          children
        )}
      </div>
    </div>
  );
}
