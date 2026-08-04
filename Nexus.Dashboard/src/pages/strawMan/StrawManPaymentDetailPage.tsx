import { useParams } from 'react-router-dom';
import { useAuth } from '../../auth/AuthContext';
import { PaymentDetailPanel } from '../../features/payments/PaymentDetailPanel';
import { PaymentDetailShell } from '../../features/payments/PaymentDetailShell';
import { listPath } from '../../features/payments/paymentPaths';
import { usePaymentDetail } from '../../features/payments/usePaymentDetail';

export function StrawManPaymentDetailPage() {
  const { paymentId = '' } = useParams();
  const { user } = useAuth();
  const { payment, loading, error, notFound } = usePaymentDetail('straw-man', paymentId);

  return (
    <PaymentDetailShell
      kicker="Laranja"
      description="Visualização somente leitura dos pagamentos vinculados ao laranja."
      breadcrumbs={[
        { label: 'Dashboard', href: '/dashboard' },
        { label: 'Pagamentos do laranja', href: listPath('straw-man') },
        { label: 'Detalhe' },
      ]}
      loading={loading}
      error={error}
      notFound={notFound}
    >
      {payment ? (
        <PaymentDetailPanel
          payment={payment}
          scope="straw-man"
          viewerAccountId={user?.accountId}
        />
      ) : null}
    </PaymentDetailShell>
  );
}
