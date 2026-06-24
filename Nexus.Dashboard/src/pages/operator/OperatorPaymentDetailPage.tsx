import { useCallback, useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { useAuth } from '../../auth/AuthContext';
import { fetchPaymentById, normalizePaymentRow } from '../../features/payments/fetchPaymentById';
import { PaymentDetailPanel } from '../../features/payments/PaymentDetailPanel';
import { listPath } from '../../features/payments/paymentPaths';
import { PageHeading } from '../../layouts/PageHeading';
import { useNotifications } from '../../notifications/NotificationContext';

export function OperatorPaymentDetailPage() {
  const { paymentId = '' } = useParams();
  const { user } = useAuth();
  const { notifyError } = useNotifications();
  const [payment, setPayment] = useState<ReturnType<typeof normalizePaymentRow> | null>(null);
  const [loading, setLoading] = useState(true);

  const load = useCallback(async () => {
    if (!paymentId.trim()) return;
    setLoading(true);
    try {
      const result = await fetchPaymentById('operator', paymentId);
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

  return (
    <div className="ops-page">
      <PageHeading
        kicker="Financeiro"
        title="Detalhe do pagamento"
        subtitle="Visualização somente leitura dos pagamentos em que você participa."
        backLink={{ to: listPath('operator'), label: 'Meus pagamentos' }}
      />

      {loading ? (
        <p className="muted">Carregando…</p>
      ) : payment ? (
        <PaymentDetailPanel
          payment={payment}
          scope="operator"
          viewerAccountId={user?.accountId}
        />
      ) : (
        <p className="muted">Pagamento não encontrado.</p>
      )}
    </div>
  );
}
