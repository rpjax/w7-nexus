import { useMemo, useState, type ReactNode } from 'react';
import {
  bindAdministratorPaymentOperator,
  deleteAdministratorPayment,
  killAdministratorPayment,
  payAdministratorPayment,
  refundAdministratorPayment,
} from '../../api/administrator/payments';
import { searchAdministratorOperatorsPicker } from '../../api/accountPickerSources';
import type { PaymentRow } from '../../api/types';
import { AccountPickerModal } from '../../components/AccountPickerModal';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { useNotifications } from '../../notifications/NotificationContext';
import {
  canKillPayment,
  canPayPayment,
  canRefundPayment,
  needsOperatorBind,
  needsSplitsForPay,
} from './PaymentDetailPanel';

type UsePaymentAdminActionsOptions = {
  payment: PaymentRow | null;
  onMutated: () => void | Promise<void>;
  onDeleted?: () => void;
};

export function usePaymentAdminActions({
  payment,
  onMutated,
  onDeleted,
}: UsePaymentAdminActionsOptions) {
  const { notifyError, notifySuccess } = useNotifications();
  const [busy, setBusy] = useState(false);
  const [killOpen, setKillOpen] = useState(false);
  const [killReason, setKillReason] = useState('');
  const [refundOpen, setRefundOpen] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [bindOperatorOpen, setBindOperatorOpen] = useState(false);

  const transitions = useMemo(() => {
    if (!payment) {
      return {
        canPay: false,
        canRefund: false,
        canKill: false,
        canDelete: false,
        needsOperator: false,
        needsSplits: false,
      };
    }

    return {
      canPay: canPayPayment(payment),
      canRefund: canRefundPayment(payment),
      canKill: canKillPayment(payment),
      canDelete: true,
      needsOperator: needsOperatorBind(payment),
      needsSplits: needsSplitsForPay(payment),
    };
  }, [payment]);

  async function run(task: () => Promise<{ ok: boolean; error?: string }>, successMessage: string) {
    setBusy(true);
    try {
      const result = await task();
      if (!result.ok) {
        notifyError(result.error ?? 'Não foi possível concluir a ação.');
        return;
      }
      notifySuccess(successMessage);
      await onMutated();
    } finally {
      setBusy(false);
    }
  }

  async function handlePay() {
    if (!payment) return;
    await run(
      () => payAdministratorPayment(payment.id),
      'Pagamento marcado como pago.',
    );
  }

  async function handleRefund() {
    if (!payment) return;
    setRefundOpen(false);
    await run(
      () => refundAdministratorPayment(payment.id),
      'Pagamento reembolsado.',
    );
  }

  async function handleKill() {
    if (!payment) return;
    const reason = killReason.trim();
    if (!reason) {
      notifyError('Informe o motivo do cancelamento.');
      return;
    }
    setKillOpen(false);
    setKillReason('');
    await run(
      () => killAdministratorPayment(payment.id, reason),
      'Pagamento cancelado.',
    );
  }

  async function handleDelete() {
    if (!payment) return;
    setDeleteOpen(false);
    setBusy(true);
    try {
      const result = await deleteAdministratorPayment(payment.id);
      if (!result.ok) {
        notifyError(result.error ?? 'Não foi possível excluir o pagamento.');
        return;
      }
      notifySuccess('Pagamento excluído.');
      onDeleted?.();
    } finally {
      setBusy(false);
    }
  }

  async function handleBindOperator(accountId: string) {
    if (!payment) return;
    setBindOperatorOpen(false);
    await run(
      () => bindAdministratorPaymentOperator(payment.id, accountId),
      'Operador vinculado ao pagamento.',
    );
  }

  const actionBar = payment ? (
    <section className="payment-admin-actions">
      <h3 className="payment-detail-panel__section-title">Transições disponíveis</h3>
      {transitions.needsOperator ? (
        <p className="payment-admin-actions__hint muted small">
          Vincule um operador antes de marcar como pago.
        </p>
      ) : null}
      {transitions.needsSplits ? (
        <p className="payment-admin-actions__hint muted small">
          Este pagamento ainda não possui splits de repasse configurados.
        </p>
      ) : null}
      <div className="payment-admin-actions__buttons">
        {transitions.needsOperator ? (
          <button type="button" className="btn btn-secondary btn-small" disabled={busy} onClick={() => setBindOperatorOpen(true)}>
            Vincular operador
          </button>
        ) : null}
        <button type="button" className="btn btn-primary btn-small" disabled={busy || !transitions.canPay} onClick={() => void handlePay()}>
          Marcar como pago
        </button>
        <button type="button" className="btn btn-secondary btn-small" disabled={busy || !transitions.canRefund} onClick={() => setRefundOpen(true)}>
          Reembolsar
        </button>
        <button type="button" className="btn btn-secondary btn-small" disabled={busy || !transitions.canKill} onClick={() => setKillOpen(true)}>
          Cancelar
        </button>
        <button type="button" className="btn btn-danger btn-small" disabled={busy || !transitions.canDelete} onClick={() => setDeleteOpen(true)}>
          Excluir
        </button>
      </div>
    </section>
  ) : null;

  const modals: ReactNode = (
    <>
      <ConfirmDialog
        open={refundOpen}
        title="Reembolsar pagamento"
        message="Confirma o reembolso deste pagamento? Não é possível reembolsar pagamentos já sacados do gateway."
        onCancel={() => setRefundOpen(false)}
        onConfirm={() => void handleRefund()}
      />
      <ConfirmDialog
        open={deleteOpen}
        title="Excluir pagamento"
        message="Esta ação remove o registro do repositório. Deseja continuar?"
        onCancel={() => setDeleteOpen(false)}
        onConfirm={() => void handleDelete()}
      />
      {killOpen ? (
        <div className="modal-backdrop" role="presentation">
          <div className="modal-card" role="dialog" aria-modal="true" aria-labelledby="kill-payment-title">
            <h2 id="kill-payment-title" className="modal-card__title">Cancelar pagamento</h2>
            <p className="modal-card__lead muted">Informe o motivo — é obrigatório no domínio.</p>
            <label className="field">
              <span className="field-label">Motivo</span>
              <textarea
                className="field-input"
                rows={3}
                value={killReason}
                onChange={(event) => setKillReason(event.target.value)}
                placeholder="Descreva o motivo do cancelamento…"
              />
            </label>
            <div className="modal-card__actions">
              <button type="button" className="btn btn-ghost" disabled={busy} onClick={() => setKillOpen(false)}>
                Voltar
              </button>
              <button type="button" className="btn btn-danger" disabled={busy} onClick={() => void handleKill()}>
                Cancelar pagamento
              </button>
            </div>
          </div>
        </div>
      ) : null}
      <AccountPickerModal
        open={bindOperatorOpen}
        title="Vincular operador"
        onClose={() => setBindOperatorOpen(false)}
        onSelected={(account) => void handleBindOperator(account.id)}
        searchAccounts={searchAdministratorOperatorsPicker}
      />
    </>
  );

  return { actionBar, modals, busy, transitions };
}
