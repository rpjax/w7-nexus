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
    <article className="admin-op-card">
      <header className="admin-op-card-header">
        <span className="admin-op-card-mark" aria-hidden="true">{personInitial(operation.name)}</span>
        <div className="admin-op-card-heading">
          <div className="admin-op-card-title-row">
            <h3 className="admin-op-card-title">{operation.name}</h3>
            <div className="admin-op-card-stats">
              {scope === 'global-admin' ? (
                <span className={`admin-op-stat ${adminCount === 0 ? 'admin-op-stat-warn' : ''}`}>
                  <span className="admin-op-stat-value">{adminCount}</span>
                  <span className="admin-op-stat-label">Admin{adminCount === 1 ? '' : 's'}</span>
                </span>
              ) : null}
              <span className="admin-op-stat">
                <span className="admin-op-stat-value">{teamCount}</span>
                <span className="admin-op-stat-label">
                  {scope === 'team-leader' ? 'Equipe liderada' : 'Equipe'}{teamCount === 1 ? '' : 's'}
                </span>
              </span>
              {scope !== 'operation-admin' ? (
                <span className="admin-op-stat">
                  <span className="admin-op-stat-value">{operatorCount}</span>
                  <span className="admin-op-stat-label">Operador{operatorCount === 1 ? '' : 'es'}</span>
                </span>
              ) : null}
            </div>
          </div>
          <p className="admin-op-card-id">
            <span className="mono" title={operation.id}>{shortId(operation.id, 24)}</span>
            <button type="button" className="btn btn-ghost btn-small admin-op-copy-id" onClick={() => void copyId()}>
              Copiar ID
            </button>
          </p>
        </div>
      </header>

      <div className="admin-op-card-overview">
        <div className="admin-op-overview-block">
          <span className="admin-op-overview-label">Descrição</span>
          <p className="admin-op-overview-text">{description || 'Sem descrição cadastrada.'}</p>
        </div>
        <div className="admin-op-timeline">
          <div className="admin-op-timeline-item">
            <span className="admin-op-timeline-label">Criada</span>
            <time className="admin-op-timeline-value">{formatDateTime(operation.createdAt)}</time>
          </div>
          <div className="admin-op-timeline-item">
            <span className="admin-op-timeline-label">Atualizada</span>
            <time className="admin-op-timeline-value">{formatDateTime(operation.updatedAt)}</time>
          </div>
        </div>
      </div>

      <div className="admin-op-card-panels admin-op-card-panels--stacked">
        {scope === 'global-admin' ? (
          <section className="admin-op-panel">
            <header className="admin-op-panel-head">
              <h4>Administradores</h4>
              <p className="muted small">Contas com permissão para gerenciar esta operação (papel Operation Administrator).</p>
            </header>

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

            <div className="admin-op-panel-actions">
              <button
                type="button"
                className="btn btn-primary btn-small"
                disabled={actions.busy}
                onClick={() => actions.onAssignAdministrator(operation.id)}
              >
                Vincular administrador
              </button>
            </div>
          </section>
        ) : null}

        <section className="admin-op-panel admin-op-panel--teams">
          <header className="admin-op-panel-head">
            <h4>
              {scope === 'team-leader' ? 'Suas equipes nesta operação' : 'Equipes e operadores'}
            </h4>
            <p className="muted small">
              {scope === 'global-admin' && 'Estrutura completa: líderes, operadores, repasses, laranjas e credenciais de gateway.'}
              {scope === 'operation-admin' && 'Crie equipes, defina líderes e configure laranjas e credenciais de gateway. Operadores e repasses ficam com cada líder.'}
              {scope === 'team-leader' && 'Gerencie operadores e regras de repasse das equipes que você lidera.'}
            </p>
          </header>

          {scope !== 'team-leader' ? (
            <div className="admin-op-create-team">
              <input
                className="nexus-input"
                value={newTeamName}
                onChange={(e) => setNewTeamName(e.target.value)}
                placeholder="Nome da nova equipe…"
                onKeyDown={(e) => { if (e.key === 'Enter') submitCreateTeam(); }}
              />
              <button
                type="button"
                className="btn btn-primary btn-small"
                disabled={actions.busy || !newTeamName.trim()}
                onClick={submitCreateTeam}
              >
                Criar equipe
              </button>
            </div>
          ) : null}

          {teamCount === 0 ? (
            <p className="admin-op-empty muted small">
              {scope === 'team-leader'
                ? 'Nenhuma equipe liderada nesta operação.'
                : 'Nenhuma equipe vinculada a esta operação.'}
            </p>
          ) : (
            <div className="admin-op-team-list">
              {teams.map((team) => (
                <AdminTeamPanel key={team.id} team={team} scope={panelScope} actions={teamActions} />
              ))}
            </div>
          )}
        </section>
      </div>

      {scope === 'global-admin' ? (
        <footer className="admin-op-card-actions">
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
