import { Link } from 'react-router-dom';
import type { ActiveBalanceRow, TransferTimelineDetails, TransferTimelineStep } from '../../api/types';
import { StatusPill } from '../finance/StatusPill';
import { cryptoAssetLabel, formatCryptoAmount } from '../../utils/cryptoWalletDisplay';
import { chainLabel, formatMoney, paymentStatusLabel, paymentStatusTone, settlementStatusLabel, transferTypeLabel } from '../../utils/financeLabels';
import { formatActiveBalanceAmount } from '../../utils/movementDisplay';
import {
  formatEnrichedAccountSubtitle,
  formatEnrichedAccountTitle,
  formatStepAmount,
} from '../../utils/transferDisplay';
import { formatDateTime, shortId } from '../../utils/format';

type MoneyTimelineProps = {
  timeline: TransferTimelineDetails;
  strawManId: string;
  focusTransferId: string;
  onMoveBalance?: (balance: ActiveBalanceRow) => void;
};

function formatBalanceAmount(balance: ActiveBalanceRow): string {
  return formatActiveBalanceAmount(balance);
}

function formatEffectAmount(amount: number, currency: string, asset?: string | null, chain?: string | null): string {
  if (asset || currency !== 'BRL') {
    const chainPrefix = chain ? `${chainLabel(chain)} · ` : '';
    return `${chainPrefix}${cryptoAssetLabel(asset ?? currency)} ${formatCryptoAmount(amount)}`;
  }
  return formatMoney(amount);
}

function buildPayoutUrl(balance: ActiveBalanceRow, strawManId: string): string {
  const params = new URLSearchParams({
    strawManId,
    sourceBalanceId: balance.balanceId,
    sourceAmount: String(balance.amount),
  });

  if (balance.account.id) {
    params.set('sourceBankAccountId', balance.account.id);
  }

  return `/dashboard/transfers/payout?${params.toString()}`;
}

function stepTone(type: string): 'info' | 'success' | 'warn' {
  switch (type) {
    case 'Withdrawal': return 'success';
    case 'Movement': return 'info';
    case 'Payout': return 'warn';
    default: return 'info';
  }
}

function TimelineStepCard({
  step,
  showTimelineChrome,
  isFocusView,
}: {
  step: TransferTimelineStep;
  showTimelineChrome: boolean;
  isFocusView: boolean;
}) {
  const tone = stepTone(step.type);
  const amount = formatStepAmount(step);
  const destination = step.transfer.destination;
  const source = step.transfer.source;

  return (
    <li
      className={[
        'transfer-step',
        showTimelineChrome ? 'transfer-step--chained' : 'transfer-step--single',
        step.isFocus ? 'transfer-step--focus' : '',
        step.isCurrent ? 'transfer-step--current' : '',
        isFocusView ? 'transfer-step--in-focus-view' : '',
      ].filter(Boolean).join(' ')}
    >
      {showTimelineChrome ? (
        <div className="transfer-step__marker" aria-hidden="true">
          <span className={`transfer-step__dot transfer-step__dot--${tone}`} />
        </div>
      ) : null}

      <article className="transfer-step__card">
        {!isFocusView ? (
          <header className="transfer-step__header">
            <div className="transfer-step__header-main">
              <div className="transfer-step__title-row">
                <h3 className="transfer-step__title">{transferTypeLabel(step.type)}</h3>
                {step.isCurrent ? <StatusPill label="Saldo ativo" tone="success" /> : null}
              </div>
              {amount ? <p className="transfer-step__amount">{amount}</p> : null}
              <p className="transfer-step__meta muted small">
                {formatDateTime(step.createdAt)}
                {' · '}
                <Link className="inline-link mono" to={`/dashboard/transfers/${step.transferId}`}>
                  {shortId(step.transferId, 12)}
                </Link>
              </p>
            </div>
          </header>
        ) : null}

        {(source || destination) && !isFocusView ? (
          <div className="transfer-step__route">
            {source ? (
              <div className="transfer-step__endpoint">
                <span className="transfer-step__endpoint-label">Origem</span>
                <strong>{formatEnrichedAccountTitle(source)}</strong>
                {formatEnrichedAccountSubtitle(source) ? (
                  <span className="muted small">{formatEnrichedAccountSubtitle(source)}</span>
                ) : null}
              </div>
            ) : null}
            {destination ? (
              <div className="transfer-step__endpoint">
                <span className="transfer-step__endpoint-label">Destino</span>
                <strong>{formatEnrichedAccountTitle(destination)}</strong>
                {formatEnrichedAccountSubtitle(destination) ? (
                  <span className="muted small">{formatEnrichedAccountSubtitle(destination)}</span>
                ) : null}
              </div>
            ) : null}
          </div>
        ) : null}

        {step.balanceEffects.length > 0 ? (
          <div className="transfer-step__section">
            <p className="transfer-step__section-label">Efeitos no saldo</p>
            <ul className="transfer-step__effects">
              {step.balanceEffects.map((effect) => (
                <li
                  key={`${effect.direction}-${effect.balanceId}`}
                  className={`transfer-step__effect transfer-step__effect--${effect.direction.toLowerCase()}`}
                >
                  <span className="transfer-step__effect-tag">
                    {effect.direction === 'Credit' ? 'Crédito' : 'Débito'}
                  </span>
                  <span className="transfer-step__effect-amount">
                    {formatEffectAmount(effect.amount, effect.currency, effect.asset, effect.chain)}
                  </span>
                  <span className="transfer-step__effect-account muted small">
                    {formatEnrichedAccountTitle(effect.account)}
                  </span>
                </li>
              ))}
            </ul>
          </div>
        ) : null}

        {step.payments.length > 0 ? (
          <div className="transfer-step__section">
            <p className="transfer-step__section-label">
              Pagamentos vinculados ({step.payments.length})
            </p>
            <ul className="transfer-step__payments">
              {step.payments.map((payment) => (
                <li key={payment.id} className="transfer-step__payment">
                  <div className="transfer-step__payment-head">
                    <strong>{formatMoney(payment.amount)}</strong>
                    <div className="transfer-step__payment-pills">
                      <StatusPill label={paymentStatusLabel(payment.status)} tone={paymentStatusTone(payment.status)} />
                      <StatusPill label={settlementStatusLabel(payment.settlementStatus)} tone="info" />
                    </div>
                  </div>
                  <p className="muted small">
                    {payment.gateway}
                    {' · '}
                    {shortId(payment.gatewayTransactionId, 14)}
                    {payment.operatorUsername ? ` · @${payment.operatorUsername}` : ''}
                  </p>
                </li>
              ))}
            </ul>
          </div>
        ) : null}

        {step.transfer.proof ? (
          <div className="transfer-step__section">
            <p className="transfer-step__section-label">Comprovante</p>
            <dl className="transfer-step__proof">
              {step.transfer.proof.pixTransactionId ? (
                <div className="transfer-step__proof-row">
                  <dt>PIX</dt>
                  <dd className="mono">{step.transfer.proof.pixTransactionId}</dd>
                </div>
              ) : null}
              {step.transfer.proof.pixAuthenticationCode ? (
                <div className="transfer-step__proof-row">
                  <dt>Autenticação</dt>
                  <dd className="mono">{step.transfer.proof.pixAuthenticationCode}</dd>
                </div>
              ) : null}
              {step.transfer.proof.cryptoTransactionId ? (
                <div className="transfer-step__proof-row">
                  <dt>On-chain</dt>
                  <dd className="mono">{step.transfer.proof.cryptoTransactionId}</dd>
                </div>
              ) : null}
            </dl>
          </div>
        ) : null}
      </article>
    </li>
  );
}

export function MoneyTimeline({ timeline, strawManId, focusTransferId, onMoveBalance }: MoneyTimelineProps) {
  const isChain = timeline.steps.length > 1;
  const supplementalSteps = isChain
    ? timeline.steps.filter((step) => step.transferId !== focusTransferId)
    : timeline.steps;

  const showSupplementalDetails = supplementalSteps.some((step) =>
    step.payments.length > 0
    || step.balanceEffects.length > 0
    || step.transfer.proof
    || step.transfer.source
    || (step.transfer.destination && step.transferId !== focusTransferId));

  return (
    <div className="transfer-timeline-layout">
      {timeline.activeBalances.length > 0 ? (
        <aside className="transfer-next-steps card ops-card" id="transfer-next-steps">
          <h2 className="transfer-next-steps__title">Próximos passos</h2>
          <p className="transfer-next-steps__hint muted small">
            Saldos disponíveis nesta cadeia.
          </p>
          <ul className="transfer-next-steps__list">
            {timeline.activeBalances.map((balance) => (
              <li key={balance.balanceId} className="transfer-next-steps__item">
                <div className="transfer-next-steps__summary">
                  <strong>{formatBalanceAmount(balance)}</strong>
                  <span>{formatEnrichedAccountTitle(balance.account)}</span>
                  <span className="mono muted small">{shortId(balance.balanceId, 10)}</span>
                </div>
                <div className="transfer-next-steps__buttons">
                  {balance.canMove ? (
                    onMoveBalance ? (
                      <button
                        type="button"
                        className="btn btn-secondary btn-sm"
                        onClick={() => onMoveBalance(balance)}
                      >
                        Movimentar
                      </button>
                    ) : (
                      <Link className="btn btn-secondary btn-sm" to={`/dashboard/transfers/movement?from=${timeline.focusTransferId}`}>
                        Movimentar
                      </Link>
                    )
                  ) : null}
                  {balance.canPayout ? (
                    <Link className="btn btn-primary btn-sm" to={buildPayoutUrl(balance, strawManId)}>
                      Repassar
                    </Link>
                  ) : null}
                </div>
              </li>
            ))}
          </ul>
        </aside>
      ) : null}

      {isChain || showSupplementalDetails ? (
        <section className="transfer-timeline card ops-card">
          {isChain ? (
            <header className="transfer-timeline__head">
              <h2 className="transfer-timeline__title">Histórico da cadeia</h2>
              <p className="transfer-timeline__sub muted small">
                {timeline.steps.length} etapas · raiz{' '}
                <Link className="inline-link mono" to={`/dashboard/transfers/${timeline.rootTransferId}`}>
                  {shortId(timeline.rootTransferId, 12)}
                </Link>
              </p>
            </header>
          ) : (
            <header className="transfer-timeline__head">
              <h2 className="transfer-timeline__title">Detalhes adicionais</h2>
            </header>
          )}

          <ol className="transfer-timeline__list">
            {(isChain ? timeline.steps : supplementalSteps).map((step) => (
              <TimelineStepCard
                key={step.transferId}
                step={step}
                showTimelineChrome={isChain}
                isFocusView={!isChain && step.transferId === focusTransferId}
              />
            ))}
          </ol>
        </section>
      ) : null}
    </div>
  );
}
