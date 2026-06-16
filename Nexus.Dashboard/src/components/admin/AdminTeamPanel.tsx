import type { TeamDetails } from '../../api/types';
import { shortId } from '../../utils/format';
import { AdminTeamGatewaySection } from './AdminTeamGatewaySection';
import type { AdminTeamPanelActions, AdminTeamPanelScope } from './adminTeamTypes';

export type { AdminTeamPanelActions, AdminTeamPanelScope };

type AdminTeamPanelProps = {
  team: TeamDetails;
  scope: AdminTeamPanelScope;
  actions: AdminTeamPanelActions;
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

export function AdminTeamPanel({ team, scope, actions }: AdminTeamPanelProps) {
  const showOperationAdmin = scope === 'full' || scope === 'operation-admin';
  const showTeamLeader = scope === 'full' || scope === 'team-leader';

  return (
    <article className="admin-op-team">
      <header className="admin-op-team-header">
        <div>
          <h5 className="admin-op-team-name">{team.name}</h5>
          <span className="admin-op-team-id mono" title={team.id}>{shortId(team.id, 18)}</span>
        </div>
        <div className="admin-op-team-actions-top">
          <span className="admin-op-team-count">
            {team.operators.length} operador{team.operators.length === 1 ? '' : 'es'}
          </span>
          {showOperationAdmin ? (
            <button
              type="button"
              className="btn btn-ghost btn-small btn-danger-outline"
              disabled={actions.busy}
              onClick={() => actions.onDeleteTeam(team.id)}
            >
              Excluir equipe
            </button>
          ) : null}
        </div>
      </header>

      {showOperationAdmin ? (
        <section className="admin-op-team-block">
          <div className="admin-op-team-block-head">
            <h6>Líder da equipe</h6>
            <div className="admin-op-inline-actions">
              {team.teamLeader ? (
                <>
                  <span className="admin-op-team-leader-value">
                    {personLabel(team.teamLeader.accountId, team.teamLeader.username)}
                  </span>
                  <button
                    type="button"
                    className="btn btn-ghost btn-small"
                    disabled={actions.busy}
                    onClick={() => actions.onUnassignLeader(team.id)}
                  >
                    Remover líder
                  </button>
                </>
              ) : (
                <>
                  <span className="muted small">Não definido</span>
                  <button
                    type="button"
                    className="btn btn-ghost btn-small"
                    disabled={actions.busy}
                    onClick={() => actions.onAssignLeader(team.id)}
                  >
                    Vincular líder
                  </button>
                </>
              )}
            </div>
          </div>
        </section>
      ) : null}

      {showTeamLeader ? (
        <>
          <section className="admin-op-team-block">
            <div className="admin-op-team-block-head">
              <h6>Operadores</h6>
              <button
                type="button"
                className="btn btn-ghost btn-small"
                disabled={actions.busy}
                onClick={() => actions.onAssignOperator(team.id)}
              >
                Alocar operador
              </button>
            </div>

            {team.operators.length === 0 ? (
              <p className="admin-op-empty muted small">Nenhum operador alocado nesta equipe.</p>
            ) : (
              <ul className="admin-op-operator-list">
                {team.operators.map((operator) => (
                  <li key={operator.accountId} className="admin-op-operator">
                    <div className="admin-op-operator-head">
                      <span className="admin-op-person-avatar" aria-hidden="true">
                        {personInitial(operator.username)}
                      </span>
                      <div className="admin-op-person-meta">
                        <span className="admin-op-person-name">{personLabel(operator.accountId, operator.username)}</span>
                        <span className="admin-op-person-id mono">{shortId(operator.accountId, 18)}</span>
                      </div>
                      <div className="admin-op-operator-actions">
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
                      </div>
                    </div>
                    <ul className="admin-op-profit-list">
                      {(operator.profitShareRule?.cuts ?? []).map((cut) => (
                        <li key={`${cut.accountId}-${cut.percentage}`} className="admin-op-profit-item">
                          <div className="admin-op-profit-track" aria-hidden="true">
                            <span
                              className="admin-op-profit-fill"
                              style={{ width: `${Math.min(100, Math.max(0, cut.percentage))}%` }}
                            />
                          </div>
                          <span className="admin-op-profit-meta">
                            <span className="admin-op-profit-user">{personLabel(cut.accountId, cut.username)}</span>
                            <span className="admin-op-profit-pct">{formatPercent(cut.percentage)}</span>
                          </span>
                        </li>
                      ))}
                    </ul>
                  </li>
                ))}
              </ul>
            )}
          </section>

          <AdminTeamGatewaySection team={team} actions={actions} />
        </>
      ) : null}
    </article>
  );
}
