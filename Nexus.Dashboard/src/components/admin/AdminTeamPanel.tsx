import type { ReactNode } from 'react';
import type { OperatorDetails, TeamDetails } from '../../api/types';
import { Link2, Percent, Plus, Trash2 } from 'lucide-react';
import { shortId } from '../../utils/format';
import { AdminTeamGatewaySection } from './AdminTeamGatewaySection';
import type { AdminTeamPanelActions, AdminTeamPanelScope } from './adminTeamTypes';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader } from '@/components/ui/card';
import { Separator } from '@/components/ui/separator';
import { cn } from '@/lib/utils';

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
}: {
  title: string;
  desc?: string;
  action?: ReactNode;
  children: ReactNode;
  className?: string;
  variant?: 'list' | 'detail';
}) {
  return (
    <section className={cn('space-y-3', className)}>
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0 space-y-1">
          <h2 className="text-base font-semibold text-foreground">{title}</h2>
          {desc ? <p className="text-sm text-muted-foreground">{desc}</p> : null}
        </div>
        {action ?? null}
      </div>
      <div>{children}</div>
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
    <li className="flex items-center gap-3 rounded-lg border border-border/50 bg-background/40 px-3 py-2">
      <Avatar size="sm">
        <AvatarFallback>{personInitial(username)}</AvatarFallback>
      </Avatar>
      <span className="min-w-0 flex-1">
        <span className="block truncate text-sm font-medium text-foreground">
          {personLabel(accountId, username)}
        </span>
        <span className="block truncate font-mono text-xs text-muted-foreground" title={accountId}>
          {shortId(accountId, 22)}
        </span>
      </span>
      {action ? <span className="shrink-0">{action}</span> : null}
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
    <li className="space-y-3 rounded-xl border border-border/60 bg-background/40 p-3">
      <div className="flex items-center justify-between gap-3">
        <div className="flex min-w-0 items-center gap-3">
          <Avatar size="sm">
            <AvatarFallback>{personInitial(operator.username)}</AvatarFallback>
          </Avatar>
          <div className="min-w-0">
            <span className="block truncate text-sm font-medium text-foreground">{label}</span>
            <span className="block truncate font-mono text-xs text-muted-foreground" title={operator.accountId}>
              {shortId(operator.accountId, 18)}
            </span>
          </div>
        </div>
        <div className="flex shrink-0 items-center gap-1">
          <Button
            type="button"
            variant="ghost"
            size="icon-sm"
            aria-label={`Editar repasse de ${label}`}
            disabled={busy}
            onClick={() => onEditProfitShare(teamId, operator)}
          >
            <Percent className="size-4" />
          </Button>
          <Button
            type="button"
            variant="destructive"
            size="icon-sm"
            aria-label={`Remover operador ${label}`}
            disabled={busy}
            onClick={() => onUnassignOperator(teamId, operator.accountId)}
          >
            <Trash2 className="size-4" />
          </Button>
        </div>
      </div>

      <div className="rounded-lg border border-border/50 bg-muted/20 px-3 py-2">
        <span className="mb-2 flex items-center gap-1.5 text-xs font-medium uppercase tracking-wide text-muted-foreground">
          <Percent className="size-3" />
          Repasse
        </span>
        {cuts.length === 0 ? (
          <span className="text-sm text-muted-foreground">Sem repasse configurado</span>
        ) : (
          <ul className="space-y-1" aria-label={`Regras de repasse de ${label}`}>
            {cuts.map((cut) => (
              <li key={`${cut.accountId}-${cut.percentage}`} className="flex items-center justify-between gap-2 text-sm">
                <span className="truncate text-foreground">{personLabel(cut.accountId, cut.username)}</span>
                <span className="shrink-0 font-medium text-foreground">{formatPercent(cut.percentage)}</span>
              </li>
            ))}
          </ul>
        )}
      </div>
    </li>
  );
}

export function AdminTeamPanel({ team, scope, actions, variant = 'list' }: AdminTeamPanelProps) {
  const isDetail = variant === 'detail';
  const showStructure = scope === 'full' || scope === 'operation-admin';
  const showPeople = scope === 'full' || scope === 'team-leader';
  const showGateway = scope === 'full' || scope === 'operation-admin';

  const leaderAssignAction = team.teamLeader ? (
    <Button
      type="button"
      variant="destructive"
      size="icon-sm"
      aria-label={`Remover líder ${personLabel(team.teamLeader.accountId, team.teamLeader.username)}`}
      disabled={actions.busy}
      onClick={() => actions.onUnassignLeader(team.id)}
    >
      <Trash2 className="size-4" />
    </Button>
  ) : (
    <Button
      type="button"
      size="icon-sm"
      aria-label="Vincular líder"
      disabled={actions.busy}
      onClick={() => actions.onAssignLeader(team.id)}
    >
      <Link2 className="size-4" />
    </Button>
  );

  const facts = (
    <dl className="grid gap-2 sm:grid-cols-2 lg:grid-cols-4">
      <div className="space-y-1 rounded-lg border border-border/50 bg-background/40 px-3 py-2">
        <dt className="text-xs font-medium uppercase tracking-wide text-muted-foreground">ID</dt>
        <dd className="truncate font-mono text-sm" title={team.id}>{shortId(team.id, isDetail ? 24 : 18)}</dd>
      </div>
      {showStructure ? (
        <div className="space-y-1 rounded-lg border border-border/50 bg-background/40 px-3 py-2">
          <dt className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Líder</dt>
          <dd className="truncate text-sm">
            {team.teamLeader
              ? personLabel(team.teamLeader.accountId, team.teamLeader.username)
              : '—'}
          </dd>
        </div>
      ) : null}
      {showPeople ? (
        <div className="space-y-1 rounded-lg border border-border/50 bg-background/40 px-3 py-2">
          <dt className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Operadores</dt>
          <dd className="text-sm">{team.operators.length}</dd>
        </div>
      ) : null}
      {showGateway && team.gatewaySelectionStrategy ? (
        <div className="space-y-1 rounded-lg border border-border/50 bg-background/40 px-3 py-2">
          <dt className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Gateway</dt>
          <dd className="text-sm font-medium">{gatewayStrategyLabel(team.gatewaySelectionStrategy)}</dd>
        </div>
      ) : null}
    </dl>
  );

  return (
    <Card className="border-border/60 bg-card/80">
      {!isDetail ? (
        <CardHeader className="gap-3">
          <div className="flex items-start justify-between gap-3">
            <div className="min-w-0 space-y-0.5">
              <span className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Equipe</span>
              <h5 className="truncate text-base font-semibold text-foreground">{team.name}</h5>
            </div>
            {showStructure ? (
              <Button
                type="button"
                variant="destructive"
                size="icon-sm"
                aria-label={`Excluir equipe ${team.name}`}
                disabled={actions.busy}
                onClick={() => actions.onDeleteTeam(team.id)}
              >
                <Trash2 className="size-4" />
              </Button>
            ) : null}
          </div>
          {facts}
        </CardHeader>
      ) : null}

      <CardContent className={cn('space-y-6', isDetail ? 'pt-6' : 'border-t border-border/60 pt-4')}>
        {isDetail ? (
          <TeamSection title="Visão geral">
            <div className="space-y-3">
              <h3 className="text-lg font-semibold text-foreground">{team.name}</h3>
              {facts}
            </div>
          </TeamSection>
        ) : null}

        {isDetail && showStructure ? (
          <>
            <Separator />
            <TeamSection title="Ações">
              <Button
                type="button"
                variant="destructive"
                size="sm"
                disabled={actions.busy}
                onClick={() => actions.onDeleteTeam(team.id)}
              >
                <Trash2 className="size-4" />
                Excluir equipe
              </Button>
            </TeamSection>
          </>
        ) : null}

        {showStructure ? (
          <>
            {isDetail ? <Separator /> : null}
            <TeamSection
              variant={variant}
              title="Líder"
              desc="Responsável pela equipe."
              action={leaderAssignAction}
            >
              {team.teamLeader ? (
                isDetail ? (
                  <ul className="space-y-2">
                    <PersonRow
                      accountId={team.teamLeader.accountId}
                      username={team.teamLeader.username}
                    />
                  </ul>
                ) : (
                  <div className="flex items-center gap-3 rounded-lg border border-border/50 bg-background/40 px-3 py-2">
                    <Avatar>
                      <AvatarFallback>{personInitial(team.teamLeader.username)}</AvatarFallback>
                    </Avatar>
                    <div className="min-w-0">
                      <span className="block text-xs text-muted-foreground">Líder da equipe</span>
                      <span className="block truncate text-sm font-medium text-foreground">
                        {personLabel(team.teamLeader.accountId, team.teamLeader.username)}
                      </span>
                      <span className="block truncate font-mono text-xs text-muted-foreground" title={team.teamLeader.accountId}>
                        {shortId(team.teamLeader.accountId, 18)}
                      </span>
                    </div>
                  </div>
                )
              ) : (
                <p className="text-sm text-muted-foreground">
                  Nenhum líder vinculado.
                </p>
              )}
            </TeamSection>
          </>
        ) : null}

        {showPeople ? (
          <>
            {showStructure ? <Separator /> : null}
            <TeamSection
              variant={variant}
              title="Operadores"
              desc="Alocação e regras de repasse."
              action={(
                <Button
                  type="button"
                  size="icon-sm"
                  aria-label="Alocar operador"
                  disabled={actions.busy}
                  onClick={() => actions.onAssignOperator(team.id)}
                >
                  <Plus className="size-4" />
                </Button>
              )}
            >
              {team.operators.length === 0 ? (
                <p className="text-sm text-muted-foreground">Nenhum operador alocado.</p>
              ) : (
                <ul className="space-y-2">
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
                      <li key={operator.accountId} className="space-y-3 rounded-xl border border-border/60 bg-background/40 p-3">
                        <div className="flex items-center justify-between gap-3">
                          <div className="flex min-w-0 items-center gap-3">
                            <Avatar size="sm">
                              <AvatarFallback>{personInitial(operator.username)}</AvatarFallback>
                            </Avatar>
                            <div className="min-w-0">
                              <span className="block truncate text-sm font-medium text-foreground">
                                {personLabel(operator.accountId, operator.username)}
                              </span>
                              <span className="block truncate font-mono text-xs text-muted-foreground" title={operator.accountId}>
                                {shortId(operator.accountId, 18)}
                              </span>
                            </div>
                          </div>
                          <div className="flex shrink-0 items-center gap-1">
                            <Button
                              type="button"
                              variant="ghost"
                              size="icon-sm"
                              aria-label={`Editar repasse de ${personLabel(operator.accountId, operator.username)}`}
                              disabled={actions.busy}
                              onClick={() => actions.onEditProfitShare(team.id, operator)}
                            >
                              <Percent className="size-4" />
                            </Button>
                            <Button
                              type="button"
                              variant="destructive"
                              size="icon-sm"
                              aria-label={`Remover operador ${personLabel(operator.accountId, operator.username)}`}
                              disabled={actions.busy}
                              onClick={() => actions.onUnassignOperator(team.id, operator.accountId)}
                            >
                              <Trash2 className="size-4" />
                            </Button>
                          </div>
                        </div>

                        <div className="rounded-lg border border-border/50 bg-muted/20 px-3 py-2">
                          <span className="mb-2 block text-xs font-medium uppercase tracking-wide text-muted-foreground">
                            Repasse
                          </span>
                          {(operator.profitShareRule?.cuts ?? []).length === 0 ? (
                            <p className="text-sm text-muted-foreground">Sem repasse configurado.</p>
                          ) : (
                            <ul className="space-y-1">
                              {(operator.profitShareRule?.cuts ?? []).map((cut) => (
                                <li key={`${cut.accountId}-${cut.percentage}`} className="flex items-center justify-between gap-2 text-sm">
                                  <span className="truncate">{personLabel(cut.accountId, cut.username)}</span>
                                  <span className="shrink-0 font-medium">{formatPercent(cut.percentage)}</span>
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
          </>
        ) : null}

        {showGateway ? (
          <>
            <Separator />
            <TeamSection
              variant={variant}
              title="Gateway"
              desc="Estratégia de roteamento e credenciais."
            >
              <AdminTeamGatewaySection team={team} actions={actions} showHeader={false} />
            </TeamSection>
          </>
        ) : null}
      </CardContent>
    </Card>
  );
}
