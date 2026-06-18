import { Link, useNavigate } from 'react-router-dom';
import type { TeamDetails } from '../../api/types';
import { Icon, IconButton } from '../../components/IconButton';
import { shortId } from '../../utils/format';
import { teamDetailPath, type TeamScope } from './teamPaths';

type TeamListItemProps = {
  team: TeamDetails;
  scope: TeamScope;
  operationId: string;
  onDelete?: (teamId: string) => void;
  deleteBusy?: boolean;
};

function StatCell({
  label,
  value,
  warn = false,
  title,
}: {
  label: string;
  value: number | string;
  warn?: boolean;
  title?: string;
}) {
  return (
    <div className={`ops-list-row__stat${warn ? ' ops-list-row__stat--warn' : ''}`}>
      <span className="ops-list-row__stat-value" title={title}>{value}</span>
      <span className="ops-list-row__stat-label">{label}</span>
    </div>
  );
}

function gatewayStrategyLabel(strategy: NonNullable<TeamDetails['gatewaySelectionStrategy']>): string {
  if (strategy === 'PerStrawman') return 'Laranja';
  if (strategy === 'PerGroup') return 'Grupo';
  return 'Manual';
}

function leaderLabel(team: TeamDetails): string {
  if (!team.teamLeader) return '—';
  const { username, accountId } = team.teamLeader;
  return username && username !== accountId ? username : shortId(accountId, 10);
}

export function TeamListItem({
  team,
  scope,
  operationId,
  onDelete,
  deleteBusy = false,
}: TeamListItemProps) {
  const navigate = useNavigate();
  const href = teamDetailPath(scope, operationId, team.id);
  const hasLeader = Boolean(team.teamLeader);
  const gatewayLabel = team.gatewaySelectionStrategy
    ? gatewayStrategyLabel(team.gatewaySelectionStrategy)
    : '—';

  function stopRowNav(event: React.MouseEvent | React.KeyboardEvent) {
    event.stopPropagation();
  }

  async function copyId(event: React.MouseEvent) {
    stopRowNav(event);
    try {
      await navigator.clipboard.writeText(team.id);
    } catch {
      // ignore
    }
  }

  function handleDelete(event: React.MouseEvent) {
    stopRowNav(event);
    onDelete?.(team.id);
  }

  function openDetail() {
    navigate(href);
  }

  function handleRowClick(event: React.MouseEvent<HTMLElement>) {
    if ((event.target as HTMLElement).closest('.ops-list-row__actions, .icon-btn, .ops-list-row__id-line')) return;
    openDetail();
  }

  return (
    <article className="ops-list-row ops-list-row--team" onClick={handleRowClick}>
      <div className="ops-list-row__grid">
        <div className="ops-list-row__identity">
          <span className="ops-list-row__kicker">Equipe</span>
          <h3 className="ops-list-row__title">{team.name}</h3>
          <div className="ops-list-row__id-line">
            <span className="ops-list-row__id ops-list-row__id--inline mono muted small" title={team.id}>
              ID {shortId(team.id, 16)}
            </span>
            <IconButton
              icon="copy"
              label="Copiar ID da equipe"
              onClick={copyId}
            />
          </div>
        </div>

        <div className="ops-list-row__stats" aria-label="Resumo da equipe">
          <StatCell
            label="Líder"
            value={leaderLabel(team)}
            warn={!hasLeader}
            title={team.teamLeader ? leaderLabel(team) : undefined}
          />
          {scope === 'global-admin' ? (
            <StatCell label="Ops" value={team.operators.length} />
          ) : null}
          <StatCell label="Gateway" value={gatewayLabel} />
        </div>

        <div className="ops-list-row__actions" onClick={stopRowNav} onKeyDown={stopRowNav}>
          {onDelete ? (
            <IconButton
              icon="trash"
              label={`Excluir equipe ${team.name}`}
              variant="danger"
              disabled={deleteBusy}
              onClick={handleDelete}
            />
          ) : null}
          <Link to={href} className="btn btn-ghost btn-small ops-list-row__detail-btn" onClick={stopRowNav}>
            <span className="ops-list-row__detail-label">Detalhes</span>
            <Icon name="chevron-right" />
          </Link>
        </div>
      </div>
    </article>
  );
}
