import type { ReactNode } from 'react';
import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import type { OperationDetails, OperationWithLedTeamsDetails } from '../../api/types';
import { DataTable } from '@/components/data/data-table';
import { createTeamColumns } from '../../features/teams/team-columns';
import { teamDetailPath } from '../../features/teams/teamPaths';
import { Copy, Plus, Trash2 } from 'lucide-react';
import { formatDateTime, shortId } from '../../utils/format';
import { AdminGatewaySection } from './AdminGatewaySection';
import { AdminTeamPanel, type AdminTeamPanelActions, type AdminTeamPanelScope } from './AdminTeamPanel';
import { CreateTeamModal } from './CreateTeamModal';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Separator } from '@/components/ui/separator';
import { cn } from '@/lib/utils';

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

function gatewayStrategyLabel(strategy: NonNullable<OperationDetails['gatewaySelectionStrategy']>): string {
  if (strategy === 'PerStrawman') return 'Laranja';
  if (strategy === 'PerGroup') return 'Grupo';
  return 'Manual';
}

function isManagedOperation(
  operation: OperationDetails | OperationWithLedTeamsDetails,
): operation is OperationDetails {
  return 'gatewaySelectionStrategy' in operation;
}

type TeamDetailsLike = OperationDetails['teams'][number];

function countOperators(teams: TeamDetailsLike[]): number {
  return teams.reduce((sum, team) => sum + team.operators.length, 0);
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
  const sectionTitle = title ?? kicker;

  return (
    <section className="space-y-3">
      {(sectionTitle || desc || action) ? (
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0 space-y-1">
            {sectionTitle ? <h2 className="text-base font-semibold text-foreground">{sectionTitle}</h2> : null}
            {title && kicker ? <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{kicker}</p> : null}
            {desc ? <p className="text-sm text-muted-foreground">{desc}</p> : null}
          </div>
          {action ?? null}
        </div>
      ) : null}
      <div>{children}</div>
    </section>
  );
}

function teamPanelScope(scope: OperationCardScope): AdminTeamPanelScope {
  if (scope === 'operation-admin') return 'operation-admin';
  if (scope === 'team-leader') return 'team-leader';
  return 'full';
}

export function AdminOperationCard({ operation, scope, actions }: AdminOperationCardProps) {
  const navigate = useNavigate();
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
  const showOperationGateway = (scope === 'global-admin' || scope === 'operation-admin') && isManagedOperation(operation);
  const canCreateTeam = scope !== 'team-leader';
  const useTeamList = scope === 'global-admin' || scope === 'operation-admin';
  const teamColumns = useMemo(() => {
    if (scope === 'team-leader') return [];
    return createTeamColumns(scope, operation.id, {
      onDelete: actions.onDeleteTeam,
      deleteBusy: actions.busy,
    });
  }, [scope, operation.id, actions.onDeleteTeam, actions.busy]);

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
    <Card className="border-border/60 bg-card/80">
      <CardContent className="space-y-6 pt-6">
        <OpSection title="Visão geral">
          <div className="space-y-4">
            <div className="space-y-1">
              <h3 className="text-lg font-semibold text-foreground">{operation.name}</h3>
              {description ? (
                <p className="text-sm text-muted-foreground">{description}</p>
              ) : (
                <p className="text-sm text-muted-foreground">Sem descrição cadastrada.</p>
              )}
            </div>

            <dl className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
              <div className="space-y-1 rounded-lg border border-border/50 bg-background/40 px-3 py-2">
                <dt className="text-xs font-medium uppercase tracking-wide text-muted-foreground">ID</dt>
                <dd className="flex items-center gap-1 font-mono text-sm" title={operation.id}>
                  <span className="truncate">{shortId(operation.id, 24)}</span>
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon-sm"
                    aria-label="Copiar ID da operação"
                    onClick={() => void copyId()}
                  >
                    <Copy className="size-4" />
                  </Button>
                </dd>
              </div>
              <div className="space-y-1 rounded-lg border border-border/50 bg-background/40 px-3 py-2">
                <dt className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Criada</dt>
                <dd className="text-sm text-foreground">{formatDateTime(operation.createdAt)}</dd>
              </div>
              {scope !== 'team-leader' ? (
                <div className="space-y-1 rounded-lg border border-border/50 bg-background/40 px-3 py-2">
                  <dt className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Atualizada</dt>
                  <dd className="text-sm text-foreground">{formatDateTime(operation.updatedAt)}</dd>
                </div>
              ) : null}
            </dl>

            <dl className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4" aria-label="Resumo da operação">
              {scope === 'global-admin' ? (
                <div className={cn(
                  'rounded-lg border px-3 py-2',
                  adminCount === 0 ? 'border-warning/40 bg-warning/5' : 'border-border/50 bg-background/40',
                )}>
                  <dt className="text-xs text-muted-foreground">Administradores</dt>
                  <dd className="text-xl font-semibold text-foreground">{adminCount}</dd>
                </div>
              ) : null}
              <div className="rounded-lg border border-border/50 bg-background/40 px-3 py-2">
                <dt className="text-xs text-muted-foreground">Equipes</dt>
                <dd className="text-xl font-semibold text-foreground">{teamCount}</dd>
              </div>
              {scope !== 'operation-admin' ? (
                <div className="rounded-lg border border-border/50 bg-background/40 px-3 py-2">
                  <dt className="text-xs text-muted-foreground">Operadores</dt>
                  <dd className="text-xl font-semibold text-foreground">{operatorCount}</dd>
                </div>
              ) : null}
              {showOperationGateway && operation.gatewaySelectionStrategy ? (
                <div className="rounded-lg border border-border/50 bg-background/40 px-3 py-2">
                  <dt className="text-xs text-muted-foreground">Gateway fallback</dt>
                  <dd className="text-base font-semibold text-foreground">
                    {gatewayStrategyLabel(operation.gatewaySelectionStrategy)}
                  </dd>
                </div>
              ) : null}
            </dl>
          </div>
        </OpSection>

        {showActionsSection ? (
          <>
            <Separator />
            <OpSection title="Ações">
              <Button
                type="button"
                variant="destructive"
                size="sm"
                disabled={actions.busy}
                onClick={() => actions.onDelete(operation.id)}
              >
                <Trash2 className="size-4" />
                Excluir operação
              </Button>
            </OpSection>
          </>
        ) : null}

        {showAdminSection ? (
          <>
            <Separator />
            <OpSection
              title="Administradores"
              desc="Quem administra esta operação."
              action={(
                <Button
                  type="button"
                  size="icon-sm"
                  aria-label="Vincular administrador"
                  disabled={actions.busy}
                  onClick={() => actions.onAssignAdministrator(operation.id)}
                >
                  <Plus className="size-4" />
                </Button>
              )}
            >
              {adminCount === 0 ? (
                <p className="text-sm text-muted-foreground">Nenhum administrador vinculado.</p>
              ) : (
                <ul className="space-y-2">
                  {administrators.map((admin) => (
                    <PersonRow
                      key={admin.accountId}
                      accountId={admin.accountId}
                      username={admin.username}
                      action={(
                        <Button
                          type="button"
                          variant="destructive"
                          size="icon-sm"
                          aria-label={`Remover administrador ${personLabel(admin.accountId, admin.username)}`}
                          disabled={actions.busy}
                          onClick={() => actions.onRemoveAdministrator(operation.id, admin.accountId)}
                        >
                          <Trash2 className="size-4" />
                        </Button>
                      )}
                    />
                  ))}
                </ul>
              )}
            </OpSection>
          </>
        ) : null}

        {showOperationGateway ? (
          <>
            <Separator />
            <OpSection
              title="Gateway da operação"
              desc="Fallback de credenciais quando nenhum operador é informado na cobrança."
            >
              <AdminGatewaySection
                scope={operation}
                actions={teamActions}
                variant="operation"
                showHeader={false}
              />
            </OpSection>
          </>
        ) : null}

        <Separator />
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
            <Button
              type="button"
              size="icon-sm"
              aria-label="Criar equipe"
              disabled={actions.busy}
              onClick={() => setCreateTeamOpen(true)}
            >
              <Plus className="size-4" />
            </Button>
          ) : undefined}
        >
          {teamCount === 0 ? (
            <p className="text-sm text-muted-foreground">
              {scope === 'team-leader'
                ? 'Nenhuma equipe liderada nesta operação.'
                : 'Nenhuma equipe vinculada. Crie a primeira equipe.'}
            </p>
          ) : useTeamList ? (
            <DataTable
              columns={teamColumns}
              data={teams}
              getRowId={(team) => team.id}
              onRowClick={(team) => navigate(teamDetailPath(scope, operation.id, team.id))}
            />
          ) : (
            <div className="space-y-3">
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
      </CardContent>

      <CreateTeamModal
        open={createTeamOpen}
        busy={actions.busy}
        operationName={operation.name}
        onClose={() => setCreateTeamOpen(false)}
        onSubmit={submitCreateTeam}
      />
    </Card>
  );
}
