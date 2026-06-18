import type { ReactNode } from 'react';
import { detailPath } from '../operations/operationPaths';
import type { TeamScope } from './teamPaths';
import { EmptyState } from '../../components/EmptyState';
import { PageHeading } from '../../layouts/PageHeading';

const TEAM_DETAIL_TITLE = 'Gerenciamento de equipe';

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
      <PageHeading
        title={loading ? 'Carregando…' : TEAM_DETAIL_TITLE}
        subtitle={!loading && !notFound && operationName ? `Operação · ${operationName}` : undefined}
        backLink={{ to: operationPath, label: 'Voltar à operação' }}
      />

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
