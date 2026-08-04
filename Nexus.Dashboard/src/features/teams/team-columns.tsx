import { useNavigate } from 'react-router-dom';
import type { ColumnDef } from '@tanstack/react-table';
import type { TeamDetails } from '@/api/types';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { cn } from '@/lib/utils';
import { shortId } from '@/utils/format';
import { teamDetailPath, type TeamScope } from './teamPaths';
import { Copy, MoreHorizontal, Trash2 } from 'lucide-react';

function gatewayStrategyLabel(strategy: NonNullable<TeamDetails['gatewaySelectionStrategy']>): string {
  if (strategy === 'PerStrawman') return 'Laranja';
  if (strategy === 'PerGroup') return 'Grupo';
  return 'Manual';
}

function leaderLabel(team: TeamDetails): string {
  if (!team.teamLeader) return '—';
  const { username, accountId } = team.teamLeader;
  return username && username !== accountId ? username : shortId(accountId, 10);
}

export function createTeamColumns(
  scope: TeamScope,
  operationId: string,
  options?: {
    onDelete?: (teamId: string) => void;
    deleteBusy?: boolean;
  },
): ColumnDef<TeamDetails>[] {
  const columns: ColumnDef<TeamDetails>[] = [
    {
      accessorKey: 'name',
      header: 'Equipe',
      cell: ({ row }) => {
        const team = row.original;
        return (
          <div className="min-w-0">
            <p className="font-medium">{team.name}</p>
            <p className="truncate font-mono text-xs text-muted-foreground" title={team.id}>
              {shortId(team.id, 16)}
            </p>
          </div>
        );
      },
    },
    {
      id: 'leader',
      header: 'Líder',
      cell: ({ row }) => {
        const team = row.original;
        const hasLeader = Boolean(team.teamLeader);
        return (
          <span
            className={cn('text-sm', !hasLeader && 'font-medium text-destructive')}
            title={team.teamLeader ? leaderLabel(team) : undefined}
          >
            {leaderLabel(team)}
          </span>
        );
      },
    },
  ];

  if (scope === 'global-admin') {
    columns.push({
      id: 'operators',
      header: 'Ops',
      cell: ({ row }) => row.original.operators.length,
    });
  }

  columns.push(
    {
      id: 'gateway',
      header: 'Gateway',
      cell: ({ row }) => {
        const team = row.original;
        return team.gatewaySelectionStrategy
          ? gatewayStrategyLabel(team.gatewaySelectionStrategy)
          : '—';
      },
    },
    {
      id: 'actions',
      header: '',
      cell: ({ row }) => (
        <TeamRowActions
          team={row.original}
          scope={scope}
          operationId={operationId}
          onDelete={options?.onDelete}
          deleteBusy={options?.deleteBusy}
        />
      ),
    },
  );

  return columns;
}

function TeamRowActions({
  team,
  scope,
  operationId,
  onDelete,
  deleteBusy,
}: {
  team: TeamDetails;
  scope: TeamScope;
  operationId: string;
  onDelete?: (teamId: string) => void;
  deleteBusy?: boolean;
}) {
  const navigate = useNavigate();
  const href = teamDetailPath(scope, operationId, team.id);

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon-sm" aria-label={`Ações da equipe ${team.name}`}>
          <MoreHorizontal className="size-4" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuItem onClick={() => navigate(href)}>Abrir detalhes</DropdownMenuItem>
        <DropdownMenuItem onClick={() => void navigator.clipboard.writeText(team.id)}>
          <Copy className="size-4" />
          Copiar ID
        </DropdownMenuItem>
        {onDelete ? (
          <DropdownMenuItem
            variant="destructive"
            disabled={deleteBusy}
            onClick={() => onDelete(team.id)}
          >
            <Trash2 className="size-4" />
            Excluir {team.name}
          </DropdownMenuItem>
        ) : null}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
