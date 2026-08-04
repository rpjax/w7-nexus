import { useState, type ReactNode } from 'react';
import type { GatewaySelectionStrategy, TeamAccountDetails } from '../../api/types';
import { Check, Link2, Plus, Trash2 } from 'lucide-react';
import { shortId } from '../../utils/format';
import {
  GATEWAY_LABELS,
  GATEWAY_STRATEGY_OPTIONS,
  type AdminGatewayActions,
  type GatewayScopeDetails,
  type GatewaySectionVariant,
} from './adminGatewayTypes';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group';
import { cn } from '@/lib/utils';

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
    <div className="mb-3 flex items-start justify-between gap-3">
      <div className="min-w-0 space-y-1">
        <h3 className="text-sm font-semibold text-foreground">{title}</h3>
        {desc ? <p className="text-sm text-muted-foreground">{desc}</p> : null}
      </div>
      {action ?? null}
    </div>
  );
}

function GatewayEmpty({ title, message }: { title: string; message: string }) {
  return (
    <div className="rounded-lg border border-dashed border-border/60 bg-muted/20 px-3 py-4 text-center">
      <p className="text-sm font-medium text-foreground">{title}</p>
      <p className="mt-1 text-sm text-muted-foreground">{message}</p>
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
    <li className="flex items-center gap-3 rounded-lg border border-border/50 bg-background/40 px-3 py-2">
      <Avatar size="sm">
        <AvatarFallback>{personInitial(straw.username)}</AvatarFallback>
      </Avatar>
      <span className="min-w-0 flex-1">
        <span className="block truncate text-sm font-medium text-foreground">
          {personLabel(straw.accountId, straw.username)}
        </span>
        <span className="block truncate font-mono text-xs text-muted-foreground" title={straw.accountId}>
          {shortId(straw.accountId, 22)}
        </span>
      </span>
      <Button
        type="button"
        variant="destructive"
        size="icon-sm"
        aria-label={`Remover laranja ${personLabel(straw.accountId, straw.username)}`}
        disabled={busy}
        onClick={onRemove}
      >
        <Trash2 className="size-4" />
      </Button>
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
    <div className="space-y-4">
      {showHeader ? (
        <div className="space-y-1">
          <h2 className="text-base font-semibold text-foreground">Gateway</h2>
          <p className="text-sm text-muted-foreground">
            {intro ?? 'Estratégia de roteamento e credenciais.'}
          </p>
        </div>
      ) : null}

      {intro && !showHeader ? (
        <p className="text-sm text-muted-foreground">{intro}</p>
      ) : null}

      <div
        className={cn(
          'flex items-start gap-2 rounded-lg border px-3 py-2 text-sm',
          status.tone === 'ok'
            ? 'border-success/30 bg-success/5 text-foreground'
            : 'border-warning/30 bg-warning/5 text-foreground',
        )}
        role="status"
      >
        <span
          className={cn(
            'mt-1.5 size-2 shrink-0 rounded-full',
            status.tone === 'ok' ? 'bg-success' : 'bg-warning',
          )}
          aria-hidden="true"
        />
        <span>{status.message}</span>
      </div>

      <div className="space-y-2">
        <p className="text-sm font-medium text-foreground">Estratégia de roteamento</p>
        <RadioGroup
          value={strategy}
          onValueChange={(value) => {
            if (value !== strategy) {
              actions.onGatewayStrategyChange(scope.id, value as GatewaySelectionStrategy);
            }
          }}
          disabled={actions.busy}
          className="grid gap-2 sm:grid-cols-3"
          aria-label="Estratégia de gateway"
        >
          {GATEWAY_STRATEGY_OPTIONS.map((opt) => {
            const active = strategy === opt.value;
            return (
              <Label
                key={opt.value}
                htmlFor={`gateway-strategy-${opt.value}`}
                className={cn(
                  'relative cursor-pointer rounded-xl border px-3 py-3 text-left transition-colors',
                  active
                    ? 'border-primary/50 bg-primary/10 ring-1 ring-primary/20'
                    : 'border-border/60 bg-card/40 hover:bg-muted/40',
                  actions.busy && 'cursor-not-allowed opacity-50',
                )}
              >
                <RadioGroupItem
                  id={`gateway-strategy-${opt.value}`}
                  value={opt.value}
                  className="sr-only"
                />
                {active ? (
                  <span className="absolute top-2 right-2 text-primary" aria-hidden="true">
                    <Check className="size-3.5" />
                  </span>
                ) : null}
                <span className="block text-sm font-medium text-foreground">{opt.label}</span>
                <span className="mt-0.5 block text-xs text-muted-foreground">{opt.hint}</span>
              </Label>
            );
          })}
        </RadioGroup>
        <p className="text-sm text-muted-foreground">{strategyMeta.detail}</p>
      </div>

      {strategy === 'PerStrawman' ? (
        <div className="rounded-xl border border-border/60 bg-card/40 p-3">
          <GatewayPanelHead
            title={strawCopy.title}
            desc="Contas usadas para filtrar credenciais nos gateways (Frendz, SigiloPay, Wintech)."
            action={(
              <Button
                type="button"
                size="icon-sm"
                aria-label="Vincular laranja"
                disabled={actions.busy}
                onClick={() => actions.onAssignStrawMan(scope.id)}
              >
                <Plus className="size-4" />
              </Button>
            )}
          />

          {(scope.strawMen ?? []).length === 0 ? (
            <GatewayEmpty title={strawCopy.emptyTitle} message={strawCopy.emptyMessage} />
          ) : (
            <ul className="space-y-2">
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
        <div className="rounded-xl border border-border/60 bg-card/40 p-3">
          <GatewayPanelHead
            title="Grupos de credenciais"
            desc="Grupos cadastrados no repositório de gateway. Todas as credenciais do grupo ficam elegíveis."
          />

          <div className="mb-3 flex gap-2">
            <Input
              value={groupIdInput}
              onChange={(e) => setGroupIdInput(e.target.value)}
              placeholder="ID do grupo de credenciais…"
              aria-label="ID do grupo de credenciais"
              onKeyDown={(e) => { if (e.key === 'Enter') submitGroup(); }}
            />
            <Button
              type="button"
              size="icon-sm"
              aria-label="Vincular grupo"
              disabled={actions.busy || !groupIdInput.trim()}
              onClick={submitGroup}
            >
              <Link2 className="size-4" />
            </Button>
          </div>

          {(scope.gatewayCredentialsGroups ?? []).length === 0 ? (
            <GatewayEmpty
              title="Nenhum grupo vinculado"
              message="Informe o ID do grupo criado no módulo de gateways."
            />
          ) : (
            <ul className="space-y-2">
              {(scope.gatewayCredentialsGroups ?? []).map((group) => (
                <li key={group.id} className="flex items-start justify-between gap-3 rounded-lg border border-border/50 bg-background/40 px-3 py-2">
                  <div className="min-w-0 space-y-0.5">
                    <span className="block truncate text-sm font-medium text-foreground">{group.name}</span>
                    <span className="block truncate font-mono text-xs text-muted-foreground" title={group.id}>
                      {shortId(group.id, 22)}
                    </span>
                    <span className="text-xs text-muted-foreground">
                      {group.credentialCount} credencial{group.credentialCount === 1 ? '' : 'is'}
                    </span>
                  </div>
                  <Button
                    type="button"
                    variant="destructive"
                    size="icon-sm"
                    aria-label={`Remover grupo ${group.name}`}
                    disabled={actions.busy}
                    onClick={() => actions.onUnassignGatewayGroup(scope.id, group.id)}
                  >
                    <Trash2 className="size-4" />
                  </Button>
                </li>
              ))}
            </ul>
          )}
        </div>
      ) : null}

      {strategy === 'Manual' ? (
        <div className="rounded-xl border border-border/60 bg-card/40 p-3">
          <GatewayPanelHead
            title={manualCopy.title}
            desc={manualCopy.desc}
            action={(
              <Button
                type="button"
                size="icon-sm"
                aria-label="Vincular credencial"
                disabled={actions.busy}
                onClick={() => actions.onAssignGatewayCredential(scope.id)}
              >
                <Plus className="size-4" />
              </Button>
            )}
          />

          {(scope.gatewayCredentials ?? []).length === 0 ? (
            <GatewayEmpty title={manualCopy.emptyTitle} message={manualCopy.emptyMessage} />
          ) : (
            <ul className="space-y-2">
              {(scope.gatewayCredentials ?? []).map((credential) => (
                <li key={credential.id} className="flex items-start justify-between gap-3 rounded-lg border border-border/50 bg-background/40 px-3 py-2">
                  <div className="min-w-0 space-y-0.5">
                    <span className="flex flex-wrap items-center gap-2">
                      <span className="truncate text-sm font-medium text-foreground">{credential.name}</span>
                      <Badge variant="secondary">
                        {GATEWAY_LABELS[credential.gateway] ?? credential.gateway}
                      </Badge>
                    </span>
                    <span className="block truncate font-mono text-xs text-muted-foreground" title={credential.id}>
                      {shortId(credential.id, 22)}
                    </span>
                  </div>
                  <Button
                    type="button"
                    variant="destructive"
                    size="icon-sm"
                    aria-label={`Remover credencial ${credential.name}`}
                    disabled={actions.busy}
                    onClick={() => actions.onUnassignGatewayCredential(scope.id, credential.id)}
                  >
                    <Trash2 className="size-4" />
                  </Button>
                </li>
              ))}
            </ul>
          )}
        </div>
      ) : null}
    </div>
  );
}
