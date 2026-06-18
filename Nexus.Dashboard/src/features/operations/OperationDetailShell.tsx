import { Link } from 'react-router-dom';
import type { ReactNode } from 'react';
import { EmptyState } from '../../components/EmptyState';

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
      <section className="page-header ops-page-header--compact">
        <p className="muted small page-nav-back">
          <Link to={listPath}>← {listLabel}</Link>
        </p>
      </section>

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
