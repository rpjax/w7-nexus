import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { getTransferTimeline } from '../../api/transfers';
import type { TransferTimelineDetails, TransferTimelineStep } from '../../api/types';
import { MoneyTimeline } from '../../components/finance/MoneyTimeline';
import { EmptyState } from '../../components/EmptyState';
import { Icon } from '../../components/IconButton';
import { StatusPill } from '../../components/finance/StatusPill';
import { transferTypeLabel } from '../../utils/financeLabels';
import {
  formatEnrichedAccountSubtitle,
  formatEnrichedAccountTitle,
  formatStepAmount,
} from '../../utils/transferDisplay';
import { formatDateTime, shortId } from '../../utils/format';
import { useNotifications } from '../../notifications/NotificationContext';

function TransferHero({
  step,
  timeline,
}: {
  step: TransferTimelineStep;
  timeline: TransferTimelineDetails;
}) {
  const [copied, setCopied] = useState(false);
  const amount = formatStepAmount(step);
  const destination = step.transfer.destination;

  async function copyId() {
    try {
      await navigator.clipboard.writeText(timeline.focusTransferId);
      setCopied(true);
      window.setTimeout(() => setCopied(false), 1800);
    } catch {
      // ignore
    }
  }

  return (
    <section className="transfer-hero" aria-label="Resumo da transferência">
      <div className="transfer-hero__top">
        <div className="transfer-hero__identity">
          <p className="transfer-hero__kicker">Financeiro</p>
          <div className="transfer-hero__title-row">
            <h1 className="transfer-hero__title">{transferTypeLabel(step.type)}</h1>
            {step.isCurrent ? <StatusPill label="Saldo ativo" tone="success" /> : null}
          </div>
          {amount ? <p className="transfer-hero__amount">{amount}</p> : null}
        </div>
        <div className="transfer-hero__meta-grid">
          {timeline.strawMan ? (
            <div className="transfer-hero__meta-item">
              <span className="transfer-hero__meta-label">Laranja</span>
              <span className="transfer-hero__meta-value">@{timeline.strawMan.username}</span>
            </div>
          ) : null}
          <div className="transfer-hero__meta-item">
            <span className="transfer-hero__meta-label">Data</span>
            <span className="transfer-hero__meta-value">{formatDateTime(step.createdAt)}</span>
          </div>
          <div className="transfer-hero__meta-item transfer-hero__meta-item--wide">
            <span className="transfer-hero__meta-label">Identificador</span>
            <span className="transfer-hero__meta-value transfer-hero__id-row">
              <span className="mono" title={timeline.focusTransferId}>{shortId(timeline.focusTransferId, 18)}</span>
              <button type="button" className="transfer-hero__copy" onClick={() => void copyId()}>
                <Icon name="copy" />
                {copied ? 'Copiado' : 'Copiar'}
              </button>
            </span>
          </div>
        </div>
      </div>

      {destination ? (
        <div className="transfer-hero__destination">
          <span className="transfer-hero__dest-label">Destino</span>
          <strong className="transfer-hero__dest-title">{formatEnrichedAccountTitle(destination)}</strong>
          {formatEnrichedAccountSubtitle(destination) ? (
            <span className="transfer-hero__dest-sub muted">{formatEnrichedAccountSubtitle(destination)}</span>
          ) : null}
        </div>
      ) : null}

      <div className="transfer-hero__actions">
        <Link className="btn btn-secondary btn-sm btn-with-icon" to="/dashboard/transfers/movement">
          <Icon name="chevron-right" />
          Nova movimentação
        </Link>
        <Link className="btn btn-primary btn-sm btn-with-icon" to="/dashboard/transfers/payout">
          <Icon name="link" />
          Novo repasse
        </Link>
        {timeline.activeBalances.length > 0 ? (
          <a className="btn btn-ghost btn-sm" href="#transfer-next-steps">
            Ver saldos disponíveis
          </a>
        ) : null}
      </div>
    </section>
  );
}

export function WithdrawalDetailPage() {
  const { transferId = '' } = useParams();
  const { notifyError } = useNotifications();
  const [timeline, setTimeline] = useState<TransferTimelineDetails | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!transferId) return;
    void (async () => {
      setLoading(true);
      const result = await getTransferTimeline(transferId);
      if (!result.ok) {
        notifyError(result.error);
        setTimeline(null);
      } else {
        setTimeline(result.data);
      }
      setLoading(false);
    })();
  }, [transferId, notifyError]);

  if (loading) {
    return (
      <div className="transfer-detail-page">
        <p className="transfer-detail-page__back muted small">
          <Link to="/dashboard/transfers">← Lista de transferências</Link>
        </p>
        <div className="transfer-detail-skeleton" aria-busy="true" aria-live="polite">
          <div className="transfer-detail-skeleton__hero" />
          <div className="transfer-detail-skeleton__block" />
        </div>
      </div>
    );
  }

  if (!timeline) {
    return (
      <div className="transfer-detail-page">
        <EmptyState
          title="Transferência não encontrada"
          message="Verifique o identificador ou volte à lista."
        />
        <p><Link className="btn btn-primary" to="/dashboard/transfers">Voltar à lista</Link></p>
      </div>
    );
  }

  const focusStep = timeline.steps.find((step) => step.isFocus) ?? timeline.steps[timeline.steps.length - 1];
  const strawManId = timeline.strawMan?.id ?? focusStep?.transfer.strawMan.id ?? '';

  return (
    <div className="transfer-detail-page ops-page">
      <p className="transfer-detail-page__back muted small">
        <Link to="/dashboard/transfers">← Lista de transferências</Link>
      </p>

      {focusStep ? (
        <TransferHero step={focusStep} timeline={timeline} />
      ) : null}

      <MoneyTimeline timeline={timeline} strawManId={strawManId} focusTransferId={timeline.focusTransferId} />
    </div>
  );
}
