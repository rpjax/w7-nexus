import type { TeamDetails } from '../../api/types';
import { shortId } from '../../utils/format';
import { AdminTeamGatewaySection } from './AdminTeamGatewaySection';
import type { AdminTeamPanelActions, AdminTeamPanelScope } from './adminTeamTypes';

export type { AdminTeamPanelActions, AdminTeamPanelScope };

type AdminTeamPanelProps = {
  team: TeamDetails;
  scope: AdminTeamPanelScope;
  actions: AdminTeamPanelActions;
  index?: number;
};

function personLabel(accountId: string, username: string): string {
  return username && username !== accountId ? username : shortId(accountId, 18);
}

function formatPercent(value: number): string {
  const rounded = Math.round(value * 100) / 100;
  return Number.isInteger(rounded) ? `${rounded}%` : `${rounded.toFixed(1)}%`;
}

function personInitial(username: string): string {
  const trimmed = username.trim();
  return trimmed ? trimmed[0]!.toUpperCase() : '?';
}

function summarizeProfitShare(cuts: TeamDetails['operators'][number]['profitShareRule']['cuts']): string {
  if (!cuts.length) return 'Sem repasse';
  if (cuts.length === 1) return `${personLabel(cuts[0]!.accountId, cuts[0]!.username)} · ${formatPercent(cuts[0]!.percentage)}`;
  return `${cuts.length} beneficiários`;
}

export function AdminTeamPanel({ team, scope, actions, index }: AdminTeamPanelProps) {
  const showStructure = scope === 'full' || scope === 'operation-admin';
  const showPeople = scope === 'full' || scope === 'team-leader';
  const showGateway = scope === 'full' || scope === 'operation-admin';
  const gridClass = scope === 'full'
    ? 'admin-op-team-grid--full'
    : scope === 'operation-admin'
      ? 'admin-op-team-grid--structure-gateway'
      : 'admin-op-team-grid--people';

  return (
    <article className="admin-op-team-card">
      <header className="admin-op-team-card__head">
        <div className="admin-op-team-card__identity">
          <span className="admin-op-level-badge admin-op-level-badge--team">
            Equipe{index ? ` ${index}` : ''}
          </span>
          <div>
            <h5 className="admin-op-team-name">{team.name}</h5>
            <span className="admin-op-team-id mono" title={team.id}>{shortId(team.id, 20)}</span>
          </div>
        </div>

        <div className="admin-op-team-card__snapshot">
          {showStructure ? (
            <span className="admin-op-team-chip">
              Líder:{' '}
              {team.teamLeader
                ? personLabel(team.teamLeader.accountId, team.teamLeader.username)
                : '—'}
            </span>
          ) : null}
          {showPeople ? (
            <span className="admin-op-team-chip">
              {team.operators.length} operador{team.operators.length === 1 ? '' : 'es'}
            </span>
          ) : null}
          {showGateway && team.gatewaySelectionStrategy ? (
            <span className="admin-op-team-chip admin-op-team-chip--gateway">
              Gateway: {team.gatewaySelectionStrategy === 'PerStrawman' ? 'Laranja' : team.gatewaySelectionStrategy === 'PerGroup' ? 'Grupo' : 'Manual'}
            </span>
          ) : null}
        </div>

        {showStructure ? (
          <button
            type="button"
            className="btn btn-ghost btn-small btn-danger-outline"
            disabled={actions.busy}
            onClick={() => actions.onDeleteTeam(team.id)}
          >
            Excluir equipe
          </button>
        ) : null}
      </header>

      <div className={`admin-op-team-grid ${gridClass}`}>
        {showStructure ? (
          <div className="admin-op-team-col admin-op-team-col--structure">
            <h6 className="admin-op-col-title">Estrutura</h6>
            <p className="admin-op-col-desc muted small">Líder responsável pela equipe.</p>

            <div className="admin-op-leader-slot">
              {team.teamLeader ? (
                <>
                  <span className="admin-op-person-avatar" aria-hidden="true">
                    {personInitial(team.teamLeader.username)}
                  </span>
                  <div className="admin-op-person-meta">
                    <span className="admin-op-person-name">
                      {personLabel(team.teamLeader.accountId, team.teamLeader.username)}
                    </span>
                    <span className="admin-op-person-id mono">{shortId(team.teamLeader.accountId, 18)}</span>
                  </div>
                  <button
                    type="button"
                    className="btn btn-ghost btn-small"
                    disabled={actions.busy}
                    onClick={() => actions.onUnassignLeader(team.id)}
                  >
                    Remover
                  </button>
                </>
              ) : (
                <>
                  <p className="admin-op-empty muted small">Nenhum líder definido.</p>
                  <button
                    type="button"
                    className="btn btn-primary btn-small"
                    disabled={actions.busy}
                    onClick={() => actions.onAssignLeader(team.id)}
                  >
                    Vincular líder
                  </button>
                </>
              )}
            </div>
          </div>
        ) : null}

        {showPeople ? (
          <div className="admin-op-team-col admin-op-team-col--people">
            <div className="admin-op-col-head">
              <div>
                <h6 className="admin-op-col-title">Operadores</h6>
                <p className="admin-op-col-desc muted small">Alocação e regras de repasse.</p>
              </div>
              <button
                type="button"
                className="btn btn-primary btn-small"
                disabled={actions.busy}
                onClick={() => actions.onAssignOperator(team.id)}
              >
                Alocar
              </button>
            </div>

            {team.operators.length === 0 ? (
              <p className="admin-op-empty muted small">Nenhum operador alocado.</p>
            ) : (
              <div className="admin-op-table-wrap">
                <table className="admin-op-table">
                  <thead>
                    <tr>
                      <th>Operador</th>
                      <th>Repasse</th>
                      <th aria-label="Ações" />
                    </tr>
                  </thead>
                  <tbody>
                    {team.operators.map((operator) => (
                      <tr key={operator.accountId}>
                        <td>
                          <div className="admin-op-table-person">
                            <span className="admin-op-person-avatar admin-op-person-avatar--sm" aria-hidden="true">
                              {personInitial(operator.username)}
                            </span>
                            <span>
                              <span className="admin-op-person-name">{personLabel(operator.accountId, operator.username)}</span>
                              <span className="admin-op-person-id mono">{shortId(operator.accountId, 16)}</span>
                            </span>
                          </div>
                        </td>
                        <td>
                          <span className="admin-op-repasse-summary">
                            {summarizeProfitShare(operator.profitShareRule?.cuts ?? [])}
                          </span>
                          {(operator.profitShareRule?.cuts ?? []).length > 0 ? (
                            <ul className="admin-op-profit-inline">
                              {(operator.profitShareRule?.cuts ?? []).map((cut) => (
                                <li key={`${cut.accountId}-${cut.percentage}`}>
                                  {personLabel(cut.accountId, cut.username)} · {formatPercent(cut.percentage)}
                                </li>
                              ))}
                            </ul>
                          ) : null}
                        </td>
                        <td className="admin-op-table-actions">
                          <button
                            type="button"
                            className="btn btn-ghost btn-small"
                            disabled={actions.busy}
                            onClick={() => actions.onEditProfitShare(team.id, operator)}
                          >
                            Repasse
                          </button>
                          <button
                            type="button"
                            className="btn btn-ghost btn-small"
                            disabled={actions.busy}
                            onClick={() => actions.onUnassignOperator(team.id, operator.accountId)}
                          >
                            Remover
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        ) : null}

        {showGateway ? (
          <div className="admin-op-team-col admin-op-team-col--gateway">
            <AdminTeamGatewaySection team={team} actions={actions} />
          </div>
        ) : null}
      </div>
    </article>
  );
}
