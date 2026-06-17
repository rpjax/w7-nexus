import { useState } from 'react';
import type { GatewaySelectionStrategy, TeamAccountDetails, TeamDetails } from '../../api/types';
import { shortId } from '../../utils/format';
import type { AdminTeamPanelActions } from './adminTeamTypes';

const GATEWAY_STRATEGY_OPTIONS: {
  value: GatewaySelectionStrategy;
  label: string;
  hint: string;
  detail: string;
}[] = [
  {
    value: 'PerStrawman',
    label: 'Por laranja',
    hint: 'Filtra credenciais pelo laranja vinculado à equipe.',
    detail: 'Ideal quando cada laranja tem credenciais próprias nos gateways.',
  },
  {
    value: 'PerGroup',
    label: 'Por grupo',
    hint: 'Usa grupos pré-configurados de credenciais.',
    detail: 'Vincule um ou mais grupos do repositório de gateway.',
  },
  {
    value: 'Manual',
    label: 'Manual',
    hint: 'Lista fixa de credenciais escolhidas.',
    detail: 'Controle total sobre quais credenciais entram no roteamento.',
  },
];

const GATEWAY_LABELS: Record<string, string> = {
  frendz: 'Frendz',
  sigilopay: 'SigiloPay',
  wintech: 'Wintech',
  desconhecido: 'Gateway',
};

type AdminTeamGatewaySectionProps = {
  team: TeamDetails;
  actions: AdminTeamPanelActions;
};

function personInitial(username: string): string {
  const trimmed = username.trim();
  return trimmed ? trimmed[0]!.toUpperCase() : '?';
}

function personLabel(accountId: string, username: string): string {
  return username && username !== accountId ? username : shortId(accountId, 18);
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
    <li className="admin-op-person admin-op-person--compact">
      <span className="admin-op-person-avatar admin-op-person-avatar--straw" aria-hidden="true">
        {personInitial(straw.username)}
      </span>
      <span className="admin-op-person-meta">
        <span className="admin-op-person-name">{personLabel(straw.accountId, straw.username)}</span>
        <span className="admin-op-person-id mono" title={straw.accountId}>{shortId(straw.accountId, 22)}</span>
      </span>
      <button type="button" className="btn btn-ghost btn-small" disabled={busy} onClick={onRemove}>
        Remover
      </button>
    </li>
  );
}

function gatewayStatus(strategy: GatewaySelectionStrategy, team: TeamDetails): {
  tone: 'ok' | 'warn';
  message: string;
} {
  if (strategy === 'PerStrawman') {
    const count = team.strawMen?.length ?? 0;
    return count === 0
      ? { tone: 'warn', message: 'Nenhum laranja vinculado — cobranças podem usar apenas credenciais genéricas.' }
      : { tone: 'ok', message: `${count} laranja${count === 1 ? '' : 's'} ativo${count === 1 ? '' : 's'} para filtrar credenciais.` };
  }
  if (strategy === 'PerGroup') {
    const count = team.gatewayCredentialsGroups?.length ?? 0;
    return count === 0
      ? { tone: 'warn', message: 'Nenhum grupo vinculado — nenhuma credencial será elegível.' }
      : { tone: 'ok', message: `${count} grupo${count === 1 ? '' : 's'} vinculado${count === 1 ? '' : 's'} à equipe.` };
  }
  const count = team.gatewayCredentials?.length ?? 0;
  return count === 0
    ? { tone: 'warn', message: 'Nenhuma credencial manual — roteamento desabilitado para esta equipe.' }
    : { tone: 'ok', message: `${count} credencial${count === 1 ? '' : 'is'} manual${count === 1 ? '' : 'is'} ativa${count === 1 ? '' : 's'}.` };
}

export function AdminTeamGatewaySection({ team, actions }: AdminTeamGatewaySectionProps) {
  const [groupIdInput, setGroupIdInput] = useState('');
  const strategy = team.gatewaySelectionStrategy ?? 'PerStrawman';
  const strategyMeta = GATEWAY_STRATEGY_OPTIONS.find((opt) => opt.value === strategy) ?? GATEWAY_STRATEGY_OPTIONS[0];
  const status = gatewayStatus(strategy, team);

  function submitGroup() {
    const trimmed = groupIdInput.trim();
    if (!trimmed) return;
    actions.onAssignGatewayGroup(team.id, trimmed);
    setGroupIdInput('');
  }

  return (
    <div className="admin-op-gateway-block">
      <h6 className="admin-op-col-title">Gateway</h6>
      <p className="admin-op-col-desc muted small">Estratégia de roteamento e credenciais.</p>

      <div className={`admin-op-gateway-status admin-op-gateway-status--${status.tone}`}>
        {status.message}
      </div>

      <div className="admin-op-strategy-cards" role="radiogroup" aria-label="Estratégia de gateway">
        {GATEWAY_STRATEGY_OPTIONS.map((opt) => (
          <button
            key={opt.value}
            type="button"
            role="radio"
            aria-checked={strategy === opt.value}
            className={`admin-op-strategy-card ${strategy === opt.value ? 'is-active' : ''}`}
            disabled={actions.busy}
            onClick={() => {
              if (strategy !== opt.value) actions.onGatewayStrategyChange(team.id, opt.value);
            }}
          >
            <span className="admin-op-strategy-card-label">{opt.label}</span>
            <span className="admin-op-strategy-card-hint">{opt.hint}</span>
          </button>
        ))}
      </div>

      <p className="muted small admin-op-strategy-detail">{strategyMeta.detail}</p>

      {strategy === 'PerStrawman' ? (
        <div className="admin-op-gateway-panel">
          <div className="admin-op-resource-head">
            <div>
              <span className="admin-op-resource-label">Laranjas da equipe</span>
              <p className="muted small admin-op-resource-desc">
                Contas usadas para filtrar credenciais nos gateways (Frendz, SigiloPay, Wintech).
              </p>
            </div>
            <button
              type="button"
              className="btn btn-primary btn-small"
              disabled={actions.busy}
              onClick={() => actions.onAssignStrawMan(team.id)}
            >
              Vincular laranja
            </button>
          </div>

          {(team.strawMen ?? []).length === 0 ? (
            <div className="admin-op-gateway-empty">
              <p className="admin-op-empty">Nenhum laranja vinculado.</p>
              <p className="muted small">Vincule contas laranja para restringir quais credenciais entram no fluxo de cobrança.</p>
            </div>
          ) : (
            <ul className="admin-op-person-list">
              {(team.strawMen ?? []).map((straw) => (
                <StrawManRow
                  key={straw.accountId}
                  straw={straw}
                  busy={actions.busy}
                  onRemove={() => actions.onUnassignStrawMan(team.id, straw.accountId)}
                />
              ))}
            </ul>
          )}
        </div>
      ) : null}

      {strategy === 'PerGroup' ? (
        <div className="admin-op-gateway-panel">
          <div className="admin-op-resource-head">
            <div>
              <span className="admin-op-resource-label">Grupos de credenciais</span>
              <p className="muted small admin-op-resource-desc">
                Grupos cadastrados no repositório de gateway. Todas as credenciais do grupo ficam elegíveis.
              </p>
            </div>
          </div>

          <div className="admin-op-group-add">
            <input
              className="nexus-input"
              value={groupIdInput}
              onChange={(e) => setGroupIdInput(e.target.value)}
              placeholder="ID do grupo de credenciais…"
              onKeyDown={(e) => { if (e.key === 'Enter') submitGroup(); }}
            />
            <button
              type="button"
              className="btn btn-primary btn-small"
              disabled={actions.busy || !groupIdInput.trim()}
              onClick={submitGroup}
            >
              Vincular grupo
            </button>
          </div>

          {(team.gatewayCredentialsGroups ?? []).length === 0 ? (
            <div className="admin-op-gateway-empty">
              <p className="admin-op-empty">Nenhum grupo vinculado.</p>
              <p className="muted small">Informe o ID do grupo criado no módulo de gateways.</p>
            </div>
          ) : (
            <ul className="admin-op-gateway-item-list">
              {(team.gatewayCredentialsGroups ?? []).map((group) => (
                <li key={group.id} className="admin-op-gateway-item">
                  <div className="admin-op-gateway-item-main">
                    <strong>{group.name}</strong>
                    <span className="mono muted small" title={group.id}>{shortId(group.id, 22)}</span>
                    <span className="admin-op-gateway-item-meta">
                      {group.credentialCount} credencial{group.credentialCount === 1 ? '' : 'is'}
                    </span>
                  </div>
                  <button
                    type="button"
                    className="btn btn-ghost btn-small"
                    disabled={actions.busy}
                    onClick={() => actions.onUnassignGatewayGroup(team.id, group.id)}
                  >
                    Remover
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      ) : null}

      {strategy === 'Manual' ? (
        <div className="admin-op-gateway-panel">
          <div className="admin-op-resource-head">
            <div>
              <span className="admin-op-resource-label">Credenciais manuais</span>
              <p className="muted small admin-op-resource-desc">
                Somente estas credenciais participam do roteamento de cobrança desta equipe.
              </p>
            </div>
            <button
              type="button"
              className="btn btn-primary btn-small"
              disabled={actions.busy}
              onClick={() => actions.onAssignGatewayCredential(team.id)}
            >
              Vincular credencial
            </button>
          </div>

          {(team.gatewayCredentials ?? []).length === 0 ? (
            <div className="admin-op-gateway-empty">
              <p className="admin-op-empty">Nenhuma credencial vinculada.</p>
              <p className="muted small">Selecione credenciais ativas de Frendz, SigiloPay ou Wintech.</p>
            </div>
          ) : (
            <ul className="admin-op-gateway-item-list">
              {(team.gatewayCredentials ?? []).map((credential) => (
                <li key={credential.id} className="admin-op-gateway-item">
                  <div className="admin-op-gateway-item-main">
                    <strong>{credential.name}</strong>
                    <span className="admin-op-gateway-badge">
                      {GATEWAY_LABELS[credential.gateway] ?? credential.gateway}
                    </span>
                    <span className="mono muted small" title={credential.id}>{shortId(credential.id, 22)}</span>
                  </div>
                  <button
                    type="button"
                    className="btn btn-ghost btn-small"
                    disabled={actions.busy}
                    onClick={() => actions.onUnassignGatewayCredential(team.id, credential.id)}
                  >
                    Remover
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      ) : null}
    </div>
  );
}
