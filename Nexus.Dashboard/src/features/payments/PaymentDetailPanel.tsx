import { Link } from 'react-router-dom';
import type { PaymentRow } from '../../api/types';
import { StatusPill } from '../../components/finance/StatusPill';
import {
  formatMoney,
  paymentStatusLabel,
  paymentStatusTone,
  settlementStatusLabel,
  settlementStatusTone,
  distributionStatusLabel,
  distributionStatusTone,
} from '../../utils/financeLabels';
import { formatUtc, shortId, shortTx } from '../../utils/format';
import type { PaymentScope } from './paymentPaths';

type PaymentDetailPanelProps = {
  payment: PaymentRow;
  scope: PaymentScope;
  viewerAccountId?: string | null;
};

function MetaRow({ label, value, mono = false }: { label: string; value: string; mono?: boolean }) {
  return (
    <div className="payment-detail-meta__row">
      <span className="payment-detail-meta__label">{label}</span>
      <span className={`payment-detail-meta__value${mono ? ' mono' : ''}`}>{value}</span>
    </div>
  );
}

export function PaymentDetailPanel({ payment, scope, viewerAccountId }: PaymentDetailPanelProps) {
  const splits = payment.splits ?? [];
  const showWithdrawalCta = scope !== 'global-admin'
    && payment.status === 'Paid'
    && payment.settlementStatus === 'Unsettled';

  return (
    <div className="payment-detail-panel">
      <header className="payment-detail-panel__hero">
        <div>
          <p className="payment-detail-panel__kicker">Pagamento</p>
          <h2 className="payment-detail-panel__amount">{formatMoney(payment.amount)}</h2>
          <p className="payment-detail-panel__id mono" title={payment.id}>{shortId(payment.id)}</p>
        </div>
        <div className="payment-detail-panel__pills">
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
        <h3 className="payment-detail-panel__section-title">Identificação</h3>
        <div className="payment-detail-meta">
          <MetaRow label="Operação" value={shortId(payment.operationId)} mono />
          <MetaRow label="Gateway" value={payment.gateway} />
          <MetaRow label="Transação gateway" value={shortTx(payment.gatewayTransactionId)} mono />
          <MetaRow label="Operador" value={payment.operatorId ? shortId(payment.operatorId) : '—'} mono />
          <MetaRow label="Laranja" value={shortId(payment.strawManId)} mono />
        </div>
      </section>

      {splits.length > 0 ? (
        <section className="payment-detail-panel__section">
          <h3 className="payment-detail-panel__section-title">Repasses</h3>
          <ul className="payment-split-list">
            {splits.map((split) => {
              const isViewer = viewerAccountId && split.accountId === viewerAccountId;
              return (
                <li
                  key={`${split.accountId}-${split.percentage}`}
                  className={`payment-split-list__item${isViewer ? ' payment-split-list__item--highlight' : ''}`}
                >
                  <span className="mono" title={split.accountId}>{shortId(split.accountId)}</span>
                  <span>{split.percentage.toFixed(2)}%</span>
                  <strong>{formatMoney(split.amount)}</strong>
                </li>
              );
            })}
          </ul>
        </section>
      ) : null}

      <section className="payment-detail-panel__section">
        <h3 className="payment-detail-panel__section-title">Histórico</h3>
        <div className="payment-detail-meta">
          <MetaRow label="Criado em" value={formatUtc(payment.createdAt)} />
          <MetaRow label="Pago em" value={payment.paidAt ? formatUtc(payment.paidAt) : '—'} />
          <MetaRow label="Reembolsado em" value={payment.refundedAt ? formatUtc(payment.refundedAt) : '—'} />
          <MetaRow label="Sacado em" value={payment.withdrawnAt ? formatUtc(payment.withdrawnAt) : '—'} />
          <MetaRow label="Repassado em" value={payment.distributedAt ? formatUtc(payment.distributedAt) : '—'} />
          {payment.killedAt ? <MetaRow label="Cancelado em" value={formatUtc(payment.killedAt)} /> : null}
          {payment.killReason ? <MetaRow label="Motivo do cancelamento" value={payment.killReason} /> : null}
        </div>
      </section>

      {showWithdrawalCta ? (
        <div className="payment-detail-panel__cta">
          <Link className="btn btn-secondary" to="/dashboard/transfers/new">
            Registrar transferência
          </Link>
        </div>
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
