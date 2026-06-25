import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { getTransferTimeline } from '../../api/transfers';
import type { TransferTimelineDetails, TransferTimelineStep } from '../../api/types';
import { MoneyTimeline } from '../../components/finance/MoneyTimeline';
import { MovementComposerModal } from '../../components/finance/MovementComposerModal';
import { PayoutComposerModal } from '../../components/finance/PayoutComposerModal';
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
  onOpenMovement,
  onOpenPayout,
  canPayout,
}: {
  step: TransferTimelineStep;
  timeline: TransferTimelineDetails;
  onOpenMovement: () => void;
  onOpenPayout: () => void;
  canPayout: boolean;
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
        <button
          type="button"
          className="btn btn-secondary btn-sm btn-with-icon"
          onClick={onOpenMovement}
        >
          <Icon name="chevron-right" />
          Nova movimentação
        </button>
        <button
          type="button"
          className={`btn btn-primary btn-sm btn-with-icon${canPayout ? '' : ' is-disabled'}`}
          disabled={!canPayout}
          onClick={() => { if (canPayout) onOpenPayout(); }}
        >
          <Icon name="link" />
          Novo repasse
        </button>
        {timeline.activeBalances.length > 0 ? (
          <a className="btn btn-ghost btn-sm" href="#transfer-next-steps">
            Ver saldos disponíveis
          </a>
        ) : (
          <p className="transfer-hero__actions-hint muted small">
            Sem saldo disponível nesta cadeia — o valor já foi movimentado ou repassado integralmente.
          </p>
        )}
      </div>
    </section>
  );
}

export function WithdrawalDetailPage() {
  const { transferId = '' } = useParams();
  const navigate = useNavigate();
  const { notifyError, notifySuccess } = useNotifications();
  const [timeline, setTimeline] = useState<TransferTimelineDetails | null>(null);
  const [loading, setLoading] = useState(true);
  const [movementOpen, setMovementOpen] = useState(false);
  const [movementBalanceId, setMovementBalanceId] = useState<string | null>(null);
  const [payoutOpen, setPayoutOpen] = useState(false);
  const [payoutBalanceId, setPayoutBalanceId] = useState<string | null>(null);

  async function loadTimeline(id: string) {
    setLoading(true);
    const result = await getTransferTimeline(id);
    if (!result.ok) {
      notifyError(result.error);
      setTimeline(null);
    } else {
      setTimeline(result.data);
    }
    setLoading(false);
  }

  useEffect(() => {
    if (!transferId) return;
    void loadTimeline(transferId);
  }, [transferId]);

  function openMovement(balanceId?: string | null) {
    const movable = timeline?.activeBalances.filter((balance) => balance.canMove) ?? [];
    setMovementBalanceId(balanceId ?? movable[0]?.balanceId ?? null);
    setMovementOpen(true);
  }

  function openPayout(balanceId?: string | null) {
    const payable = timeline?.activeBalances.filter(
      (balance) => balance.canPayout && balance.account.kind === 'BankAccount',
    ) ?? [];
    setPayoutBalanceId(balanceId ?? payable[0]?.balanceId ?? null);
    setPayoutOpen(true);
  }

  function handleMovementSuccess(newTransferId: string) {
    setMovementOpen(false);
    notifySuccess('Movimentação registrada.');
    navigate(`/dashboard/transfers/${newTransferId}`);
  }

  function handlePayoutSuccess(newTransferId: string) {
    setPayoutOpen(false);
    notifySuccess('Repasse registrado.');
    navigate(`/dashboard/transfers/${newTransferId}`);
  }

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
  const payoutBalances = timeline.activeBalances.filter(
    (balance) => balance.canPayout && balance.account.kind === 'BankAccount',
  );

  return (
    <div className="transfer-detail-page ops-page">
      <p className="transfer-detail-page__back muted small">
        <Link to="/dashboard/transfers">← Lista de transferências</Link>
      </p>

      {focusStep ? (
        <TransferHero
          step={focusStep}
          timeline={timeline}
          canPayout={payoutBalances.length > 0}
          onOpenMovement={() => openMovement()}
          onOpenPayout={() => openPayout()}
        />
      ) : null}

      <MoneyTimeline
        timeline={timeline}
        focusTransferId={timeline.focusTransferId}
        onMoveBalance={(balance) => openMovement(balance.balanceId)}
        onPayoutBalance={(balance) => openPayout(balance.balanceId)}
      />

      <MovementComposerModal
        open={movementOpen}
        onClose={() => {
          setMovementOpen(false);
          setMovementBalanceId(null);
        }}
        strawManId={strawManId}
        strawManUsername={timeline.strawMan?.username}
        activeBalances={timeline.activeBalances}
        initialBalanceId={movementBalanceId}
        onSuccess={handleMovementSuccess}
      />

      <PayoutComposerModal
        open={payoutOpen}
        onClose={() => {
          setPayoutOpen(false);
          setPayoutBalanceId(null);
        }}
        strawManId={strawManId}
        strawManUsername={timeline.strawMan?.username}
        activeBalances={timeline.activeBalances}
        initialBalanceId={payoutBalanceId}
        onSuccess={handlePayoutSuccess}
      />
    </div>
  );
}
