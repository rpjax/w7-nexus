import { useNavigate } from 'react-router-dom';
import type { ColumnDef } from '@tanstack/react-table';
import type { ScriptSummary } from '@/api/scripts/types';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { formatRelativeTime } from '@/features/scripts/formatRelativeTime';
import { scriptStudioPath } from '@/features/scripts/scriptPaths';
import { Copy, MoreHorizontal } from 'lucide-react';

function prodVersion(script: ScriptSummary): string | null {
  return script.channels.find((channel) => channel.routeValue === 'prod')?.version ?? null;
}

function resolutionLabel(script: ScriptSummary): string {
  return script.hostPatterns.length > 0 ? 'Host-scoped' : 'Name-only';
}

export function createScriptColumns(): ColumnDef<ScriptSummary>[] {
  return [
    {
      accessorKey: 'name',
      header: 'Script',
      cell: ({ row }) => {
        const script = row.original;
        const liveProd = prodVersion(script);
        return (
          <div className="min-w-0">
            <div className="flex flex-wrap items-center gap-2">
              <p className="font-medium">{script.name}</p>
              <Badge variant="outline" className="font-mono text-xs font-normal">
                P{script.priority}
              </Badge>
              {liveProd ? (
                <Badge variant="success" className="font-mono text-xs font-normal">
                  prod · {liveProd}
                </Badge>
              ) : (
                <Badge variant="warning">Sem prod</Badge>
              )}
            </div>
            {script.description ? (
              <p className="mt-1 line-clamp-2 text-sm text-muted-foreground">{script.description}</p>
            ) : null}
          </div>
        );
      },
    },
    {
      id: 'mode',
      header: 'Modo',
      cell: ({ row }) => (
        <Badge variant={row.original.hostPatterns.length > 0 ? 'info' : 'secondary'}>
          {resolutionLabel(row.original)}
        </Badge>
      ),
    },
    {
      id: 'hosts',
      header: 'Hosts',
      cell: ({ row }) => {
        const count = row.original.hostPatterns.length;
        return count > 0 ? `${count} padrão(ões)` : '—';
      },
    },
    {
      id: 'channels',
      header: 'Canais',
      cell: ({ row }) => `${row.original.channels.length}`,
    },
    {
      accessorKey: 'updatedAt',
      header: 'Atualizado',
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground">
          {formatRelativeTime(row.original.updatedAt)}
        </span>
      ),
    },
    {
      id: 'actions',
      header: '',
      cell: ({ row }) => <ScriptRowActions script={row.original} />,
    },
  ];
}

function ScriptRowActions({ script }: { script: ScriptSummary }) {
  const navigate = useNavigate();
  const href = scriptStudioPath(script.id);

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon-sm" aria-label="Ações" onClick={(e) => e.stopPropagation()}>
          <MoreHorizontal className="size-4" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuItem onClick={() => navigate(href)}>Abrir studio</DropdownMenuItem>
        <DropdownMenuItem onClick={() => void navigator.clipboard.writeText(script.id)}>
          <Copy className="size-4" />
          Copiar ID
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
