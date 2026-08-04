import { useNavigate } from 'react-router-dom';
import type { ColumnDef } from '@tanstack/react-table';
import type { PaymentRow } from '@/api/types';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { formatPaymentOperation } from '@/features/payments/paymentDisplay';
import { detailPath, type PaymentScope } from '@/features/payments/paymentPaths';
import {
  distributionStatusLabel,
  distributionStatusTone,
  formatMoney,
  paymentStatusLabel,
  paymentStatusTone,
  settlementStatusLabel,
  settlementStatusTone,
} from '@/utils/financeLabels';
import { formatUtc } from '@/utils/format';
import { Copy, MoreHorizontal } from 'lucide-react';

const toneVariant: Record<'info' | 'success' | 'warn' | 'danger', 'info' | 'success' | 'warning' | 'destructive'> = {
  info: 'info',
  success: 'success',
  warn: 'warning',
  danger: 'destructive',
};

function StatusBadge({ label, tone }: { label: string; tone: 'info' | 'success' | 'warn' | 'danger' }) {
  return <Badge variant={toneVariant[tone]}>{label}</Badge>;
}

export function createPaymentColumns(
  scope: PaymentScope,
  options?: { highlightAccountId?: string | null },
): ColumnDef<PaymentRow>[] {
  return [
    {
      accessorKey: 'amount',
      header: 'Valor',
      cell: ({ row }) => (
        <span className="font-semibold tabular-nums">{formatMoney(row.original.amount)}</span>
      ),
    },
    {
      id: 'status',
      header: 'Status',
      cell: ({ row }) => {
        const payment = row.original;
        return (
          <div className="flex flex-wrap gap-1">
            <StatusBadge label={paymentStatusLabel(payment.status)} tone={paymentStatusTone(payment.status)} />
            <StatusBadge
              label={settlementStatusLabel(payment.settlementStatus)}
              tone={settlementStatusTone(payment.settlementStatus)}
            />
            <StatusBadge
              label={distributionStatusLabel(payment.distributionStatus)}
              tone={distributionStatusTone(payment.distributionStatus)}
            />
          </div>
        );
      },
    },
    {
      id: 'context',
      header: 'Contexto',
      cell: ({ row }) => {
        const payment = row.original;
        const operatorSplit = payment.splits?.find(
          (split) => split.accountId === options?.highlightAccountId,
        );
        return (
          <div className="min-w-0 text-sm">
            <p className="font-medium">{formatPaymentOperation(payment)}</p>
            <p className="text-muted-foreground">
              {payment.operatorUsername
                ? `@${payment.operatorUsername}`
                : payment.strawManUsername
                  ? `@${payment.strawManUsername}`
                  : '—'}
            </p>
            {operatorSplit ? (
              <p className="text-xs text-muted-foreground">
                Seu repasse: {formatMoney(operatorSplit.amount)}
              </p>
            ) : null}
          </div>
        );
      },
    },
    {
      id: 'gateway',
      header: 'Gateway',
      cell: ({ row }) => (
        <div className="text-sm text-muted-foreground">
          <p>{row.original.gateway}</p>
          <time dateTime={row.original.createdAt}>{formatUtc(row.original.createdAt)}</time>
        </div>
      ),
    },
    {
      id: 'actions',
      header: '',
      cell: ({ row }) => (
        <PaymentRowActions payment={row.original} scope={scope} />
      ),
    },
  ];
}

function PaymentRowActions({ payment, scope }: { payment: PaymentRow; scope: PaymentScope }) {
  const navigate = useNavigate();
  const href = detailPath(scope, payment.id);

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon-sm" aria-label="Ações" onClick={(e) => e.stopPropagation()}>
          <MoreHorizontal className="size-4" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuItem onClick={() => navigate(href)}>Abrir detalhes</DropdownMenuItem>
        <DropdownMenuItem onClick={() => void navigator.clipboard.writeText(payment.id)}>
          <Copy className="size-4" />
          Copiar ID
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
