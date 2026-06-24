import { useCallback, useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { fetchPaymentById, normalizePaymentRow } from '../../features/payments/fetchPaymentById';
import { PaymentDetailPanel } from '../../features/payments/PaymentDetailPanel';
import { listPath } from '../../features/payments/paymentPaths';
import { usePaymentAdminActions } from '../../features/payments/usePaymentAdminActions';
import { PageHeading } from '../../layouts/PageHeading';
import { useNotifications } from '../../notifications/NotificationContext';

export function AdminPaymentDetailPage() {
  const { paymentId = '' } = useParams();
  const navigate = useNavigate();
  const { notifyError } = useNotifications();
  const [payment, setPayment] = useState<ReturnType<typeof normalizePaymentRow> | null>(null);
  const [loading, setLoading] = useState(true);

  const load = useCallback(async () => {
    if (!paymentId.trim()) return;
    setLoading(true);
    try {
      const result = await fetchPaymentById('global-admin', paymentId);
      if (!result.ok) {
        notifyError(result.error);
        setPayment(null);
        return;
      }
      setPayment(normalizePaymentRow(result.data!));
    } finally {
      setLoading(false);
    }
  }, [notifyError, paymentId]);

  useEffect(() => {
    void load();
  }, [load]);

  const { actionBar, modals } = usePaymentAdminActions({
    payment,
    onMutated: load,
    onDeleted: () => navigate(listPath('global-admin')),
  });

  return (
    <div className="ops-page">
      <PageHeading
        kicker="Administração"
        kickerVariant="admin"
        title="Detalhe do pagamento"
        subtitle="Transições aplicam as mesmas regras do domínio — sem bypass administrativo."
        backLink={{ to: listPath('global-admin'), label: 'Todos os pagamentos' }}
      />

      {loading ? (
        <p className="muted">Carregando…</p>
      ) : payment ? (
        <>
          <PaymentDetailPanel payment={payment} scope="global-admin" />
          {actionBar}
        </>
      ) : (
        <p className="muted">Pagamento não encontrado.</p>
      )}

      {modals}
    </div>
  );
}
