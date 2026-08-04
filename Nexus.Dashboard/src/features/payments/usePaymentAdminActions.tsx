import { useMemo, useState, type ReactNode } from 'react';
import {
  bindAdministratorPaymentOperator,
  deleteAdministratorPayment,
  killAdministratorPayment,
  markAdministratorPaymentDistributed,
  payAdministratorPayment,
  refundAdministratorPayment,
} from '../../api/administrator/payments';
import { searchAdministratorOperatorsPicker } from '../../api/accountPickerSources';
import type { PaymentRow } from '../../api/types';
import { AccountPickerDialog } from '@/components/data/entity-picker-dialog';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog';
import { useNotifications } from '../../notifications/NotificationContext';
import {
  canKillPayment,
  canMarkPaymentDistributed,
  canPayPayment,
  canRefundPayment,
  needsOperatorBind,
  needsSplitsForPay,
} from './PaymentDetailPanel';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';

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
        canMarkDistributed: false,
        canDelete: false,
        needsOperator: false,
        needsSplits: false,
      };
    }

    return {
      canPay: canPayPayment(payment),
      canRefund: canRefundPayment(payment),
      canKill: canKillPayment(payment),
      canMarkDistributed: canMarkPaymentDistributed(payment),
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

  async function handleMarkDistributed() {
    if (!payment) return;
    await run(
      () => markAdministratorPaymentDistributed(payment.id),
      'Pagamento marcado como repassado às partes.',
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
    <Card>
      <CardContent className="grid gap-3 p-4">
        <h3 className="text-sm font-semibold text-foreground">Transições disponíveis</h3>
        {transitions.needsOperator ? (
          <p className="text-sm text-muted-foreground">
            Vincule um operador antes de marcar como pago.
          </p>
        ) : null}
        {transitions.needsSplits ? (
          <p className="text-sm text-muted-foreground">
            Este pagamento ainda não possui splits de repasse configurados.
          </p>
        ) : null}
        <div className="flex flex-col gap-2">
          {transitions.needsOperator ? (
            <Button type="button" variant="secondary" size="sm" disabled={busy} onClick={() => setBindOperatorOpen(true)}>
              Vincular operador
            </Button>
          ) : null}
          <Button type="button" size="sm" disabled={busy || !transitions.canPay} onClick={() => void handlePay()}>
            Marcar como pago
          </Button>
          <Button type="button" variant="secondary" size="sm" disabled={busy || !transitions.canRefund} onClick={() => setRefundOpen(true)}>
            Reembolsar
          </Button>
          <Button type="button" variant="secondary" size="sm" disabled={busy || !transitions.canMarkDistributed} onClick={() => void handleMarkDistributed()}>
            Marcar como repassado
          </Button>
          <Button type="button" variant="secondary" size="sm" disabled={busy || !transitions.canKill} onClick={() => setKillOpen(true)}>
            Cancelar
          </Button>
          <Button type="button" variant="destructive" size="sm" disabled={busy || !transitions.canDelete} onClick={() => setDeleteOpen(true)}>
            Excluir
          </Button>
        </div>
      </CardContent>
    </Card>
  ) : null;

  const modals: ReactNode = (
    <>
      <AlertDialog open={refundOpen} onOpenChange={(open) => { if (!open) setRefundOpen(false); }}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Reembolsar pagamento</AlertDialogTitle>
            <AlertDialogDescription>
              Confirma o reembolso deste pagamento? Não é possível reembolsar pagamentos já sacados do gateway.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancelar</AlertDialogCancel>
            <AlertDialogAction onClick={() => void handleRefund()}>Confirmar</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
      <AlertDialog open={deleteOpen} onOpenChange={(open) => { if (!open) setDeleteOpen(false); }}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Excluir pagamento</AlertDialogTitle>
            <AlertDialogDescription>
              Esta ação remove o registro do repositório. Deseja continuar?
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancelar</AlertDialogCancel>
            <AlertDialogAction onClick={() => void handleDelete()}>Confirmar</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
      <Dialog open={killOpen} onOpenChange={(open) => { if (!open) setKillOpen(false); }}>
        <DialogContent showCloseButton={false}>
          <DialogHeader>
            <DialogTitle>Cancelar pagamento</DialogTitle>
            <DialogDescription>Informe o motivo — é obrigatório no domínio.</DialogDescription>
          </DialogHeader>
          <div className="space-y-2">
            <Label htmlFor="kill-reason">Motivo</Label>
            <Textarea
              id="kill-reason"
              rows={3}
              value={killReason}
              onChange={(event) => setKillReason(event.target.value)}
              placeholder="Descreva o motivo do cancelamento…"
            />
          </div>
          <DialogFooter>
            <Button type="button" variant="ghost" disabled={busy} onClick={() => setKillOpen(false)}>
              Voltar
            </Button>
            <Button type="button" variant="destructive" disabled={busy} onClick={() => void handleKill()}>
              Cancelar pagamento
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
      <AccountPickerDialog
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
