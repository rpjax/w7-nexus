import type { ReactNode } from 'react';
import type { OperatorDetails, TeamDetails } from '../../api/types';
import { Icon, IconButton } from '../IconButton';
import { shortId } from '../../utils/format';
import { AdminTeamGatewaySection } from './AdminTeamGatewaySection';
import type { AdminTeamPanelActions, AdminTeamPanelScope } from './adminTeamTypes';

export type { AdminTeamPanelActions, AdminTeamPanelScope };

type AdminTeamPanelProps = {
  team: TeamDetails;
  scope: AdminTeamPanelScope;
  actions: AdminTeamPanelActions;
  variant?: 'list' | 'detail';
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

function gatewayStrategyLabel(strategy: NonNullable<TeamDetails['gatewaySelectionStrategy']>): string {
  if (strategy === 'PerStrawman') return 'Laranja';
  if (strategy === 'PerGroup') return 'Grupo';
  return 'Manual';
}

function TeamSection({
  title,
  desc,
  action,
  children,
  className = '',
  variant = 'list',
}: {
  title: string;
  desc?: string;
  action?: ReactNode;
  children: ReactNode;
  className?: string;
  variant?: 'list' | 'detail';
}) {
  const sectionClass = variant === 'detail' ? 'admin-op-section' : 'admin-op-team-section';

  return (
    <section className={`${sectionClass} ${className}`.trim()}>
      <div className="admin-op-section__head">
        <div className="admin-op-section__head-text">
          <h2 className="admin-op-section-title">{title}</h2>
          {desc ? <p className="admin-op-section-desc muted small">{desc}</p> : null}
        </div>
        {action ?? null}
      </div>
      <div className="admin-op-section__body">{children}</div>
    </section>
  );
}

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

function OperatorDetailRow({
  operator,
  teamId,
  busy,
  onEditProfitShare,
  onUnassignOperator,
}: {
  operator: OperatorDetails;
  teamId: string;
  busy: boolean;
  onEditProfitShare: (teamId: string, operator: OperatorDetails) => void;
  onUnassignOperator: (teamId: string, operatorId: string) => void;
}) {
  const cuts = operator.profitShareRule?.cuts ?? [];
  const label = personLabel(operator.accountId, operator.username);

  return (
    <li className="admin-op-operator-row">
      <div className="admin-op-operator-row__main">
        <div className="admin-op-operator-row__identity">
          <span className="admin-op-person-avatar admin-op-person-avatar--sm" aria-hidden="true">
            {personInitial(operator.username)}
          </span>
          <div className="admin-op-operator-row__meta">
            <span className="admin-op-person-name">{label}</span>
            <span className="admin-op-person-id mono" title={operator.accountId}>
              {shortId(operator.accountId, 18)}
            </span>
          </div>
        </div>
        <div className="admin-op-operator-row__actions icon-btn-group">
          <IconButton
            icon="percent"
            label={`Editar repasse de ${label}`}
            disabled={busy}
            onClick={() => onEditProfitShare(teamId, operator)}
          />
          <IconButton
            icon="trash"
            label={`Remover operador ${label}`}
            variant="danger"
            disabled={busy}
            onClick={() => onUnassignOperator(teamId, operator.accountId)}
          />
        </div>
      </div>

      <div className="admin-op-operator-row__repasse">
        <div className="admin-op-operator-row__repasse-inner">
          <span className="admin-op-operator-row__repasse-kicker">
            <Icon name="percent" />
            Repasse
          </span>
          {cuts.length === 0 ? (
            <span className="admin-op-operator-row__repasse-empty muted small">Sem repasse configurado</span>
          ) : (
            <ul className="admin-op-operator-row__cuts" aria-label={`Regras de repasse de ${label}`}>
              {cuts.map((cut) => (
                <li key={`${cut.accountId}-${cut.percentage}`} className="admin-op-operator-row__cut">
                  <span className="admin-op-operator-row__cut-name">
                    {personLabel(cut.accountId, cut.username)}
                  </span>
                  <span className="admin-op-operator-row__cut-pct">{formatPercent(cut.percentage)}</span>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>
    </li>
  );
}

export function AdminTeamPanel({ team, scope, actions, variant = 'list' }: AdminTeamPanelProps) {
  const isDetail = variant === 'detail';
  const showStructure = scope === 'full' || scope === 'operation-admin';
  const showPeople = scope === 'full' || scope === 'team-leader';
  const showGateway = scope === 'full' || scope === 'operation-admin';
  const rootClass = isDetail
    ? 'admin-op-card admin-op-card--detail admin-op-team-detail'
    : 'ops-list-item ops-list-item--team';

  const leaderAssignAction = team.teamLeader ? (
    <IconButton
      icon="trash"
      label={`Remover líder ${personLabel(team.teamLeader.accountId, team.teamLeader.username)}`}
      variant="danger"
      disabled={actions.busy}
      onClick={() => actions.onUnassignLeader(team.id)}
    />
  ) : (
    <IconButton
      icon="link"
      label="Vincular líder"
      variant="primary"
      disabled={actions.busy}
      onClick={() => actions.onAssignLeader(team.id)}
    />
  );

  return (
    <article className={rootClass}>
      {isDetail ? (
        <section className="admin-op-section">
          <div className="admin-op-section__head">
            <div className="admin-op-section__head-text">
              <h2 className="admin-op-section-title">Visão geral</h2>
            </div>
          </div>
          <div className="admin-op-section__body">
            <div className="admin-op-identity">
              <h3 className="admin-op-card-title">{team.name}</h3>
              <dl className="admin-op-identity__facts">
                <div className="admin-op-fact">
                  <dt>ID</dt>
                  <dd className="mono admin-op-fact-id-text" title={team.id}>{shortId(team.id, 24)}</dd>
                </div>
                {showStructure ? (
                  <div className="admin-op-fact">
                    <dt>Líder</dt>
                    <dd>
                      {team.teamLeader
                        ? personLabel(team.teamLeader.accountId, team.teamLeader.username)
                        : '—'}
                    </dd>
                  </div>
                ) : null}
                {showPeople ? (
                  <div className="admin-op-fact">
                    <dt>Operadores</dt>
                    <dd>{team.operators.length}</dd>
                  </div>
                ) : null}
                {showGateway && team.gatewaySelectionStrategy ? (
                  <div className="admin-op-fact">
                    <dt>Gateway</dt>
                    <dd className="admin-op-team-fact--gateway">
                      {gatewayStrategyLabel(team.gatewaySelectionStrategy)}
                    </dd>
                  </div>
                ) : null}
              </dl>
            </div>
          </div>
        </section>
      ) : (
        <header className="admin-op-team-card__head">
          <div className="admin-op-team-card__bar">
            <div className="admin-op-team-card__title-block">
              <span className="admin-op-team-card__kicker">Equipe</span>
              <h5 className="admin-op-team-name">{team.name}</h5>
            </div>
            {showStructure ? (
              <IconButton
                icon="trash"
                label={`Excluir equipe ${team.name}`}
                variant="danger"
                disabled={actions.busy}
                onClick={() => actions.onDeleteTeam(team.id)}
              />
            ) : null}
          </div>

          <dl className="admin-op-team-facts">
            <div className="admin-op-fact">
              <dt>ID</dt>
              <dd className="mono admin-op-fact-id-text" title={team.id}>{shortId(team.id, 18)}</dd>
            </div>
            {showStructure ? (
              <div className="admin-op-fact">
                <dt>Líder</dt>
                <dd>
                  {team.teamLeader
                    ? personLabel(team.teamLeader.accountId, team.teamLeader.username)
                    : '—'}
                </dd>
              </div>
            ) : null}
            {showPeople ? (
              <div className="admin-op-fact">
                <dt>Operadores</dt>
                <dd>{team.operators.length}</dd>
              </div>
            ) : null}
            {showGateway && team.gatewaySelectionStrategy ? (
              <div className="admin-op-fact">
                <dt>Gateway</dt>
                <dd className="admin-op-team-fact--gateway">
                  {gatewayStrategyLabel(team.gatewaySelectionStrategy)}
                </dd>
              </div>
            ) : null}
          </dl>
        </header>
      )}

      {isDetail && showStructure ? (
        <section className="admin-op-section">
          <div className="admin-op-section__head">
            <div className="admin-op-section__head-text">
              <h2 className="admin-op-section-title">Ações</h2>
            </div>
          </div>
          <div className="admin-op-section__body">
            <div className="admin-op-actions">
              <button
                type="button"
                className="btn btn-danger btn-small btn-with-icon"
                disabled={actions.busy}
                onClick={() => actions.onDeleteTeam(team.id)}
              >
                <Icon name="trash" />
                Excluir equipe
              </button>
            </div>
          </div>
        </section>
      ) : null}

      {showStructure ? (
        <TeamSection
          variant={variant}
          title="Líder"
          desc="Responsável pela equipe."
          action={leaderAssignAction}
        >
          {team.teamLeader ? (
            isDetail ? (
              <ul className="admin-op-person-list">
                <PersonRow
                  accountId={team.teamLeader.accountId}
                  username={team.teamLeader.username}
                />
              </ul>
            ) : (
              <div className="admin-op-role-card">
                <span className="admin-op-person-avatar" aria-hidden="true">
                  {personInitial(team.teamLeader.username)}
                </span>
                <div className="admin-op-role-card__body">
                  <span className="admin-op-role-card__label">Líder da equipe</span>
                  <span className="admin-op-person-name">
                    {personLabel(team.teamLeader.accountId, team.teamLeader.username)}
                  </span>
                  <span className="admin-op-person-id mono" title={team.teamLeader.accountId}>
                    {shortId(team.teamLeader.accountId, 18)}
                  </span>
                </div>
              </div>
            )
          ) : (
            <p className={`admin-op-empty muted small${isDetail ? '' : ' admin-op-col-empty-hint'}`}>
              Nenhum líder vinculado.
            </p>
          )}
        </TeamSection>
      ) : null}

      {showPeople ? (
        <TeamSection
          variant={variant}
          title="Operadores"
          desc="Alocação e regras de repasse."
          action={(
            <IconButton
              icon="plus"
              label="Alocar operador"
              variant="primary"
              disabled={actions.busy}
              onClick={() => actions.onAssignOperator(team.id)}
            />
          )}
        >
          {team.operators.length === 0 ? (
            <p className="admin-op-empty muted small">Nenhum operador alocado.</p>
          ) : (
            <ul className={`admin-op-operator-list${isDetail ? ' admin-op-operator-list--flat' : ''}`}>
              {team.operators.map((operator) => (
                isDetail ? (
                  <OperatorDetailRow
                    key={operator.accountId}
                    operator={operator}
                    teamId={team.id}
                    busy={actions.busy}
                    onEditProfitShare={actions.onEditProfitShare}
                    onUnassignOperator={actions.onUnassignOperator}
                  />
                ) : (
                  <li key={operator.accountId} className="admin-op-operator-card">
                    <div className="admin-op-operator-card__head">
                      <div className="admin-op-operator-card__identity">
                        <span className="admin-op-person-avatar admin-op-person-avatar--sm" aria-hidden="true">
                          {personInitial(operator.username)}
                        </span>
                        <div className="admin-op-operator-card__meta">
                          <span className="admin-op-person-name">
                            {personLabel(operator.accountId, operator.username)}
                          </span>
                          <span className="admin-op-person-id mono" title={operator.accountId}>
                            {shortId(operator.accountId, 18)}
                          </span>
                        </div>
                      </div>
                      <div className="icon-btn-group">
                        <IconButton
                          icon="percent"
                          label={`Editar repasse de ${personLabel(operator.accountId, operator.username)}`}
                          disabled={actions.busy}
                          onClick={() => actions.onEditProfitShare(team.id, operator)}
                        />
                        <IconButton
                          icon="trash"
                          label={`Remover operador ${personLabel(operator.accountId, operator.username)}`}
                          variant="danger"
                          disabled={actions.busy}
                          onClick={() => actions.onUnassignOperator(team.id, operator.accountId)}
                        />
                      </div>
                    </div>

                    <div className="admin-op-operator-card__repasse">
                      <span className="admin-op-operator-card__repasse-label">Repasse</span>
                      {(operator.profitShareRule?.cuts ?? []).length === 0 ? (
                        <p className="admin-op-operator-card__repasse-empty muted small">Sem repasse configurado.</p>
                      ) : (
                        <ul className="admin-op-profit-cuts">
                          {(operator.profitShareRule?.cuts ?? []).map((cut) => (
                            <li key={`${cut.accountId}-${cut.percentage}`}>
                              <span className="admin-op-profit-cut-name">
                                {personLabel(cut.accountId, cut.username)}
                              </span>
                              <span className="admin-op-profit-cut-pct">{formatPercent(cut.percentage)}</span>
                            </li>
                          ))}
                        </ul>
                      )}
                    </div>
                  </li>
                )
              ))}
            </ul>
          )}
        </TeamSection>
      ) : null}

      {showGateway ? (
        <TeamSection
          variant={variant}
          title="Gateway"
          desc="Estratégia de roteamento e credenciais."
          className="admin-op-team-section--gateway"
        >
          <AdminTeamGatewaySection team={team} actions={actions} showHeader={false} />
        </TeamSection>
      ) : null}
    </article>
  );
}
