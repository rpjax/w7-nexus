import type { ReactNode } from 'react';
import { useState } from 'react';
import type { OperationDetails, OperationWithLedTeamsDetails } from '../../api/types';
import { TeamListItem } from '../../features/teams/TeamListItem';
import { Icon, IconButton } from '../IconButton';
import { formatDateTime, shortId } from '../../utils/format';
import { AdminTeamPanel, type AdminTeamPanelActions, type AdminTeamPanelScope } from './AdminTeamPanel';
import { CreateTeamModal } from './CreateTeamModal';

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

function OpSection({
  kicker,
  title,
  desc,
  action,
  children,
}: {
  kicker?: string;
  title?: string;
  desc?: string;
  action?: ReactNode;
  children: ReactNode;
}) {
  return (
    <section className="admin-op-section">
      {(kicker || title || desc || action) ? (
        <div className="admin-op-section__head">
          <div>
            {kicker ? <span className="admin-op-section__kicker">{kicker}</span> : null}
            {title ? <h4 className="admin-op-section-title">{title}</h4> : null}
            {desc ? <p className="admin-op-section-desc muted small">{desc}</p> : null}
          </div>
          {action ?? null}
        </div>
      ) : null}
      <div className="admin-op-section__body">{children}</div>
    </section>
  );
}

function teamPanelScope(scope: OperationCardScope): AdminTeamPanelScope {
  if (scope === 'operation-admin') return 'operation-admin';
  if (scope === 'team-leader') return 'team-leader';
  return 'full';
}

export function AdminOperationCard({ operation, scope, actions }: AdminOperationCardProps) {
  const [createTeamOpen, setCreateTeamOpen] = useState(false);
  const administrators = 'administrators' in operation ? (operation.administrators ?? []) : [];
  const teams = operation.teams ?? [];
  const adminCount = administrators.length;
  const teamCount = teams.length;
  const operatorCount = countOperators(teams);
  const description = operation.description?.trim();
  const panelScope = teamPanelScope(scope);
  const showAdminSection = scope === 'global-admin';
  const showActionsSection = scope === 'global-admin';
  const canCreateTeam = scope !== 'team-leader';
  const useTeamList = scope === 'global-admin' || scope === 'operation-admin';

  async function copyId() {
    try {
      await navigator.clipboard.writeText(operation.id);
    } catch {
      // ignore clipboard errors
    }
  }

  function submitCreateTeam(name: string) {
    actions.onCreateTeam(operation.id, name);
    setCreateTeamOpen(false);
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
    <article className={`admin-op-card admin-op-card--${scope} admin-op-card--detail`}>
      <OpSection kicker="Operação">
        <div className="admin-op-identity">
          <div className="admin-op-identity__primary">
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
              <dd className="mono admin-op-fact-dd--id" title={operation.id}>
                <span className="admin-op-fact-id-text">{shortId(operation.id, 24)}</span>
                <IconButton
                  icon="copy"
                  label="Copiar ID da operação"
                  onClick={() => void copyId()}
                />
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

          <dl className="admin-op-metrics" aria-label="Resumo da operação">
            {scope === 'global-admin' ? (
              <div className={`admin-op-metric${adminCount === 0 ? ' admin-op-metric--warn' : ''}`}>
                <dt className="admin-op-metric__label">Administradores</dt>
                <dd className="admin-op-metric__value">{adminCount}</dd>
              </div>
            ) : null}
            <div className="admin-op-metric">
              <dt className="admin-op-metric__label">Equipes</dt>
              <dd className="admin-op-metric__value">{teamCount}</dd>
            </div>
            {scope !== 'operation-admin' ? (
              <div className="admin-op-metric">
                <dt className="admin-op-metric__label">Operadores</dt>
                <dd className="admin-op-metric__value">{operatorCount}</dd>
              </div>
            ) : null}
          </dl>
        </div>
      </OpSection>

      {showActionsSection ? (
        <OpSection title="Ações">
          <div className="admin-op-actions">
            <button
              type="button"
              className="btn btn-danger btn-small btn-with-icon"
              disabled={actions.busy}
              onClick={() => actions.onDelete(operation.id)}
            >
              <Icon name="trash" />
              Excluir operação
            </button>
          </div>
        </OpSection>
      ) : null}

      {showAdminSection ? (
        <OpSection
          title="Administradores"
          desc="Quem administra esta operação."
          action={(
            <IconButton
              icon="plus"
              label="Vincular administrador"
              variant="primary"
              disabled={actions.busy}
              onClick={() => actions.onAssignAdministrator(operation.id)}
            />
          )}
        >
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
                    <IconButton
                      icon="trash"
                      label={`Remover administrador ${personLabel(admin.accountId, admin.username)}`}
                      variant="danger"
                      disabled={actions.busy}
                      onClick={() => actions.onRemoveAdministrator(operation.id, admin.accountId)}
                    />
                  )}
                />
              ))}
            </ul>
          )}
        </OpSection>
      ) : null}

      <OpSection
        title="Equipes"
        desc={
          scope === 'global-admin'
            ? 'Cada equipe agrupa líder, operadores e configuração de gateway.'
            : scope === 'operation-admin'
              ? 'Defina líderes e configure gateway por equipe.'
              : 'Suas equipes nesta operação — operadores e repasses.'
        }
        action={canCreateTeam ? (
          <IconButton
            icon="plus"
            label="Criar equipe"
            variant="primary"
            disabled={actions.busy}
            onClick={() => setCreateTeamOpen(true)}
          />
        ) : undefined}
      >
        {teamCount === 0 ? (
          <p className="admin-op-empty muted small">
            {scope === 'team-leader'
              ? 'Nenhuma equipe liderada nesta operação.'
              : 'Nenhuma equipe vinculada. Crie a primeira equipe.'}
          </p>
        ) : useTeamList ? (
          <div className="ops-list" role="list">
            {teams.map((team) => (
              <TeamListItem
                key={team.id}
                team={team}
                scope={scope}
                operationId={operation.id}
                onDelete={actions.onDeleteTeam}
                deleteBusy={actions.busy}
              />
            ))}
          </div>
        ) : (
          <div className="admin-op-team-stack">
            {teams.map((team) => (
              <AdminTeamPanel
                key={team.id}
                team={team}
                scope={panelScope}
                actions={teamActions}
              />
            ))}
          </div>
        )}
      </OpSection>

      <CreateTeamModal
        open={createTeamOpen}
        busy={actions.busy}
        operationName={operation.name}
        onClose={() => setCreateTeamOpen(false)}
        onSubmit={submitCreateTeam}
      />
    </article>
  );
}
