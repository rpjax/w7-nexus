import { Link } from 'react-router-dom';
import type { ReactNode } from 'react';
import { detailPath } from '../operations/operationPaths';
import type { TeamScope } from './teamPaths';
import { EmptyState } from '../../components/EmptyState';

type TeamDetailShellProps = {
  scope: TeamScope;
  operationId: string;
  operationName?: string;
  loading: boolean;
  notFound: boolean;
  children: ReactNode;
};

export function TeamDetailShell({
  scope,
  operationId,
  operationName,
  loading,
  notFound,
  children,
}: TeamDetailShellProps) {
  const operationPath = detailPath(scope, operationId);

  return (
    <div className="ops-page ops-detail-page">
      <section className="page-header ops-page-header--compact">
        <p className="muted small page-nav-back">
          <Link to={operationPath}>← Voltar à operação</Link>
        </p>
        {operationName ? (
          <p className="muted small ops-team-detail__operation">{operationName}</p>
        ) : null}
      </section>

      <div className="ops-detail">
        {loading ? (
          <p className="muted ops-detail__status">Carregando equipe…</p>
        ) : notFound ? (
          <EmptyState
            title="Equipe não encontrada"
            message="A equipe não existe ou você não tem acesso a ela."
          />
        ) : (
          children
        )}
      </div>
    </div>
  );
}
