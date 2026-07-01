import { useState } from 'react';
import type { PaymentRow } from '../../api/types';
import { StatusPill } from '../../components/finance/StatusPill';
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
} from '../../utils/financeLabels';
import { formatDateTime, shortId } from '../../utils/format';
import type { PaymentScope } from './paymentPaths';

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
    <article className={`payment-context-card${empty ? ' payment-context-card--empty' : ''}`}>
      <span className="payment-context-card__kicker">{kicker}</span>
      <strong className="payment-context-card__title">{title}</strong>
      {subtitle ? <span className="payment-context-card__subtitle muted small">{subtitle}</span> : null}
    </article>
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
    <div className="payment-detail-layout">
      <div className="payment-detail-layout__main">
        <header className="payment-detail-hero">
          <div className="payment-detail-hero__main">
            <div className="payment-detail-hero__topline">
              <span className="payment-detail-hero__gateway">{formatGatewayLabel(payment.gateway)}</span>
              <span className="payment-detail-hero__tx mono" title={payment.gatewayTransactionId}>
                {formatGatewayTransaction(payment.gatewayTransactionId)}
              </span>
            </div>
            <p className="payment-detail-hero__amount">{formatMoney(payment.amount)}</p>
            <p className="payment-detail-hero__operation muted">{operationTitle}</p>
          </div>
          <div className="payment-detail-hero__pills">
            <StatusPill label={paymentStatusLabel(payment.status)} tone={paymentStatusTone(payment.status)} />
            <StatusPill
              label={settlementStatusLabel(payment.settlementStatus)}
              tone={settlementStatusTone(payment.settlementStatus)}
            />
            <StatusPill
              label={distributionStatusLabel(payment.distributionStatus)}
              tone={distributionStatusTone(payment.distributionStatus)}
            />
          </div>
        </header>

        <section className="payment-detail-panel__section">
          <h3 className="payment-detail-panel__section-title">Participantes</h3>
          <div className="payment-context-grid">
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
          <section className="payment-detail-panel__section">
            <h3 className="payment-detail-panel__section-title">Repasses</h3>
            <ul className="payment-split-cards">
              {splits.map((split) => {
                const isViewer = viewerAccountId && split.accountId === viewerAccountId;
                const participant = formatSplitParticipant(split);
                const role = formatSplitRole(split);
                const width = Math.max(4, Math.min(100, split.percentage));

                return (
                  <li
                    key={`${split.accountId}-${split.percentage}`}
                    className={`payment-split-card${isViewer ? ' payment-split-card--highlight' : ''}`}
                  >
                    <div className="payment-split-card__head">
                      <span className="payment-split-card__avatar" aria-hidden="true">
                        {participantInitials(participant)}
                      </span>
                      <div className="payment-split-card__identity">
                        <strong>{participant}</strong>
                        {role ? <span className="payment-split-card__role muted small">{role}</span> : null}
                      </div>
                      <div className="payment-split-card__amounts">
                        <strong>{formatMoney(split.amount)}</strong>
                        <span className="muted small">{split.percentage.toFixed(2)}%</span>
                      </div>
                    </div>
                    <div className="payment-split-card__bar" aria-hidden="true">
                      <span style={{ width: `${width}%` }} />
                    </div>
                  </li>
                );
              })}
            </ul>
          </section>
        ) : null}

        <section className="payment-detail-panel__section">
          <h3 className="payment-detail-panel__section-title">Linha do tempo</h3>
          <ol className="payment-timeline">
            {timeline.map((event, index) => (
              <li
                key={event.key}
                className={`payment-timeline__item${event.tone === 'warn' ? ' payment-timeline__item--warn' : ''}${index === timeline.length - 1 ? ' payment-timeline__item--last' : ''}`}
              >
                <span className="payment-timeline__dot" aria-hidden="true" />
                <div className="payment-timeline__content">
                  <span className="payment-timeline__label">{event.label}</span>
                  <span className="payment-timeline__value">{event.value}</span>
                </div>
              </li>
            ))}
          </ol>
        </section>

        <section className="payment-detail-panel__section payment-detail-panel__section--technical">
          <button
            type="button"
            className="payment-technical-toggle"
            aria-expanded={technicalOpen}
            onClick={() => setTechnicalOpen((open) => !open)}
          >
            Detalhes técnicos
            <span aria-hidden="true">{technicalOpen ? '▾' : '▸'}</span>
          </button>
          {technicalOpen ? (
            <div className="payment-technical-grid mono small">
              <div><span>ID pagamento</span><span title={payment.id}>{shortId(payment.id, 18)}</span></div>
              <div><span>ID operação</span><span title={payment.operationId}>{shortId(payment.operationId, 18)}</span></div>
              <div><span>ID operador</span><span title={payment.operatorId ?? ''}>{payment.operatorId ? shortId(payment.operatorId, 18) : '—'}</span></div>
              <div><span>ID laranja</span><span title={payment.strawManId}>{shortId(payment.strawManId, 18)}</span></div>
              <div className="payment-technical-grid__wide">
                <span>Transação gateway</span>
                <span title={payment.gatewayTransactionId}>{payment.gatewayTransactionId}</span>
              </div>
            </div>
          ) : null}
        </section>
      </div>

      {actionsSlot ? (
        <aside className="payment-detail-layout__aside">
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
