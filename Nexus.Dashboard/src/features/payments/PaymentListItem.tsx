import { Link, useNavigate } from 'react-router-dom';
import type { PaymentRow } from '../../api/types';
import { StatusPill } from '../../components/finance/StatusPill';
import {
  formatMoney,
  paymentStatusLabel,
  paymentStatusTone,
  settlementStatusLabel,
  settlementStatusTone,
} from '../../utils/financeLabels';
import { formatUtc, shortId, shortTx } from '../../utils/format';
import { detailPath, type PaymentScope } from './paymentPaths';

type PaymentListItemProps = {
  payment: PaymentRow;
  scope: PaymentScope;
  highlightAccountId?: string | null;
};

export function PaymentListItem({ payment, scope, highlightAccountId }: PaymentListItemProps) {
  const navigate = useNavigate();
  const href = detailPath(scope, payment.id);
  const operatorSplit = payment.splits?.find((split) => split.accountId === highlightAccountId);

  function openDetail() {
    navigate(href);
  }

  function handleRowClick(event: React.MouseEvent<HTMLElement>) {
    if ((event.target as HTMLElement).closest('a, button')) return;
    openDetail();
  }

  return (
    <article
      className="payment-list-item ops-list-row"
      role="button"
      tabIndex={0}
      onClick={handleRowClick}
      onKeyDown={(event) => {
        if (event.key === 'Enter' || event.key === ' ') {
          event.preventDefault();
          openDetail();
        }
      }}
    >
      <div className="payment-list-item__grid ops-list-row__grid">
        <div className="payment-list-item__identity ops-list-row__identity">
          <p className="payment-list-item__amount">{formatMoney(payment.amount)}</p>
          <div className="payment-list-item__pills">
            <StatusPill label={paymentStatusLabel(payment.status)} tone={paymentStatusTone(payment.status)} />
            <StatusPill
              label={settlementStatusLabel(payment.settlementStatus)}
              tone={settlementStatusTone(payment.settlementStatus)}
            />
          </div>
          <p className="payment-list-item__meta muted small">
            <span className="mono" title={payment.id}>{shortId(payment.id)}</span>
            <span className="ops-list-row__meta-sep" aria-hidden="true">·</span>
            {payment.gateway}
            <span className="ops-list-row__meta-sep" aria-hidden="true">·</span>
            {formatUtc(payment.createdAt)}
          </p>
        </div>

        <div className="payment-list-item__stats ops-list-row__stats">
          <div className="ops-list-row__stat">
            <span className="ops-list-row__stat-value mono">{shortId(payment.operationId)}</span>
            <span className="ops-list-row__stat-label">Operação</span>
          </div>
          <div className="ops-list-row__stat">
            <span className="ops-list-row__stat-value mono" title={payment.gatewayTransactionId}>
              {shortTx(payment.gatewayTransactionId)}
            </span>
            <span className="ops-list-row__stat-label">Tx gateway</span>
          </div>
          {operatorSplit ? (
            <div className="ops-list-row__stat">
              <span className="ops-list-row__stat-value">{formatMoney(operatorSplit.amount)}</span>
              <span className="ops-list-row__stat-label">Seu repasse</span>
            </div>
          ) : null}
        </div>

        <div className="payment-list-item__actions ops-list-row__actions">
          <Link className="btn btn-ghost btn-small" to={href} onClick={(event) => event.stopPropagation()}>
            Detalhes
          </Link>
        </div>
      </div>
    </article>
  );
}
