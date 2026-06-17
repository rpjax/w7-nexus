import type { ReactNode } from 'react';
import { useState } from 'react';
import type { OperationDetails, OperationWithLedTeamsDetails } from '../../api/types';
import { formatDateTime, shortId } from '../../utils/format';
import { AdminTeamPanel, type AdminTeamPanelActions, type AdminTeamPanelScope } from './AdminTeamPanel';

export type OperationCardScope = 'global-admin' | 'operation-admin' | 'team-leader';

export type AdminOperationCardActions = AdminTeamPanelActions & {
  onAssignAdministrator: (operationId: string) => void;
  onRemoveAdministrator: (operationId: string, administratorId: string) => void;
  onDelete: (operationId: string) => void;
  onCreateTeam: (operationId: string, name: string) => void;
};

type AdminOperationCardProps = {
  operation: OperationDetails | OperationWithLedTeamsDetails;
  scope: OperationCardScope;
  actions: AdminOperationCardActions;
};

function personInitial(username: string): string {
  const trimmed = username.trim();
  return trimmed ? trimmed[0]!.toUpperCase() : '?';
}

function personLabel(accountId: string, username: string): string {
  return username && username !== accountId ? username : shortId(accountId, 18);
}

function countOperators(teams: TeamDetailsLike[]): number {
  return teams.reduce((sum, team) => sum + team.operators.length, 0);
}

type TeamDetailsLike = OperationDetails['teams'][number];

function PersonRow({
  accountId,
  username,
  action,
}: {
  accountId: string;
  username: string;
  action?: ReactNode;
}) {
  return (
    <li className="admin-op-person">
      <span className="admin-op-person-avatar" aria-hidden="true">{personInitial(username)}</span>
      <span className="admin-op-person-meta">
        <span className="admin-op-person-name">{personLabel(accountId, username)}</span>
        <span className="admin-op-person-id mono" title={accountId}>{shortId(accountId, 22)}</span>
      </span>
      {action ? <span className="admin-op-person-action">{action}</span> : null}
    </li>
  );
}

function teamPanelScope(scope: OperationCardScope): AdminTeamPanelScope {
  if (scope === 'operation-admin') return 'operation-admin';
  if (scope === 'team-leader') return 'team-leader';
  return 'full';
}

export function AdminOperationCard({ operation, scope, actions }: AdminOperationCardProps) {
  const [newTeamName, setNewTeamName] = useState('');
  const administrators = 'administrators' in operation ? (operation.administrators ?? []) : [];
  const teams = operation.teams ?? [];
  const adminCount = administrators.length;
  const teamCount = teams.length;
  const operatorCount = countOperators(teams);
  const description = operation.description?.trim();
  const panelScope = teamPanelScope(scope);
  const showAdminSection = scope === 'global-admin';

  async function copyId() {
    try {
      await navigator.clipboard.writeText(operation.id);
    } catch {
      // ignore clipboard errors
    }
  }

  function submitCreateTeam() {
    const name = newTeamName.trim();
    if (!name) return;
    actions.onCreateTeam(operation.id, name);
    setNewTeamName('');
  }

  const teamActions: AdminTeamPanelActions = {
    busy: actions.busy,
    onDeleteTeam: actions.onDeleteTeam,
    onAssignLeader: actions.onAssignLeader,
    onUnassignLeader: actions.onUnassignLeader,
    onAssignOperator: actions.onAssignOperator,
    onUnassignOperator: actions.onUnassignOperator,
    onEditProfitShare: actions.onEditProfitShare,
    onGatewayStrategyChange: actions.onGatewayStrategyChange,
    onAssignStrawMan: actions.onAssignStrawMan,
    onUnassignStrawMan: actions.onUnassignStrawMan,
    onAssignGatewayCredential: actions.onAssignGatewayCredential,
    onUnassignGatewayCredential: actions.onUnassignGatewayCredential,
    onAssignGatewayGroup: actions.onAssignGatewayGroup,
    onUnassignGatewayGroup: actions.onUnassignGatewayGroup,
  };

  return (
    <article className={`admin-op-card admin-op-card--${scope}`}>
      <header className="admin-op-identity">
        <div className="admin-op-identity__primary">
          <span className="admin-op-level-badge">Operação</span>
          <div className="admin-op-identity__title-block">
            <h3 className="admin-op-card-title">{operation.name}</h3>
            {description ? (
              <p className="admin-op-identity__desc">{description}</p>
            ) : (
              <p className="admin-op-identity__desc muted">Sem descrição cadastrada.</p>
            )}
          </div>
        </div>

        <dl className="admin-op-identity__facts">
          <div className="admin-op-fact">
            <dt>ID</dt>
            <dd className="mono" title={operation.id}>
              {shortId(operation.id, 24)}
              <button type="button" className="btn btn-ghost btn-small admin-op-copy-id" onClick={() => void copyId()}>
                Copiar
              </button>
            </dd>
          </div>
          <div className="admin-op-fact">
            <dt>Criada</dt>
            <dd>{formatDateTime(operation.createdAt)}</dd>
          </div>
          {scope !== 'team-leader' ? (
            <div className="admin-op-fact">
              <dt>Atualizada</dt>
              <dd>{formatDateTime(operation.updatedAt)}</dd>
            </div>
          ) : null}
        </dl>

        <div className="admin-op-identity__stats" aria-label="Resumo da operação">
          {scope === 'global-admin' ? (
            <div className={`admin-op-summary-stat${adminCount === 0 ? ' admin-op-summary-stat--warn' : ''}`}>
              <span className="admin-op-summary-stat__value">{adminCount}</span>
              <span className="admin-op-summary-stat__label">Administradores</span>
            </div>
          ) : null}
          <div className="admin-op-summary-stat">
            <span className="admin-op-summary-stat__value">{teamCount}</span>
            <span className="admin-op-summary-stat__label">Equipes</span>
          </div>
          {scope !== 'operation-admin' ? (
            <div className="admin-op-summary-stat">
              <span className="admin-op-summary-stat__value">{operatorCount}</span>
              <span className="admin-op-summary-stat__label">Operadores</span>
            </div>
          ) : null}
        </div>
      </header>

      <div className={`admin-op-layout${showAdminSection ? ' admin-op-layout--split' : ''}`}>
        {showAdminSection ? (
          <aside className="admin-op-aside">
            <div className="admin-op-aside__head">
              <div>
                <h4 className="admin-op-section-title">Administradores</h4>
                <p className="admin-op-section-desc muted small">Quem administra esta operação.</p>
              </div>
              <button
                type="button"
                className="btn btn-primary btn-small"
                disabled={actions.busy}
                onClick={() => actions.onAssignAdministrator(operation.id)}
              >
                Vincular
              </button>
            </div>

            {adminCount === 0 ? (
              <p className="admin-op-empty muted small">Nenhum administrador vinculado.</p>
            ) : (
              <ul className="admin-op-person-list">
                {administrators.map((admin) => (
                  <PersonRow
                    key={admin.accountId}
                    accountId={admin.accountId}
                    username={admin.username}
                    action={(
                      <button
                        type="button"
                        className="btn btn-ghost btn-small"
                        disabled={actions.busy}
                        onClick={() => actions.onRemoveAdministrator(operation.id, admin.accountId)}
                      >
                        Remover
                      </button>
                    )}
                  />
                ))}
              </ul>
            )}
          </aside>
        ) : null}

        <section className="admin-op-main">
          <div className="admin-op-main__head">
            <div>
              <h4 className="admin-op-section-title">Equipes</h4>
              <p className="admin-op-section-desc muted small">
                {scope === 'global-admin' && 'Cada equipe agrupa líder, operadores e configuração de gateway.'}
                {scope === 'operation-admin' && 'Defina líderes e configure gateway por equipe.'}
                {scope === 'team-leader' && 'Suas equipes nesta operação — operadores e repasses.'}
              </p>
            </div>

            {scope !== 'team-leader' ? (
              <div className="admin-op-create-team">
                <input
                  className="nexus-input"
                  value={newTeamName}
                  onChange={(e) => setNewTeamName(e.target.value)}
                  placeholder="Nova equipe…"
                  aria-label="Nome da nova equipe"
                  onKeyDown={(e) => { if (e.key === 'Enter') submitCreateTeam(); }}
                />
                <button
                  type="button"
                  className="btn btn-primary btn-small"
                  disabled={actions.busy || !newTeamName.trim()}
                  onClick={submitCreateTeam}
                >
                  Criar
                </button>
              </div>
            ) : null}
          </div>

          {teamCount === 0 ? (
            <p className="admin-op-empty muted small">
              {scope === 'team-leader'
                ? 'Nenhuma equipe liderada nesta operação.'
                : 'Nenhuma equipe vinculada. Crie a primeira acima.'}
            </p>
          ) : (
            <div className="admin-op-team-stack">
              {teams.map((team, index) => (
                <AdminTeamPanel
                  key={team.id}
                  team={team}
                  scope={panelScope}
                  actions={teamActions}
                  index={index + 1}
                />
              ))}
            </div>
          )}
        </section>
      </div>

      {scope === 'global-admin' ? (
        <footer className="admin-op-card-footer">
          <button
            type="button"
            className="btn btn-danger btn-small"
            disabled={actions.busy}
            onClick={() => actions.onDelete(operation.id)}
          >
            Excluir operação
          </button>
        </footer>
      ) : null}
    </article>
  );
}
