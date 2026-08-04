import { useNavigate } from 'react-router-dom';
import type { ColumnDef } from '@tanstack/react-table';
import type { OperationDetails, OperationWithLedTeamsDetails } from '@/api/types';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { formatDateTime, shortId } from '@/utils/format';
import { detailPath, type OperationScope } from '@/features/operations/operationPaths';
import { Copy, MoreHorizontal, Trash2 } from 'lucide-react';

function countOperators(teams: OperationDetails['teams']): number {
  return teams.reduce((sum, team) => sum + team.operators.length, 0);
}

export function createOperationColumns(
  scope: OperationScope,
  options?: {
    onDelete?: (operationId: string) => void;
    deleteBusy?: boolean;
  },
): ColumnDef<OperationDetails | OperationWithLedTeamsDetails>[] {
  return [
    {
      accessorKey: 'name',
      header: 'Operação',
      cell: ({ row }) => {
        const operation = row.original;
        return (
          <div className="min-w-0">
            <p className="font-medium">{operation.name}</p>
            <p className="truncate text-sm text-muted-foreground">
              {operation.description?.trim() || 'Sem descrição'}
            </p>
          </div>
        );
      },
    },
    {
      id: 'teams',
      header: 'Equipes',
      cell: ({ row }) => row.original.teams?.length ?? 0,
    },
    {
      id: 'operators',
      header: 'Operadores',
      cell: ({ row }) => countOperators((row.original as OperationDetails).teams ?? []),
    },
    {
      accessorKey: 'updatedAt',
      header: 'Atualizada',
      cell: ({ row }) => (
        <div className="text-sm text-muted-foreground">
          <time dateTime={row.original.updatedAt}>{formatDateTime(row.original.updatedAt)}</time>
          <p className="font-mono text-xs">{shortId(row.original.id, 12)}</p>
        </div>
      ),
    },
    {
      id: 'actions',
      header: '',
      cell: ({ row }) => {
        const operation = row.original;
        const href = detailPath(scope, operation.id);
        return (
          <OperationRowActions
            operationId={operation.id}
            operationName={operation.name}
            href={href}
            onDelete={scope === 'global-admin' ? options?.onDelete : undefined}
            deleteBusy={options?.deleteBusy}
          />
        );
      },
    },
  ];
}

function OperationRowActions({
  operationId,
  operationName,
  href,
  onDelete,
  deleteBusy,
}: {
  operationId: string;
  operationName: string;
  href: string;
  onDelete?: (operationId: string) => void;
  deleteBusy?: boolean;
}) {
  const navigate = useNavigate();

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon-sm" aria-label="Ações">
          <MoreHorizontal className="size-4" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuItem onClick={() => navigate(href)}>Abrir detalhes</DropdownMenuItem>
        <DropdownMenuItem
          onClick={() => void navigator.clipboard.writeText(operationId)}
        >
          <Copy className="size-4" />
          Copiar ID
        </DropdownMenuItem>
        {onDelete ? (
          <DropdownMenuItem
            variant="destructive"
            disabled={deleteBusy}
            onClick={() => onDelete(operationId)}
          >
            <Trash2 className="size-4" />
            Excluir {operationName}
          </DropdownMenuItem>
        ) : null}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
