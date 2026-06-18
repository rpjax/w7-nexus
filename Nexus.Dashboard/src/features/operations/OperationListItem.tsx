import { Link, useNavigate } from 'react-router-dom';
import type { OperationDetails, OperationWithLedTeamsDetails } from '../../api/types';
import { Icon, IconButton } from '../../components/IconButton';
import { formatDateTime, shortId } from '../../utils/format';
import { detailPath, type OperationScope } from './operationPaths';

type OperationListItemProps = {
  operation: OperationDetails | OperationWithLedTeamsDetails;
  scope: OperationScope;
  onDelete?: (operationId: string) => void;
  deleteBusy?: boolean;
};

function countOperators(teams: OperationDetails['teams']): number {
  return teams.reduce((sum, team) => sum + team.operators.length, 0);
}

function countViewerTeams(operation: OperationDetails, scope: OperationScope): number {
  if (scope === 'operator') {
    return operation.teams.length;
  }
  return operation.teams?.length ?? 0;
}

function StatCell({
  label,
  value,
  warn = false,
}: {
  label: string;
  value: number | string;
  warn?: boolean;
}) {
  return (
    <div className={`ops-list-row__stat${warn ? ' ops-list-row__stat--warn' : ''}`}>
      <span className="ops-list-row__stat-value">{value}</span>
      <span className="ops-list-row__stat-label">{label}</span>
    </div>
  );
}

export function OperationListItem({
  operation,
  scope,
  onDelete,
  deleteBusy = false,
}: OperationListItemProps) {
  const navigate = useNavigate();
  const href = detailPath(scope, operation.id);
  const description = operation.description?.trim();
  const adminCount = 'administrators' in operation ? (operation.administrators?.length ?? 0) : 0;
  const teamCount = countViewerTeams(operation as OperationDetails, scope);
  const operatorCount = countOperators((operation as OperationDetails).teams ?? []);

  function stopRowNav(event: React.MouseEvent | React.KeyboardEvent) {
    event.stopPropagation();
  }

  async function copyId(event: React.MouseEvent) {
    stopRowNav(event);
    try {
      await navigator.clipboard.writeText(operation.id);
    } catch {
      // ignore
    }
  }

  function handleDelete(event: React.MouseEvent) {
    stopRowNav(event);
    onDelete?.(operation.id);
  }

  function openDetail() {
    navigate(href);
  }

  function handleRowClick(event: React.MouseEvent<HTMLElement>) {
    if ((event.target as HTMLElement).closest('.ops-list-row__actions')) return;
    openDetail();
  }

  return (
    <article
      className="ops-list-row"
      onClick={handleRowClick}
    >
      <div className="ops-list-row__grid">
        <div className="ops-list-row__identity">
          <span className="ops-list-row__kicker">Operação</span>
          <h3 className="ops-list-row__title">{operation.name}</h3>
          <p className="ops-list-row__desc muted small">
            {description || 'Sem descrição cadastrada.'}
          </p>
        </div>

        <div className="ops-list-row__stats" aria-label="Resumo">
          {scope === 'global-admin' ? (
            <StatCell label="Admins" value={adminCount} warn={adminCount === 0} />
          ) : null}
          <StatCell label="Equipes" value={teamCount} />
          {scope !== 'operation-admin' ? (
            <StatCell label="Operadores" value={operatorCount} />
          ) : null}
        </div>

        <div className="ops-list-row__meta muted small">
          <time dateTime={operation.updatedAt}>{formatDateTime(operation.updatedAt)}</time>
          <span className="ops-list-row__meta-sep" aria-hidden="true">·</span>
          <span className="ops-list-row__id mono" title={operation.id}>
            {shortId(operation.id, 16)}
          </span>
        </div>

        <div className="ops-list-row__actions" onClick={stopRowNav} onKeyDown={stopRowNav}>
          <IconButton
            icon="copy"
            label="Copiar ID da operação"
            onClick={copyId}
          />
          {scope === 'global-admin' && onDelete ? (
            <IconButton
              icon="trash"
              label={`Excluir operação ${operation.name}`}
              variant="danger"
              disabled={deleteBusy}
              onClick={handleDelete}
            />
          ) : null}
          <Link to={href} className="btn btn-ghost btn-small ops-list-row__detail-btn" onClick={stopRowNav}>
            Detalhes
            <Icon name="chevron-right" />
          </Link>
        </div>
      </div>
    </article>
  );
}
