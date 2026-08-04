import { useParams } from 'react-router-dom';
import { useAuth } from '../../auth/AuthContext';
import { PaymentDetailPanel } from '../../features/payments/PaymentDetailPanel';
import { PaymentDetailShell } from '../../features/payments/PaymentDetailShell';
import { listPath } from '../../features/payments/paymentPaths';
import { usePaymentDetail } from '../../features/payments/usePaymentDetail';

export function OperatorPaymentDetailPage() {
  const { paymentId = '' } = useParams();
  const { user } = useAuth();
  const { payment, loading, error, notFound } = usePaymentDetail('operator', paymentId);

  return (
    <PaymentDetailShell
      kicker="Financeiro"
      description="Visualização somente leitura dos pagamentos em que você participa."
      breadcrumbs={[
        { label: 'Dashboard', href: '/dashboard' },
        { label: 'Meus pagamentos', href: listPath('operator') },
        { label: 'Detalhe' },
      ]}
      loading={loading}
      error={error}
      notFound={notFound}
    >
      {payment ? (
        <PaymentDetailPanel
          payment={payment}
          scope="operator"
          viewerAccountId={user?.accountId}
        />
      ) : null}
    </PaymentDetailShell>
  );
}
