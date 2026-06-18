import { useState, type ReactNode } from 'react';
import type { GatewaySelectionStrategy, TeamAccountDetails } from '../../api/types';
import { Icon, IconButton } from '../IconButton';
import { shortId } from '../../utils/format';
import {
  GATEWAY_LABELS,
  GATEWAY_STRATEGY_OPTIONS,
  type AdminGatewayActions,
  type GatewayScopeDetails,
  type GatewaySectionVariant,
} from './adminGatewayTypes';

type AdminGatewaySectionProps = {
  scope: GatewayScopeDetails;
  actions: AdminGatewayActions;
  variant: GatewaySectionVariant;
  showHeader?: boolean;
};

function personInitial(username: string): string {
  const trimmed = username.trim();
  return trimmed ? trimmed[0]!.toUpperCase() : '?';
}

function personLabel(accountId: string, username: string): string {
  return username && username !== accountId ? username : shortId(accountId, 18);
}

function GatewayPanelHead({
  title,
  desc,
  action,
}: {
  title: string;
  desc?: string;
  action?: ReactNode;
}) {
  return (
    <div className="gw-panel__head">
      <div className="gw-panel__head-text">
        <h3 className="gw-panel__title">{title}</h3>
        {desc ? <p className="gw-panel__desc muted small">{desc}</p> : null}
      </div>
      {action ?? null}
    </div>
  );
}

function GatewayEmpty({ title, message }: { title: string; message: string }) {
  return (
    <div className="gw-panel__empty">
      <p className="gw-panel__empty-title">{title}</p>
      <p className="muted small">{message}</p>
    </div>
  );
}

function StrawManRow({
  straw,
  busy,
  onRemove,
}: {
  straw: TeamAccountDetails;
  busy: boolean;
  onRemove: () => void;
}) {
  return (
    <li className="gw-list-row">
      <span className="admin-op-person-avatar admin-op-person-avatar--straw" aria-hidden="true">
        {personInitial(straw.username)}
      </span>
      <span className="gw-list-row__meta">
        <span className="admin-op-person-name">{personLabel(straw.accountId, straw.username)}</span>
        <span className="admin-op-person-id mono" title={straw.accountId}>{shortId(straw.accountId, 22)}</span>
      </span>
      <span className="gw-list-row__action">
        <IconButton
          icon="trash"
          label={`Remover laranja ${personLabel(straw.accountId, straw.username)}`}
          variant="danger"
          disabled={busy}
          onClick={onRemove}
        />
      </span>
    </li>
  );
}

function gatewayStatus(
  strategy: GatewaySelectionStrategy,
  scope: GatewayScopeDetails,
  variant: GatewaySectionVariant,
): { tone: 'ok' | 'warn'; message: string } {
  const entity = variant === 'operation' ? 'operação' : 'equipe';

  if (strategy === 'PerStrawman') {
    const count = scope.strawMen?.length ?? 0;
    return count === 0
      ? {
          tone: 'warn',
          message: variant === 'operation'
            ? 'Nenhum laranja vinculado — fallback pode usar apenas credenciais genéricas.'
            : 'Nenhum laranja vinculado — cobranças podem usar apenas credenciais genéricas.',
        }
      : {
          tone: 'ok',
          message: `${count} laranja${count === 1 ? '' : 's'} ativo${count === 1 ? '' : 's'} para filtrar credenciais${variant === 'operation' ? ' no fallback' : ''}.`,
        };
  }
  if (strategy === 'PerGroup') {
    const count = scope.gatewayCredentialsGroups?.length ?? 0;
    return count === 0
      ? { tone: 'warn', message: `Nenhum grupo vinculado — nenhuma credencial será elegível nesta ${entity}.` }
      : { tone: 'ok', message: `${count} grupo${count === 1 ? '' : 's'} vinculado${count === 1 ? '' : 's'} à ${entity}.` };
  }
  const count = scope.gatewayCredentials?.length ?? 0;
  return count === 0
    ? { tone: 'warn', message: `Nenhuma credencial manual — roteamento desabilitado para esta ${entity}.` }
    : { tone: 'ok', message: `${count} credencial${count === 1 ? '' : 'is'} manual${count === 1 ? '' : 'is'} ativa${count === 1 ? '' : 's'}.` };
}

function sectionIntro(variant: GatewaySectionVariant): string | undefined {
  if (variant === 'operation') {
    return 'Padrão de credenciais usado quando nenhum operador é informado na geração de pagamentos.';
  }
  return undefined;
}

function strawPanelCopy(variant: GatewaySectionVariant) {
  return variant === 'operation'
    ? {
        title: 'Laranjas da operação',
        emptyTitle: 'Nenhum laranja vinculado',
        emptyMessage: 'Vincule contas laranja para restringir quais credenciais entram no fallback de cobrança.',
      }
    : {
        title: 'Laranjas da equipe',
        emptyTitle: 'Nenhum laranja vinculado',
        emptyMessage: 'Vincule contas laranja para restringir quais credenciais entram no fluxo de cobrança.',
      };
}

function manualPanelCopy(variant: GatewaySectionVariant) {
  return variant === 'operation'
    ? {
        title: 'Credenciais manuais',
        desc: 'Somente estas credenciais participam do roteamento fallback desta operação.',
        emptyTitle: 'Nenhuma credencial vinculada',
        emptyMessage: 'Selecione credenciais ativas de Frendz, SigiloPay ou Wintech.',
      }
    : {
        title: 'Credenciais manuais',
        desc: 'Somente estas credenciais participam do roteamento de cobrança desta equipe.',
        emptyTitle: 'Nenhuma credencial vinculada',
        emptyMessage: 'Selecione credenciais ativas de Frendz, SigiloPay ou Wintech.',
      };
}

export function AdminGatewaySection({
  scope,
  actions,
  variant,
  showHeader = true,
}: AdminGatewaySectionProps) {
  const [groupIdInput, setGroupIdInput] = useState('');
  const strategy = scope.gatewaySelectionStrategy ?? 'PerStrawman';
  const strategyMeta = GATEWAY_STRATEGY_OPTIONS.find((opt) => opt.value === strategy) ?? GATEWAY_STRATEGY_OPTIONS[0];
  const status = gatewayStatus(strategy, scope, variant);
  const intro = sectionIntro(variant);
  const strawCopy = strawPanelCopy(variant);
  const manualCopy = manualPanelCopy(variant);

  function submitGroup() {
    const trimmed = groupIdInput.trim();
    if (!trimmed) return;
    actions.onAssignGatewayGroup(scope.id, trimmed);
    setGroupIdInput('');
  }

  return (
    <div className="admin-op-gateway-block gw-block">
      {showHeader ? (
        <div className="gw-block__standalone-head">
          <h2 className="admin-op-section-title">Gateway</h2>
          <p className="admin-op-section-desc muted small">
            {intro ?? 'Estratégia de roteamento e credenciais.'}
          </p>
        </div>
      ) : null}

      {intro && !showHeader ? (
        <p className="gw-block__context muted small">{intro}</p>
      ) : null}

      <div className={`gw-status gw-status--${status.tone}`} role="status">
        <span className="gw-status__dot" aria-hidden="true" />
        <span>{status.message}</span>
      </div>

      <div className="gw-strategy">
        <p className="gw-strategy__label">Estratégia de roteamento</p>
        <div className="gw-strategy__options" role="radiogroup" aria-label="Estratégia de gateway">
          {GATEWAY_STRATEGY_OPTIONS.map((opt) => {
            const active = strategy === opt.value;
            return (
              <button
                key={opt.value}
                type="button"
                role="radio"
                aria-checked={active}
                className={`gw-strategy__opt${active ? ' is-active' : ''}`}
                disabled={actions.busy}
                onClick={() => {
                  if (!active) actions.onGatewayStrategyChange(scope.id, opt.value);
                }}
              >
                {active ? (
                  <span className="gw-strategy__opt-check" aria-hidden="true">
                    <Icon name="check" />
                  </span>
                ) : null}
                <span className="gw-strategy__opt-label">{opt.label}</span>
                <span className="gw-strategy__opt-hint">{opt.hint}</span>
              </button>
            );
          })}
        </div>
        <p className="gw-strategy__detail muted small">{strategyMeta.detail}</p>
      </div>

      {strategy === 'PerStrawman' ? (
        <div className="gw-panel">
          <GatewayPanelHead
            title={strawCopy.title}
            desc="Contas usadas para filtrar credenciais nos gateways (Frendz, SigiloPay, Wintech)."
            action={(
              <IconButton
                icon="plus"
                label="Vincular laranja"
                variant="primary"
                disabled={actions.busy}
                onClick={() => actions.onAssignStrawMan(scope.id)}
              />
            )}
          />

          {(scope.strawMen ?? []).length === 0 ? (
            <GatewayEmpty title={strawCopy.emptyTitle} message={strawCopy.emptyMessage} />
          ) : (
            <ul className="gw-list">
              {(scope.strawMen ?? []).map((straw) => (
                <StrawManRow
                  key={straw.accountId}
                  straw={straw}
                  busy={actions.busy}
                  onRemove={() => actions.onUnassignStrawMan(scope.id, straw.accountId)}
                />
              ))}
            </ul>
          )}
        </div>
      ) : null}

      {strategy === 'PerGroup' ? (
        <div className="gw-panel">
          <GatewayPanelHead
            title="Grupos de credenciais"
            desc="Grupos cadastrados no repositório de gateway. Todas as credenciais do grupo ficam elegíveis."
          />

          <div className="gw-panel__toolbar">
            <input
              className="nexus-input"
              value={groupIdInput}
              onChange={(e) => setGroupIdInput(e.target.value)}
              placeholder="ID do grupo de credenciais…"
              aria-label="ID do grupo de credenciais"
              onKeyDown={(e) => { if (e.key === 'Enter') submitGroup(); }}
            />
            <IconButton
              icon="link"
              label="Vincular grupo"
              variant="primary"
              disabled={actions.busy || !groupIdInput.trim()}
              onClick={submitGroup}
            />
          </div>

          {(scope.gatewayCredentialsGroups ?? []).length === 0 ? (
            <GatewayEmpty
              title="Nenhum grupo vinculado"
              message="Informe o ID do grupo criado no módulo de gateways."
            />
          ) : (
            <ul className="gw-list">
              {(scope.gatewayCredentialsGroups ?? []).map((group) => (
                <li key={group.id} className="gw-list-row gw-list-row--stacked">
                  <div className="gw-list-row__meta">
                    <span className="admin-op-person-name">{group.name}</span>
                    <span className="admin-op-person-id mono" title={group.id}>{shortId(group.id, 22)}</span>
                    <span className="gw-list-row__tag muted small">
                      {group.credentialCount} credencial{group.credentialCount === 1 ? '' : 'is'}
                    </span>
                  </div>
                  <span className="gw-list-row__action">
                    <IconButton
                      icon="trash"
                      label={`Remover grupo ${group.name}`}
                      variant="danger"
                      disabled={actions.busy}
                      onClick={() => actions.onUnassignGatewayGroup(scope.id, group.id)}
                    />
                  </span>
                </li>
              ))}
            </ul>
          )}
        </div>
      ) : null}

      {strategy === 'Manual' ? (
        <div className="gw-panel">
          <GatewayPanelHead
            title={manualCopy.title}
            desc={manualCopy.desc}
            action={(
              <IconButton
                icon="plus"
                label="Vincular credencial"
                variant="primary"
                disabled={actions.busy}
                onClick={() => actions.onAssignGatewayCredential(scope.id)}
              />
            )}
          />

          {(scope.gatewayCredentials ?? []).length === 0 ? (
            <GatewayEmpty title={manualCopy.emptyTitle} message={manualCopy.emptyMessage} />
          ) : (
            <ul className="gw-list">
              {(scope.gatewayCredentials ?? []).map((credential) => (
                <li key={credential.id} className="gw-list-row gw-list-row--stacked">
                  <div className="gw-list-row__meta">
                    <span className="gw-list-row__title-row">
                      <span className="admin-op-person-name">{credential.name}</span>
                      <span className="admin-op-gateway-badge">
                        {GATEWAY_LABELS[credential.gateway] ?? credential.gateway}
                      </span>
                    </span>
                    <span className="admin-op-person-id mono" title={credential.id}>{shortId(credential.id, 22)}</span>
                  </div>
                  <span className="gw-list-row__action">
                    <IconButton
                      icon="trash"
                      label={`Remover credencial ${credential.name}`}
                      variant="danger"
                      disabled={actions.busy}
                      onClick={() => actions.onUnassignGatewayCredential(scope.id, credential.id)}
                    />
                  </span>
                </li>
              ))}
            </ul>
          )}
        </div>
      ) : null}
    </div>
  );
}
