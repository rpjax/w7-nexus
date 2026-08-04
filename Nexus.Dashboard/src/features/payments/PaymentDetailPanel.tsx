import { useState } from 'react';
import type { PaymentRow } from '../../api/types';
import {
  formatGatewayLabel,
  formatGatewayTransaction,
  formatPaymentOperation,
  formatPaymentParticipant,
  formatSplitParticipant,
  formatSplitRole,
  participantInitials,
} from './paymentDisplay';
import {
  formatMoney,
  paymentStatusLabel,
  paymentStatusTone,
  settlementStatusLabel,
  settlementStatusTone,
  distributionStatusLabel,
  distributionStatusTone,
  statusToneToBadgeVariant,
} from '../../utils/financeLabels';
import { formatDateTime, shortId } from '../../utils/format';
import type { PaymentScope } from './paymentPaths';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { cn } from '@/lib/utils';

type PaymentDetailPanelProps = {
  payment: PaymentRow;
  scope: PaymentScope;
  viewerAccountId?: string | null;
  actionsSlot?: React.ReactNode;
};

type ContextCardProps = {
  kicker: string;
  title: string;
  subtitle?: string | null;
  empty?: boolean;
};

function ContextCard({ kicker, title, subtitle, empty = false }: ContextCardProps) {
  return (
    <Card className={cn(empty && 'border-dashed opacity-80')}>
      <CardContent className="grid gap-1 p-4">
        <span className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">{kicker}</span>
        <strong className="text-base leading-snug">{title}</strong>
        {subtitle ? <span className="text-sm text-muted-foreground">{subtitle}</span> : null}
      </CardContent>
    </Card>
  );
}

type TimelineEvent = {
  key: string;
  label: string;
  value: string;
  tone?: 'default' | 'warn';
};

function buildTimeline(payment: PaymentRow): TimelineEvent[] {
  const events: TimelineEvent[] = [
    { key: 'created', label: 'Criado', value: formatDateTime(payment.createdAt) },
  ];

  if (payment.paidAt) events.push({ key: 'paid', label: 'Pago', value: formatDateTime(payment.paidAt) });
  if (payment.withdrawnAt) events.push({ key: 'withdrawn', label: 'Sacado', value: formatDateTime(payment.withdrawnAt) });
  if (payment.distributedAt) events.push({ key: 'distributed', label: 'Repassado', value: formatDateTime(payment.distributedAt) });
  if (payment.refundedAt) events.push({ key: 'refunded', label: 'Reembolsado', value: formatDateTime(payment.refundedAt), tone: 'warn' });
  if (payment.killedAt) {
    events.push({
      key: 'killed',
      label: 'Cancelado',
      value: payment.killReason
        ? `${formatDateTime(payment.killedAt)} · ${payment.killReason}`
        : formatDateTime(payment.killedAt),
      tone: 'warn',
    });
  }

  return events;
}

export function PaymentDetailPanel({ payment, scope: _scope, viewerAccountId, actionsSlot }: PaymentDetailPanelProps) {
  const [technicalOpen, setTechnicalOpen] = useState(false);
  const splits = payment.splits ?? [];
  const timeline = buildTimeline(payment);

  const operationTitle = formatPaymentOperation(payment);
  const operatorTitle = formatPaymentParticipant(payment.operatorUsername, 'Sem operador');
  const strawManTitle = formatPaymentParticipant(payment.strawManUsername, 'Sem laranja');

  return (
    <div className="mt-3 grid gap-4 lg:grid-cols-[minmax(0,1fr)_minmax(16rem,20rem)] lg:items-start">
      <div className="grid gap-4">
        <Card className="border-primary/20 bg-gradient-to-br from-primary/5 to-background">
          <CardContent className="flex flex-wrap items-start justify-between gap-4 p-5">
            <div>
              <div className="mb-1 flex flex-wrap items-center gap-2">
                <Badge variant="info" className="uppercase tracking-wider">
                  {formatGatewayLabel(payment.gateway)}
                </Badge>
                <span className="font-mono text-xs text-muted-foreground" title={payment.gatewayTransactionId}>
                  {formatGatewayTransaction(payment.gatewayTransactionId)}
                </span>
              </div>
              <p className="text-3xl font-bold tracking-tight text-foreground">{formatMoney(payment.amount)}</p>
              <p className="mt-1 text-sm text-muted-foreground">{operationTitle}</p>
            </div>
            <div className="flex flex-wrap gap-1.5">
              <Badge variant={statusToneToBadgeVariant(paymentStatusTone(payment.status))}>
                {paymentStatusLabel(payment.status)}
              </Badge>
              <Badge variant={statusToneToBadgeVariant(settlementStatusTone(payment.settlementStatus))}>
                {settlementStatusLabel(payment.settlementStatus)}
              </Badge>
              <Badge variant={statusToneToBadgeVariant(distributionStatusTone(payment.distributionStatus))}>
                {distributionStatusLabel(payment.distributionStatus)}
              </Badge>
            </div>
          </CardContent>
        </Card>

        <section className="grid gap-2">
          <h3 className="text-sm font-semibold text-foreground">Participantes</h3>
          <div className="grid gap-3 md:grid-cols-3">
            <ContextCard kicker="Operação" title={operationTitle} />
            <ContextCard
              kicker="Operador"
              title={operatorTitle}
              subtitle={payment.operatorUsername ? 'Responsável pelo pagamento' : 'Aguardando vínculo'}
              empty={!payment.operatorUsername}
            />
            <ContextCard
              kicker="Laranja"
              title={strawManTitle}
              subtitle={payment.strawManUsername ? 'Titular do pagamento' : null}
              empty={!payment.strawManUsername}
            />
          </div>
        </section>

        {splits.length > 0 ? (
          <section className="grid gap-2">
            <h3 className="text-sm font-semibold text-foreground">Repasses</h3>
            <ul className="flex flex-col gap-2">
              {splits.map((split) => {
                const isViewer = viewerAccountId && split.accountId === viewerAccountId;
                const participant = formatSplitParticipant(split);
                const role = formatSplitRole(split);
                const width = Math.max(4, Math.min(100, split.percentage));

                return (
                  <li
                    key={`${split.accountId}-${split.percentage}`}
                    className={cn(
                      'rounded-lg border border-border/40 bg-muted/20 p-4',
                      isViewer && 'border-warning/40 bg-warning/5',
                    )}
                  >
                    <div className="grid grid-cols-[auto_1fr_auto] items-center gap-3">
                      <span
                        className="inline-flex size-8 items-center justify-center rounded-full border border-primary/25 bg-primary/15 text-xs font-bold text-foreground"
                        aria-hidden="true"
                      >
                        {participantInitials(participant)}
                      </span>
                      <div className="min-w-0 grid gap-0.5">
                        <strong className="truncate">{participant}</strong>
                        {role ? <span className="text-sm text-muted-foreground">{role}</span> : null}
                      </div>
                      <div className="grid justify-items-end gap-0.5">
                        <strong>{formatMoney(split.amount)}</strong>
                        <span className="text-sm text-muted-foreground">{split.percentage.toFixed(2)}%</span>
                      </div>
                    </div>
                    <div className="mt-2 h-1 overflow-hidden rounded-full bg-muted" aria-hidden="true">
                      <span
                        className="block h-full rounded-full bg-gradient-to-r from-primary/85 to-primary/55"
                        style={{ width: `${width}%` }}
                      />
                    </div>
                  </li>
                );
              })}
            </ul>
          </section>
        ) : null}

        <section className="grid gap-2">
          <h3 className="text-sm font-semibold text-foreground">Linha do tempo</h3>
          <ol className="grid">
            {timeline.map((event, index) => (
              <li
                key={event.key}
                className={cn(
                  'relative grid grid-cols-[1rem_minmax(0,1fr)] gap-3 pb-3',
                  index !== timeline.length - 1 && 'before:absolute before:bottom-0 before:left-[0.45rem] before:top-4 before:w-0.5 before:bg-border/60',
                )}
              >
                <span
                  className={cn(
                    'mt-0.5 size-3 rounded-full bg-primary shadow-[0_0_0_3px] shadow-primary/15',
                    event.tone === 'warn' && 'bg-warning shadow-warning/15',
                  )}
                  aria-hidden="true"
                />
                <div className="grid gap-0.5">
                  <span className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                    {event.label}
                  </span>
                  <span className="text-sm leading-relaxed">{event.value}</span>
                </div>
              </li>
            ))}
          </ol>
        </section>

        <section className="grid gap-2 pt-1">
          <Button
            type="button"
            variant="ghost"
            size="sm"
            className="h-auto w-fit gap-1.5 p-0 text-muted-foreground hover:text-foreground"
            aria-expanded={technicalOpen}
            onClick={() => setTechnicalOpen((open) => !open)}
          >
            Detalhes técnicos
            <span aria-hidden="true">{technicalOpen ? '▾' : '▸'}</span>
          </Button>
          {technicalOpen ? (
            <div className="rounded-lg border border-dashed border-border/40 bg-muted/20 p-3 font-mono text-xs">
              <div className="grid gap-2">
                <div className="grid gap-1 sm:grid-cols-[7rem_1fr] sm:items-baseline sm:gap-3">
                  <span className="text-muted-foreground">ID pagamento</span>
                  <span title={payment.id}>{shortId(payment.id, 18)}</span>
                </div>
                <div className="grid gap-1 sm:grid-cols-[7rem_1fr] sm:items-baseline sm:gap-3">
                  <span className="text-muted-foreground">ID operação</span>
                  <span title={payment.operationId}>{shortId(payment.operationId, 18)}</span>
                </div>
                <div className="grid gap-1 sm:grid-cols-[7rem_1fr] sm:items-baseline sm:gap-3">
                  <span className="text-muted-foreground">ID operador</span>
                  <span title={payment.operatorId ?? ''}>{payment.operatorId ? shortId(payment.operatorId, 18) : '—'}</span>
                </div>
                <div className="grid gap-1 sm:grid-cols-[7rem_1fr] sm:items-baseline sm:gap-3">
                  <span className="text-muted-foreground">ID laranja</span>
                  <span title={payment.strawManId}>{shortId(payment.strawManId, 18)}</span>
                </div>
                <div className="grid gap-1">
                  <span className="text-muted-foreground">Transação gateway</span>
                  <span title={payment.gatewayTransactionId}>{payment.gatewayTransactionId}</span>
                </div>
              </div>
            </div>
          ) : null}
        </section>
      </div>

      {actionsSlot ? (
        <aside className="sticky top-3 self-start">
          {actionsSlot}
        </aside>
      ) : null}
    </div>
  );
}

export function canPayPayment(payment: PaymentRow): boolean {
  return payment.status === 'Pending'
    && Boolean(payment.operatorId)
    && (payment.splits?.length ?? 0) > 0;
}

export function canRefundPayment(payment: PaymentRow): boolean {
  return payment.status === 'Paid' && payment.settlementStatus !== 'Withdrawn';
}

export function canMarkPaymentDistributed(payment: PaymentRow): boolean {
  return payment.status === 'Paid'
    && payment.settlementStatus === 'Withdrawn'
    && payment.distributionStatus === 'Pending';
}

export function canKillPayment(payment: PaymentRow): boolean {
  return payment.status !== 'Dead';
}

export function needsOperatorBind(payment: PaymentRow): boolean {
  return payment.status === 'Pending' && !payment.operatorId;
}

export function needsSplitsForPay(payment: PaymentRow): boolean {
  return payment.status === 'Pending'
    && Boolean(payment.operatorId)
    && (payment.splits?.length ?? 0) === 0;
}
