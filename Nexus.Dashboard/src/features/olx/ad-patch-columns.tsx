import type { ColumnDef } from '@tanstack/react-table';
import type { OlxAdminAdPatchRow, OlxOperatorAdPatchRow } from '@/api/olx/types';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import {
  formatAdPatchTitle,
  formatOptionalPrice,
  patchStatusLabel,
  patchStatusTone,
} from '@/features/olx/adPatchDisplay';
import { resolveOlxOperationLabel } from '@/features/olx/useOlxOperationLabels';
import { formatDateTime } from '@/utils/format';
import { MoreHorizontal } from 'lucide-react';

type AdPatchRow = OlxOperatorAdPatchRow | OlxAdminAdPatchRow;

export type AdPatchColumnActions = {
  onImpersonate?: (row: AdPatchRow) => void;
  onEditPrices?: (row: AdPatchRow) => void;
  onUnimpersonate?: (row: AdPatchRow) => void;
  onForceUnimpersonate?: (row: AdPatchRow) => void;
  busy?: boolean;
  currentAccountId?: string | null;
};

export function createAdPatchColumns(
  scope: 'operator' | 'admin',
  options: AdPatchColumnActions & { operationLabels?: Record<string, string>; operatorLabels?: Record<string, string> },
): ColumnDef<AdPatchRow>[] {
  const columns: ColumnDef<AdPatchRow>[] = [
    {
      id: 'ad',
      header: 'Anúncio',
      cell: ({ row }) => {
        const ad = row.original;
        const hasPatchedPrices = ad.originalPrice != null || ad.promotionalPrice != null;
        return (
          <div className="min-w-0">
            <p className="font-medium">{formatAdPatchTitle(ad.adId)}</p>
            {ad.adUrl ? (
              <a
                className="truncate text-sm text-primary underline-offset-2 hover:underline"
                href={ad.adUrl}
                target="_blank"
                rel="noreferrer noopener"
                onClick={(e) => e.stopPropagation()}
              >
                Abrir anúncio
              </a>
            ) : (
              <p className="text-sm text-muted-foreground">Sem URL</p>
            )}
            <div className="mt-1 flex flex-wrap gap-1">
              <StatusBadge impersonating={ad.isImpersonating} />
              {hasPatchedPrices ? (
                <Badge variant="warning">Preços definidos</Badge>
              ) : (
                <Badge variant="info">Sem preços patch</Badge>
              )}
            </div>
          </div>
        );
      },
    },
    {
      id: 'prices',
      header: 'Preços',
      cell: ({ row }) => (
        <div className="text-sm">
          <p>
            <span className="text-muted-foreground">Promo: </span>
            {formatOptionalPrice(row.original.promotionalPrice)}
          </p>
          <p>
            <span className="text-muted-foreground">Orig: </span>
            {formatOptionalPrice(row.original.originalPrice)}
          </p>
        </div>
      ),
    },
    {
      id: 'operation',
      header: 'Operação',
      cell: ({ row }) => (
        <span className="text-sm" title={row.original.operationId}>
          {resolveOlxOperationLabel(row.original.operationId, options.operationLabels ?? {})}
        </span>
      ),
    },
  ];

  if (scope === 'admin') {
    columns.push({
      id: 'operator',
      header: 'Operador OLX',
      cell: ({ row }) => {
        const operatorId = 'operatorId' in row.original ? row.original.operatorId : null;
        const label = operatorId ? options.operatorLabels?.[operatorId] : null;
        return (
          <span className="text-sm">
            {label ? `@${label}` : operatorId ? 'Conta vinculada' : '—'}
          </span>
        );
      },
    });
  }

  columns.push(
    {
      accessorKey: 'updatedAt',
      header: 'Atualizado',
      cell: ({ row }) => (
        <time className="text-sm text-muted-foreground" dateTime={row.original.updatedAt}>
          {formatDateTime(row.original.updatedAt)}
        </time>
      ),
    },
    {
      id: 'actions',
      header: '',
      cell: ({ row }) => (
        <AdPatchRowActions row={row.original} scope={scope} options={options} />
      ),
    },
  );

  return columns;
}

function StatusBadge({ impersonating }: { impersonating: boolean }) {
  const tone = patchStatusTone(impersonating);
  return (
    <Badge variant={tone === 'success' ? 'success' : 'info'}>
      {patchStatusLabel(impersonating)}
    </Badge>
  );
}

function AdPatchRowActions({
  row,
  scope,
  options,
}: {
  row: AdPatchRow;
  scope: 'operator' | 'admin';
  options: AdPatchColumnActions;
}) {
  const operatorId = 'operatorId' in row ? row.operatorId : null;
  const isOwn = scope === 'operator' || (operatorId && options.currentAccountId && operatorId === options.currentAccountId);
  const canEditPrices = scope === 'operator' && row.isImpersonating && isOwn;
  const canUnimpersonate = scope === 'operator' && row.isImpersonating && isOwn;
  const canImpersonate = scope === 'operator' && !row.isImpersonating;
  const canForceUnimpersonate = scope === 'admin' && row.isImpersonating && Boolean(operatorId);
  const hasActions = canImpersonate || canEditPrices || canUnimpersonate || canForceUnimpersonate;

  if (!hasActions) return null;

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="ghost"
          size="icon-sm"
          aria-label="Ações"
          disabled={options.busy}
          onClick={(e) => e.stopPropagation()}
        >
          <MoreHorizontal className="size-4" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        {canImpersonate ? (
          <DropdownMenuItem onClick={() => options.onImpersonate?.(row)}>Assumir</DropdownMenuItem>
        ) : null}
        {canEditPrices ? (
          <DropdownMenuItem onClick={() => options.onEditPrices?.(row)}>Editar preços</DropdownMenuItem>
        ) : null}
        {canUnimpersonate ? (
          <>
            <DropdownMenuSeparator />
            <DropdownMenuItem onClick={() => options.onUnimpersonate?.(row)}>Liberar</DropdownMenuItem>
          </>
        ) : null}
        {canForceUnimpersonate ? (
          <DropdownMenuItem variant="destructive" onClick={() => options.onForceUnimpersonate?.(row)}>
            Forçar liberação
          </DropdownMenuItem>
        ) : null}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
